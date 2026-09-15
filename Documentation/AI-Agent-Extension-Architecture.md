# Nexus AI Agent Extension — Architecture & Maintenance

> **What this is.** The "agentic layer" added on `feature/agent-ext-scaffold` (extended since):
> a deterministic CLI, a navigable knowledge base, seven agent skills, and a refactored CLAUDE.md
> map that together let an AI coding agent author, debug, and review nexus tests — and assist
> manual QAs with semi-manual testing — with the framework's own idioms instead of guessing
> from prose.
>
> **Audience.** Engineers maintaining the extension, and the agent itself (this doc is
> discoverable via the root `CLAUDE.md` "Load on demand" table).

---

## 1. Why it exists

An agent dropped into this repo previously had to: discover artifact paths by reading prose,
diagnose failures by eyeballing raw `page_source` HTML, infer flow/field structure by grepping,
and re-load a large skill file on every trigger. The agentic layer replaces that with:

- **Deterministic tools** for recurring multi-step reasoning (`nexus-agent <noun> <verb>`),
- **A knowledge base read on demand** (topic leaves, not one mega-file),
- **Thin skills** that *navigate* knowledge and orchestrate tools rather than carrying content,
- **A map-shaped CLAUDE.md** that points to per-area detail.

Four doctrines run through all of it (see §7): *ask don't assume*, *skills navigate don't carry*,
*deterministic tools beat agent inference*, *cooperative over autonomous*.

---

## 2. The four artifacts

```
┌──────────────────────────────────────────────────────────────────────┐
│ .claude/skills/                7 SKILL.md — thin orchestration         │
│   nexus-test-author · nexus-debug · nexus-test-review ·                │
│   nexus-framework-review · nexus-secrets · nexus-qa-assist             │
│        │ navigate (kb/philosophy)        │ orchestrate (tc/failure/…)  │
│        ▼                                  ▼                            │
│ Documentation/agent-knowledge/   Bolt.Automation.AgentTools (CLI)      │
│   index.yml + 85 leaves            nexus-agent <noun> <verb>           │
│   framework/ domain/ recipes/      kb tc failure code browser          │
│   philosophy/                      philosophy secrets                  │
│                                            ▼ reflects / drives          │
│                                   Bolt.Automation.* framework projects  │
├──────────────────────────────────────────────────────────────────────┤
│ CLAUDE.md (root map) + 5 per-project CLAUDE.md  — load-on-demand index  │
└──────────────────────────────────────────────────────────────────────┘
```

| Artifact | Location | Tracked? |
|---|---|---|
| CLI (`nexus-agent`) | `Bolt.Automation.AgentTools/` | yes (in `.sln`) |
| Knowledge base | `Documentation/agent-knowledge/` (`index.yml` + 85 `.md`) | yes (`!Documentation/**/*.md`) |
| Skills | `.claude/skills/nexus-*/SKILL.md` (7) | yes (force-added; `*.md` is otherwise ignored) |
| CLAUDE.md map | root + `Bolt.Automation.{ApiClients,Common,Core,FrontEnds,Tests}/CLAUDE.md` | yes (force-added) |
| QA scenario files | `.qa-scenarios/*.yml` | **no — gitignored by design** (see `recipe:qa-scenario-format`) |

---

## 3. The CLI — `Bolt.Automation.AgentTools`

A .NET 10 console exe named **`nexus-agent`** (`<AssemblyName>`), added to `Bolt.Automation.sln`.

### 3.1 Dispatch model

`Program.Main` is a two-stage dispatcher:

1. **`--host-mode <port>`** → the browser host (§3.5). Internal; spawned by the client, never typed.
2. Otherwise the **first arg is the noun**; the rest is handed to that noun's router
   (`Commands/<Noun>/<Noun>Router.cs`), which uses `CommandLineParser` to bind a
   `[Verb]`-attributed options class.

