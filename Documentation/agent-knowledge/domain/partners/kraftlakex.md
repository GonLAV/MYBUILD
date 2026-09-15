---
topic: partner:kraftlakex
summary: KraftlakeX (KLX) — CL/BOP focused; Cert/DeclinationReason quirks, class collapsing, app-form-label overlay.
status: ready
---

# Partner · Kraftlake (Bolt X) — `KRAFTLAKEX`

> **API `partner` field:** `Kraftlake (Bolt X)` · **Common abbreviations:** KLX, KraftlakeX, Kraftlake, KL

**Cache representation:** 6 TCs.

## Business model

LSP partner — agents belong to LSP groups (`AOR-TEST`, etc.). Multi-tenant under the Bolt X umbrella.

## Entry path (canonical)

"Access KLX flow, use the user `lsp1_aortest@test.com` group `AOR-TEST`" (TC 240782, step 1). Login via standard STS; agent's `GroupExternalId` is `AOR-TEST`.

## Features in cache

Interview (3), ADBX (3), CMAPI (1).

## Sub-areas observed

`KLX | Interview E2E | CL Auto`, `KLX | ADBX to CM | Sales case`, `KLX | CMAPI | Sales case`.

## Real TCs

- **240782** — KLX | Interview E2E | CL Auto | old interview, submitting quote (full E2E to result page).
- **237437** — KraftlakeX | CL | BOP | Markets results | CL_offline page | Offline request.
- **235615** — Kraftlake | Agent Interview | Personal Home | Result page | Application forms E2E.
- **197386** — KLX | ADBX to CM | Sales case | Create | PL | Bind request | Dummy quote | Message.
- **198617** — KLX | CMAPI | Sales case | KLX login | ADBX | Lead page.
- **188906** — QA | KL (X) — ADBX Adjustments — Create note for sales case creation without messages.

## KLX old-interview vs new-interview

KLX has two CL flow generations. The "old interview" (TC 240782) routes through `/CL_Start`, `/MarketResults`, `/CL_Business`, `/CL_Vehicle`, `/CL_Operator`, `/CL_Policy`, `/CL_Applicant`. The new interview uses different URLs. The existing flow `InterviewCLAutoFlow` targets the **old** interview.

If a TC explicitly says "old interview," reuse `InterviewCLAutoFlow`. If it doesn't specify and the URL fragments don't match, you may need a new flow.

## Class collapsing

KLX collapses `PolicyData.Vehicles[?(@__id__='guid')].Year` (the Angular schema name) into HTML class names like:

```
PolicyDataVehicles__id__-{shortGuid}-YearVehicleDropdownWithSearch...
```

Selectors that work on BOLTAG (`//*[contains(@class,'PolicyData.PLYear')]//ng-select`) miss on KLX. **Fix:** use XPath OR with both schemes:

```xpath
//*[contains(@class,'PolicyData.PLYear')]//ng-select |
//*[contains(@class,'PolicyDataVehicles') and contains(@class,'Year') and not(contains(@class,'IsDifferent'))]//ng-select
```

Always include a `not(contains(@class,'SiblingPrefix'))` clause for any field whose name is a substring of another field on the same page (`Year` vs `YearOfDriving`).

## VIN decode flow on the Vehicle page

KLX Vehicle page uses VIN-decode-then-fill: type the VIN, click Submit, wait for Year/Make/Model/BodyStyle to populate, *then* fill the rest. Implemented in `Product_VehiclePage.FillVinAndDecodeAsync`:

1. Type VIN into `//*[contains(@class,'vehicle-search-block')]//input[@placeholder='Type VIN Number']`.
2. Press Tab to commit through Angular validators.
3. Wait for Submit button to be **not disabled**.
4. Click Submit.
5. Wait for `Loader.First` to hide.
6. Wait up to 30s for the Year ng-select to flip to `ng-valid`.

If the decode signal never arrives, log a warning and continue — the downstream dropdown will fail loudly enough. See [../../recipes/troubleshooting/vin-decode-flake.md](../../recipes/troubleshooting/vin-decode-flake.md).

