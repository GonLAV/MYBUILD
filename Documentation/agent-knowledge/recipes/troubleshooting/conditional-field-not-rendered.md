---
topic: troubleshooting:conditional-field-not-rendered
summary: "Override skip log 'container not present' — field is conditionally rendered, order trigger fields first."
status: ready
---

# Conditional field expected but container not present

**Symptom:** Page-object override logs `"container '<class>' not present, skipping"` or arrow-wrapper helper times out at `WaitForAsync`.

**Likely cause:** The field is conditionally rendered based on a previous answer:

- *Field hidden by an answer earlier in the flow.* `HomeAutoInsurance = "No"` removes `CL_MedPay`, `CL_Combined_UM_UIM`, `CL_FireTheftCAC` from the CL Policy DOM. Accept the skip — the test's intent doesn't change.
- *Field renders only after an upstream selection.* `CL_FireTheftCAC` appears only after Comprehensive + Collision deductibles are selected. Order the override iteration so trigger fields fill first; the `WaitForAsync(state=Attached, Timeout=3000)` inside the helper covers render latency.

**Diagnosis:** Open `page_source_*.html` and grep for the class. If absent and the answer set explains why, ignore the skip. If present but slow to render, raise the helper's timeout.

**Cross-references:** [../override-patterns.md](../override-patterns.md) pattern F (waiting for conditionally-rendered fields), [conditional-reveal.md](conditional-reveal.md).
