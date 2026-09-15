using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bolt.Automation.AgentTools.Failure;
using Bolt.Automation.AgentTools.SessionState;
using Bolt.Automation.ApiClients.GetQuoteApi.Extensions;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi;
using Bolt.Automation.ApiClients.PlatformApi.Extensions;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Projects.STS;
using Bolt.Automation.TestDataProvider.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>
/// Routes HTTP requests to session operations. One instance per host process.
/// Requests are handled serially by <see cref="HostProgram"/>, so no per-session
/// locking is needed here. Every response is snake_case JSON; HTTP status carries
/// success (200) vs. app error (4xx/5xx) so the client can branch without parsing.
/// </summary>
internal sealed class HostController(SessionRegistry registry, int port, Action<string> log)
{
    private const int InlineHtmlCap = 400_000;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    // Quote files are authored with C# PascalCase names; case-insensitive keeps it forgiving.
    private static readonly JsonSerializerOptions QuoteReadOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly SessionDirManager _sessions = new();

    /// <summary>Handles one request. Returns true if the host should keep running.</summary>
    public async Task<bool> HandleAsync(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath.Trim('/').ToLowerInvariant() ?? string.Empty;
        var keepRunning = true;
        (int Status, object Body) result;

        try
        {
            using var body = await ReadBodyAsync(ctx).ConfigureAwait(false);
            var root = body?.RootElement ?? default;

            switch (path)
            {
                case "status": result = (200, Status()); break;
                case "navigate": result = await NavigateAsync(root).ConfigureAwait(false); break;
                case "open": result = await OpenAsync(root).ConfigureAwait(false); break;
                case "open-quote": result = await OpenQuoteAsync(root).ConfigureAwait(false); break;
                case "quote-start": result = await QuoteStartAsync(root).ConfigureAwait(false); break;
                case "fill": result = await FillAsync(root).ConfigureAwait(false); break;
                case "continue": result = await ContinueAsync(root).ConfigureAwait(false); break;
                case "record-prep": result = await RecordPrepAsync(root).ConfigureAwait(false); break;
                case "raw": result = await RawAsync(root).ConfigureAwait(false); break;
                case "screenshot": result = await ScreenshotAsync(root).ConfigureAwait(false); break;
                case "inspect": result = await InspectAsync(root).ConfigureAwait(false); break;
                case "pause": result = Pause(root); break;
                case "resume": result = Resume(root); break;
                case "close": result = Close(root); break;
                case "list": result = (200, ListSessions()); break;
                case "shutdown":
                    keepRunning = false;
                    result = (200, new { status = "shutting_down", pid = Environment.ProcessId });
                    break;
                default:
                    result = (404, Err("unknown_endpoint", $"No endpoint '/{path}'."));
                    break;
            }
        }
        catch (Exception ex)
        {
            log($"[host] {path} error: {ex.Message}");
            result = (500, Err("host_error", (ex.InnerException ?? ex).Message));
        }

        await WriteJsonAsync(ctx, result.Status, result.Body).ConfigureAwait(false);
        return keepRunning;
    }

    private object Status() => new
    {
        status = "ready",
        pid = Environment.ProcessId,
        port,
        sessions = registry.Count,
    };

    // ---- navigate ---------------------------------------------------------

