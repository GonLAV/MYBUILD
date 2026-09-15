---
topic: domain:tc-api
summary: TC fetch API semantics — NexusLogger endpoints, auth token, response shape.
status: ready
---

# Test-case API (nexus-logger)

> **When to read:** Phase 1, every time, **first** — if the user provides a TC ID, fetch via the API instead of asking them to paste.
>
> **Implementation:** the AgentTools CLI's `tc fetch` / `tc auth set-token` commands wrap this. This file documents the semantics; the CLI handles the actual HTTP + caching.

## Base URL and auth

```
Base:           https://nexus-logger.auto.boltx.us
Health:         GET /            → SPA shell (HTML)
Auth:           JWT Bearer in Authorization header
Audience (aud): c609c1cd-8c0a-4967-a457-43bebef600a7
Issuer:         https://login.microsoftonline.com/28c2a766-03cc-4b78-8f57-8d5d5f8b65bd/v2.0
```

The user authenticates with their Azure AD identity (their `@boltinc.com` email). The CLI receives the Bearer token from the user (via `tc auth set-token`) and uses it directly. **Tokens expire ~65 minutes after issuance** (`exp` claim in the JWT). If the CLI encounters HTTP 401, exit non-zero with a clear "token expired — run `tc auth set-token` with a fresh token" message; do not attempt refresh.

## Test-case endpoints

All under `/api/azure-devops/`. The middleware `app.use('/api/azure-devops/*', jwtAuth)` enforces auth.

### `GET /api/azure-devops/test-cases` — list

Query params (all optional):

| Param | Type | Notes |
|---|---|---|
| `partner` | string | UI label, e.g. `Bolt AG`, `Kraftlake (Bolt X)`, `Progressive`. URL-encode the value. |
| `feature` | string | e.g. `Interview`, `D2C`, `ADBX`, `GetQuoteAPI`, `STS & Login`. |
| `tcType` | string | `E2E Test`, `API Test`, `UI/UX Test`, `Negative Test`. |
| `sanityRegression` | string | `Sanity` or `Regression`. |
| `state` | string | `Design`, `Pending Review`, `Approved`, `Pending Automation`, `Automated`, `Ready`, `Deprecated`. |
| `areaPath` | string | e.g. `Epos\RnD\Dmytro's Team`. |
| `top` | integer | Limit results; default ~50. |
| `search` | string | Free-text title search (WIQL CONTAINS). |

Response: `{ "data": TestCaseSummary[] }` — each item has `id`, `url`, `title`, `state`, `priority`, `areaPath`, `iterationPath`, `tags`, `assignedTo`, `partner`, `feature`, `tcType`, `sanityRegression`, `createdDate`, `changedDate`. (No `description`, `steps`, or `links` — use the detail endpoint for those.)

### `GET /api/azure-devops/test-cases/{id}` — detail

Path: `id` is the ADO work-item integer.

Response: `{ "data": TestCase }` — full schema below.

### `POST /api/azure-devops/test-cases` — create

