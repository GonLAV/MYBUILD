---
topic: troubleshooting:tab-switch-ordering
summary: After ADBX popup adds a tab, switch to last tab + set FrontEnd before CreatePage<T>.
status: ready
---

# Tab switch ordering after ADBX popup

**Symptom:** After clicking ADD on the New Quote popup, the new tab opens but `PageFactory.CreatePage<NextPage>()` errors with the wrong URL.

**Likely cause:** Tab switch happens asynchronously; if you call `PageFactory.CreatePage` before the new tab is the active context, you get the old tab.

**Fix:** Switch tabs explicitly before instantiating:
```csharp
await BrowserManager.SwitchToLastTabAsync();
ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);  // if crossing FrontEnds
var startPage = PageFactory.CreatePage<Product_StartPage>();
```

**Cross-references:** [../../framework/popups.md](../../framework/popups.md), [../../framework/overview.md](../../framework/overview.md) "Crucial pattern: a single test often spans multiple FrontEnds".