    private async Task<(int, object)> NavigateAsync(JsonElement body)
    {
        var tenant = GetStr(body, "tenant");
        var env = GetStr(body, "env");
        var flowName = GetStr(body, "flow");
        var until = GetStr(body, "until");
        var headed = GetBool(body, "headed");
        var urlOverride = GetStrOrNull(body, "url");
        // Clamp: 0 would make the kernel Wait return instantly (spurious timeout) and a
        // negative TimeSpan throws in Task.Wait; cap at 1h so a typo can't wedge the host.
        var timeoutSeconds = Math.Clamp(GetInt(body, "timeout_seconds", 240), 5, 3600);

        if (tenant.Length == 0 || env.Length == 0 || flowName.Length == 0 || until.Length == 0)
            return (400, Err("input_error", "navigate requires tenant, env, flow, and until."));

        var frontEnds = typeof(PlaywrightExecutor).Assembly;
        var resolved = FlowResolver.Resolve(frontEnds, flowName, out var available);
        if (resolved == null)
            return (404, Err("flow_not_found", $"Unknown flow '{flowName}'.", new { available }));
        if (resolved.Pages.Count == 0)
            return (422, Err("flow_empty", $"Flow '{flowName}' registered no pages."));

        var tStart = resolved.Pages[0];
        var tEnd = resolved.Pages.FirstOrDefault(p =>
            string.Equals(p.Name, until, StringComparison.OrdinalIgnoreCase));
        if (tEnd == null)
            return (404, Err("page_not_found",
                $"Page '{until}' is not in flow '{flowName}'.",
                new { pages = resolved.Pages.Select(p => p.Name).ToList() }));

        // Optional per-run field overrides (QA-supplied values). Merged over the
        // flow's DefaultData by the executor's MergeFormData, same as a test's formData.
        var formData = ReadDataObject(body, "form_data");

        var id = SessionDirManager.NewSessionId();
        var snap = new ScopeSnapshot
        {
            SessionId = id,
            Tenant = tenant,
            Env = env,
            Flow = resolved.FlowName,
            Until = until,
            Headed = headed,
            CreatedAtUtc = DateTime.UtcNow.ToString("o"),
        };

        BrowserSession session;
        try
        {
            session = SessionBootstrapper.Boot(id, tenant, env, headed, snap);
        }
        catch (Exception ex)
        {
            return (422, Err("boot_failed", (ex.InnerException ?? ex).Message));
        }

        // Set the project context the framework can't auto-detect — the test base
        // classes set it explicitly (e.g. D2CTestBase sets FrontEndType.D2C).
        var feOverride = GetStrOrNull(body, "front_end");
        FrontEndType? frontEnd = null;
        if (!string.IsNullOrEmpty(feOverride))
        {
            if (!Enum.TryParse<FrontEndType>(feOverride, ignoreCase: true, out var parsed))
            {
                session.Dispose();
                return (400, Err("input_error",
                    $"Unknown front-end '{feOverride}'. Expected: {string.Join(", ", Enum.GetNames<FrontEndType>())}."));
            }
            frontEnd = parsed;
        }
        frontEnd ??= FrontEndResolver.Resolve(resolved);
        if (frontEnd != null)
            session.ScopeContext.Set(ctx => ctx.FrontEnd, frontEnd.Value);

        // Resolve the start URL (explicit override wins).
        var startUrl = urlOverride;
        if (string.IsNullOrEmpty(startUrl))
        {
            var urlData = session.ScopeContext.Get(ctx => ctx.UrlDataCollection);
            var (u, e) = StartUrlResolver.Resolve(resolved, urlData?.FrontEnd);
            if (u == null) { session.Dispose(); return (422, Err("start_url_unresolved", e!)); }
            startUrl = u;
        }
        session.ScopeContext.Set(ctx => ctx.CurrentUrl, startUrl);
        snap.StartUrl = startUrl;

        log($"[host] navigate session={id} flow={resolved.FlowName} {tStart.Name}->{tEnd.Name} url={startUrl}");

        // Register up front so the session is inspectable even if the walk times out or fails.
        registry.Add(session);

        Task execTask;
        try
        {
            var exec = typeof(PlaywrightExecutor).GetMethod(nameof(PlaywrightExecutor.Execute))!
                .MakeGenericMethod(tStart, tEnd);
            // Offload to the thread pool: the reflection-invoked Execute runs a blocking
            // synchronous prefix (Playwright driver launch / browser init) before it yields
            // its Task. Invoking it inline would wedge the host's request loop thread and
            // bypass the kernel wait below; Task.Run keeps the loop free so the wait governs.
            //
            // Args are bound BY PARAMETER NAME, not position: Execute has grown optional
            // parameters before (pagesToAdd) and a hardcoded array count throws
            // TargetParameterCountException the next time it happens.
            var formDataDict = formData?.Count > 0
                ? formData.ToDictionary(kv => kv.Key, kv => kv.Value)
                : null;
            var execArgs = exec.GetParameters().Select(p => p.Name switch
            {
                "flowType" => resolved.FlowValue,
                "formData" => (object?)formDataDict,
                "fillForms" => true,
                "startUrl" => startUrl,
                _ => p.HasDefaultValue ? p.DefaultValue : null,
            }).ToArray();
            execTask = Task.Run(() => (Task)exec.Invoke(session.Executor, execArgs)!);
        }
        catch (Exception ex)
        {
            // MakeGenericMethod constraint failure (a flow page not : IInterview) or a
            // synchronous Invoke throw — remove+dispose so we don't leak the booted browser.
            registry.Remove(id);
            return (422, Err("navigate_setup_failed", (ex.InnerException ?? ex).Message,
                new { session_id = id }));
        }

        // Bound the walk with a KERNEL wait on THIS already-allocated thread — not Task.Delay
        // and not Task.Run. The framework's sync-over-async browser init starves the thread
        // pool, so neither an async timer nor a Task.Run watchdog can be scheduled to fire.
        // Task.Wait(timeout) blocks the current request thread (the host is serial, so that's
        // fine) and its timeout is OS-enforced — it always returns. Walk continuations still
        // get threads from the pre-seeded pool (SetMinThreads in HostProgram).
        log($"[host] navigate session={id} waiting up to {timeoutSeconds}s for walk");
        var completed = execTask.Wait(TimeSpan.FromSeconds(timeoutSeconds));
        log($"[host] navigate session={id} wait completed={completed}");
        if (!completed)
        {
            // Do NOT read BrowserManager here — the still-running init may hold its internal
            // lock, so a live status read would block. Fall back to the known start URL.
            snap.CurrentUrl ??= startUrl;
            snap.LastActionUtc = DateTime.UtcNow.ToString("o");
            try { _sessions.WriteScope(id, snap); } catch { /* best-effort */ }
            log($"[host] navigate session={id} TIMEOUT after {timeoutSeconds}s");
            return (422, Err("navigate_timeout",
                $"Flow walk did not reach '{until}' within {timeoutSeconds}s. Browser left open (session {id}) for inspection; "
                + "raise --timeout if the flow is legitimately slow, or inspect where it stalled.",
                new { session_id = id, current_url = snap.CurrentUrl }));
        }

        try
        {
            await execTask.ConfigureAwait(false); // observe walk exceptions
        }
        catch (Exception ex)
        {
            UpdateSnapshot(session, snap);
            _sessions.WriteScope(id, snap);
            return (422, Err("navigate_failed", (ex.InnerException ?? ex).Message,
                new { session_id = id, current_url = snap.CurrentUrl }));
        }

        snap.CurrentPage = tEnd.Name;
        UpdateSnapshot(session, snap);
        _sessions.WriteScope(id, snap);

        return (200, new
        {
            session_id = id,
            current_url = snap.CurrentUrl,
            current_page = tEnd.Name,
            headed,
            start_url = startUrl,
        });
    }

    // ---- open -------------------------------------------------------------

