using System.Collections.Concurrent;
using System.Diagnostics;
using Bolt.Automation.WorkerAgent.Models;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.WorkerAgent.Services;

/// <summary>
/// Main worker BackgroundService.
/// Registers on startup, performs initial code sync, spawns concurrent work loops,
/// handles self-update for test code, deregisters on shutdown.
/// </summary>
public sealed class WorkerService : BackgroundService
{
    private readonly OrchestratorClient _client;
    private readonly TestExecutor _executor;
    private readonly GitService _git;
    private readonly BuildService _build;
    private readonly WorkerOptions _options;
    private readonly ILogger<WorkerService> _logger;
    private readonly ConcurrentDictionary<int, string> _activeWorkItems = new();
    private readonly object _versionLock = new();
    private readonly SemaphoreSlim _buildSemaphore = new(1, 1);

    /// <summary>
    /// Set by HeartbeatService on the first successful heartbeat. ExecuteAsync awaits
    /// this before spawning work loops so we never poll /api/work before the orchestrator
    /// has registered us (first heartbeat with full static payload is now the registration).
    /// </summary>
    private readonly TaskCompletionSource<bool> _firstHeartbeatComplete = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Max time to wait for first heartbeat before giving up and exiting.</summary>
    private static readonly TimeSpan FirstHeartbeatTimeout = TimeSpan.FromSeconds(60);

    // Code version tracking
    private CodeVersion _codeVersion;
    private volatile bool _pendingDevelopUpdate;
    private volatile bool _draining;

    // Branch + commit the git working tree is actually checked out on. Diverges
    // from _codeVersion when a checkout succeeded but the build failed —
    // _codeVersion only advances on build success. Tracks the commit too: a failed
    // same-branch pinned-commit build moves the tree without changing the branch
    // label, which a branch-only field cannot see. Recovery and the pre-execution
    // guard must consult this, not _codeVersion, to know the real tree state.
    private (string Branch, string Commit) _workingTree = ("develop", "unknown");

    // Recent build failures per requested branch@commit. Entries expire after
    // BuildFailureMemoTtl. Work items requiring a memo'd target are released
    // without another fetch/checkout/build cycle, so an unbuildable feature
    // branch cannot put the worker into a tight rebuild loop against git and
    // the orchestrator.
    private readonly ConcurrentDictionary<string, DateTime> _recentBuildFailures = new();
    private static readonly TimeSpan BuildFailureMemoTtl = TimeSpan.FromMinutes(10);

    /// <summary>Status reported to orchestrator: idle, busy, building, draining.</summary>
    public string CurrentStatus
    {
        get
        {
            if (_draining) return "draining";
            if (IsBuilding) return "building";
            return _activeWorkItems.Count > 0 ? "busy" : "idle";
        }
    }

    /// <summary>
    /// Signals when a build is in progress. Non-null TCS means "building". Slots waiting
    /// on a build await its Task so they wake the instant the build completes, without polling.
    /// </summary>
    private volatile TaskCompletionSource<bool>? _buildCompletion;

    private bool IsBuilding => _buildCompletion is not null;

    /// <summary>Number of currently active work loops processing items.</summary>
    public int ActiveCount => _activeWorkItems.Count;

    /// <summary>Returns IDs of all work items currently being executed across all slots.</summary>
    public string[] ActiveWorkItemIds => _activeWorkItems.Values.ToArray();

    /// <summary>
    /// Slots that can accept new work right now. Zero while the worker is building or
    /// draining — even if some slots look idle — because they are frozen waiting on the
    /// build semaphore. This is the authoritative signal the orchestrator uses for branch
    /// packing; inferring capacity from `maxConcurrency - activeCount` caused the
    /// mid-build deadlock documented in docs/release-notes.
    /// </summary>
    public int ReadySlots
    {
        get
        {
            if (_draining || IsBuilding) return 0;
            var busy = _activeWorkItems.Count;
            return Math.Max(0, _options.Concurrency - busy);
        }
    }

    /// <summary>Current code version — thread-safe read.</summary>
    public CodeVersion CodeVersion
    {
        get { lock (_versionLock) return _codeVersion; }
    }

    /// <summary>Branch/commit the working tree is checked out on — thread-safe read.</summary>
    private (string Branch, string Commit) WorkingTree
    {
        get { lock (_versionLock) return _workingTree; }
    }

