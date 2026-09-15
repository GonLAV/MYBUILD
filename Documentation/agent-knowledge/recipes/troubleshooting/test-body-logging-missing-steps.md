---
topic: troubleshooting:test-body-logging-missing-steps
summary: Test body uses Info() instead of StartStep — report has no step scopes; wrap test phases in steps.
status: ready
---

# Test method body has logging but report is empty

**Symptom:** Test runs and asserts, but the test report has no step entries.

**Likely causes:**

- You used `_logger.Info("...")` instead of wrapping in `_logger.ExecuteStepAsync`. Plain `Info` doesn't create step scopes.
- Forgot the third argument (expected result) — step still emits but lacks the expected-vs-actual comparison.
- An assertion threw and its actual value is nowhere in the report — the message didn't name it and nothing logged it where it was gathered. Fix the message first; `LogDataValidation` is only for values a message can't carry.

**Fix:** See [../../framework/logging.md](../../framework/logging.md) "Step granularity" and "Checklist before considering logging done."

**Cross-references:** [../../philosophy/logging-where-work-happens.md](../../philosophy/logging-where-work-happens.md), [../../philosophy/tests-stay-clean.md](../../philosophy/tests-stay-clean.md).