    /// <summary>
    /// Opens a session at a URL with full tenant/env context but NO flow walk — the
    /// primitive for product areas that have no registered FlowType (ADBX, admin
    /// surfaces, STS-auth entry points). Optionally resolves a named test user from
    /// the tenant's <c>UserTestDataCollection</c> (its <c>LoginUrl</c> is the default
    /// destination) and, with <c>login=true</c>, performs the framework's own STS
    /// login — credentials never leave the framework.
    /// </summary>
    private async Task<(int, object)> OpenAsync(JsonElement body)
    {
        var tenant = GetStr(body, "tenant");
        var env = GetStr(body, "env");
        var urlOverride = GetStrOrNull(body, "url");
        var userName = GetStrOrNull(body, "user");
        var feName = GetStrOrNull(body, "front_end");
        var doLogin = GetBool(body, "login");
        var headed = GetBool(body, "headed");
        var timeoutSeconds = Math.Clamp(GetInt(body, "timeout_seconds", 120), 5, 3600);

        if (tenant.Length == 0 || env.Length == 0)
            return (400, Err("input_error", "open requires tenant and env."));
        if (string.IsNullOrEmpty(urlOverride) && string.IsNullOrEmpty(userName))
            return (400, Err("input_error", "open requires url, user (whose LoginUrl is used), or both."));
        if (doLogin && string.IsNullOrEmpty(userName))
            return (400, Err("input_error", "login requires a user — the framework logs in with that user's stored credentials."));

        FrontEndType? frontEnd = null;
        if (!string.IsNullOrEmpty(feName))
        {
            if (!Enum.TryParse<FrontEndType>(feName, ignoreCase: true, out var parsed))
                return (400, Err("input_error",
                    $"Unknown front-end '{feName}'. Expected: {string.Join(", ", Enum.GetNames<FrontEndType>())}."));
            frontEnd = parsed;
        }

        var id = SessionDirManager.NewSessionId();
        var snap = new ScopeSnapshot
        {
            SessionId = id,
            Tenant = tenant,
            Env = env,
            Flow = $"Open ({userName ?? "url"})",
            Until = string.Empty,
            Headed = headed,
            CreatedAtUtc = DateTime.UtcNow.ToString("o"),
        };

        BrowserSession session;
        try { session = SessionBootstrapper.Boot(id, tenant, env, headed, snap); }
        catch (Exception ex) { return (422, Err("boot_failed", (ex.InnerException ?? ex).Message)); }

        if (frontEnd != null)
            session.ScopeContext.Set(ctx => ctx.FrontEnd, frontEnd.Value);

        var url = urlOverride;
        if (!string.IsNullOrEmpty(userName))
        {
            UserTestData? user;
            try
            {
                var accessor = session.Scope.ServiceProvider.GetRequiredService<TestContextAccessor>();
                user = ResolveUser(accessor.CurrentUserCollection, userName);
            }
            catch (Exception ex) { session.Dispose(); return (422, Err("setup_failed", (ex.InnerException ?? ex).Message)); }
            if (user == null)
            {
                session.Dispose();
                return (400, Err("input_error",
                    $"Unknown or unset user '{userName}' for {tenant}/{env}. "
                    + $"Available: {string.Join(", ", AvailableUserNames())}."));
            }
            session.ScopeContext.Set(ctx => ctx.CurrentUser, user);
            if (string.IsNullOrEmpty(url)) url = user.LoginUrl;
            if (string.IsNullOrEmpty(url))
            {
                session.Dispose();
                return (422, Err("no_url", $"User '{userName}' has no LoginUrl for {tenant}/{env}; pass an explicit url."));
            }
        }

        session.ScopeContext.Set(ctx => ctx.CurrentUrl, url!);
        snap.StartUrl = url;

        registry.Add(session);

        var opTask = Task.Run(async () =>
        {
            await session.BrowserManager.NavigateAsync(url!).ConfigureAwait(false);
            if (doLogin)
            {
                var page = await session.BrowserManager.GetPageAsync().ConfigureAwait(false);
                var helper = new FrontEnds.PlaywrightBase.PageHelper(page, scopeContext: session.ScopeContext);
                var loginPage = new STS_LoginPage(session.BrowserManager, helper, session.ScopeContext);
                await loginPage.Login().ConfigureAwait(false);
            }
        });

        log($"[host] open session={id} tenant={tenant} env={env} user={userName ?? "-"} login={doLogin} waiting up to {timeoutSeconds}s");
        if (!opTask.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            snap.LastActionUtc = DateTime.UtcNow.ToString("o");
            try { _sessions.WriteScope(id, snap); } catch { /* best-effort */ }
            return (422, Err("open_timeout",
                $"Open{(doLogin ? " + login" : "")} did not finish within {timeoutSeconds}s. Browser left open (session {id}).",
                new { session_id = id }));
        }

        try { await opTask.ConfigureAwait(false); }
        catch (Exception ex)
        {
            UpdateSnapshot(session, snap);
            _sessions.WriteScope(id, snap);
            return (422, Err("open_failed", (ex.InnerException ?? ex).Message,
                new { session_id = id, current_url = snap.CurrentUrl }));
        }

        UpdateSnapshot(session, snap);
        _sessions.WriteScope(id, snap);

        return (200, new
        {
            session_id = id,
            current_url = snap.CurrentUrl,
            headed,
            logged_in = doLogin,
            start_url = url,
        });
    }

    // ---- open-quote -------------------------------------------------------