    /// <summary>True when the working tree matches the last successfully built code.</summary>
    private bool WorkingTreeMatchesBuild
    {
        get
        {
            lock (_versionLock)
                return _workingTree.Branch == _codeVersion.Branch
                    && _workingTree.Commit == _codeVersion.CommitHash;
        }
    }

    /// <summary>Outcome of a checkout-and-build attempt — callers memo/report differently per kind.</summary>
    private enum BuildAttemptOutcome
    {
        Success,
        /// <summary>Fetch/auth/checkout/commit-mismatch failure — likely transient, never memoed.</summary>
        GitFailure,
        /// <summary>The code at the target did not restore/compile — deterministic, memoed.</summary>
        BuildFailure,
        /// <summary>Target hit the failure memo while this slot waited on the build semaphore.</summary>
        SkippedRecentFailure,
    }

    private static string BuildFailureKey(string branch, string commit) => $"{branch}@{commit}";

    private bool HasRecentBuildFailure(string key)
    {
        if (!_recentBuildFailures.TryGetValue(key, out var failedAt)) return false;
        if (DateTime.UtcNow - failedAt < BuildFailureMemoTtl) return true;
        // Value-comparing remove: a plain TryRemove(key) could delete a FRESH memo
        // written by another slot between our TryGetValue and the removal.
        _recentBuildFailures.TryRemove(new KeyValuePair<string, DateTime>(key, failedAt));
        return false;
    }

    /// <summary>
    /// Records a deterministic build failure for a work-item-requested target.
    /// Called inside <see cref="CheckoutAndBuildAsync"/> before the build semaphore
    /// is released, so a sibling slot already queued on it sees the failure on its
    /// post-semaphore re-check. Recovery/self-update builds (null workItemId) never
    /// memo — develop must always stay retryable.
    /// </summary>
    private void RecordBuildFailureMemo(string branch, string? targetCommit, string? workItemId)
    {
        if (workItemId == null || targetCommit == null) return;
        _recentBuildFailures[BuildFailureKey(branch, targetCommit)] = DateTime.UtcNow;
    }

    public WorkerService(
        OrchestratorClient client,
        TestExecutor executor,
        GitService git,
        BuildService build,
        IOptions<WorkerOptions> options,
        ILogger<WorkerService> logger)
    {
        _client = client;
        _executor = executor;
        _git = git;
        _build = build;
        _options = options.Value;
        _logger = logger;

        // Initial code version from baked-in image
        _codeVersion = new CodeVersion("develop", "unknown", DateTime.UtcNow.ToString("o"));
    }

    /// <summary>
    /// Called by HeartbeatService after the first successful heartbeat (which also serves
    /// as registration). Releases the ExecuteAsync wait so work loops can start.
    /// </summary>
    public void SignalFirstHeartbeatComplete() => _firstHeartbeatComplete.TrySetResult(true);

    /// <summary>
    /// Signals the worker to stop accepting new work and finish in-flight items.
    /// Called by HeartbeatService on drain signal instead of immediate shutdown.
    /// </summary>
    public void RequestDrain()
    {
        if (_draining) return;
        _draining = true;
        _logger.LogWarning("Drain requested — finishing in-flight work, rejecting new items");
    }

    /// <summary>True when all work loops have drained.</summary>
    public bool IsDrained => _draining && _activeWorkItems.IsEmpty && !IsBuilding;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting worker agent — concurrency={Concurrency}", _options.Concurrency);

        // Resolve initial code version from git / baked-in COMMIT_HASH
        await InitializeCodeVersionAsync(stoppingToken);

        // Registration is the first successful heartbeat. HeartbeatService sends one
        // immediately on startup with the full static payload (podName, cpuCount, version,
        // codeVersion, …), which the orchestrator treats as an upsert. If develop HEAD has
        // moved since this image was built, the response's developHead triggers
        // OnDevelopHeadChanged → _pendingDevelopUpdate, and the first work-loop iteration
        // rebuilds before picking up work.
        try
        {
            await _firstHeartbeatComplete.Task.WaitAsync(FirstHeartbeatTimeout, stoppingToken);
        }
        catch (TimeoutException)
        {
            _logger.LogError(
                "First heartbeat did not complete within {Timeout}s — orchestrator unreachable, exiting",
                FirstHeartbeatTimeout.TotalSeconds);
            return;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Spawn concurrent work loops
        var workers = Enumerable.Range(0, _options.Concurrency)
            .Select(i => WorkLoopAsync(i, stoppingToken))
            .ToArray();

        await Task.WhenAll(workers);

        // Deregister with a short timeout so we don't block shutdown (#17)
        try
        {
            using var deregisterCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _client.DeregisterAsync("shutdown", deregisterCts.Token);
            _logger.LogInformation("Deregistered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deregistration failed");
        }
    }

