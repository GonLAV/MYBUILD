---
topic: philosophy:compact-comments
summary: Comments explain the non-obvious why, compactly. Durable/wide-relevance knowledge goes to the KB, not inline prose.
status: ready
---

# Comments stay compact; durable knowledge lives in the KB

A comment earns its place by explaining what the code cannot say for itself — the **non-obvious why**: a race, a backend quirk, a deliberate omission, a signal that isn't self-evident. Comments that restate the code, narrate the steps, or re-explain a platform behavior are noise; they rot, they drift from the code, and they bury the one comment that mattered.

## The rules

1. **Explain the why, not the what.** `// wait for the offer to detach — the button stays until the async E&S result lands` is worth keeping. `// click the button` is not. If a reader can see it from the code, don't write it.
2. **Compact, not essays.** One or two lines. A method that needs three paragraphs to explain itself is either doing too much (split it) or carrying knowledge that belongs in the KB (link it). Multi-line comment blocks on every new method are a smell.
3. **Wide-relevance knowledge → the KB, not inline.** A new platform feature, a carrier/tenant behavior, a flow quirk that future tests will also hit — that is knowledge with a lifetime longer than this one method. Put it in a KB leaf (`Documentation/agent-knowledge/…`), a skill reference, or a `domain:` note, and let the code link to it (`// see kb domain:<topic>`). Inline prose is invisible to `nexus-agent kb search`; a leaf is found by everyone who needs it.
4. **Don't narrate steps.** `// Step 1 — …` above an `ExecuteStepAsync("…")` (or any self-describing call) duplicates the label. See [tests-stay-clean.md](tests-stay-clean.md) check 9.
5. **XML doc comments: one-line summary.** A `<summary>` should state what the member does and return, tersely. Reserve extra lines for a genuinely non-obvious contract (e.g. "returns null when the determination can't be made"), not for restating the signature.

## The test

Before writing a comment, ask: *would this be more useful to future-me as a searchable KB leaf than as prose only readers of this exact file will ever see?* If yes, it belongs in the KB. If the comment survives that test and still explains a non-obvious why in a line or two, keep it.

## Cross-references

- [tests-stay-clean.md](tests-stay-clean.md) — the test-layer application (check 9: no step-narration comments).
- [../recipes/test-review-rubric.md](../recipes/test-review-rubric.md) — the review checklist flags verbose/narration comments.