    /// <summary>
    /// Mirrors an OnlineQuote test (e.g. D2C 237026): set the configured front-end +
    /// auth user, create a GetQuote application via <see cref="IGetQuoteApi"/>, then open
    /// the questionnaire URL directly. Unlike <see cref="NavigateAsync"/> there is NO flow
    /// walk — the application's forcePageSkipping URL lands on the quote page itself.
    /// Front-end, user, and data line (personal/commercial) are all parameterized;
    /// they default to D2C / OnlineQuote / personal.
    /// </summary>
    private async Task<(int, object)> OpenQuoteAsync(JsonElement body)
    {
        var tenant = GetStr(body, "tenant");
        var env = GetStr(body, "env");
        var quoteJson = GetStr(body, "quote_json");
        var appendUrl = GetStrOrNull(body, "append_url") ?? string.Empty;
        var feName = GetStrOrNull(body, "front_end") ?? "D2C";
        var userName = GetStrOrNull(body, "user") ?? "OnlineQuote";
        var line = (GetStrOrNull(body, "line") ?? "personal").ToLowerInvariant();
        var headed = GetBool(body, "headed");
        var timeoutSeconds = Math.Clamp(GetInt(body, "timeout_seconds", 180), 5, 3600);
        // Consumer-API chain extensions (mirrors GetQuoteApiHelper): create a random
        // applicant first, submit + poll after create, and/or skip opening a browser
        // entirely (pure API setup for an agent-side continuation, e.g. TC 110599).
        var createApplicant = GetBool(body, "create_applicant");
        var submit = GetBool(body, "submit");
        var doOpen = !GetBool(body, "no_open");
        // provider=personal-home builds Data via PersonalLineDataProvider (framework
        // defaults), with quote_json — when present — acting as a flat overrides object
        // instead of a full payload. Keeps QA scenarios from hand-maintaining payloads.
        var provider = GetStrOrNull(body, "provider")?.ToLowerInvariant();

        if (tenant.Length == 0 || env.Length == 0)
            return (400, Err("input_error", "open-quote requires tenant and env."));
        if (provider == null && quoteJson.Length == 0)
            return (400, Err("input_error", "open-quote requires quote_json (full payload) or provider (framework-built payload)."));
        if (provider is not (null or "personal-home"))
            return (400, Err("input_error", $"Unknown provider '{provider}'. Expected: personal-home."));
        if (provider != null && line == "commercial")
            return (400, Err("input_error", "provider=personal-home requires line=personal."));
        if (!Enum.TryParse<FrontEndType>(feName, ignoreCase: true, out var frontEnd))
            return (400, Err("input_error",
                $"Unknown front-end '{feName}'. Expected: {string.Join(", ", Enum.GetNames<FrontEndType>())}."));
        if (line != "personal" && line != "commercial")
            return (400, Err("input_error", $"Unknown line '{line}'. Expected: personal | commercial."));

        var id = SessionDirManager.NewSessionId();
        var snap = new ScopeSnapshot
        {
            SessionId = id,
            Tenant = tenant,
            Env = env,
            Flow = $"OnlineQuote ({frontEnd})",
            Until = string.Empty,
            Headed = headed,
            CreatedAtUtc = DateTime.UtcNow.ToString("o"),
        };

        BrowserSession session;
        try { session = SessionBootstrapper.Boot(id, tenant, env, headed, snap); }
        catch (Exception ex) { return (422, Err("boot_failed", (ex.InnerException ?? ex).Message)); }

        // Set FrontEnd + the auth user the consumer flow needs — SessionBootstrapper sets
        // neither, which is why a raw token URL otherwise hits /error/technical.
        session.ScopeContext.Set(ctx => ctx.FrontEnd, frontEnd);
        IGetQuoteApi? api;
        try
        {
            var accessor = session.Scope.ServiceProvider.GetRequiredService<TestContextAccessor>();
            var user = ResolveUser(accessor.CurrentUserCollection, userName);
            if (user == null)
            {
                session.Dispose();
                return (400, Err("input_error",
                    $"Unknown or unset user '{userName}' for {tenant}/{env}. "
                    + $"Available: {string.Join(", ", AvailableUserNames())}."));
            }
            session.ScopeContext.Set(ctx => ctx.CurrentUser, user);
            api = session.Scope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>().GetService<IGetQuoteApi>();
        }
        catch (Exception ex) { session.Dispose(); return (422, Err("setup_failed", (ex.InnerException ?? ex).Message)); }
        if (api == null) { session.Dispose(); return (422, Err("api_unavailable", "IGetQuoteApi is not registered in this scope.")); }

        // Register up front so the session is inspectable even if create/navigate is slow.
        registry.Add(session);

        // Generic over the data shape so personal- and commercial-line payloads share one path.
        // The quote file uses C# PascalCase names; case-insensitive keeps it forgiving.
        async Task<OpenQuoteResult> RunAsync<T>()
        {
            ApplicationRequestModel<T> request;
            if (provider == "personal-home")
            {
                request = (ApplicationRequestModel<T>)(object)BuildPersonalHomeRequest(
                    quoteJson.Length > 0 ? quoteJson : null);
            }
            else
            {
                request = JsonSerializer.Deserialize<ApplicationRequestModel<T>>(quoteJson, QuoteReadOptions)
                    ?? throw new InvalidOperationException("Quote JSON deserialized to null.");
            }
            if (request.Data == null)
                throw new InvalidOperationException("Quote JSON had no Data.");

            var applicantId = request.ApplicantId;
            if (createApplicant)
            {
                var applicant = TestDataProvider.TestData.ApplicantTestData.ApplicantTestData.Applicants.RandomApplicant;
                var applicantResp = await RetryHelper.RetryOnTransientAsync(
                    () => api.CreateApplicantAsync(applicant), "Failed to create applicant");
                applicantId = applicantResp.Id ?? throw new InvalidOperationException("CreateApplicant returned no Id.");
                request.ApplicantId = applicantId;
            }

            var create = await RetryHelper.RetryOnTransientAsync(
                () => api.CreateApplicationAsync(request), "Failed to create application");
            create.MapIdentifiers(session.ScopeContext);
            var appId = create.Id ?? throw new InvalidOperationException("CreateApplication returned no Id.");

            object? submission = null;
            if (submit)
            {
                await RetryHelper.RetryOnTransientAsync(
                    () => api.SubmitApplicationAsync(appId), "Failed to submit application");
                var sub = await RetryHelper.RetryOnTransientAsync(
                    () => api.GetSubmissionWithPollingAsync(appId), "Failed to get submission");
                submission = new
                {
                    status = sub.Status,
                    quotes_total = sub.Quotes?.Count() ?? 0,
                    quotes_success = sub.Quotes?.Count(q => q.Status == "Success") ?? 0,
                    quote_statuses = sub.Quotes?.Select(q => q.Status).ToList(),
                };
            }

            string? url = null;
            if (doOpen)
            {
                var quest = await RetryHelper.RetryOnTransientAsync(
                    () => api.GetQuestionnaireAsync(appId), "Failed to get questionnaire");
                url = (quest.Url ?? throw new InvalidOperationException("Questionnaire returned no Url.")) + appendUrl;
                session.ScopeContext.Set(ctx => ctx.CurrentUrl, url);
                await session.BrowserManager.NavigateAsync(url);
            }

            return new OpenQuoteResult(url, appId, create.FriendlyId, applicantId, submission);
        }

        var opTask = Task.Run(() => line == "commercial"
            ? RunAsync<CommercialLineData>()
            : RunAsync<PersonalLineData>());

        log($"[host] open-quote session={id} tenant={tenant} env={env} applicant={createApplicant} submit={submit} open={doOpen} waiting up to {timeoutSeconds}s");
        var completed = opTask.Wait(TimeSpan.FromSeconds(timeoutSeconds));
        if (!completed)
        {
            snap.LastActionUtc = DateTime.UtcNow.ToString("o");
            try { _sessions.WriteScope(id, snap); } catch { /* best-effort */ }
            return (422, Err("open_quote_timeout",
                $"Quote API chain{(doOpen ? " + navigate" : "")} did not finish within {timeoutSeconds}s. Session {id} left registered.",
                new { session_id = id }));
        }

        try { await opTask.ConfigureAwait(false); }
        catch (Exception ex)
        {
            UpdateSnapshot(session, snap);
            _sessions.WriteScope(id, snap);
            return (422, Err("open_quote_failed", (ex.InnerException ?? ex).Message, new { session_id = id }));
        }

        var r = opTask.Result;
        snap.StartUrl = r.Url;
        UpdateSnapshot(session, snap);
        _sessions.WriteScope(id, snap);

        return (200, new
        {
            session_id = id,
            current_url = snap.CurrentUrl,
            headed,
            start_url = r.Url,
            application_id = r.ApplicationId,
            friendly_id = r.FriendlyId,
            applicant_id = r.ApplicantId,
            submitted = submit,
            submission = r.Submission,
            opened = doOpen,
        });
    }

    // ---- fill -------------------------------------------------------------

