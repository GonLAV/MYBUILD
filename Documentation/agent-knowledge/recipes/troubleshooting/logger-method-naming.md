---
topic: troubleshooting:logger-method-naming
summary: "_logger?.Warn doesn't exist — use Warning. Same for Info/Error/Debug/Fatal."
status: ready
---

# `_logger?.Warn` doesn't exist

**Symptom:** Compile error: "no method 'Warn' on IAutomationLogger."

**Fix:** Use `Warning`. Same with `_logger?.Info`, `_logger?.Error`, `_logger?.Debug`, `_logger?.Fatal`.

**Cross-references:** [../../framework/logging.md](../../framework/logging.md) — full IAutomationLogger interface.