For TC authoring; not relevant to the skill (the skill consumes TCs, doesn't author them).

### `PATCH /api/azure-devops/test-cases/{id}` — update

For TC editing; rarely used by the skill (when an automation engineer flips state from `Pending Automation` → `Automated` after PR merge).

## TestCase schema (detail response)

Mirrors `AzureDevOpsTestCaseSchema` in `packages/logger/src/api/schemas.ts`.

```jsonc
{
  "id": 240782,                            // ADO work-item ID
  "url": "https://azure.devops.boltx.us/.../_workitems/edit/240782",
  "title": "KLX | Interview E2E | CL Auto | old interview, submitting quote",
  "state": "Pending Automation",            // see tc-conventions.md
  "priority": 2,                            // 1-4
  "areaPath": "Epos\\RnD\\Dmytro's Team",
  "iterationPath": "Epos\\2607",
  "tags": "ADBX; CRM; default",             // comma/semicolon-separated
  "assignedTo": "Aya Abdalla",              // display name
  "reason": "Move",                          // last state-change reason
  "createdDate": "2026-04-19T08:53:44.407Z",
  "changedDate": "2026-04-19T09:01:17.947Z",
  "changedBy": "Aya Abdalla",

  // Custom Bolt fields:
  "partner": "Kraftlake (Bolt X)",
  "feature": "Interview",
  "tcType": "E2E Test",
  "sanityRegression": "Sanity",
  "tcSource": "Feature",
  "executionEnv": "QA, UAT, Staging",
  "componentModel": null,
  "featureFlag": null,
  "codeReview": null,
  "comment": null,

  "description": "<DIV>...preconditions...</DIV>",  // HTML

  "steps": [
    {
      "action": "<DIV><DIV><P>Access KLX flow, use the user:<BR/>lsp1_aortest@test.com<BR/>group: AOR-TEST</P></DIV></DIV>",
      "expectedResult": "<BR/>",
      "attachmentUrl": null,
      "attachmentFileName": null
    },
    // ...more steps
  ],

  "links": [
    {
      "rel": "System.LinkTypes.Dependency-Forward",
      "url": "https://azure.devops.boltx.us/.../workItems/241412",
      "id": 241412,
      "title": "Test Case 240782: KLX | Interview E2E | CL Auto | old interview, submitting quote",
      "state": "Active",
      "workItemType": "Task"
    }
    // ...more
  ]
}
```

### Step shape

```jsonc
{
  "action": "<HTML>...</HTML>",
  "expectedResult": "<HTML>...</HTML>",
  "attachmentUrl": "https://...",          // optional — image/file URL
  "attachmentFileName": "screenshot.png"    // optional — original filename
}
```

### Link shape

```jsonc
{
  "rel": "System.LinkTypes.Related",
  "url": "https://...",
  "id": 234567,                              // optional — present for work-item links
  "title": "...",                            // optional
  "state": "Active",                         // optional — work item state
  "workItemType": "Task" | "Test Case" | "Bug" | ...,
  "comment": "..."                           // optional
}
```

## Other useful endpoints (under `/api/azure-devops/`)

The SPA bundles references to these — useful for richer Phase 1 context:

| Endpoint | Use |
|---|---|
| `GET /api/azure-devops/area-paths` | Enumerate teams (validates the area-path on a TC). |
| `GET /api/azure-devops/iteration-paths` | Sprint enumeration. |
| `GET /api/azure-devops/teams` | Team metadata. |
| `GET /api/azure-devops/sprints` | Sprint metadata. |
| `GET /api/azure-devops/current-sprint` | The active sprint — useful for filtering recent work. |
| `GET /api/azure-devops/my-work` | Work items assigned to the current user. |
| `GET /api/azure-devops/test-plans` | Test plans (collections of TCs). |
| `GET /api/azure-devops/test-runs` | Test runs (executions). |
| `GET /api/azure-devops/automation-tasks` | Automation Task work items linked to TCs (the automation engineer's queue). |
| `GET /api/azure-devops/bugs` | Bug work items. |
| `GET /api/azure-devops/shared-steps` | Shared step definitions (reusable across TCs). |
| `GET /api/azure-devops/shared-parameters` | Shared parameters for parameterized TCs. |

## User identity

`GET /api/users/me` returns the authenticated user's profile (email, name, preferences). Useful when the skill needs to set `[Author(Author.X)]` from the user's identity.

## How the skill should use the API

### Phase 1 entry path

When the user says any of:
- "automate TC 240782"
- "/nexus-test-author 240782"
- "TC 240782 — let's go"
- (or pastes a URL like `https://azure.devops.boltx.us/.../_workitems/edit/240782`)

The skill:

1. Extracts the integer ID.
2. Runs `nexus-agent tc fetch <id>` — the CLI handles token resolution, the HTTP call, and disk caching.
3. The CLI's response is parsed into the Phase 1 manifest (see [tc-conventions.md](tc-conventions.md)).
4. The manifest is shown to the user for correction.

### When to fall back to paste mode

- HTTP 401 (token expired). Ask for fresh token via `tc auth set-token`.
- HTTP 404 (TC not found). Confirm the ID with the user.
- HTTP 500. The CLI retries once internally; if still failing, fall back to "paste the TC text."
- User explicitly says "I'll paste it." Don't push the API path.

### Caching

The CLI caches per-user at `%LOCALAPPDATA%\nexus-agent\tc\<id>.json`. Re-fetch by running `tc fetch <id> --force-refresh` — TCs change. Never commit the cache.

## Reference fetch (curl example — for debugging the API directly)

```bash
TOKEN="<paste your Azure-AD JWT>"

# Detail
curl -sS \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Accept: application/json" \
  "https://nexus-logger.auto.boltx.us/api/azure-devops/test-cases/240782" \
  | jq '.data | {id, title, partner, feature, state, stepCount: (.steps | length), descLen: (.description | length)}'

# List by partner
curl -sS \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Accept: application/json" \
  "https://nexus-logger.auto.boltx.us/api/azure-devops/test-cases?partner=Kraftlake%20%28Bolt%20X%29&top=20" \
  | jq '.data[] | {id, title, feature, state}'
```

The CLI exists so the skill doesn't have to deal with this — but the raw curl form is the source of truth when debugging an API issue.

## Error responses

| HTTP | Body | Meaning |
|---|---|---|
| 200 | `{ "data": ... }` | Success. |
| 401 | `{ "error": "Unauthorized" }` | Token missing / expired. |
| 401 | `{ "error": "Azure DevOps PAT not configured. Go to Settings to add your PAT." }` | The user authenticated to nexus-logger but hasn't linked their ADO PAT in `/api/users/me/pat`. Ask the user to add it via the SPA Settings page. |
| 404 | `{ "error": "Test case not found" }` | ID doesn't exist or user lacks ADO permissions. |
| 422 | `{ "error": "Validation error", ... }` | Bad query / body. |
| 500 | varies | Backend error; CLI retries once internally, then fails. |

## Anti-patterns

- **Sending the token in a query string.** Use the `Authorization` header. Tokens in query strings leak via referrer / server logs.
- **Assuming the token is reusable across sessions.** It expires; the CLI handles 401 by exiting with a clear "fresh token needed" message.
- **Asking the user for the TC ID and the body.** If you have the ID, run `tc fetch` — don't ask them to paste what the CLI can read.
- **Calling `POST /test-cases` from the skill.** The skill consumes; it doesn't author.
- **Using SPA HTML routes for data.** `/test-cases` (no `/api/` prefix) returns the SPA shell, not data. Always use `/api/azure-devops/...`.