    /// <summary>
    /// Fills ONLY the named FieldRegistry fields on the session's current page with
    /// the supplied values, in the order given — the semi-manual QA primitive. Goes
    /// through <c>PageHelper.InteractWithField</c> (the framework idiom), so locator
    /// strategy, field type handling, and waits all come from the registry. No
    /// DefaultValue injection: fields not named are not touched.
    /// </summary>
    private async Task<(int, object)> FillAsync(JsonElement body)
    {
        var id = GetStr(body, "session");
        var timeoutSeconds = Math.Clamp(GetInt(body, "timeout_seconds", 120), 5, 3600);
        var entries = ReadDataObject(body, "data");

        var s = registry.Get(id);
        if (s == null) return (404, Err("session_not_found", $"No live session '{id}'."));
        if (entries == null || entries.Count == 0)
            return (400, Err("input_error", "fill requires a non-empty 'data' object of field-name → value."));

        var opTask = Task.Run(async () =>
        {
            var page = await s.BrowserManager.GetPageAsync().ConfigureAwait(false);
            var helper = new FrontEnds.PlaywrightBase.PageHelper(page, scopeContext: s.ScopeContext);
            var results = new List<object>();
            var failed = 0;

            foreach (var (name, value) in entries)
            {
                try
                {
                    await helper.InteractWithField(name, value).ConfigureAwait(false);
                    results.Add(new { field = name, value, status = "filled" });
                }
                catch (Exception ex)
                {
                    failed++;
                    results.Add(new { field = name, value, status = "failed", error = (ex.InnerException ?? ex).Message });
                }
            }

            return (results, failed);
        });

        log($"[host] fill session={id} fields={entries.Count} waiting up to {timeoutSeconds}s");
        if (!opTask.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
            return (422, Err("fill_timeout",
                $"Filling {entries.Count} field(s) did not finish within {timeoutSeconds}s. Browser left open (session {id}).",
                new { session_id = id }));

        var (fieldResults, failedCount) = await opTask.ConfigureAwait(false);

        UpdateSnapshot(s, s.Snapshot);
        _sessions.WriteScope(id, s.Snapshot);

        return (failedCount == entries.Count && entries.Count > 0 ? 422 : 200, new
        {
            session_id = id,
            filled = entries.Count - failedCount,
            failed = failedCount,
            results = fieldResults,
            current_url = CurrentUrl(s),
        });
    }

    // ---- continue ---------------------------------------------------------

    /// <summary>
    /// Advances the session exactly one page: instantiates the current page object
    /// (so page-specific <c>ClickContinue</c> overrides apply), clicks continue, and —
    /// when the next page is known from the flow order or an explicit <c>expect</c> —
    /// validates the next page is ready and updates the snapshot. The step-by-step
    /// counterpart to <c>navigate</c>'s full walk.
    /// </summary>
    private async Task<(int, object)> ContinueAsync(JsonElement body)
    {
        var id = GetStr(body, "session");
        var expect = GetStrOrNull(body, "expect");
        var timeoutSeconds = Math.Clamp(GetInt(body, "timeout_seconds", 90), 5, 3600);

        var s = registry.Get(id);
        if (s == null) return (404, Err("session_not_found", $"No live session '{id}'."));

        var pageName = GetStrOrNull(body, "page") ?? s.Snapshot.CurrentPage;
        if (string.IsNullOrEmpty(pageName))
            return (400, Err("input_error",
                "The session's current page is unknown — pass 'page' with the current page type name (e.g. D2C_HouseDetailsPage)."));

        var frontEnds = typeof(PlaywrightExecutor).Assembly;
        var pageType = ResolvePageType(frontEnds, pageName);
        if (pageType == null)
            return (404, Err("page_type_not_found", $"No IInterview page type named '{pageName}' in FrontEnds."));

        // Next page: explicit expectation wins; otherwise the flow's page order (when
        // the session came from `navigate` and the current page isn't the last one).
        Type? nextType = null;
        if (!string.IsNullOrEmpty(expect))
        {
            nextType = ResolvePageType(frontEnds, expect);
            if (nextType == null)
                return (404, Err("page_type_not_found", $"No IInterview page type named '{expect}' in FrontEnds."));
        }
        else
        {
            var flow = FlowResolver.Resolve(frontEnds, s.Snapshot.Flow, out _);
            var idx = flow?.Pages.ToList().FindIndex(p => p == pageType) ?? -1;
            if (flow != null && idx >= 0 && idx + 1 < flow.Pages.Count)
                nextType = flow.Pages[idx + 1];
        }

        var flowHelper = s.Scope.ServiceProvider.GetRequiredService<PageFlowHelper>();
        var opTask = Task.Run(async () =>
        {
            var current = flowHelper.CreateAndValidatePage(pageType);
            await current.ClickContinue().ConfigureAwait(false);
            if (nextType != null)
            {
                var next = flowHelper.CreateAndValidatePage(nextType);
                await next.ValidatePageReady().ConfigureAwait(false);
            }
        });

        log($"[host] continue session={id} from={pageName} expect={nextType?.Name ?? "<unknown>"} waiting up to {timeoutSeconds}s");
        if (!opTask.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
            return (422, Err("continue_timeout",
                $"Continue from '{pageName}' did not finish within {timeoutSeconds}s. Browser left open (session {id}).",
                new { session_id = id, current_url = CurrentUrl(s) }));

        try { await opTask.ConfigureAwait(false); }
        catch (Exception ex)
        {
            UpdateSnapshot(s, s.Snapshot);
            _sessions.WriteScope(id, s.Snapshot);
            return (422, Err("continue_failed", (ex.InnerException ?? ex).Message,
                new { session_id = id, from_page = pageName, current_url = CurrentUrl(s) }));
        }

        s.Snapshot.CurrentPage = nextType?.Name;
        UpdateSnapshot(s, s.Snapshot);
        _sessions.WriteScope(id, s.Snapshot);

        return (200, new
        {
            session_id = id,
            from_page = pageName,
            current_page = nextType?.Name,
            validated = nextType != null,
            current_url = CurrentUrl(s),
        });
    }

    // ---- quote-start ------------------------------------------------------

    /// <summary>
    /// Mirrors the Progressive consumer entry path (<c>ProgressiveTestHelper.CallQuoteStartAsync</c>):
    /// a Platform <c>QuoteStart</c> call with full prefill for a named <c>AddressKey</c>, then a
    /// navigate to the returned deeplink. With prefill the HQX pages auto-advance, so the session
    /// can be stepped to Rates with <c>continue</c> (no form filling needed).
    /// </summary>
    private async Task<(int, object)> QuoteStartAsync(JsonElement body)
    {
        var tenant = GetStr(body, "tenant");
        var env = GetStr(body, "env");
        var addressKeyName = GetStr(body, "address");
        var userName = GetStrOrNull(body, "user") ?? "Consumer";
        var feName = GetStrOrNull(body, "front_end") ?? "HQXConsumer";
        var headed = GetBool(body, "headed");
        var timeoutSeconds = Math.Clamp(GetInt(body, "timeout_seconds", 180), 5, 3600);

        if (tenant.Length == 0 || env.Length == 0 || addressKeyName.Length == 0)
            return (400, Err("input_error", "quote-start requires tenant, env, and address (an AddressKey name, e.g. ID)."));
        if (!Enum.TryParse<AddressKey>(addressKeyName, ignoreCase: true, out var addressKey))
            return (400, Err("input_error",
                $"Unknown address key '{addressKeyName}'. Expected an AddressKey enum value (e.g. ID, OH, MI, PA_Meadville).",
                new { available = Enum.GetNames<AddressKey>() }));
        if (!Enum.TryParse<FrontEndType>(feName, ignoreCase: true, out var frontEnd))
            return (400, Err("input_error",
                $"Unknown front-end '{feName}'. Expected: {string.Join(", ", Enum.GetNames<FrontEndType>())}."));

        var id = SessionDirManager.NewSessionId();
        var snap = new ScopeSnapshot
        {
            SessionId = id,
            Tenant = tenant,
            Env = env,
            Flow = $"QuoteStart ({addressKeyName})",
            Until = string.Empty,
            Headed = headed,
            CreatedAtUtc = DateTime.UtcNow.ToString("o"),
        };

        BrowserSession session;
        try { session = SessionBootstrapper.Boot(id, tenant, env, headed, snap); }
        catch (Exception ex) { return (422, Err("boot_failed", (ex.InnerException ?? ex).Message)); }

        session.ScopeContext.Set(ctx => ctx.FrontEnd, frontEnd);

        IPlatformApiClientFactory factory;
        try
        {
            var accessor = session.Scope.ServiceProvider.GetRequiredService<TestContextAccessor>();
            var user = ResolveUser(accessor.CurrentUserCollection, userName);
            if (user == null)
            {
                session.Dispose();
                return (400, Err("input_error",
                    $"Unknown or unset user '{userName}' for {tenant}/{env}. "
                    + $"Available: {string.Join(", ", AvailableUserNames())}."));
            }
            session.ScopeContext.Set(ctx => ctx.CurrentUser, user);
            factory = session.Scope.ServiceProvider.GetRequiredService<IPlatformApiClientFactory>();
        }
        catch (Exception ex) { session.Dispose(); return (422, Err("setup_failed", (ex.InnerException ?? ex).Message)); }

        registry.Add(session);

        var opTask = Task.Run(async () =>
        {
            var address = TestDataProvider.TestData.PlatformAPITestData.Addresses.GetAddress(addressKey);
            var request = TestDataProvider.Providers.PlatformAPIDataProvider.QuoteStartPrefillDataProvider
                .GetFullPrefillData(address);

            var api = await factory.CreateApiClientAsync().ConfigureAwait(false);
            var response = await api.QuoteStartWithRetryAsync(request).EnsureSuccessContentAsync().ConfigureAwait(false);
            response?.MapExternalId(session.ScopeContext);

            var url = response?.WebsiteURL;
            if (string.IsNullOrEmpty(url))
                throw new InvalidOperationException("QuoteStart returned no WebsiteURL.");

            session.ScopeContext.Set(ctx => ctx.CurrentUrl, url);
            await session.BrowserManager.NavigateAsync(url).ConfigureAwait(false);
            return (Url: url, ExternalId: response?.BoltExternalId);
        });

        log($"[host] quote-start session={id} tenant={tenant} env={env} address={addressKeyName} waiting up to {timeoutSeconds}s");
        if (!opTask.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            snap.LastActionUtc = DateTime.UtcNow.ToString("o");
            try { _sessions.WriteScope(id, snap); } catch { /* best-effort */ }
            return (422, Err("quote_start_timeout",
                $"QuoteStart + navigate did not finish within {timeoutSeconds}s. Browser left open (session {id}).",
                new { session_id = id }));
        }

        try { await opTask.ConfigureAwait(false); }
        catch (Exception ex)
        {
            UpdateSnapshot(session, snap);
            _sessions.WriteScope(id, snap);
            return (422, Err("quote_start_failed", (ex.InnerException ?? ex).Message,
                new { session_id = id, current_url = snap.CurrentUrl }));
        }

        snap.StartUrl = opTask.Result.Url;
        UpdateSnapshot(session, snap);
        _sessions.WriteScope(id, snap);

        return (200, new
        {
            session_id = id,
            current_url = snap.CurrentUrl,
            external_id = opTask.Result.ExternalId,
            headed,
            start_url = opTask.Result.Url,
        });
    }

    // ---- record-prep ------------------------------------------------------

    /// <summary>
    /// Prepares a Playwright-codegen recording session from a live session: exports the
    /// browser context's storage state (cookies + localStorage — so the recorder browser
    /// is authenticated the same way) and reports the current URL as the recording start
    /// point. The client then spawns <c>playwright codegen --load-storage …</c>.
    /// </summary>
    private async Task<(int, object)> RecordPrepAsync(JsonElement body)
    {
        var id = GetStr(body, "session");
        var s = registry.Get(id);
        if (s == null) return (404, Err("session_not_found", $"No live session '{id}'."));

        var context = await s.BrowserManager.GetContextAsync().ConfigureAwait(false);
        var dir = _sessions.EnsureSessionDir(id);
        var path = Path.Combine(dir, "storage-state.json");
        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = path }).ConfigureAwait(false);

        var page = await s.BrowserManager.GetPageAsync().ConfigureAwait(false);
        return (200, new
        {
            session_id = id,
            storage_state_path = path,
            current_url = page.Url,
            tenant = s.Snapshot.Tenant,
            env = s.Snapshot.Env,
            // The recorder must launch the session's channel browser — bundled
            // Chromium may not exist on a QA machine (side-by-side install error).
            channel = s.Channel,
            // Headed donor session = a second look-alike window on screen; the client
            // warns the QA which window is the recorder.
            headed = s.Snapshot.Headed,
        });
    }