Multi-word verbs (`tc cache list`, `tc auth set-token`) are rewritten to a hyphenated single
token (`cache-list`, `auth-set-token`) inside the router, because `CommandLineParser` verbs are
single-token. The whole dispatch is wrapped in a **last-resort handler** that turns any uncaught
exception into a clean exit-2 error rather than a stack-trace crash.

### 3.2 Output & exit codes

- All output is **JSON**, snake_case (`JsonNamingPolicy.SnakeCaseLower`), via
  `Commands/CommandBase.cs` (`EmitJsonAsync` / `EmitErrorAsync`).
- **Exit codes:** `0` success · `2` user-facing error (error JSON on **stderr**) · `3` input error.
- Success JSON → stdout; error JSON → stderr. Agents branch on the exit code and parse stdout.

### 3.3 The nouns

| Noun | Verbs | Purpose |
|---|---|---|
| `kb` | `lookup --topic` · `search <terms> [--type]` · `describe --file` | Navigate the knowledge base. |
| `tc` | `fetch <id>` · `cache list` · `cache clear [--older-than]` · `auth set-token --token` | Fetch ADO test cases via the nexus-logger proxy; cache locally. |
| `failure` | `summarize --test` · `page-source --test [--scope]` · `screenshot --test` | Diagnose a failed test from its on-disk artifacts. |
| `code` | `find-similar --pattern` · `field-lookup <name>` · `flow-trace --flow` · `diff-impact --ref` · `wip-stop --note` | Query the codebase (grep + reflection). |
| `browser` | `navigate` · `open` · `open-quote` · `quote-start` · `fill` · `continue` · `record` · `parse-recording` · `raw` · `screenshot` · `inspect` · `pause` · `resume` · `close` · `list` | Drive a live Playwright session via a long-lived host (idle-exits after 4h with no sessions); `record` hands the session's auth state to the native Playwright codegen recorder for exploratory capture, `parse-recording` structures the result, `##…##` fills are verification markers (cleanup: `recipe:recording-cleanup`). |
| `philosophy` | `lookup --area` | Map a review area to the philosophy docs that govern it. |
| `secrets` | `sync [--force] [--ttl]` · `status` | Local secrets bundle from AWS Secrets Manager (see the `nexus-secrets` skill). |
| `doctor` | `[--fix]` | QA-machine bootstrap/diagnosis: repo, build freshness, Playwright, AWS CLI/SSO, secrets, `BOLT_SECRETS_PATH`, allowlist; records the last-known-good build for broken-develop fallback. |
| `saml` | `mint` | Mint a signed SAML assertion for callers that cannot sign one themselves; prints the raw base64 assertion on stdout (data channel — everything else logs to stderr). |

Detailed semantics per noun live in `Documentation/agent-knowledge/` (e.g. `domain:tc-api`) and the
command source; the highlights:

- **`kb`** — loads the canonical `index.yml` (YamlDotNet) and resolves topic → leaf. `search` builds
  a token index over filename/topic/headings/first-paragraph with weighted boosts
  (topic ×3, file ×2, heading ×2, body ×1) and a CamelCase-splitting tokenizer, persisted to a
  schema-versioned cache at `%TMP%\nexus-agent\kb-index.json` (invalidated by leaf-set change or
  mtime). `describe` returns a leaf's headings; a path-traversal guard keeps reads under the KB root.
- **`tc`** — a hand-rolled `Refit` client (`INexusLoggerApi`) against
  `https://nexus-logger.auto.boltx.us/api/azure-devops/test-cases/{id}` (read-only proxy over Azure
  DevOps; **not** the platform auth pipeline). Bearer token is user-supplied (Azure AD JWT, ~65 min
  TTL), stored at `%LOCALAPPDATA%\nexus-agent\token` with **user-only ACL** (`icacls` on Windows,
  `chmod 0600` elsewhere) and read back via `NEXUS_TC_TOKEN` env override then file. Responses cache
  at `%LOCALAPPDATA%\nexus-agent\tc\<id>.json`.