    private async Task InitializeCodeVersionAsync(CancellationToken ct)
    {
        // Try live git first
        if (_git.IsGitRepository)
        {
            try
            {
                var commitHash = await _git.GetCurrentCommitHashAsync(ct);
                lock (_versionLock)
                {
                    _codeVersion = new CodeVersion("develop", commitHash, DateTime.UtcNow.ToString("o"));
                    _workingTree = ("develop", commitHash);
                }
                _logger.LogInformation("Initial code version: {CommitHash}", commitHash);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve commit hash from git — trying baked-in COMMIT_HASH file");
            }
        }

        // Fall back to the commit hash baked in at image build time
        var commitHashFile = Path.Combine(_options.RepoRootPath, "COMMIT_HASH");
        if (File.Exists(commitHashFile))
        {
            var hash = (await File.ReadAllTextAsync(commitHashFile, ct)).Trim();
            if (!string.IsNullOrEmpty(hash))
            {
                lock (_versionLock)
                {
                    _codeVersion = new CodeVersion("develop", hash, DateTime.UtcNow.ToString("o"));
                    _workingTree = ("develop", hash);
                }
                _logger.LogInformation("Initial code version from baked-in COMMIT_HASH: {CommitHash}", hash);
                return;
            }
        }

        _logger.LogWarning("Could not resolve initial commit hash — using 'unknown'");
    }

    /// <summary>
    /// Called by HeartbeatService when orchestrator signals a new develop HEAD.
    /// If idle → rebuild immediately. If busy → flag for later.
    /// </summary>
    public void OnDevelopHeadChanged(DevelopHead developHead)
    {
        var current = CodeVersion;
        if (current.CommitHash == developHead.CommitHash) return;
        if (current.Branch != "develop")
        {
            // On feature branch — record for later use when we return to develop
            _pendingDevelopUpdate = true;
            _logger.LogDebug(
                "Develop HEAD changed to {CommitHash} — noted for when we return to develop",
                developHead.CommitHash);
            return;
        }

        if (_activeWorkItems.IsEmpty && !IsBuilding)
        {
            _logger.LogInformation("Develop HEAD changed to {CommitHash} — idle, will rebuild on next poll cycle",
                developHead.CommitHash);
            _pendingDevelopUpdate = true;
        }
        else
        {
            _logger.LogInformation("Develop HEAD changed to {CommitHash} — busy, flagging for later",
                developHead.CommitHash);
            _pendingDevelopUpdate = true;
        }
    }

