---
topic: troubleshooting:break-fix-break-loop
summary: Two unrelated fixes interfere with each other; revert, apply one fix per iteration, commit between.
status: ready
---

# Two unrelated fixes → break-fix-break loop

**Symptom:** You fixed locator A, then locator B failed, you fixed B, now A fails differently.

**Likely cause:** You changed two things in one iteration without isolating which one fixed (or broke) what.

**Fix:** Revert. Apply one fix at a time. Commit per fix with a focused subject line. Re-run between commits. Slow is fast; fast is slow.