- **`failure`** — the novel value-add: correlates the TRX result (`Bolt.Automation.Common.Reporting.TrxParser`
  + per-test extraction in `TrxScanner`), the captured `page_source_*.html`, and the final screenshot
  for a given FQN, with a Levenshtein nearest-match when the exact folder isn't found. `page-source`
  scopes the DOM (`form`/`page`/`all`) via `HtmlScoper`. Artifacts are located TFM/config-agnostically
  under `Bolt.Automation.Tests/bin/**/TestResults/` (override: `NEXUS_TEST_RESULTS`).
- **`code`** — `find-similar`/`diff-impact` use ripgrep (managed `EnumerateFiles`+`Regex` fallback);
  `field-lookup`/`flow-trace` use **reflection** (§3.4). `diff-impact` maps changed files → philosophy
  areas via `Code/PhilosophyAreaMap.cs`. `wip-stop` commits a `WIP-STOP:` checkpoint + `RESUME-NOTE.md`.
- **`philosophy`** — `Commands/Philosophy/PhilosophyAreas.cs` maps an area name (and aliases) → the
  governing `philosophy/*.md` leaves, enriched with each leaf's frontmatter summary.

### 3.4 Reflection & framework coupling

`code field-lookup` / `flow-trace` reflect over `Bolt.Automation.FrontEnds.dll`. **They do not take a
ProjectReference to FrontEnds** — they load the DLL *by path* from a consuming app's output
(`Bolt.Automation.Tests/bin/<cfg>/net<tfm>/`) because a class library's own `bin` lacks the NuGet
closure (Playwright etc.), while the Tests output has it (`ReflectionLoader` ranks candidates by
"has `Microsoft.Playwright.dll` beside it" → host TFM → Debug → newest). A **stale-DLL pre-flight**
refuses to reflect over a DLL older than the newest FrontEnds `.cs` (→ "rebuild first", exit 2).

`browser` is the exception: it **does** ProjectReference `Core` + `FrontEnds` + `TestDataProvider`
+ `Microsoft.Playwright`, because it must *construct and drive* the DI graph, not just inspect it.

### 3.5 The browser host (the high-risk piece)

`browser navigate` needs a Playwright session that **survives across CLI invocations** (so the
agent's shell isn't tied up while the browser is live). Design:

- A **long-lived host process** (`nexus-agent --host-mode 5151`) owns Playwright and serves commands
  over `System.Net.HttpListener` on `127.0.0.1:5151` (no ASP.NET dependency). Requests are handled
  **serially**.
- The **client** (`Browser/Client/HostClient.cs`) discovers the host via a lock file
  (`%TMP%\nexus-agent\host.lock` = `{pid,port}`), spawns `--host-mode` if absent, polls `/status`
  until ready, and **respawns once on connection-refused** (but never on timeout — see below).
- Each `navigate` boots a **fresh DI scope + `BrowserManager` + `PlaywrightExecutor`** (bound to that
  same manager via `PlaywrightExecutorFactory`), kept alive for the session lifetime in a
  `SessionRegistry`. The host sets `ctx.Tenant/Environment/UrlDataCollection/CurrentUrl/FrontEnd`
  (mirroring what the test base classes do), resolves the `FlowType` enum + start/end page types by
  reflection, and invokes the generic `Execute<TStart,TEnd>` via `MakeGenericMethod`.