    private async Task WorkLoopAsync(int threadId, CancellationToken ct)
    {
        _logger.LogInformation("Work loop {ThreadId} started", threadId);

        while (!ct.IsCancellationRequested)
        {
            // Exit loop gracefully when draining — let in-flight work on other slots finish
            if (_draining)
            {
                _logger.LogInformation("Work loop {ThreadId} exiting — drain in progress", threadId);
                break;
            }

            try
            {
                // Check for pending develop update. Only meaningful when we're ALREADY on
                // develop — the flag refreshes develop to its latest HEAD. On a feature
                // branch this must NOT fire: we'd abandon the in-flight job, tearing down
                // feature-branch builds after each completed test and stranding items that
                // other slots have already claimed. The develop HEAD will be picked up
                // naturally when the return-to-develop path runs after HasWork=false, and
                // if we land on a stale local develop, the next heartbeat flags it and this
                // branch fires on the following iteration.
                //
                // Gated on _activeWorkItems.IsEmpty && !IsBuilding so we don't rebuild
                // under live tests; any slot can initiate, _buildSemaphore serializes.
                if (_pendingDevelopUpdate
                    && CodeVersion.Branch == "develop"
                    && _activeWorkItems.IsEmpty
                    && !IsBuilding)
                {
                    await HandlePendingDevelopUpdateAsync(ct);
                }

                var current = CodeVersion;
                var response = await _client.GetWorkAsync(current.Branch, current.CommitHash, ct);

                if (!response.HasWork)
                {
                    // On a feature branch with no work remaining — return to develop.
                    // Any slot can initiate; _buildSemaphore ensures only one rebuild runs.
                    // Also fires when only the working tree drifted (a checkout whose
                    // build failed): CodeVersion still reads the old build but the tree
                    // needs restoring before it can serve work cleanly.
                    if ((CodeVersion.Branch != "develop" || !WorkingTreeMatchesBuild)
                        && _activeWorkItems.IsEmpty && !IsBuilding)
                    {
                        _logger.LogInformation(
                            "No work available for {Branch} (tree on {TreeBranch}@{TreeCommit}) — returning to develop",
                            CodeVersion.Branch, WorkingTree.Branch, WorkingTree.Commit);
                        var ok = await CheckoutAndBuildAsync("develop", null, workItemId: null, ct);
                        if (ok == BuildAttemptOutcome.Success)
                        {
                            _logger.LogInformation("Returned to develop successfully");
                            continue;
                        }
                        _logger.LogError("Failed to return to develop — retrying in 30s");
                        await Task.Delay(30_000, ct);
                        continue;
                    }

                    var retryAfter = (response.RetryAfter ?? 5) * 1000;
                    await Task.Delay(retryAfter, ct);
                    continue;
                }

                // Guard: orchestrator returned HasWork=true but WorkItem is null (#18)
                if (response.WorkItem is null)
                {
                    _logger.LogWarning("Orchestrator returned HasWork=true but WorkItem is null — skipping");
                    await Task.Delay(2000, ct);
                    continue;
                }

                var workItem = response.WorkItem;

                // Re-check drain after receiving work but before executing (#12 — TOCTOU fix)
                if (_draining)
                {
                    _logger.LogWarning("Drain set after picking up {WorkItemId} — releasing without execution",
                        workItem.Id);
                    await ReleaseWorkItemSafelyAsync(workItem.Id, "drain signalled post-claim", ct);
                    break;
                }

                _logger.LogInformation("Picked up work item {WorkItemId} — {TestName}",
                    workItem.Id, workItem.DisplayName);

                // Treat null Branch as "develop" to avoid running on wrong branch (#21)
                var effectiveBranch = workItem.Branch ?? "develop";

                // Check if work item requires a branch/commit switch.
                // Commit switch is skipped when git is unavailable — worker runs with baked-in code.
                var needsBranchSwitch = effectiveBranch != current.Branch;
                var needsCommitSwitch = _git.IsGitRepository
                    && !needsBranchSwitch
                    && workItem.CommitHash != null
                    && workItem.CommitHash != current.CommitHash;

                if (needsBranchSwitch || needsCommitSwitch)
                {
                    // Don't re-attempt a target that just failed to build — the memo
                    // saves the fetch/checkout/restore cycle that would fail again.
                    var failureKey = BuildFailureKey(effectiveBranch, workItem.CommitHash ?? current.CommitHash);
                    if (HasRecentBuildFailure(failureKey))
                    {
                        _logger.LogWarning(
                            "Work item {WorkItemId} requires {Branch}@{Commit}, which failed to build recently — releasing without rebuild",
                            workItem.Id, effectiveBranch, workItem.CommitHash ?? "<head>");
                        await ReleaseWorkItemSafelyAsync(workItem.Id, "recent build failure for required commit", ct);
                        await Task.Delay(5000, ct);
                        continue;
                    }

                    // Cross-branch or commit-mismatch pickup: claim first, build second.
                    // Any slot may initiate a rebuild — coordination is at the _buildSemaphore
                    // level (one dotnet build at a time). Proceed only when this worker has
                    // no in-flight items so we don't rebuild under a running test.
                    if (_activeWorkItems.IsEmpty)
                    {
                        var targetBranch = effectiveBranch;
                        var targetCommit = workItem.CommitHash ?? current.CommitHash;

                        _logger.LogInformation(
                            "Work item requires {Branch}@{Commit} — rebuilding before execution",
                            targetBranch, targetCommit);

                        _activeWorkItems[threadId] = workItem.Id;
                        BuildAttemptOutcome outcome;
                        try
                        {
                            outcome = await CheckoutAndBuildAsync(targetBranch, targetCommit, workItem.Id, ct);

                            if (outcome == BuildAttemptOutcome.BuildFailure)
                            {
                                _logger.LogWarning(
                                    "Build failed for {Branch}@{Commit} — releasing work item {WorkItemId} back to the queue",
                                    targetBranch, targetCommit, workItem.Id);

                                // The failure memo was already recorded inside
                                // CheckoutAndBuildAsync, before the build semaphore was
                                // released — so both a sibling slot queued on the semaphore
                                // and anyone re-claiming after this release will see it.

                                // Explicit release first: decrements attempt, clears isExhausted,
                                // wakes long-polls. Without this, the item would stay locked to us
                                // for the full 10-minute visibility timeout while the build loop
                                // rebuilt; meanwhile zombie recovery would continue to view it as
                                // "running". ReleaseWorkItemSafelyAsync is idempotent — a race
                                // with the orchestrator build-status release is a safe no-op.
                                await ReleaseWorkItemSafelyAsync(workItem.Id, "build failed", ct);
                                await ReportBuildFailureAsync(targetBranch, targetCommit, workItem.Id, ct);
                            }
                            else if (outcome == BuildAttemptOutcome.GitFailure)
                            {
                                _logger.LogWarning(
                                    "Checkout of {Branch}@{Commit} failed on a git operation — releasing work item {WorkItemId} back to the queue",
                                    targetBranch, targetCommit, workItem.Id);
                                await ReleaseWorkItemSafelyAsync(workItem.Id, "checkout failed", ct);
                                await ReportBuildFailureAsync(targetBranch, targetCommit, workItem.Id, ct);
                            }
                            else if (outcome == BuildAttemptOutcome.SkippedRecentFailure)
                            {
                                await ReleaseWorkItemSafelyAsync(workItem.Id, "recent build failure for required commit", ct);
                            }
                        }
                        finally
                        {
                            _activeWorkItems.TryRemove(threadId, out _);
                        }

                        if (outcome != BuildAttemptOutcome.Success)
                        {
                            // Recovery runs only after this slot deregistered from
                            // _activeWorkItems, and only when the whole worker is idle —
                            // a develop checkout under live tests would swap the tree's
                            // data files out from under them.
                            await TryRecoverDevelopTreeAsync(ct);
                            await Task.Delay(5000, ct);
                            continue;
                        }
                    }
                    else
                    {
                        // Other slots are busy — we cannot rebuild under them. Release the
                        // item explicitly so another worker (or another slot on this worker
                        // after the current build finishes) can claim it immediately. Without
                        // this, the item would stay locked to us for the 10-minute visibility
                        // timeout — which is the exact behaviour that stranded items in the
                        // "4-tests-per-iteration" feature-branch pathology.
                        await ReleaseWorkItemSafelyAsync(workItem.Id, "cross-branch slot contention", ct);
                        await Task.Delay(2000, ct);
                        continue;
                    }
                }

                // Wait if a build is in progress — prevents executing tests against transitional code.
                // Slots wake the instant the build completes via the TaskCompletionSource (no polling).
                var pendingBuild = _buildCompletion;
                if (pendingBuild is not null)
                {
                    await pendingBuild.Task.WaitAsync(ct);
                }

                // Re-read code version after potential build — branch may have changed.
                // The working tree must match the built binaries exactly (branch AND
                // commit): after a failed build the tree can sit on a different
                // checkout than the binaries, and tests read data files from the
                // tree at runtime, not only compiled assemblies.
                var postBuildVersion = CodeVersion;
                var tree = WorkingTree;
                var treeMatchesBuild = tree.Branch == postBuildVersion.Branch
                    && tree.Commit == postBuildVersion.CommitHash;
                if (postBuildVersion.Branch != effectiveBranch ||
                    !treeMatchesBuild ||
                    (workItem.CommitHash != null && postBuildVersion.CommitHash != workItem.CommitHash))
                {
                    _logger.LogWarning(
                        "Code state changed during build ({Branch}@{Commit}, tree on {TreeBranch}@{TreeCommit}) — work item needs {WantBranch}@{WantCommit}, releasing",
                        postBuildVersion.Branch, postBuildVersion.CommitHash, tree.Branch, tree.Commit,
                        effectiveBranch, workItem.CommitHash);
                    await ReleaseWorkItemSafelyAsync(workItem.Id, "code version shifted during build wait", ct);
                    if (!treeMatchesBuild)
                        await TryRecoverDevelopTreeAsync(ct);
                    await Task.Delay(2000, ct);
                    continue;
                }

                // Execute the test
                _activeWorkItems[threadId] = workItem.Id;
                try
                {
                    var result = await _executor.ExecuteAsync(workItem, threadId, response.Environment, ct);

                    // Attach code version to result
                    var resultWithVersion = result with { CodeVersion = CodeVersion };

                    // Defense-in-depth retry for result reporting — this is the most critical call
                    await ReportResultWithRetryAsync(resultWithVersion, workItem.Id, ct);

                    if (result.Passed)
                        _logger.LogInformation("Work item {WorkItemId} passed", workItem.Id);
                    else
                        _logger.LogWarning("Work item {WorkItemId} failed: {Error}",
                            workItem.Id, result.ErrorSummary ?? "unknown");
                }
                finally
                {
                    _activeWorkItems.TryRemove(threadId, out _);
                }

            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Work loop error");
                try { await Task.Delay(5000, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        _logger.LogInformation("Work loop {ThreadId} stopped", threadId);
    }

    private async Task HandlePendingDevelopUpdateAsync(CancellationToken ct)
    {
        _pendingDevelopUpdate = false;

        if (!_git.IsGitRepository)
        {
            _logger.LogWarning("Skipping develop update — no .git directory at {RepoRoot}. Image was built without git history.", _options.RepoRootPath);
            return;
        }

        try
        {
            _logger.LogInformation("Handling pending develop update — fetching latest");
            var latestCommit = await _git.GetRemoteHeadHashAsync("develop", ct);
            var current = CodeVersion;

            if (current.CommitHash != latestCommit)
            {
                await CheckoutAndBuildAsync("develop", latestCommit, workItemId: null, ct);
            }
            else
            {
                _logger.LogInformation("Already at latest develop ({CommitHash})", latestCommit);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to handle pending develop update");
        }
    }

    /// <summary>
    /// Restores a drifted working tree (checkout moved, build failed) back to develop.
    /// Only runs when this worker is fully idle — a checkout under live tests would
    /// swap the tree's data files out from under them. Safe to call opportunistically:
    /// no-op when the tree is already on develop or the worker is busy. If every slot
    /// skips it while busy, the pre-execution tree guard keeps releasing mismatched
    /// items until the last finishing slot gets here.
    /// </summary>
    private async Task TryRecoverDevelopTreeAsync(CancellationToken ct)
    {
        if (WorkingTreeMatchesBuild) return;
        if (!_activeWorkItems.IsEmpty || IsBuilding) return;

        var tree = WorkingTree;
        var built = CodeVersion;
        _logger.LogInformation(
            "Working tree {Branch}@{Commit} does not match built code {BuiltBranch}@{BuiltCommit} — restoring develop",
            tree.Branch, tree.Commit, built.Branch, built.CommitHash);
        var outcome = await CheckoutAndBuildAsync("develop", null, workItemId: null, ct);
        if (outcome != BuildAttemptOutcome.Success)
        {
            _logger.LogError("Recovery build to develop failed — delaying 30s before retry");
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }

    /// <summary>
    /// Checks out a branch and builds. Returns <see cref="BuildAttemptOutcome.Success"/>
    /// when the target is checked out and built (or already was);
    /// <see cref="BuildAttemptOutcome.GitFailure"/> for fetch/auth/checkout/mismatch
    /// failures (likely transient); <see cref="BuildAttemptOutcome.BuildFailure"/> when
    /// the code at the target did not restore/compile;
    /// <see cref="BuildAttemptOutcome.SkippedRecentFailure"/> when the failure memo
    /// showed the target broken while this slot waited on the semaphore.
    /// If targetCommit is null, resolves the remote HEAD after fetch.
    /// Serialized with <see cref="_buildSemaphore"/> to prevent concurrent builds (#13).
    /// <paramref name="workItemId"/> is threaded into git error logs via
    /// <see cref="GitOpContext"/> so failures attribute to the work item that
    /// triggered them. Pass <c>null</c> for return-to-develop / pending-update
    /// paths where no specific work item is in flight.
    /// </summary>
    private async Task<BuildAttemptOutcome> CheckoutAndBuildAsync(
        string branch, string? targetCommit, string? workItemId, CancellationToken ct)
    {
        await _buildSemaphore.WaitAsync(ct);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _buildCompletion = tcs;
        var gitCtx = new GitOpContext(workItemId, branch, targetCommit);

        // Captured before checkout moves the tree: restore is needed whenever the
        // checkout actually changes the tree. Keying this on CodeVersion would skip
        // the restore on the recovery build back to develop after a failed feature
        // build (CodeVersion still reads develop), leaving whatever the failed
        // feature restore wrote in obj/ to poison a --no-restore build.
        var previousTree = WorkingTree;
        try
        {
            // Re-validate under the semaphore: while this slot waited, the previous
            // holder may have already built this exact target — or proven it broken.
            var builtVersion = CodeVersion;
            var currentTree = WorkingTree;
            if (builtVersion.Branch == branch
                && currentTree.Branch == branch
                && currentTree.Commit == builtVersion.CommitHash
                && (targetCommit == null || builtVersion.CommitHash == targetCommit))
            {
                _logger.LogInformation(
                    "Already built {Branch}@{Commit} — skipping rebuild",
                    branch, targetCommit ?? builtVersion.CommitHash);
                return BuildAttemptOutcome.Success;
            }

            if (workItemId != null && targetCommit != null
                && HasRecentBuildFailure(BuildFailureKey(branch, targetCommit)))
            {
                _logger.LogWarning(
                    "{Branch}@{Commit} failed to build while this slot waited on the build semaphore — not rebuilding",
                    branch, targetCommit);
                return BuildAttemptOutcome.SkippedRecentFailure;
            }

            var sw = Stopwatch.StartNew();

            // --- Phase 1: fetch (network, may fail with auth/network errors) ---
            try
            {
                await _git.FetchAsync(gitCtx, ct);
            }
            catch (GitAuthException ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Git auth failed during fetch for {Branch}@{Commit} (workItem={WorkItemId}). "
                    + "PAT may be invalid/expired or a stale extraheader survived the startup scrub — "
                    + "see the auth-failure diagnostic above for the current http.* config.",
                    branch, targetCommit ?? "<head>", workItemId ?? "<none>");
                return BuildAttemptOutcome.GitFailure;
            }
            catch (GitFetchException ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Git fetch failed for {Branch}@{Commit} (workItem={WorkItemId}, phase=fetch)",
                    branch, targetCommit ?? "<head>", workItemId ?? "<none>");
                return BuildAttemptOutcome.GitFailure;
            }

            // --- Phase 2: checkout (local, may fail with checkout errors) ---
            string actualCommit;
            try
            {
                if (targetCommit != null)
                    await _git.CheckoutCommitAsync(targetCommit, gitCtx, ct);
                else
                    await _git.CheckoutAsync(branch, gitCtx, ct);

                // The tree has moved even if anything below fails (including the
                // rev-parse below) — record it immediately so recovery paths know
                // the checkout state independently of CodeVersion. "unknown" (the
                // branch-HEAD case before rev-parse resolves) is conservative: it
                // never matches a built commit, so recovery treats it as drifted.
                lock (_versionLock) _workingTree = (branch, targetCommit ?? "unknown");

                actualCommit = await _git.GetCurrentCommitHashAsync(ct);
                lock (_versionLock) _workingTree = (branch, actualCommit);
            }
            catch (GitCheckoutException ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Git checkout failed for {Branch}@{Commit} (workItem={WorkItemId}, phase=checkout)",
                    branch, targetCommit ?? "<head>", workItemId ?? "<none>");
                return BuildAttemptOutcome.GitFailure;
            }

            if (targetCommit != null && actualCommit != targetCommit)
            {
                _logger.LogError(
                    "Commit mismatch after checkout: expected {Expected}, got {Actual} "
                    + "(workItem={WorkItemId}, branch={Branch})",
                    targetCommit, actualCommit, workItemId ?? "<none>", branch);
                return BuildAttemptOutcome.GitFailure;
            }

            // --- Phase 3: build (no git, may fail with compiler/test errors) ---
            try
            {
                // Restore whenever the checkout changed the tree at all: same-branch
                // commit switches can change package references too (Central Package
                // Management), and a --no-restore build against stale assets fails.
                var needsRestore = previousTree.Branch != branch || previousTree.Commit != actualCommit;
                var buildResult = await _build.BuildAsync(restore: needsRestore, ct);
                sw.Stop();

                if (!buildResult.Success)
                {
                    _logger.LogError(
                        "Build failed after checkout of {Branch}@{Commit} (workItem={WorkItemId}, phase=build): {Error}",
                        branch, actualCommit, workItemId ?? "<none>", buildResult.Error);
                    RecordBuildFailureMemo(branch, targetCommit, workItemId);
                    return BuildAttemptOutcome.BuildFailure;
                }

                // Update code version
                lock (_versionLock)
                {
                    _codeVersion = new CodeVersion(branch, actualCommit, DateTime.UtcNow.ToString("o"));
                }
                _pendingDevelopUpdate = false;

                _logger.LogInformation(
                    "Successfully checked out and built {Branch}@{Commit} in {Duration}ms",
                    branch, actualCommit, sw.ElapsedMilliseconds);

                // Report build success
                try
                {
                    await _client.PostBuildStatusAsync(new BuildStatusRequest(
                        Branch: branch,
                        CommitHash: actualCommit,
                        Success: true,
                        Error: null,
                        Duration: sw.ElapsedMilliseconds
                    ), ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to report build success to orchestrator");
                }

                return BuildAttemptOutcome.Success;
            }
            catch (OperationCanceledException)
            {
                // Shutdown/drain cancellation is not a build failure — memoing it
                // would blacklist a healthy target. Let it propagate.
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Build phase failed unexpectedly for {Branch}@{Commit} (workItem={WorkItemId}, phase=build)",
                    branch, actualCommit, workItemId ?? "<none>");
                RecordBuildFailureMemo(branch, targetCommit, workItemId);
                return BuildAttemptOutcome.BuildFailure;
            }
        }
        finally
        {
            // Clear the completion signal before releasing the semaphore so any slot that
            // wakes up on tcs.Task sees _buildCompletion == null on re-check.
            _buildCompletion = null;
            tcs.TrySetResult(true);
            _buildSemaphore.Release();
        }
    }

    private async Task ReportResultWithRetryAsync(SubmitResultRequest result, string workItemId, CancellationToken ct)
    {
        const int maxAttempts = 5;
        int[] delaysMs = [2000, 5000, 10_000, 15_000];

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _client.ReportResultAsync(result, ct);
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    _logger.LogError(ex, "Failed to report result for {WorkItemId} after {Attempts} attempts — result lost",
                        workItemId, maxAttempts);
                    return; // Don't throw — we don't want to crash the work loop
                }

                _logger.LogWarning(ex, "ReportResult attempt {Attempt}/{Max} failed for {WorkItemId} — retrying in {Delay}ms",
                    attempt, maxAttempts, workItemId, delaysMs[attempt - 1]);
                await Task.Delay(delaysMs[attempt - 1], ct);
            }
        }
    }

