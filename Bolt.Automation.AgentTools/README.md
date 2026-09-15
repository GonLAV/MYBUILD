# Bolt.Automation.AgentTools

Executable CLI (`nexus-agent`) invoked by the AI-agent skills (in `.claude/skills/`) to perform deterministic operations against the Nexus framework — knowledge-base navigation, test-case fetching, failure diagnosis, code analysis, live browser session control, secrets sync, machine diagnosis, and SAML minting.

## Why a CLI

Recurring multi-step agent reasoning gets promoted to a CLI command when a deterministic implementation is faster, cheaper in tokens, and less error-prone than asking the agent to figure it out anew each time. Architecture + maintenance guide: `Documentation/AI-Agent-Extension-Architecture.md`.

## Surface

```
nexus-agent <noun> <verb> [args]
```

Nouns (all live): `kb` · `tc` · `failure` · `code` · `browser` · `philosophy` · `secrets` · `doctor` · `saml`.

Run `nexus-agent --help` for the full verb listing; per-noun semantics are documented in `Documentation/AI-Agent-Extension-Architecture.md` §3.3 and the knowledge base.

## Conventions

- **Output**: JSON to stdout by default; pass `--format table` for compact human-readable.
- **Exit codes**: `0` success · `2` user-facing error (e.g. token expired, file not found) · `3` input error (bad args).
- **stderr** for diagnostics — never mixed with the data on stdout. `saml mint` relies on this: the raw base64 assertion is the *only* thing on stdout so callers can do `saml=$(nexus-agent saml mint ...)`.