    // ---- raw --------------------------------------------------------------

    /// <summary>
    /// Executes one raw Playwright action on the live session's current page — the replay
    /// primitive for recorded exploratory steps whose fields are NOT (yet) in the
    /// FieldRegistry. Selector is a Playwright selector-engine string (css, <c>text=</c>,
    /// <c>role=</c>, <c>#id</c>, …). XPath is rejected (house rule).
    /// </summary>
    private async Task<(int, object)> RawAsync(JsonElement body)
    {
        var id = GetStr(body, "session");
        var action = GetStr(body, "action").ToLowerInvariant();
        var selector = GetStr(body, "selector");
        var value = GetStrOrNull(body, "value");
        var timeoutSeconds = Math.Clamp(GetInt(body, "timeout_seconds", 30), 5, 3600);

        var s = registry.Get(id);
        if (s == null) return (404, Err("session_not_found", $"No live session '{id}'."));
        if (selector.Length == 0)
            return (400, Err("input_error", "raw requires a selector."));
        if (selector.StartsWith("//") || selector.StartsWith("xpath=", StringComparison.OrdinalIgnoreCase))
            return (400, Err("input_error", "XPath selectors are not allowed (house rule) — use css / text= / role= engines."));
        // Every action the recording parser can emit must be replayable here —
        // parse-recording's contract is that its actions feed `raw` directly.
        var needsValue = action is "fill" or "select" or "press" or "type";
        if (needsValue && string.IsNullOrEmpty(value))
            return (400, Err("input_error", $"raw action '{action}' requires a value."));

        var opTask = Task.Run(async () =>
        {
            var page = await s.BrowserManager.GetPageAsync().ConfigureAwait(false);
            var locator = page.Locator(selector);
            var actionTimeout = (float)TimeSpan.FromSeconds(timeoutSeconds).TotalMilliseconds;
            switch (action)
            {
                case "click": await locator.ClickAsync(new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                case "dblclick": await locator.DblClickAsync(new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                case "fill": await locator.FillAsync(value!, new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                case "check": await locator.CheckAsync(new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                case "uncheck": await locator.UncheckAsync(new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                case "select": await locator.SelectOptionAsync(value!, new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                case "press": await locator.PressAsync(value!, new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                case "type": await locator.PressSequentiallyAsync(value!, new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                case "hover": await locator.HoverAsync(new() { Timeout = actionTimeout }).ConfigureAwait(false); break;
                default: throw new ArgumentException($"Unknown raw action '{action}'. Expected: click, dblclick, fill, check, uncheck, select, press, type, hover.");
            }
        });

        log($"[host] raw session={id} action={action} selector={selector} waiting up to {timeoutSeconds + 5}s");
        // Small buffer over the Playwright action timeout so the locator's own clean
        // TimeoutError surfaces instead of a blunt kernel-wait abandonment.
        if (!opTask.Wait(TimeSpan.FromSeconds(timeoutSeconds + 5)))
            return (422, Err("raw_timeout",
                $"Raw {action} on '{selector}' did not finish within {timeoutSeconds + 5}s. Browser left open (session {id}).",
                new { session_id = id }));

        try { await opTask.ConfigureAwait(false); }
        catch (ArgumentException ex)
        {
            return (400, Err("input_error", ex.Message));
        }
        catch (Exception ex)
        {
            UpdateSnapshot(s, s.Snapshot);
            _sessions.WriteScope(id, s.Snapshot);
            return (422, Err("raw_failed", (ex.InnerException ?? ex).Message,
                new { session_id = id, action, selector, current_url = CurrentUrl(s) }));
        }

        UpdateSnapshot(s, s.Snapshot);
        _sessions.WriteScope(id, s.Snapshot);

        return (200, new { session_id = id, action, selector, value, current_url = CurrentUrl(s) });
    }

    // ---- screenshot -------------------------------------------------------

    private async Task<(int, object)> ScreenshotAsync(JsonElement body)
    {
        var id = GetStr(body, "session");
        var scope = (GetStrOrNull(body, "scope") ?? "viewport").ToLowerInvariant();
        var selector = GetStrOrNull(body, "selector");

        var s = registry.Get(id);
        if (s == null) return (404, Err("session_not_found", $"No live session '{id}'."));

        var page = await s.BrowserManager.GetPageAsync().ConfigureAwait(false);
        var dir = _sessions.EnsureSessionDir(id);
        var path = Path.Combine(dir, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        if (scope == "element")
        {
            if (string.IsNullOrEmpty(selector))
                return (400, Err("input_error", "scope=element requires --selector."));
            await page.Locator(selector).ScreenshotAsync(new LocatorScreenshotOptions { Path = path })
                .ConfigureAwait(false);
        }
        else
        {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = scope == "full" })
                .ConfigureAwait(false);
        }

        return (200, new { session_id = id, path, scope, current_url = page.Url });
    }

    // ---- inspect ----------------------------------------------------------

    private async Task<(int, object)> InspectAsync(JsonElement body)
    {
        var id = GetStr(body, "session");
        var scope = (GetStrOrNull(body, "scope") ?? "form").ToLowerInvariant();

        var s = registry.Get(id);
        if (s == null) return (404, Err("session_not_found", $"No live session '{id}'."));

        var page = await s.BrowserManager.GetPageAsync().ConfigureAwait(false);
        var html = await page.ContentAsync().ConfigureAwait(false);
        var scoped = HtmlScoper.Scope(html, scope);

        var truncated = scoped.Html.Length > InlineHtmlCap;
        var outHtml = truncated ? scoped.Html[..InlineHtmlCap] : scoped.Html;

        return (200, new
        {
            session_id = id,
            current_url = page.Url,
            title = await page.TitleAsync().ConfigureAwait(false),
            scope = scoped.EffectiveScope,
            note = scoped.Note,
            truncated,
            html = outHtml,
        });
    }

    // ---- pause / resume ---------------------------------------------------

    private (int, object) Pause(JsonElement body)
    {
        var id = GetStr(body, "session");
        var reason = GetStr(body, "reason");
        var s = registry.Get(id);
        if (s == null) return (404, Err("session_not_found", $"No live session '{id}'."));

        s.Snapshot.Paused = true;
        s.Snapshot.PauseReason = reason;
        s.Snapshot.LastActionUtc = DateTime.UtcNow.ToString("o");
        _sessions.WritePauseReason(id, reason);
        _sessions.WriteScope(id, s.Snapshot);

        // Returns immediately; the browser stays open for the user. The agent's
        // next step is AskUserQuestion (see spike report §pause semantics).
        return (200, new { session_id = id, paused = true, reason });
    }

    private (int, object) Resume(JsonElement body)
    {
        var id = GetStr(body, "session");
        var s = registry.Get(id);
        if (s == null) return (404, Err("session_not_found", $"No live session '{id}'."));

        s.Snapshot.Paused = false;
        s.Snapshot.PauseReason = null;
        s.Snapshot.LastActionUtc = DateTime.UtcNow.ToString("o");
        _sessions.WriteScope(id, s.Snapshot);

        return (200, new { session_id = id, paused = false });
    }

    // ---- close ------------------------------------------------------------

    private (int, object) Close(JsonElement body)
    {
        if (GetBool(body, "all"))
        {
            var n = registry.Count;
            registry.DisposeAll();
            return (200, new { closed = n });
        }

        var id = GetStr(body, "session");
        if (id.Length == 0)
            return (400, Err("input_error", "close requires a session id or all=true."));
        // `closed` is a COUNT on both branches (all=true returns one too) — one JSON shape.
        return registry.Remove(id)
            ? (200, new { session_id = id, closed = 1 })
            : (404, Err("session_not_found", $"No live session '{id}'."));
    }

    // ---- list -------------------------------------------------------------

    private object ListSessions() => new
    {
        count = registry.Count,
        sessions = registry.All().Select(s =>
        {
            var url = CurrentUrl(s);
            return new
            {
                session_id = s.Id,
                s.Snapshot.Tenant,
                s.Snapshot.Env,
                s.Snapshot.Flow,
                current_url = url,
                current_page = s.Snapshot.CurrentPage,
                paused = s.Snapshot.Paused,
                // Dead sessions must be visibly dead: unreachable = browser/driver gone,
                // blank = the window was likely closed (page recreated as about:blank) —
                // OR a just-started operation that hasn't navigated yet, so re-list
                // before closing a blank session. chrome-error = renderer crashed.
                state = url == null ? "unreachable"
                    : url.StartsWith("chrome-error", StringComparison.OrdinalIgnoreCase) ? "unreachable"
                    : url.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ? "blank_window_closed"
                    : "live",
            };
        }).ToList(),
    };

    /// <summary>Outcome of the open-quote API chain (url is null when no_open was set).</summary>
    private sealed record OpenQuoteResult(string? Url, string ApplicationId, string? FriendlyId, string? ApplicantId, object? Submission);

    /// <summary>
    /// Builds a PersonalHome application request from the framework's own data provider
    /// (<c>PersonalLineDataProvider.GetPersonalHomeData</c>), then shallow-merges the
    /// optional overrides object on top: an <c>Address</c> property is passed to the
    /// provider (it shapes dependent defaults); every other property replaces the
    /// provider value by name via a JSON round-trip, so types resolve naturally.
    /// </summary>
    private static ApplicationRequestModel<PersonalLineData> BuildPersonalHomeRequest(string? overridesJson)
    {
        JsonObject? overrides = null;
        if (overridesJson != null)
            overrides = JsonNode.Parse(overridesJson) as JsonObject
                ?? throw new InvalidOperationException("Overrides must be a single flat JSON object of PersonalLineData properties.");

        Common.Models.TestData.Data.Address? address = null;
        if (overrides != null && overrides.Remove("Address", out var addressNode) && addressNode != null)
            address = addressNode.Deserialize<Common.Models.TestData.Data.Address>(QuoteReadOptions);

        var data = TestDataProvider.Providers.GetQuoteApiDataProvider.PersonalLineDataProvider
            .GetPersonalHomeData(address: address);

        if (overrides is { Count: > 0 })
        {
            var node = JsonSerializer.SerializeToNode(data)!.AsObject();
            foreach (var (key, value) in overrides)
                node[key] = value?.DeepClone();
            data = node.Deserialize<PersonalLineData>(QuoteReadOptions)
                ?? throw new InvalidOperationException("Overrides merge produced no data.");
        }

        return new ApplicationRequestModel<PersonalLineData>
        {
            Products = TestDataProvider.TestData.ApplicationTestData.ApplicationTestData.Products.Homeowners,
            Data = data,
        };
    }

    // ---- helpers ----------------------------------------------------------

    private static void UpdateSnapshot(BrowserSession s, ScopeSnapshot snap)
    {
        snap.CurrentUrl = CurrentUrl(s);
        snap.LastActionUtc = DateTime.UtcNow.ToString("o");
        snap.Paused = s.Snapshot.Paused;
    }

    private static string? CurrentUrl(BrowserSession s)
    {
        try { return s.BrowserManager.GetCurrentTab()?.Url; }
        catch { return null; }
    }

    // Resolve a UserTestDataCollection entry by property name (case-insensitive), e.g.
    // "OnlineQuote", "LakeviewConsumer". Returns null if the name is unknown or unset for the tenant/env.
    private static UserTestData? ResolveUser(UserTestDataCollection collection, string name)
    {
        var prop = typeof(UserTestDataCollection).GetProperty(name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.PropertyType == typeof(UserTestData) ? prop.GetValue(collection) as UserTestData : null;
    }

    private static IEnumerable<string> AvailableUserNames() =>
        typeof(UserTestDataCollection)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(UserTestData))
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal);

    private static object Err(string status, string message, object? detail = null) =>
        detail == null ? new { status, message } : new { status, message, detail };

    /// <summary>
    /// Reads a JSON object property as ordered field-name → value pairs. Values may be
    /// strings, numbers, or booleans (coerced to their JSON literal text) — QA-authored
    /// data files shouldn't fail on an unquoted ZIP code. Key case is preserved
    /// (FieldRegistry names are case-sensitive). Null when absent or not an object.
    /// </summary>
    private static List<KeyValuePair<string, string>>? ReadDataObject(JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Object)
            return null;

        var list = new List<KeyValuePair<string, string>>();
        foreach (var prop in v.EnumerateObject())
        {
            var value = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString() ?? string.Empty
                : prop.Value.GetRawText();
            list.Add(new KeyValuePair<string, string>(prop.Name, value));
        }
        return list;
    }

    /// <summary>
    /// Resolves a page type name (e.g. <c>D2C_HouseDetailsPage</c>) to its
    /// <see cref="Bolt.Automation.FrontEnds.Interfaces.IInterview"/> implementation
    /// in the FrontEnds assembly, case-insensitively.
    /// </summary>
    private static Type? ResolvePageType(Assembly frontEnds, string pageName)
    {
        Type?[] types;
        try { types = frontEnds.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types; }

        return types.FirstOrDefault(t =>
            t is { IsClass: true, IsAbstract: false }
            && string.Equals(t.Name, pageName, StringComparison.OrdinalIgnoreCase)
            && typeof(FrontEnds.Interfaces.IInterview).IsAssignableFrom(t));
    }

    private static async Task<JsonDocument?> ReadBodyAsync(HttpListenerContext ctx)
    {
        if (!ctx.Request.HasEntityBody) return null;
        using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? Encoding.UTF8);
        var text = await reader.ReadToEndAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(text)) return null;
        try { return JsonDocument.Parse(text); }
        catch { return null; }
    }

    private static async Task WriteJsonAsync(HttpListenerContext ctx, int status, object body)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body, Json));
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        ctx.Response.OutputStream.Close();
    }

    private static string GetStr(JsonElement e, string name) => GetStrOrNull(e, name) ?? string.Empty;

    private static string? GetStrOrNull(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static bool GetBool(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) &&
        (v.ValueKind == JsonValueKind.True || (v.ValueKind == JsonValueKind.String && bool.TryParse(v.GetString(), out var b) && b));

    private static int GetInt(JsonElement e, string name, int fallback) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : fallback;
}
