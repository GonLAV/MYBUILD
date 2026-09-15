---
name: nexus-framework-review
description: Review a change to the nexus FRAMEWORK (FrontEnds, Core, Common, ApiClients, TestDataProvider; page objects, FieldRegistry, flows, DI, logging, helpers) for blast radius and design-philosophy impact, not just local correctness. Trigger when the user asks to review a framework/infrastructure change, a PR touching shared code, or a new/changed public API. Do NOT trigger for test-only changes (nexus-test-review), authoring a test (nexus-test-author), or debugging a failure (nexus-debug).
---

# nexus-framework-review

Review framework changes with awareness of who they affect and which design principles they touch. A one-line change in shared code can ripple across hundreds of tests. Navigate the KB on demand with `nexus-agent kb` / `nexus-agent philosophy`.

## Doctrine: impact first, ask about intent

- A framework change is never local. Establish the blast radius before judging.
- A public-API change without a stated backward-compat intent is a question to the author, not an automatic defect. Ask.
- When a change diverges from a documented principle, cite the principle and ask whether the divergence is intentional.

## CLI commands this skill uses

| Need | Command |
|---|---|
| Touched files, changed symbols, consumers, philosophy areas | `nexus-agent code diff-impact --ref <git-ref>` |
| Philosophy docs for an affected area | `nexus-agent philosophy lookup --area <area>` |
| Other consumers of a symbol | `nexus-agent code find-similar --pattern <Symbol>` |
| Framework idiom details | `nexus-agent kb lookup --topic framework:<topic>` |

## Flow

1. **Map the blast radius.** `code diff-impact --ref <base>` (e.g. `origin/develop` or `HEAD~5`) lists, for each changed `.cs`, the changed symbols, the consumer files that reference them, and the philosophy areas the path maps to. A high consumer count on a shared symbol means high risk; weight the review accordingly.
2. **Open the philosophy for each area** diff-impact surfaced, via `philosophy lookup --area <area>`. Common ones:
   - `FieldRegistry` / form data: `field-registry` (`philosophy/sparse-dictionaries.md`).
   - Page objects / `*Page.cs` / `InterviewBase`: `page-objects` (`philosophy/fluent-page-objects.md`).
   - Logging: `logging` (`philosophy/logging-where-work-happens.md`).
3. **Judge against the principle, not taste.** Does the change keep page objects intent-revealing? Keep field dictionaries sparse (no dense per-field classes)? Keep logging in implementations? Read the relevant `framework:<topic>` leaf for the mechanism it changes.
4. **Probe public-API and contract changes.** If a public signature changed, run `code find-similar --pattern <Symbol>` to see callers; ask the author about backward-compat intent and migration. Flag silent behavior changes to shared helpers.
5. **Report.** Lead with blast radius (N consumer files across M projects). For each finding cite a consumer file and a philosophy or framework leaf. Separate "must fix" from "intentional? confirm with the author".

## What to flag

- A shared or public API change with many consumers and no compat note. Ask intent.
- Dense per-field classes instead of the sparse `FieldRegistry` dictionary pattern.
- Page objects leaking Playwright primitives instead of intent methods.
- Logging moved into, or expected from, test methods.
- New `Thread.Sleep`, XPath, raw `HttpClient`, or direct env-var reads in framework code.

## KB topics to open first

Whatever `diff-impact` surfaces, plus `philosophy:design-decisions` (ADRs) for precedent. Then `framework:<topic>` for the specific mechanism changed.