**Suppression of decoded fields.** After decode, set VIN/PLYear/PLMake/PLModel/VehicleOwnerShip to empty strings in the filtered formData so `MergeDataManager` doesn't re-select them through the cascading dropdowns.

## Markets page Get Quotes button

KLX Markets routes to `/MarketResults` (not `/Markets`). The Continue button is `<button class="Get button default … navy quotes type-button">`. `InterviewBase.ContinueButton` was extended to include `button.Get.quotes`, so `Product_MarketsPage` is bare — no `ClickContinue` override needed.

## Cert + DeclinationReason via DependsOn (no override)

The Farmers-declination certification checkbox **reveals** the `DeclinationReason` ng-select after a few seconds of Angular hydration. The cleaned `wip/klx-cl-auto-tc240782` branch handles this entirely in the field registry — no `Product_StartPage.FillForm` override is needed:

```csharp
[Certification] = new UIElement {
    Locators = "//*[contains(@class,'PolicyData.CLCertifyFarmersDeclined')]//label[contains(@class,'checkbox-wrapper')]",
    FieldType = UIFieldType.Button,
    DefaultValue = "true",
    Pages = [typeof(Product_StartPage)],
},
[DeclinationReason] = new UIElement {
    Locators = "//*[contains(@class,'PolicyData.CL_IneligibleReason')]//ng-select",
    FieldType = UIFieldType.Dropdown,
    DefaultValue = "Class/SIC code not available with Farmers",
    Pages = [typeof(Product_StartPage)],
    DependsOn = Certification,
    DependsOnValue = "true",
    InteractionOptions = new ElementInteractionOptions { Timeout = 15000 }
},
```

`FormDataHelper.GetOrderedFields` topologically sorts so `Certification` fills first; `DeclinationReason` skips unless cert is `"true"`; `InteractionOptions.Timeout = 15000` absorbs the hydration delay (3s flakes, 15s reliable). See [../../recipes/override-patterns.md](../../recipes/override-patterns.md) "Override pattern A".

The Cert field renders as `<label class="checkbox-wrapper">` wrapping a hidden input — `FieldType = Button` clicking the label, not `Checkbox` (strict-mode `CheckAsync` fails on the hidden input).

The DeclinationReason locator uses the underscore-prefixed Angular schema name: `PolicyData.CL_IneligibleReason` (not `PolicyData.DeclinationReason`).

## Switcher Yes/No on Business page

The Business page uses `<label class="switcher-wrapper">` to wrap hidden checkboxes. `OwnerInvolvedInDailyOperations` and `TowingOrHauling` are required on this page; both use the switcher pattern with `FieldType = Button` (not Checkbox) — strict-mode `CheckAsync` fails on the hidden input.

## FEIN must be exactly 9 digits

The TC may use placeholder values like `4785X`. KLX Business page validates FEIN as 9 numeric digits:

```csharp
[FieldNames.FederalIDNumber] = "478500001",  // not "4785X"
```

If you let the placeholder through, the field shows a validation error and `ClickContinue` fails.

## Account-match popup behavior

When entering through "open existing account → New Quote → ADD with overlapping data," the account-match popup **always** shows in our test runs (the data we use matches an existing customer). Default to `ResolveAsync(softMatchAction: AccountMatchSoftAction.SelectExisting)` — that handles both hard-match and soft-match.

## CL Policy floated-label ng-selects

On `/CL_Policy` almost every ng-select has an `<app-form-label>` overlay that intercepts the center-click Playwright uses to open the panel. Affected fields:

- `CurrentBopCarrier` (search dropdown — "Please select the insurance company …")
- `CL_BIPD`, `CL_MedPay`, `CL_Combined_UM_UIM`, `CL_UMPD` (limit dropdowns)
- Per-vehicle `CL_Comprehensive`, `CL_Collision`, `CL_FireTheftCAC`
- Per-vehicle `CL_FullGlass`, `CL_RoadSide` (Yes/No switchers — same intercept)

Fix: `Product_CLPolicyPage.FillForm` pulls the affected keys out of `formData`, runs `base.FillForm` for everything else, then iterates a `(FieldName, ClassFragment, FirstIfMissing)` tuple list and clicks `.ng-arrow-wrapper`. Falls back to the first available option when the requested text isn't in the dropdown (carrier-specific). See [../../recipes/override-patterns.md](../../recipes/override-patterns.md) "Override pattern F" + [../../recipes/troubleshooting/floated-label-interception.md](../../recipes/troubleshooting/floated-label-interception.md).