    /// <summary>
    /// Returns a claimed work item to the pending queue on the orchestrator.
    /// Used when the worker has called /api/work, received an item, but discovers
    /// it cannot execute it right now (slot contention during a sibling slot's
    /// rebuild, code version shifted while awaiting the build, drain signalled
    /// post-claim). Without this call, the item would stay <c>running/lockedBy</c>
    /// until the 10-minute visibility timeout expired — stranding it from the
    /// active worker pool for that entire window. Failure is logged but never
    /// rethrown: the visibility timeout is always the safety net.
    /// </summary>
    private async Task ReleaseWorkItemSafelyAsync(string workItemId, string reason, CancellationToken ct)
    {
        try
        {
            var resp = await _client.ReleaseWorkItemAsync(workItemId, reason, ct);
            if (resp.Released)
            {
                _logger.LogInformation(
                    "Released work item {WorkItemId} back to queue ({Reason})",
                    workItemId, reason);
            }
            else
            {
                _logger.LogDebug(
                    "Release of {WorkItemId} was a no-op ({Reason}) — already released or reclaimed",
                    workItemId, reason);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // `reason` is the caller's *intent* ("cross-branch slot contention",
            // "build failed", "drain signalled post-claim"), not the cause of
            // the release *failure*. The failure itself is in `ex` — typically
            // an HTTP 5xx from the orchestrator or a transport error.
            _logger.LogWarning(ex,
                "Failed to release {WorkItemId} (intended reason: {Reason}) — "
                + "orchestrator call failed, falling back to visibility timeout",
                workItemId, reason);
        }
    }

    private async Task ReportBuildFailureAsync(string branch, string commit, string workItemId, CancellationToken ct)
    {
        try
        {
            await _client.PostBuildStatusAsync(new BuildStatusRequest(
                Branch: branch,
                CommitHash: commit,
                Success: false,
                Error: "Build failed",
                Duration: 0,
                WorkItemId: workItemId
            ), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to report build failure to orchestrator");
        }
    }
}