- Sessions persist under `%TMP%\nexus-agent\sessions\<id>\` (`scope.json`, `url.txt`, screenshots,
  `pause-reason.md`). `pause`/`resume` toggle a flag and return immediately; the actual human pause is
  driven by the agent calling `AskUserQuestion`.

**The thread-pool lesson (do not regress).** The framework's `BrowserManager` uses *sync-over-async*
(`.GetAwaiter().GetResult()` inside a lock) during init — fine under NUnit (which guards it with a
semaphore + AsyncToSyncAdapter), but from the standalone host it caused **total thread-pool
starvation**: navigate hung and *no* async timeout (`Task.Delay`/`Task.Run` watchdog) could even be
scheduled. The fixes that make it reliable:

1. **`ThreadPool.SetMinThreads(256, 256)`** at host startup — pre-seeds the pool so init continuations
   never wait on slow growth. (64 was insufficient; 256 cleared it.)
2. **A kernel `Task.Wait(timeout)` on the request thread** (not `Task.Delay`) bounds the walk —
   OS-enforced, immune to pool starvation. `--timeout` (clamped to `[5,3600]`s).
3. **The client does not retry on timeout** — a timeout-retry requeues a duplicate walk behind the
   serial host loop and spawns zombie hosts.

The Phase-0 spike (`.skill-explore/spike-pause-resume.md`) validated the HttpListener-across-`await`
model before any of this was built.

---

## 4. The knowledge base — `Documentation/agent-knowledge/`

- **`index.yml` is canonical** (84 topic records: `topic`, `summary`, `primary`, `related[]`).
  `INDEX.md` is a regenerated human-readable mirror. 85 leaf `.md` files total (incl. 5 INDEX.md).
- Leaves carry frontmatter (`--- topic / summary / status ---`) + markdown body, grouped:
  - `framework/` — how the codebase works (overview, field-registry, ui-field-types, page-objects,
    flows-executor, test-class, popups, logging, test-design).
  - `domain/` — business knowledge (glossary, tc-api, tc-conventions, product-areas; `lob/*`
    per line-of-business; `partners/*` per tenant).
  - `recipes/` — task playbooks (locator-recipes, override-patterns, test-review-rubric;
    `troubleshooting/*` per symptom).
  - `philosophy/` — net-new prose on *why* (sparse-dictionaries, fluent-page-objects,
    logging-where-work-happens, tests-stay-clean, when-to-automate, design-decisions/ADRs).
- **Topic keys** follow `area:slug` (`framework:flows-executor`, `domain:tc-api`, `philosophy:tests-stay-clean`).
  Tenant/LOB specifics are reached via `kb search`, not memorized keys.

---

## 5. The skills — `.claude/skills/`

Seven `SKILL.md` files, each thin orchestration (no `references/` subdir — they *navigate* the KB):

| Skill | Audience | Triggers on | Orchestrates |
|---|---|---|---|
| `nexus-test-author` | automation team | "automate TC X" / pasted TC | `tc fetch` → `kb` → `code field-lookup`/`flow-trace`/`find-similar` → iterate → `failure summarize` |
| `nexus-debug` | automation team | "why did test X fail" / "show me page Y" | `failure summarize` **first** → `failure page-source` → `browser navigate --headed` |
| `nexus-test-review` | automation team | "review this test" | `code find-similar` (siblings) → `recipe:test-review-rubric` → `philosophy lookup` |
| `nexus-framework-review` | automation team | "review this framework change/PR" | `code diff-impact` (blast radius) → `philosophy lookup` per area → `kb` |
| `nexus-secrets` | any dev machine | secrets/`BOLT_SECRETS_PATH` failures, first-time setup | AWS SSO walkthrough → `secrets sync`/`status` → env-resolution troubleshooting |
| `nexus-dev-setup` | any dev machine | reimaged/reset machine, first clone, "SDK not found" / "Bolt.* package not found" / missing Playwright browsers | toolchain (.NET 10, PS7, VS Code) → NuGet restore vs private feed → Playwright browsers → runsettings → nexus-secrets → verified build + green smoke test |
| `nexus-qa-assist` | **manual QAs / product teams** | "get me to page X with Y prefilled", "help me test X manually", replay a saved scenario | silent preflight (pull/build/secrets) → `kb`/`flow-trace`/`field-lookup` mapping → `browser navigate`/`open-quote`/`fill`/`pause`/`continue` cadence → scenario save/replay (`recipe:qa-scenario-format`, `.qa-scenarios/`, gitignored) |

Every skill opens with the *ask-don't-assume* doctrine and names explicit STOP-and-ask triggers
(unknown tenant, ambiguous mapping, 3 failed iterations, public-API change without stated intent,
need for real PII / SSO hardware key / captcha). `nexus-qa-assist` additionally holds the
**cooperative-over-autonomous** line hardest: pauses are never auto-confirmed — the human
verification is the product, not an obstacle.

---

## 6. The CLAUDE.md map

The root `CLAUDE.md` is a **map**, not an encyclopedia: project overview, build/test commands,
solution structure, a consolidated **Never-do** list (no `Thread.Sleep`/XPath/raw `HttpClient`/direct
env-var reads/secret logging), the AI-logging summary, and a **Load-on-demand** table pointing to the
five per-project `CLAUDE.md` files + this KB. Per-project files carry the area detail; the **NUnit
parameterized-test conventions live verbatim** in `Bolt.Automation.Tests/CLAUDE.md` (the orchestrator's
FQN discovery depends on those exact rules).

---

## 7. Doctrine (the "why" behind the shape)

1. **Ask, don't assume.** Pausing is the protocol. Three mechanisms: `AskUserQuestion`,
   `browser pause`, `code wip-stop`.
2. **Skills navigate, they don't carry.** Knowledge lives in the KB; skills read only the leaves a
   task needs. Keeps agent context small and the KB single-sourced.
3. **Deterministic tools beat agent inference.** Recurring reasoning (find the failure artifacts,
   resolve a field, walk a flow) is promoted to a CLI command — faster, cheaper, reproducible.
4. **Cooperative over autonomous.** Yield the keyboard when the user is better placed (hardware key,
   captcha, real data); surface structured status.

ADRs for specific choices (CommandLineParser, grep-vs-Roslyn, HttpListener host, YAML-canonical index)
are recorded in `Documentation/agent-knowledge/philosophy/design-decisions.md`.

---

## 8. On-disk & runtime footprint

| Path | Contents | Lifecycle |
|---|---|---|
| `%LOCALAPPDATA%\nexus-agent\token` | bearer token (user-only ACL) | until replaced/expired |
| `%LOCALAPPDATA%\nexus-agent\tc\<id>.json` | cached test cases | until `tc cache clear` |
| `%TMP%\nexus-agent\kb-index.json` | KB search index cache | rebuilt on KB change/schema bump |
| `%TMP%\nexus-agent\host.lock` | browser host `{pid,port}` | host lifetime |
| `%TMP%\nexus-agent\host.log` | host append-only log | grows; safe to delete |
| `%TMP%\nexus-agent\sessions\<id>\` | per-session browser state | until cleaned |

**None of this is ever committed** — it is all machine-local. Env overrides: `NEXUS_TC_TOKEN`,
`NEXUS_KB_ROOT`, `NEXUS_TEST_RESULTS`, `BOLT_CONFIG_PATH` (set by the browser host so the framework's
`ConfigurationLoader` finds `appsettings*.json` outside the Tests project).

---

## 9. Maintenance routine

### 9.1 Add or change a knowledge leaf
1. Add/edit the `.md` under the right `Documentation/agent-knowledge/<area>/` with frontmatter
   (`topic`, `summary`, `status: ready`).
2. Add/update its entry in **`index.yml`** (`topic`, `summary`, `primary` path, `related`).
3. Regenerate `INDEX.md` to match (keep the mirror in sync).
4. No code change needed — the search cache auto-invalidates on the next `kb search` (leaf-set or
   mtime change). Sanity check: `nexus-agent kb lookup --topic <new-key>` and `kb describe --file <path>`.

### 9.2 Add a CLI command
1. New verb: add a `[Verb]` options class + handler under `Commands/<Noun>/`, register it in that
   noun's `<Noun>Router`. New noun: add a router + a `case` in `Program.Main` and the `--help` text.
2. Emit output **only** through `CommandBase.EmitJsonAsync`/`EmitErrorAsync` (snake_case, exit-code
   convention). Don't hand-roll `Console.WriteLine` JSON.
3. Validate inputs up front and return exit `3` for input errors; reserve `2` for runtime/user-facing
   failures. The global handler is a net, not a substitute for per-command validation.

### 9.3 Keep the KB in sync with the framework
When framework behaviour changes, update the matching leaf — `code diff-impact --ref <base>` lists the
**philosophy/framework areas** each changed file touches (via `PhilosophyAreaMap`), which is the
checklist of leaves to review. Adding a tenant/LOB → add a `domain/partners/*` or `domain/lob/*` leaf
+ index entry. Adding a product/flow → check `framework:flows-executor` is still accurate.

### 9.4 Maintain the skills
Skills reference CLI commands and KB topic keys by name. If you rename a command/verb or a topic key,
grep the four `SKILL.md` files and update them. Keep them thin — new knowledge goes in a KB leaf the
skill *points to*, never inlined into the skill.

### 9.5 Reflection & build hygiene
`code field-lookup`/`flow-trace` and all `browser` commands need a **current** `FrontEnds` build in the
Tests output. After changing FrontEnds: `dotnet build Bolt.Automation.sln` before using them (the
stale-DLL pre-flight will otherwise tell you to). The reflection loader and the browser host both probe
`Bolt.Automation.Tests/bin/<cfg>/net<tfm>/` ranked by TFM.

### 9.6 Browser host operations
- **Kill a stray host before rebuilding** the CLI (`taskkill /IM nexus-agent.exe /F`) — a running host
  locks the exe.
- Orphan headed Chromium after a crash: `Get-Process chrome | ? Path -like '*ms-playwright*' | Stop-Process -Force`
  (path-filtered so you don't kill the user's own Chrome).
- A headed deep walk is genuinely slow (~30–60s); `navigate_timeout` leaves the browser open for
  `inspect`/`screenshot`. **Do not** reintroduce a `Task.Delay`-based timeout or client-side
  timeout-retry (see §3.5).
- First-time browser use needs `playwright install chromium` (the package drops `playwright.ps1` in the
  build output).

### 9.7 TFM bumps & cache schema
- A target-framework bump (e.g. `net10.0`→`net11.0`) flows automatically: the reflection loader and
  `ConfigLocator` build the `net{Major}.{Minor}` probe string from `Environment.Version`. Just rebuild.
- If the KB index **cache shape** or tokenizer rules change, bump `SimpleIndexer.CacheSchemaVersion`
  so old caches self-invalidate. If the `tc` response shape changes, the cache is JSON-tolerant but
  re-fetch with `tc fetch <id> --force-refresh`.

### 9.8 Secrets & machine-local state
- The bearer token is short-lived and stored hardened; **never** log it, commit it, or echo it.
  `TokenStore` fails closed — if ACL hardening fails it deletes the token rather than store it weakly.
- Everything under `%TMP%\nexus-agent` and `%LOCALAPPDATA%\nexus-agent` is disposable; delete to reset.

### 9.9 Pre-merge verification checklist
1. `dotnet build Bolt.Automation.sln` — clean.
2. Smoke the surface: `kb lookup`/`search`, `code field-lookup VIN`, `code flow-trace --flow D2CCondoFlow`,
   `code diff-impact --ref HEAD~1`, `philosophy lookup --area page-objects`, `tc cache list`,
   `failure summarize --test <known-FQN>`, `browser list`.
3. If the browser host changed: one headed `browser navigate … --until <start-page>` + `screenshot` +
   kill-host→`browser list` (respawn).
4. KB consistency: every `index.yml` topic's `primary` resolves; every leaf appears in `index.yml`.
5. No dangling references to removed skill names (`git grep` for them).

### 9.10 Periodic review cadence
- **Per framework PR that touches FrontEnds/Core/Common:** run `nexus-framework-review`; update any KB
  leaf the diff-impact areas flag.
- **Quarterly / on tenant onboarding:** refresh `domain/partners/*` and `domain/lob/*`; re-run the
  skill evals (plan §D.5 — 3 prompts per skill, at least one where the right answer is to STOP and ask).
- **On Playwright/.NET upgrades:** re-verify the browser host lifecycle (§9.6) — its threading is the
  most upgrade-sensitive area.

---

## 10. Extension points & known limits

- **`code diff-impact`** is grep-based (v1); the seam to swap in a Roslyn `IConsumerFinder` exists.
- **`code coverage`** is a deliberate future noun (not built).
- **Browser host concurrency:** the host is single-user/interactive by design. A walk that overruns
  `--timeout` is abandoned (left for inspection); issuing a *second* navigate while a leaked walk still
  runs could contaminate `FieldRegistryProvider` static state — acceptable for interactive debugging,
  documented rather than locked down.
- **`AskUserQuestion` from the CLI:** today the CLI exits 2 with a structured "I need…"; the agent makes
  the actual ask. A future RPC could let the CLI signal it directly.

---

## 10b. The nexus-logger MCP toolkit (companion, not part of this repo)

Some sessions also expose a **`nexus-logger` MCP server** — the same service behind the
`tc` proxy, surfaced directly as agent tools. It is configured per machine/session
(Claude Code MCP config), not shipped by this repo; skills must treat it as
**optional** and keep the CLI paths as the always-available fallback. High-level map:

| Tool group | Tools (abridged) | Relationship to this extension |
|---|---|---|
| Runs & jobs | `create_run` · `get_run` · `list_runs` · `list_run_failures` · `get_job_status` · `cancel_job` · `list_templates` | Orchestrated EKS runs — beyond the CLI's scope; use directly. |
| Test intelligence | `search_tests` · `get_test_history` · `get_test_logs` · `get_test_artifacts` · `get_flaky_tests` · `get_consecutive_failures` · `get_failure_groups` | Complements `failure` (which reads **local** artifacts); the MCP reads **CI** history. Use MCP for "is this flaky in CI?", CLI `failure` for "why did my local run fail?". |
| ADO test cases | `get_test_case` · `search_test_cases` · `get_bugs_for_test_case` · `get_current_sprint` · `get_my_work` | Overlaps `tc fetch`, **without the manual JWT dance** — prefer MCP when connected; `tc fetch` stays the headless/CI fallback. |
| Environment | `get_environment_health` · `get_platform_status` | Feeds session preflight: a degraded QA env is worth knowing *before* driving a browser through it. |
| Agent Inbox | `list/get/create/update/delete_inbox_item` | Async agent-to-agent handoff queue (own finalize workflow in the server instructions). Natural channel for QA-assist handoffs to the automation team (found bugs, registry-adoption requests). Never put secrets in items — server-enforced. |

Skill guidance (kept high-level on purpose — the server's own instructions govern usage):
**nexus-qa-assist** checks `get_environment_health` in preflight when the server is
connected and can hand findings to the automation team via the inbox;
**nexus-test-author / nexus-debug** prefer `get_test_case` / test-intelligence tools over
`tc fetch` when connected. None of the skills may *require* the MCP server.

---

## 11. Troubleshooting quick reference

| Symptom | Likely cause → fix |
|---|---|
| `kb` says "could not locate index.yml" | Running outside the repo → set `NEXUS_KB_ROOT`, or run from the repo. |
| `code flow-trace` says "rebuild first" | Stale `FrontEnds.dll` → `dotnet build Bolt.Automation.sln`. |
| `tc fetch` → `unauthorized` | Token expired (~65 min) → `tc auth set-token --token <fresh-jwt>`. |
| `tc fetch` → `network_error` | Off VPN / DNS → connect to corp network. |
| `browser navigate` hangs / `MSB3027` on build | Stray host locking the exe → `taskkill /IM nexus-agent.exe /F`; check `%TMP%\nexus-agent\host.log`. |
| `browser` "host did not become ready" | Port 5151 busy or host crashed on boot → check `host.log`, kill stale `nexus-agent.exe`, delete `host.lock`. |
| `failure summarize` finds nothing | Wrong/old artifacts dir → set `NEXUS_TEST_RESULTS`, or run the test once to produce a TRX. |

---

*Implemented across `feature/agent-ext-scaffold` (Phases 0–10). Companion docs:
`agent-knowledge/philosophy/design-decisions.md` (ADRs), `AI-Agent-Logging-Instructions.md`
(logging rules the agent must follow), and the per-project `CLAUDE.md` files.*
