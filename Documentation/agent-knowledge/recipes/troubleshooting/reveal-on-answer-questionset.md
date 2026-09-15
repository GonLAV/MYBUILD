---
topic: troubleshooting:reveal-on-answer-questionset
summary: Expected question set undercounted because sub-fields reveal only after a parent is answered; trust the live run, not static DOM or the manual TC list.
status: ready
---

# Reveal-on-answer question set — expected list is undercounted

**Symptom:** A "verify the displayed questions match" test (carrier questions, interview question sets) fails with `Extra: <sub-field labels>` even though your expected list matches the manual TC exactly. Example (TC 245315, PGR E&S Bamboo Surplus): the TC lists 5 questions, but the live page renders 7 — answering `PL_Bankruptcy` = Yes reveals `CurrentlyOnBankruptcy` + `PastBankruptcy` checkboxes that the extractor then picks up.

**Likely cause:** Some questions are **child fields that reveal only after their parent is answered.** Before the parent is answered, they are absent (or in an empty, `aria-hidden`/`inert` `children-wrapper` container) — so a static DOM snapshot taken pre-answer, *and* the manual TC's written list, both undercount. The test answers the parent (e.g. via the field's `DefaultValue = "true"`), the children render, and `ExtractCarrierQuestionsAsync` surfaces them as "extra".

**Why static analysis misleads here:** reading the page object, the registry, or even a pasted pre-answer DOM will confidently produce the *wrong* expected count. Only the live run — after the parent field is answered — shows the true rendered set. Do not over-invest in static review of "what should render" for these validations.

**Fix:**
- Run the test once, read the logged `Actual Questions` line, and make the expected list match the rendered reality (including revealed sub-fields).
- Cross-check against sibling carriers: in this framework, every HO3/HO6 list that includes `PL_Bankruptcy` also includes `CurrentlyOnBankruptcy` + `PastBankruptcy` — a strong prior that answering bankruptcy=Yes reveals them. Use siblings as the hypothesis, the run as the proof.
- If the TC author's intent truly is "only the top-level questions," that's a conversation with the author — but the automated assertion must match the DOM, so encode the revealed set and note the divergence from the TC text.

**Related:** [conditional-reveal.md](conditional-reveal.md) is the *fill-mechanics* cousin (a field you must fill appears mid-fill); this leaf is about the *expected-data* count for a validation. See also [../override-patterns.md](../override-patterns.md) for how reveals are modeled (`DependsOn`).
