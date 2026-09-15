---
topic: partner:progressivepl
summary: ProgressivePL (PGR) — HQX 2.0 flows, HO3/HO6 product switching, Plymouth Rock CovMod.
status: ready
---

# Partner · Progressive — `PROGRESSIVEPL`

> **API `partner` field:** `Progressive` · **Common abbreviations:** PGR, Progressive

**Cache representation:** 6 TCs.

## Business model

Carrier-of-record + heavy D2C consumer flow (HQX 2.0). Both Consumer (`HQXConsumer`) and Agent (`HQXAgent`) flows.

## Entry path

- **D2C:** standard consumer quote start with address + LOB selection.
- **Agent:** HQX Agent uses `PgrAutoFlow` / `PgrHomeFlow`.

## Features in cache

Interview (2), D2C (2 — both HQX 2.0), GetQuoteAPI (2).

## Sub-areas observed

`HQX 2.0`, `Consumer`, `MPQ3`, `INET` (consumer access channel), `ASI CovMod`, `Plymouth Rock`, `PaaS`.

## Real TCs

- **241340** — PGR | HQX 2.0 | Condo — Overview – Verify correct questions and summary values per section.
- **242142** — PGR | HQX 2.0 | HO6 | RoofResponsible enforced when Condominium is selected on Overview.
- **238828** — PGR | Consumer | Adding "Cane Corso" to Dog Breed dropdown list (Pets LOB).
- **238832** — PGR | INET | Accessibility — Zooming/Fluid UI response.
- **239957** — Progressive | Get Quote API | Ignore Custom Fields.
- **240777** — PGR | PlymouthRock | HO3 | Re-Enable Feature Flag for Plymouth Rock CovMods.

## Recipe — MPQ3 QuoteStart → Overview edit → kickout → QuoteStatus

Consumer tests that start a quote via the Platform API, edit one field on the HQX Overview, and then assert the backend `QuoteStatus` share one shape (e.g. TC 248527 MFH kickout, TC 89383 Renters complete). Reference impls: `Bolt.Automation.Tests/Tests/Progressive/DRFlows/MPQ3QuoteStatusTests.cs` and `MPQ3DRTests.cs`.

- **Base class `ProgressiveUITestBase`** already sets `FrontEndType.HQXConsumer` and exposes `_progressiveHelper` (`ProgressiveTestHelper`) + `_platformApiFactory`.
- **Entry:** `_progressiveHelper.CallQuoteStartAsync(request, navigate: true)` → `(externalId, startUrl)`. `QuoteStartPrefillDataProvider.GetStandardPrefillData(address)` defaults to `LOBCd = "Home"` and `RedirectURL = "https://www.progressive.com/"`. Set `request.SourceName = "MPQ3"` (Renters variants use `"MPQ3_Renters"`). User: `TestContextAccessor.CurrentUserCollection.Consumer`.
- **Land on Overview:** `_progressiveHelper.HandleThreePQIfPresentAsync()` handles the optional 3PQ page and returns `HQXConsumer_OverviewPage`.
- **Edit a section field inline** (matches `CondoTests`): `await overviewPage.ExpandSectionAsync(HQXConsumer_OverviewPage.SectionNames.Property); await _pageHelper.InteractWithField(<Field>, <value>);` with `using static …FormData.Common.FieldNames`. Don't wrap two-line edits in a bespoke page-object method — the section-expand-then-interact idiom is the sibling convention.
  - **Home style** = `PLTypeOfDwelling` (`ng-select[name='PLTypeOfDwelling']`) in the **Property** section. Verified option labels: `"Condominium"`, `"Manufactured/Mobile Home"`. Selecting MFH is what triggers the kickout — no `SingleFamilyHome` pre-toggle needed.
- **Kickout redirect:** `overviewPage.ClickContinue()` then `_pageHelper.WaitForNavigationOrUrlContainsAsync("progressive.com")` — the app redirects to the request's `RedirectURL` when the selection is unsupported.
- **QuoteStatus assert:** `platformApi.QuoteStatusWithPollingAsync(req, QuoteStatus.Incomplete, QuoteSecondaryStatus.ManufacturedHome)` polls until both match (100 s default; omit the expected args to poll for `Complete`/`None`). Build the request with `QuoteStatusDataProvider.CreateQuoteStatusData(externalId, sourceName)`. Assert `QuoteStatus`, `SecondaryStatus`, and `MsgStatusCd == "Success"` (`MsgStatusCd` is on `AcordBaseResponse`). `QuoteStatus`/`QuoteSecondaryStatus` enums live in `ApiClients/PlatformApi/Entities/Common/Enums.cs`.

## Known quirks

- **MFH is out of appetite** — selecting "Manufactured/Mobile Home" as the home style on the Overview kicks the consumer out to `progressive.com`; the quote resolves to `QuoteStatus=Incomplete` / `SecondaryStatus=ManufacturedHome` in the Platform API.
- **HQX 2.0** has its own page set distinct from older `HQXConsumer` flows.
- **Plymouth Rock CovMods** are gated by `ABTest-cvg-mod-exp` feature flag — TCs explicitly toggle the flag.
- **Custom-fields API behavior** is feature-flag controlled (`dynamic-custom-fields`) — same payload returns 422 vs silent accept depending on flag.
- **Feature areas split** between HQX Consumer and HQX Agent — verify which one the TC is targeting.

## Cross-references

[INDEX.md](INDEX.md), [../lob/home.md](../lob/home.md), [../lob/condo.md](../lob/condo.md), [../lob/pets.md](../lob/pets.md).