## CL Policy mat-datepicker EffectiveDate

The "When do you need your insurance to begin?" field is a Material datepicker; the visible `<input placeholder='MM/DD/YYYY'>` is rejected by Playwright `FillAsync` (the underlying matinput is `display: none`). Same JS DOM-event workaround as Operator DOB. `Product_CLPolicyPage` handles it in `FillForm` after `base.FillForm`. See [../../recipes/override-patterns.md](../../recipes/override-patterns.md) "Override pattern E" + [../../recipes/troubleshooting/mat-datepicker-fillasync.md](../../recipes/troubleshooting/mat-datepicker-fillasync.md).

## CL Policy conditional fields

`HomeAutoInsurance = "No"` ("Do you currently have a commercial auto policy?") **hides** `CL_MedPay`, `CL_Combined_UM_UIM`, `CL_FireTheftCAC` from the DOM. Conversely, `CL_FireTheftCAC` only **renders after** Comprehensive and Collision deductibles are selected.

Practical consequence: the arrow-wrapper helper inside `Product_CLPolicyPage` does `WaitForAsync(state=Attached, Timeout = 3000)` on each container before bailing — too-fast checks skip fields that haven't rendered yet. Conversely, fields that legitimately disappear log a benign "container not present, skipping" warning — correct.

## CL Policy per-vehicle coverage classes

Per-vehicle coverage controls live inside a container whose `class` attribute is the Angular path `PolicyData.Vehicles[?(@__id__='<guid>')].CL_X`. Because XPath `contains()` is substring-based, `contains(@class,'CL_FullGlass')` matches without needing the `Vehicles[?(...)]` prefix. The CL-prefixed leaf names are:

| Class fragment | Field | Type |
|---|---|---|
| `CL_FullGlass` | Full Glass | Yes/No switcher |
| `CL_RoadSide` | Road Side Assistance | Yes/No switcher |
| `CL_Comprehensive` | Comprehensive Deductible | dropdown |
| `CL_Collision` | Collision Deductible | dropdown |
| `CL_StatedAmount` | Current vehicle value | input |
| `CL_TransportationExpense` | Rental Reimbursement | dropdown |
| `CL_FireTheftCAC` | Fire & Theft w/CAC | dropdown |

## Staging timing

KLX Staging is intermittently flaky. Specific click + nav-wait failures we've seen: STS login submit, `/CL_Start` Continue, `/CL_Vehicle` Continue — all retry-passable. Retry the test before investigating; only dig in if the same field fails 3+ times in a row.

## Page identifiers

| Page | URL fragment | `PageIdentifier` | Page-object class |
|---|---|---|---|
| Start (Business Contact) | `/CL_Start` | `_Start` | `Product_StartPage` |
| Markets | `/MarketResults` | `MarketResults` | `Product_MarketsPage` |
| Business | `/CL_Business` | `_Business` | `Product_BusinessPage` |
| Vehicle | `/CL_Vehicle` | `_Vehicle` | `Product_VehiclePage` |
| Operator | `/CL_Operator` | `_Operator` | `Product_OperatorPage` |
| Policy (CL) | `/CL_Policy` | `CL_Policy` | `Product_CLPolicyPage` |
| Applicant | `/CL_Applicant` | `_Applicant` | `Product_ApplicantPage` |
| Results | (varies) | `Results` | `Product_ResultsPage` |

Notes:

- `_Start` is shared with BOLTAG personal-line `Start` — that reuse is intentional.
- CL Policy uses the **unique** identifier `CL_Policy` (not `_Policy`) because the PL `Product_PolicyPage` also uses `_Policy`, and a substring of `_Policy` would match both. CL needed its own page class for the floated-label / mat-datepicker overrides described above.

## Cross-references

[INDEX.md](INDEX.md), [../lob/cl-auto.md](../lob/cl-auto.md), [../lob/bop.md](../lob/bop.md).
