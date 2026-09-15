# Interview Test Suite — Runtime Baseline

**ADO work item:** [254521](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/254521) — "Measure the Interview tests per test and per page, and rank what costs the most time"
**Date:** 2026-09-11 (data collected 2026-09-07)
**Branch / commit:** `update-categories` — same commit across all six runs
**Runs:** 3× UAT (`job-mL2GtiYw`, `job-RMUUeU7c`, `job-P0BpS990`) + 3× Staging (`job-MiaeAlzc`, `job-obpt7k87`, `job-jTwCQDBy`), filtered `Category=Interview`.
QA was dropped: the environment is unstable and does not produce representative timing data.

---

## Methodology (acceptance point 4)

**Per-page timing was derived from the `logs` collection. No framework instrumentation was added.**

The two options in the task description were (a) add one step per page to the flow, or (b) derive page times from the logs collection. Option (b) was used, because the framework already emits a `Page ready: <Page Name> (Nms)` line with a millisecond timestamp on every page transition, which is enough to reconstruct the whole timeline without touching production test code.

Definitions used throughout:

| Term | Definition |
|---|---|
| **Test duration** | `durationMs` from `nexus-logger` `get_test_history`, keyed by ADO test case ID. |
| **Page-ready time** | The `(Nms)` value inside `Page ready: X (Nms)` — how long the framework blocked waiting for that page to validate and render. |
| **Time spent on a page** | Interval between this page's `Page ready` timestamp and the **next** page's `Page ready` timestamp. This is the number in the "Time on page" column: it covers the field-filling on that page plus whatever the app does before the next page appears. |
| **Share of test total** | Time spent on page ÷ test `durationMs`. |

Every per-page table below reconciles to 99.9–100.1 % of the recorded test duration, so nothing is unaccounted for.

Data sources: `get_test_history` (per-run durations), `get_test_logs` with `category=General` (page timeline), `category=UiAction` (per-field actions and ng-select counts), `category=Exception` (failure attribution), and direct reads of `ElementInteractionHelper.cs`, `FormDataHelper.cs` and `Product_ResultsPage.cs` for the fixed-sleep constants.

**Representative runs** used for page/phase detail: `job-mL2GtiYw` (UAT) and `job-MiaeAlzc` (Staging). Run-to-run spread is low (see §1), so a single representative run is sound for structure; where a number varies materially between runs it is called out.

### Two corrections to the earlier draft of this document

1. **ng-select fill counts were estimated, not measured, and were wrong.** The earlier figure of "~18 ng-select fills per test, ~30 s of fixed sleep" was derived from the proportion of ng-select registrations in the registry (119/339 ≈ 35 %), not from run logs. Measured counts range from **0 to 26 per test**. A CL BOP test does 4; a PL Homeowners test does 26. See §5.
2. **The fixed sleep per ng-select fill is 1400 ms, not ~1500 ms.** `HandleNgSelectDropdown` contains exactly four `Task.Delay` calls — 200 + 500 + 200 + 500 — and the remaining waits on that path are real element waits, not sleeps.

---

## 1. Baseline table — three runs per test, in seconds, with median (acceptance point 1)

All durations in seconds. `F` marks a failed run. Median is the middle of the three runs as recorded, regardless of outcome; where outcomes are mixed, the passing-run figures are noted underneath.

### Staging — `job-MiaeAlzc` / `job-obpt7k87` / `job-jTwCQDBy`

| Test | TC | Run 1 | Run 2 | Run 3 | **Median** |
|---|---|---|---|---|---|
| UNIFY_SalesEnvironment_PL_NewAccount_HO3_TX | [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328) | 342.8 | 327.0 | 326.8 | **327.0** |
| UNIFY_SalesEnvironment_CL_BOP_PolicyBinder_E2E | [253208](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253208) | 316.8 | 315.3 | 317.5 | **316.8** |
| UNIFY_SalesEnvironment_PL_NewAccount_HO6_AZ | [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) | 301.8 | 307.9 | 320.2 | **307.9** |
| UNIFY_SalesEnvironment_CL_BOP_NewAccount_To_Rates_E2E | [253181](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253181) | 289.2 | 289.4 | 289.9 | **289.4** |
| UNIFY_SalesEnvironment_CL_WC_ExistingAccount_ToPolicyBinder_E2E | [253185](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253185) | 183.4 | 182.8 | 184.8 | **183.4** |
| UNIFY_SalesEnvironment_CL_WC_NewAccount_To_Rates_E2E | [253222](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253222) | 177.5 | 174.9 | 176.7 | **176.7** |
| UNIFY_SalesEnvironment_CL_Auto_NewAccount_To_Rates_E2E | [253224](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253224) | 170.3 | 178.6 | 169.0 | **170.3** |
| UNIFY_PL_Home_ConsumerApi_To_AgentRates_E2E | [110599](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/110599) | 110.5 | 84.1 `F` | 109.5 | **109.5** |
| KLX_CLAuto_E2E_SubmitToRates | [240782](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/240782) | 53.5 `F` | 53.3 `F` | 53.3 `F` | **53.3** |
| UNIFY_PL_Renters_FQ_Payment_E2E | [123432](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/123432) | 9.9 `F` | 7.7 `F` | 7.5 `F` | **7.7** |

[110599](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/110599) passing runs only: 110.5 and 109.5 → 110.0 s.

### UAT — `job-mL2GtiYw` / `job-RMUUeU7c` / `job-P0BpS990`

| Test | TC | Run 1 | Run 2 | Run 3 | **Median** |
|---|---|---|---|---|---|
| BOLTAG_TCPA_Interview_Receive_Email_By_Bolt_No_Test | [231402](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/231402) | 394.4 `F` | 393.8 `F` | 232.8 `F` | **393.8** |
| BOLTACCESS_CL_BOP_Admitted_Carriers_Request_Application | [236071](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236071) | 300.2 | 295.6 | 292.9 | **295.6** |
| BOLTACCESS_CL_GL_Acord_Carriers | [237268](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237268) | 276.5 `F` | 274.9 `F` | 275.8 `F` | **275.8** |
| BOLTACCESS_CL_CLAuto_Progressive_StateNotSupported_OfflineRequest | [237500](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237500) | 172.6 | 182.5 | 179.7 | **179.7** |
| BOLTACCESS_CL_CLAuto_Progressive_Offline_Request | [237634](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237634) | 173.7 | 172.3 | 182.2 | **173.7** |
| BOLTACCESS_CL_BOP_Additional_Carriers_Offline_Request | [236042](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236042) | 170.7 | 169.4 | 169.4 | **169.4** |
| BOLTAG_CL_Consumer_WC_E2E_Test | [133598](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/133598) | 124.7 `F` | 123.1 `F` | 117.2 `F` | **123.1** |
| KLX_CL_BOP_ResultsPage_ApplicationForms_Download | [235689](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/235689) | 120.4 | 130.5 | 121.7 | **121.7** |
| UNIFY_PL_Home_ConsumerApi_To_AgentRates_E2E | [110599](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/110599) | 97.1 | 118.5 | 96.2 | **97.1** |
| BOLTACCESS_CL_BOP_MarketsAvailability_OfflineRequest | [235799](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/235799) | 68.3 | 69.9 | 66.9 | **68.3** |
| COMPARION_PL_Auto_CT_RuleEngine_E2E | [223497](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/223497) | 41.2 `F` | 40.1 `F` | 39.8 `F` | **40.1** |
| KLX_CLAuto_E2E_SubmitToRates | [240782](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/240782) | 34.5 `F` | 34.5 `F` | 36.0 `F` | **34.5** |
| UNIFY_PL_Renters_FQ_Payment_E2E | [123432](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/123432) | 27.6 | 27.2 | 27.5 | **27.5** |
| KLX_CL_BOP_MarketsPage_OfflineRequest | [237437](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237437) | 19.5 | 24.3 `F` | 26.9 `F` | **24.3** |
| Unify_D2C_Invalid_VIN_Validation | [239923](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/239923) | 3.4 `F` | 3.3 `F` | 3.4 `F` | **3.4** |

**Run-to-run stability.** Excluding [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328) (spread 16.0 s) and [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) (spread 18.4 s), every passing test's three runs fall within 3–10 s of each other — typically under 4 % of the test duration. Flake is therefore small relative to the structural costs identified below, and the rankings in §6 are not an artefact of run selection. The two PL tests are the exception, and their spread is driven entirely by the carrier-rating wait (§6, contributor 8).

**Data-quality flag resolved.** [229271](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/229271) `UNIFY_PL_Homeowners_BambooCarriers_E2E` appears in none of the six runs. It is **not** silently excluded by the orchestrator: `get_test_history` shows it executing on 2026-09-11 in run `L-06653` with outcome `Skipped — "Test skipped: Not configured to run in Staging environment"`. It is environment-gated and simply has no environment configured among UAT/Staging. It needs a tenant/environment configuration decision, not an orchestrator investigation.

---

## 2. Per-page tables (acceptance point 2)

One table per test. "Page-ready" is the render/validation wait for that page; "Time on page" is dwell until the next page is ready; "Share" is of the test total.

### Staging — `job-MiaeAlzc`

#### UNIFY_SalesEnvironment_PL_NewAccount_HO3_TX — [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328) — PASS — 342.8 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup, browser launch, login nav)* | — | 1.5 | 0.4 % |
| STS Login | 0.04 | 1.4 | 0.4 % |
| ADBX Home (New Quote + account modal) | 0.76 | 15.0 | 4.4 % |
| **Start** | 1.99 | **66.9** | **19.5 %** |
| Markets | 1.72 | 4.3 | 1.3 % |
| Home | 3.27 | 26.2 | 7.6 % |
| Structure | 3.79 | 24.6 | 7.2 % |
| Features | 6.33 | 19.3 | 5.6 % |
| **Policy** | 4.36 | **60.4** | **17.6 %** |
| **Applicant** | 3.80 | **84.1** | **24.5 %** |
| Results (check rates) | 15.74 | 39.2 | 11.4 % |
| **Total** | **41.8** | **342.8** | **100 %** |

#### UNIFY_SalesEnvironment_CL_BOP_PolicyBinder_E2E — [253208](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253208) — PASS — 316.8 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 1.7 | 0.5 % |
| STS Login | 0.05 | 1.8 | 0.6 % |
| ADBX Home | 0.86 | 25.4 | 8.0 % |
| **Business Profile** | 16.33 | **40.1** | **12.7 %** |
| Product Selection | 0.98 | 2.8 | 0.9 % |
| Market Results | 1.65 | 2.7 | 0.9 % |
| Insurance History | 1.66 | 9.2 | 2.9 % |
| Locations | 1.70 | 4.4 | 1.4 % |
| **Employee** | 1.60 | **46.2** | **14.6 %** |
| Owners and Officers | 1.98 | 25.9 | 8.2 % |
| Owners and Officers *(retry)* | 1.94 | 3.0 | 1.0 % |
| Market Selections | 1.73 | 2.5 | 0.8 % |
| **CL Additional Questions — Travelers** | 0.83 | **109.0** | **34.4 %** |
| Results (rating) | 7.67 | 13.1 | 4.1 % |
| Quote Summary (Sold note + binder) | 1.00 | 29.0 | 9.1 % |
| **Total** | **40.0** | **316.8** | **100 %** |

#### UNIFY_SalesEnvironment_PL_NewAccount_HO6_AZ — [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) — PASS — 301.8 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 1.5 | 0.5 % |
| STS Login | 0.05 | 1.1 | 0.4 % |
| ADBX Home (New Quote + account modal) | 0.62 | 13.6 | 4.5 % |
| **Start** | 1.27 | **66.8** | **22.1 %** |
| Markets | 1.58 | 3.6 | 1.2 % |
| Home | 2.56 | 25.6 | 8.5 % |
| Structure | 3.28 | 19.6 | 6.5 % |
| Features | 3.15 | 18.3 | 6.1 % |
| **Policy** | 2.88 | **55.7** | **18.4 %** |
| **Applicant** | 3.04 | **49.8** | **16.5 %** |
| Results (check rates) | 7.84 | 46.4 | 15.4 % |
| **Total** | **26.3** | **301.8** | **100 %** |

#### UNIFY_SalesEnvironment_CL_BOP_NewAccount_To_Rates_E2E — [253181](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253181) — PASS — 289.2 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 2.6 | 0.9 % |
| STS Login | 0.05 | 1.5 | 0.5 % |
| ADBX Home | 0.84 | 25.4 | 8.8 % |
| **Business Profile** | 16.28 | **40.4** | **14.0 %** |
| Product Selection | 1.40 | 2.5 | 0.9 % |
| Market Results | 1.38 | 2.7 | 0.9 % |
| Insurance History | 1.65 | 9.5 | 3.3 % |
| Locations | 1.58 | 4.5 | 1.6 % |
| **Employee** | 1.69 | **45.9** | **15.9 %** |
| Owners and Officers | 1.69 | 25.7 | 8.9 % |
| Owners and Officers *(retry)* | 1.74 | 2.8 | 1.0 % |
| Market Selections | 1.50 | 2.5 | 0.9 % |
| **CL Additional Questions — Travelers** | 0.86 | **109.6** | **37.9 %** |
| Results (rating + ACORD) | 8.27 | 13.4 | 4.6 % |
| **Total** | **38.9** | **289.2** | **100 %** |

#### UNIFY_SalesEnvironment_CL_WC_ExistingAccount_ToPolicyBinder_E2E — [253185](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253185) — PASS — 183.4 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 1.6 | 0.8 % |
| STS Login | 0.04 | 1.5 | 0.8 % |
| ADBX Home | 0.83 | 2.3 | 1.2 % |
| Accounts Grid | 1.39 | 4.2 | 2.3 % |
| Accounts Summary | 0.01 | 5.7 | 3.1 % |
| **Business Profile** | 4.51 | **43.3** | **23.6 %** |
| Product Selection | 2.04 | 2.6 | 1.4 % |
| Market Results | 1.49 | 2.5 | 1.4 % |
| Insurance History | 1.46 | 15.4 | 8.4 % |
| **Employee** | 1.72 | **22.3** | **12.2 %** |
| Owners and Officers | 2.08 | 17.6 | 9.6 % |
| Owners and Officers *(retry)* | 2.08 | 4.2 | 2.3 % |
| Market Selections | 2.09 | 11.2 | 6.1 % |
| CL Additional Questions — Travelers | 0.96 | 13.4 | 7.3 % |
| Results (rating) | 8.66 | 5.0 | 2.7 % |
| **Quote Summary (Sold note)** | 0.91 | **30.5** | **16.7 %** |
| ADBX Policy Summary | 0.01 | 0.2 | 0.1 % |
| **Total** | **30.3** | **183.4** | **100 %** |

#### UNIFY_SalesEnvironment_CL_WC_NewAccount_To_Rates_E2E — [253222](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253222) — PASS — 177.5 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 2.0 | 1.1 % |
| STS Login | 0.05 | 1.3 | 0.7 % |
| ADBX Home | 0.87 | 24.3 | 13.7 % |
| **Business Profile** | 15.25 | **40.4** | **22.8 %** |
| Product Selection | 1.24 | 2.3 | 1.3 % |
| Market Results | 1.17 | 3.3 | 1.9 % |
| Insurance History | 1.69 | 15.6 | 8.8 % |
| **Employee** | 1.86 | **22.5** | **12.7 %** |
| Owners and Officers | 2.31 | 17.3 | 9.7 % |
| Owners and Officers *(retry)* | 1.85 | 3.9 | 2.2 % |
| Market Selections | 1.82 | 2.7 | 1.5 % |
| CL Additional Questions — CNA | 1.21 | 3.5 | 2.0 % |
| CL Additional Questions — Employers | 2.38 | 3.6 | 2.0 % |
| CL Additional Questions — Liberty Mutual | 2.43 | 8.8 | 5.0 % |
| CL Additional Questions — Travelers | 2.72 | 7.5 | 4.2 % |
| ACORD 130 — Operations and Risk Exposure | 2.82 | 12.1 | 6.8 % |
| Results (rating) | 10.90 | 6.3 | 3.5 % |
| **Total** | **50.6** | **177.5** | **100 %** |

#### UNIFY_SalesEnvironment_CL_Auto_NewAccount_To_Rates_E2E — [253224](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253224) — PASS — 170.3 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 1.8 | 1.1 % |
| STS Login | 0.04 | 1.9 | 1.1 % |
| ADBX Home | 1.05 | 25.4 | 14.9 % |
| **Business Profile** | 16.24 | **40.3** | **23.7 %** |
| Product Selection | 1.34 | 2.6 | 1.5 % |
| Market Results | 1.49 | 2.9 | 1.7 % |
| Insurance History | 1.81 | 13.2 | 7.7 % |
| Applicants And Drivers | 1.57 | 7.6 | 4.5 % |
| **Commercial Vehicles** | 1.91 | **30.5** | **17.9 %** |
| Commercial Auto Coverages | 2.05 | 19.3 | 11.3 % |
| Market Selections | 1.77 | 12.5 | 7.3 % |
| Results (rating) | 10.83 | 12.2 | 7.2 % |
| **Total** | **40.1** | **170.3** | **100 %** |

### UAT — `job-mL2GtiYw`

#### BOLTACCESS_CL_BOP_Admitted_Carriers_Request_Application — [236071](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236071) — PASS — 300.2 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 3.2 | 1.1 % |
| STS Login | 0.11 | 2.9 | 1.0 % |
| ADBX Home | 2.11 | 4.3 | 1.4 % |
| Accounts Grid | 1.38 | 3.6 | 1.2 % |
| Accounts Summary | 0.01 | 6.7 | 2.2 % |
| **Business Profile** | 0.02 | **40.8** | **13.6 %** |
| Product Selection | 0.84 | 2.0 | 0.7 % |
| Market Results | 0.83 | 1.9 | 0.6 % |
| Insurance History | 0.80 | 7.7 | 2.6 % |
| Locations | 0.79 | 3.6 | 1.2 % |
| **Employee** | 0.83 | **45.1** | **15.0 %** |
| Owners and Officers | 0.84 | 25.0 | 8.3 % |
| Owners and Officers *(retry)* | 0.91 | 2.1 | 0.7 % |
| Market Selections | 0.79 | 8.8 | 2.9 % |
| **CL Additional Questions — Travelers** | 0.19 | **107.6** | **35.9 %** |
| Results (poll + Request Application) | 6.27 | 34.9 | 11.6 % |
| **Total** | **16.7** | **300.2** | **100 %** |

#### BOLTACCESS_CL_CLAuto_Progressive_StateNotSupported_OfflineRequest — [237500](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237500) — PASS — 172.6 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 2.0 | 1.2 % |
| STS Login | 0.07 | 1.9 | 1.1 % |
| ADBX Home | 1.12 | 2.9 | 1.7 % |
| Accounts Grid | 1.49 | 3.4 | 2.0 % |
| Accounts Summary | 0.02 | 6.7 | 3.9 % |
| **Business Profile** | 0.02 | **38.0** | **22.0 %** |
| Product Selection | 0.81 | 2.6 | 1.5 % |
| Market Results | 1.44 | 1.9 | 1.1 % |
| Insurance History | 0.81 | 12.4 | 7.2 % |
| Applicants And Drivers | 1.03 | 5.8 | 3.3 % |
| **Commercial Vehicles** | 0.96 | **29.3** | **17.0 %** |
| Commercial Auto Coverages | 1.10 | 20.2 | 11.7 % |
| Market Selections | 0.81 | 9.7 | 5.6 % |
| Results (tab checks) | 8.02 | 10.6 | 6.1 % |
| Block Bind Operator | 1.82 | 2.1 | 1.2 % |
| Block Bind Vehicle | 2.07 | 9.3 | 5.4 % |
| Results (offline request) | 6.76 | 13.8 | 8.0 % |
| **Total** | **28.4** | **172.6** | **100 %** |

#### BOLTACCESS_CL_CLAuto_Progressive_Offline_Request — [237634](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237634) — PASS — 173.7 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 3.7 | 2.1 % |
| STS Login | 0.11 | 2.8 | 1.6 % |
| ADBX Home | 1.77 | 4.1 | 2.4 % |
| Accounts Grid | 1.00 | 3.6 | 2.1 % |
| Accounts Summary | 0.02 | 6.7 | 3.8 % |
| **Business Profile** | 0.03 | **38.4** | **22.1 %** |
| Product Selection | 0.92 | 2.0 | 1.2 % |
| Market Results | 0.93 | 1.9 | 1.1 % |
| Insurance History | 0.80 | 12.4 | 7.1 % |
| Applicants And Drivers | 0.99 | 5.7 | 3.3 % |
| **Commercial Vehicles** | 0.92 | **29.2** | **16.8 %** |
| Commercial Auto Coverages | 0.98 | 18.3 | 10.6 % |
| Market Selections | 0.89 | 10.8 | 6.2 % |
| Results | 9.16 | 8.4 | 4.9 % |
| Block Bind Operator | 1.80 | 2.2 | 1.3 % |
| Block Bind Vehicle | 2.12 | 9.5 | 5.5 % |
| Results (offline request) | 6.91 | 14.0 | 8.0 % |
| **Total** | **29.4** | **173.7** | **100 %** |

#### BOLTACCESS_CL_BOP_Additional_Carriers_Offline_Request — [236042](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236042) — PASS — 170.7 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 3.0 | 1.7 % |
| STS Login | 0.10 | 2.2 | 1.3 % |
| ADBX Home | 1.34 | 3.1 | 1.8 % |
| Accounts Grid | 1.32 | 3.4 | 2.0 % |
| Accounts Summary | 0.04 | 6.6 | 3.9 % |
| **Business Profile** | 0.02 | **40.1** | **23.5 %** |
| Product Selection | 0.81 | 1.9 | 1.1 % |
| Market Results | 0.82 | 1.9 | 1.1 % |
| Insurance History | 0.80 | 7.5 | 4.4 % |
| Locations | 0.79 | 3.6 | 2.1 % |
| **Employee** | 0.80 | **45.1** | **26.4 %** |
| Owners and Officers | 0.84 | 24.7 | 14.5 % |
| Owners and Officers *(retry)* | 0.80 | 2.0 | 1.2 % |
| Market Selections | 0.79 | 7.6 | 4.5 % |
| Results (offline request) | 5.79 | 17.9 | 10.5 % |
| **Total** | **15.1** | **170.7** | **100 %** |

#### KLX_CL_BOP_ResultsPage_ApplicationForms_Download — [235689](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/235689) — PASS — 120.4 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(API token + EnterQuote navigation)* | — | 8.8 | 7.3 % |
| Business Profile | 3.86 | 11.8 | 9.8 % |
| Product Selection | 0.79 | 1.9 | 1.6 % |
| Market Results | 0.79 | 1.8 | 1.5 % |
| Insurance History | 0.79 | 7.6 | 6.3 % |
| Locations | 0.80 | 3.6 | 3.0 % |
| **Employee** | 0.80 | **45.4** | **37.7 %** |
| Owners and Officers | 1.16 | 24.7 | 20.5 % |
| Owners and Officers *(retry)* | 0.85 | 2.1 | 1.7 % |
| Market Selections | 0.80 | 7.5 | 6.3 % |
| Results (ACORD download) | 5.71 | 5.2 | 4.3 % |
| **Total** | **16.3** | **120.4** | **100 %** |

#### UNIFY_PL_Home_ConsumerApi_To_AgentRates_E2E — [110599](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/110599) — PASS — 97.1 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| ***(GetQuote API create + submit + poll — no browser)*** | — | **55.5** | **57.2 %** |
| STS Login | 0.04 | 1.2 | 1.2 % |
| ADBX Home | 0.75 | 1.4 | 1.4 % |
| Quote Summary | 0.01 | 4.9 | 5.0 % |
| Results (first load) | 4.58 | 6.2 | 6.4 % |
| Applicant (edit phone) | 2.07 | 18.4 | 19.0 % |
| Results (re-rate) | 18.16 | 9.5 | 9.8 % |
| **Total** | **25.6** | **97.1** | **100 %** |

#### BOLTACCESS_CL_BOP_MarketsAvailability_OfflineRequest — [235799](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/235799) — PASS — 68.3 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(setup / login nav)* | — | 3.2 | 4.7 % |
| STS Login | 0.09 | 2.7 | 4.0 % |
| ADBX Home | 2.00 | 3.8 | 5.5 % |
| Accounts Grid | 0.97 | 3.5 | 5.2 % |
| Accounts Summary | 0.02 | 6.7 | 9.8 % |
| **Business Profile** | 0.02 | **39.3** | **57.6 %** |
| Product Selection | 0.87 | 1.9 | 2.8 % |
| Market Results (offline request) | 0.81 | 7.1 | 10.4 % |
| **Total** | **4.8** | **68.3** | **100 %** |

#### UNIFY_PL_Renters_FQ_Payment_E2E — [123432](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/123432) — PASS — 27.6 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| ***(API: create app, select plan, get redirect URL)*** | — | **19.4** | **70.1 %** |
| lemonade Payment Integration | 0.04 | 8.3 | 29.9 % |
| **Total** | **0.04** | **27.6** | **100 %** |

#### KLX_CL_BOP_MarketsPage_OfflineRequest — [237437](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237437) — PASS — 19.5 s

| Page | Page-ready (s) | Time on page (s) | Share |
|---|---|---|---|
| *(API token + EnterQuote navigation)* | — | 7.1 | 36.5 % |
| Business Profile | 2.25 | 2.6 | 13.6 % |
| Product Selection | 0.94 | 1.9 | 9.6 % |
| Market Results (offline request) | 0.80 | 7.9 | 40.4 % |
| **Total** | **4.0** | **19.5** | **100 %** |

---

## 3. Per-test phase split (acceptance point 3)

Four phases, all in seconds. Definitions:

- **API setup** — windows with no browser page-ready activity: GetQuote/Interview API application creation, submit-and-poll, token/EnterQuote navigation, plus browser launch and DB connectivity check.
- **Navigation** — sum of all `Page ready (Nms)` values **excluding** the Results page (counted under rates), i.e. render/validation waits on page transitions.
- **Form filling** — the remainder: field interactions plus the per-field backend round-trips that occur between them. Computed as `Total − API − Navigation − Rates`.
- **Waiting for rates** — the Results-page render wait plus explicit carrier-rating and carrier-appearance polling windows.

| TC | Env | Total | API setup | Navigation | Form filling | Waiting for rates |
|---|---|---|---|---|---|---|
| [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328) | Staging | 342.8 | 1.5 | 26.0 | **266.7** | 48.6 |
| [253208](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253208) | Staging | 316.8 | 1.7 | 32.3 | **267.1** | 15.7 |
| [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) | Staging | 301.8 | 1.5 | 18.4 | **234.5** | 47.4 |
| [236071](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236071) | UAT | 300.2 | 3.2 | 10.4 | **254.3** | 32.3 |
| [253181](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253181) | Staging | 289.2 | 2.6 | 30.6 | **239.7** | 16.3 |
| [253185](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253185) | Staging | 183.4 | 1.6 | 21.6 | **147.5** | 12.7 |
| [237500](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237500) | UAT | 172.6 | 2.0 | 13.6 | **142.2** | 14.8 |
| [253222](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253222) | Staging | 177.5 | 2.0 | 39.7 | **120.9** | 14.9 |
| [237634](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237634) | UAT | 173.7 | 3.7 | 13.3 | **140.6** | 16.1 |
| [253224](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253224) | Staging | 170.3 | 1.8 | 29.3 | **120.4** | 18.8 |
| [236042](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236042) | UAT | 170.7 | 3.0 | 9.3 | **148.6** | 9.8 |
| [235689](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/235689) | UAT | 120.4 | 8.8 | 10.6 | **95.3** | 5.7 |
| [110599](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/110599) | UAT | 97.1 | **55.5** | 2.9 | 16.0 | 22.7 |
| [235799](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/235799) | UAT | 68.3 | 3.2 | 4.8 | **60.3** | 0.0 |
| [123432](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/123432) | UAT | 27.6 | **19.4** | 0.04 | 8.2 | 0.0 |
| [237437](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237437) | UAT | 19.5 | 7.1 | 4.0 | 8.4 | 0.0 |
| **Suite (one run)** | | **2931.9** | **118.6** | **266.8** | **2270.7** | **275.8** |
| **Share** | | 100 % | 4.0 % | 9.1 % | **77.4 %** | 9.4 % |

**Reading of this table.** Form filling dominates at 77 % of suite runtime, but almost none of that is Playwright typing — field interactions themselves are logged 70–120 ms apart. The bulk is the dead time *between* consecutive field fills, which is where the large gaps in §6 live. Navigation (9.1 %) and rate-waiting (9.4 %) are each an order of magnitude smaller. API setup is negligible except for the two API-seeded tests, where it is by construction the whole test.

---

## 4. Failures excluded from the ranking

These are excluded from §2/§3/§6 because a failed run short-circuits and its duration does not represent real page traversal. They are real signal and are reported here with the live medians from §1.

| Test | TC | Env | Median | Root cause |
|---|---|---|---|---|
| BOLTAG_TCPA_Interview_Receive_Email_By_Bolt_No_Test | [231402](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/231402) | UAT | 393.8 s | Results-page timeout after Applicant; 4 required fields unanswered (IsMailAddress, AnyAdditionalInsured, InsuranceFraud, IAgreeToReceiveEmailsByBolt). **Largest single CI time sink in the suite.** |
| BOLTACCESS_CL_GL_Acord_Carriers | [237268](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237268) | UAT | 275.8 s | Stuck on `CL_AdditionalQuestions_LibertyMutual_CP`; required "cost of subcontracted work" unanswered, page never advances. Real bug. |
| BOLTAG_CL_Consumer_WC_E2E_Test | [133598](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/133598) | UAT | 123.1 s | Locations-page timeout; 2 required fields unanswered (AnnualPayroll, NumberOfVehicles). |
| KLX_CLAuto_E2E_SubmitToRates | [240782](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/240782) | Staging | 53.3 s | Real navigation bug — stuck at `CL_Start`, never reaches Business Profile. |
| KLX_CLAuto_E2E_SubmitToRates | [240782](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/240782) | UAT | 34.5 s | Test-account password expired. Not a framework issue. |
| COMPARION_PL_Auto_CT_RuleEngine_E2E | [223497](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/223497) | UAT | 40.1 s | Upstream ADBX kickout, error code 203. Not a framework issue. |
| UNIFY_PL_Renters_FQ_Payment_E2E | [123432](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/123432) | Staging | 7.7 s | 404 "State/product is not in appetite" — Staging state configuration. |
| Unify_D2C_Invalid_VIN_Validation | [239923](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/239923) | UAT | 3.4 s | API null-ref before any UI. |
| KLX_CL_BOP_MarketsPage_OfflineRequest | [237437](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237437) | UAT | 24.3 s | Flaky, 2/3 fail — offline-request button does not flip to "Request Submitted". |
| UNIFY_PL_Home_ConsumerApi_To_AgentRates_E2E | [110599](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/110599) | Staging | — | Flaky, 1/3 fail — "Rates are not displayed on results page". |

The ranking in §6 covers the 16 passing test/environment combinations, not all 22 tests, because failures are broken out here.

---

## 5. ng-select fills and fixed sleeps (acceptance point 5)

**Registry composition.** `FieldRegistryInterview.cs` has **339** `Locators =` registrations, of which **119 (35.1 %)** target an `ng-select`. Dispatch is via [`ElementInteractionHelper.SelectDropdown`](../Bolt.Automation.FrontEnds/PlaywrightBase/Helpers/ElementInteractionHelper.cs) → `HandleNgSelectDropdown`.

**Fixed sleep per fill, from the code, not estimated:**

| Handler | Fixed `Task.Delay` calls | Total fixed sleep |
|---|---|---|
| `HandleNgSelectDropdown` | 200 + 500 + 200 + 500 ms | **1400 ms** |
| `HandleSelectDropdown` (plain `<select>`) | 200 + 500 ms | **700 ms** |
| `WaitForElementToDisappearAsync`, element already gone | 2 × 500 ms retry delay | **1000 ms** |
| `Product_ResultsPage.ClickMarketCategoryTab` / `EnsureTabActive` | 1000 ms each | **1000 ms** per tab click |

Everything else on the ng-select path (`OpenNgSelectPanel`, panel visible/hidden waits, option click) is a real element wait, not a sleep.

**Measured fills per test.** Counted from `category=UiAction` logs — every `[Select]`/`[SearchSelect]` action against an `ng-select` locator, including failed attempts.

| TC | Test | ng-select fills | Fixed sleep (s) | Share of test |
|---|---|---|---|---|
| [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328) | PL HO3 TX | **26** *(measured)* | 36.4 | 10.6 % |
| [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) | PL HO6 AZ | **21** *(measured)* | 29.4 | 9.7 % |
| [253224](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253224) | CL Auto | **18** *(measured)* | 25.2 | 14.8 % |
| [237500](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237500) | CL Auto (UAT) | 18 *(same flow)* | 25.2 | 14.6 % |
| [237634](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237634) | CL Auto (UAT) | 18 *(same flow)* | 25.2 | 14.5 % |
| [253222](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253222) | CL WC | **6** *(measured)* | 8.4 | 4.7 % |
| [253185](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253185) | CL WC | 6 *(same flow)* | 8.4 | 4.6 % |
| [253181](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253181) | CL BOP | **4** *(measured)* | 5.6 | 1.9 % |
| [253208](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253208) | CL BOP | 4 *(same flow)* | 5.6 | 1.8 % |
| [236042](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236042) | CL BOP (UAT) | 4 *(same flow)* | 5.6 | 3.3 % |
| [236071](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236071) | CL BOP (UAT) | 4 *(same flow)* | 5.6 | 1.9 % |
| [235689](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/235689) | CL BOP (KLX) | 4 *(same flow)* | 5.6 | 4.7 % |
| [235799](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/235799) | BOP, stops at Markets | 1 *(same flow)* | 1.4 | 2.0 % |
| [110599](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/110599) | PL via API | **0** *(measured)* | 0.0 | 0 % |
| [123432](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/123432) | Renters payment | 0 | 0.0 | 0 % |
| [237437](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237437) | BOP markets page | 0 | 0.0 | 0 % |
| | **Suite total (one run)** | **~134** | **187.6 s** | **6.4 %** |

Counts marked *(measured)* were taken directly from that test's own UiAction log. Counts marked *(same flow)* are carried from the measured test that walks the identical flow and page set; these were not separately instrumented and are the one estimated figure in this document.

**No `Thread.Sleep` violations** exist anywhere in `Bolt.Automation.FrontEnds` or the Interview tests — the hard rule holds.

**Verdict on ng-select.** 187.6 s per suite run is real and worth removing, but it is 6.4 % of runtime, and the distribution is skewed: it matters for the two PL tests (~10 % each) and the three CL Auto tests (~15 % each), and is near-irrelevant for the CL BOP tests (~2 %) that sit at the top of the cost ranking. It is a secondary target.

---

## 6. Ranked time contributors, with named cause (acceptance point 6)

Each entry is classified as one of **fixed sleep**, **poll ceiling**, **real backend wait**, or **retry**. "Cost per run" is seconds per full suite run (all affected tests summed); "per occurrence" is per affected test.

| # | Contributor | Cause | Per occurrence | Tests affected | **Cost per run** |
|---|---|---|---|---|---|
| 1 | **CL Additional Questions — Travelers: building-detail fields** (ReplacementCost → YearOriginalConstruction → YearRoof → SquareFootage → SquareFootageOccupied → NumberOfStories) — 6 consecutive fills, 15.5–21 s apart each, nothing logged between | **real backend wait** | ~108 s | [253181](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253181), [253208](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253208), [236071](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236071) | **325.6 s** |
| 2 | **Business Profile → Product Selection transition** — industry set ~2 s in, then 33–38 s of silence before Product Selection is ready | **real backend wait** | 38–43 s | 9 CL tests | **353.6 s** |
| 3 | **Employee page dwell** — 26 s before the first FullTime/PartTime click, then a further ~16 s before the payroll fill | **real backend wait** | 45 s (BOP) / 22 s (WC) | 7 tests | **272.5 s** |
| 4 | **PL Start page** — 62 s between the last logged field fill and the post-fill validation warning, with zero log lines in between | **real backend wait** *(see caveat)* | ~62 s | [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328), [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) | **124.0 s** |
| 5 | **PL Policy page** — first fill pass leaves 24–27 fields invalid; `RetryInvalidFieldsAsync` re-fills 12–15 of them, including 5 ng-selects | **retry** | 9–12 s | [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328), [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) | **20.6 s** |
| 6 | **ng-select fixed sleep** — 1400 ms × fills | **fixed sleep** | 0–36.4 s | 13 tests | **187.6 s** |
| 7 | **Owners & Officers `TitleRelationshipCustomDropdown` retry** — 3 s locator timeout, 3 click retries against an element that intercepts pointer events, failure screenshot (~1.8 s), then full page re-validation (visible as a duplicate `Page ready: Owners and Officers`) | **retry** | ~10.3 s | 7 tests | **72.1 s** |
| 8 | **Post-Results settle** — `Page ready: Results` → `Page ready: Results (5ms)` measured at 4.005–4.029 s on **18 separate occasions across both environments**. The ±12 ms invariance proves this is framework-fixed, not backend-dependent | **fixed sleep** | 4.01 s × 1–2 | 13 tests | **72.2 s** |
| 9 | **Commercial Vehicles year-dropdown retry** — the `YearVehicleDropdownWithSearch` ng-select fails, screenshots, and recovers ~10.7 s later | **retry** | ~10.7 s | [253224](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253224), [237500](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237500), [237634](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237634) | **32.1 s** |
| 10 | **Carrier rating wait, fast path** — logged at 8.000 / 8.006 / 7.998 s in three independent CL tests. The invariance indicates a ~2 s poll grid resolving on the 4th interval, not a true 8 s backend wait | **poll ceiling** | 8.0 s | [253181](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253181), [253208](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253208), [253224](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253224) | **24.0 s** |
| 11 | **Carrier rating wait, slow path** — 39.5 s ([252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361)) and 12.9 s + 20.0 s carrier-render ([252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328)), with an explicit framework warning that the tab declared 6 carriers but only 4 rendered within 20 s | **real backend wait** | 33–40 s | [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328), [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) | **72.4 s** |
| 12 | **PL Applicant page: YearsAtAddress → MonthsAtAddress → EmploymentIndustry → OccupationStr** — 15.5 s between the two address fills in both PL tests; the EmploymentIndustry → OccupationStr dependent-dropdown reload took 29.1 s in [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328) but only 2.4 s in [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) | **real backend wait** (flaky) | 17.9–44.6 s | [252328](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252328), [252361](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/252361) | **62.5 s** |
| 13 | **`WaitForCarrierToAppear` polling** — 2000 ms grid, 13 polls of "Found 0 carriers" before Travelers appears | **poll ceiling** | 26.0 s total, ≤2 s wasted | [236071](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/236071) | **26.0 s** (≤2 s avoidable) |
| 14 | **"Record a Sold note"** — 28.5–30.5 s on the Quote Summary page | **real backend wait** | ~29.5 s | [253185](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253185), [253208](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/253208) | **59.0 s** |
| 15 | **GetQuote API submit + poll** — application creation, submission and ~5 s-interval polling, no browser involved | **poll ceiling** | 55.5 s | [110599](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/110599) | **55.5 s** |
| 16 | **Plain `<select>` fixed sleep** — 700 ms on the ADBX account-source dropdown | **fixed sleep** | 0.7 s | 8 tests | **5.6 s** |

**Caveat on contributor 4.** The 62 s PL Start-page gap has **no log coverage at all**: the last `[Fill]` is at 15:42:04.024 and the next line is the validation warning at 15:43:06.549, with no exceptions logged in between (`category=Exception` returns zero entries for this test) and Debug-level logging not persisted by the worker. `ProcessField` swallows and logs field failures, so the absence of exception entries rules out per-field locator timeouts. The most likely explanation is the address-autocomplete resolution followed by conditional re-render of the YearsAtAddress/MonthsAtAddress/PropertyAddress fields — which is why those three appear in the "cannot retry" list — but this is inference, not measurement. **This is the single largest unattributable block in the suite (124 s/run) and the top instrumentation priority.**

---

## 7. Framework time vs Bolt backend time (acceptance point 7)

"Framework time" means time the framework spends by construction regardless of how fast Bolt responds — fixed sleeps, poll-interval granularity, and retry overhead. "Bolt backend time" means time the framework is blocked waiting for the application or API to produce something.

Basis: one full suite run, the 16 passing test/environment combinations, **2931.9 s** total.

### Framework time

| Item | Cause | Seconds/run |
|---|---|---|
| ng-select fixed sleeps (1400 ms × ~134 fills) | fixed sleep | 187.6 |
| Post-Results settle (4.01 s × 18 occurrences, invariant) | fixed sleep | 72.2 |
| Owners & Officers `TitleRelationship` retry cycle | retry | 72.1 |
| Commercial Vehicles year-dropdown retry | retry | 32.1 |
| Carrier-rating 8 s poll floor (3 CL tests) | poll ceiling | 24.0 |
| PL Policy-page invalid-field re-fill pass | retry | 20.6 |
| Plain `<select>` fixed sleeps (700 ms × 8) | fixed sleep | 5.6 |
| `WaitForCarrierToAppear` 2 s grid overshoot | poll ceiling | ≤2.0 |
| **Framework total** | | **≈ 416 s** |

### Bolt backend time

| Item | Seconds/run |
|---|---|
| Business Profile → Product Selection transition | 353.6 |
| CL Travelers building-detail field saves | 325.6 |
| Employee page saves | 272.5 |
| PL Start page (unattributed — see §6 caveat) | 124.0 |
| Carrier rating, slow path | 72.4 |
| PL Applicant dependent-dropdown reloads | 62.5 |
| "Record a Sold note" | 59.0 |
| GetQuote API submit + poll | 55.5 |
| Page render/validation waits (Σ page-ready, non-Results) | 266.8 |
| All remaining inter-field and inter-page waits | ~924 |
| **Backend total** | **≈ 2516 s** |

### Totals

| | Seconds/run | Share |
|---|---|---|
| **Framework** | **416** | **14.2 %** |
| **Bolt backend** | **2516** | **85.8 %** |
| **Total** | **2932** | 100 % |

**Conclusion.** Roughly **six sevenths of Interview suite runtime is spent waiting on the Bolt application**, not inside the framework. Removing every fixed sleep and every retry in the framework would cut a full suite run from ~2932 s to ~2516 s — a 14 % improvement. The four largest single line items (contributors 1–4 in §6, totalling ~1076 s, or 37 % of the whole suite) are all backend-side, and three of them are per-field save latency on forms the tests must fill regardless.

---

## 8. Optimisation proposal (acceptance point 8)

Each item lists the estimated saving per suite run, the effort, and the risk. Nothing here has been actioned.

| # | Optimisation | Saving/run | Effort | Risk |
|---|---|---|---|---|
| 1 | **Instrument the gaps before optimising them.** Add a per-field timing log line (field name, elapsed ms) in `FormDataHelper.ProcessField`, and a per-page step boundary in the flow executor. Without it, 124 s/run (contributor 4) and much of contributors 1–3 cannot be attributed to a specific call. | 0 s directly; unlocks ~1076 s of targeted work | **S** — one log line in `ProcessField`, one in the flow executor | **Low** — additive logging only; watch log volume at 200 entries/call in `get_test_logs` |
| 2 | **Investigate the Business-Profile → Product-Selection transition** (353.6 s/run). Reproducible to ±1 s across 9 tests, 2 environments and 6 runs. Almost certainly a single backend call (account/industry save, or product-appetite resolution). Needs a platform-team trace, not a framework change. | up to 300 s if halved | **M** — cross-team; requires backend tracing | **Low** for the test suite; the fix lands in the product |
| 3 | **Investigate per-field autosave on CL building-detail and Employee forms** (598 s/run combined). Fields save one at a time at 15–21 s each. If the app debounces or batches these saves, both contributors collapse. | up to 450 s if batched | **M** — product change | **Medium** — changes app save semantics; needs its own regression pass |
| 4 | **Fix the Owners & Officers `TitleRelationshipCustomDropdown` locator** (72.1 s/run). The failure is deterministic: a `control-container … disabled` div intercepts pointer events, so all 3 click retries fail, a screenshot is taken, and the page is re-validated. Either wait for the control to become enabled or target the inner element. | ~72 s | **S** — one registry entry plus a wait condition | **Low** — currently always fails and recovers, so a fix can only reduce work |
| 5 | **Remove the 4.01 s post-Results settle** (72.2 s/run). Measured invariant at 4.005–4.029 s across 18 occurrences. Replace the fixed component with a real condition on the loader/rates being present. | ~60 s | **S** — `Product_ResultsPage` | **Medium** — the settle currently masks a race; removing it without a proper wait condition would cause flake on the Results page |
| 6 | **Cut ng-select fixed sleep from 1400 ms to ~400 ms** (187.6 s/run at present). Replace the four `Task.Delay` calls in `HandleNgSelectDropdown` with waits on the panel's actual open/closed state — the method already awaits `.ng-dropdown-panel:visible` and its hidden state, so the pre/post delays are largely redundant. | ~130 s | **M** — `ElementInteractionHelper`, affects every tenant and flow, not just Interview | **Medium–High** — this is the single hottest shared code path in the framework; needs a full multi-tenant regression run before merge |
| 7 | **Fix the Commercial Vehicles year-dropdown** (32.1 s/run) — same class of problem as #4, in the vehicle-year searchable dropdown. | ~32 s | **S** | **Low** |
| 8 | **Tighten the carrier-rating poll from 2000 ms to 500 ms** (up to 26 s/run of granularity waste, ~24 s of which is the 8 s floor in 3 CL tests). | ~20 s | **S** — `Product_ResultsPage.WaitForCarrierToAppear` and the rating wait | **Low** — more polls, slightly more DOM reads; no semantic change |
| 9 | **Reduce the PL Policy-page invalid-field re-fill** (20.6 s/run). 24–27 fields report invalid after the first pass and 12–15 are re-filled. Ordering the fill pass so dependent fields are filled after their parents would cut most of the retry. | ~15 s | **M** — `GetOrderedFields` ordering rules per page | **Medium** — reordering field fills can change which conditional fields appear; needs per-tenant verification |
| 10 | **Fix or quarantine the four consistently-failing tests** — [231402](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/231402) (393.8 s), [237268](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/237268) (275.8 s), [133598](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/133598) (123.1 s), [240782](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/240782) (53.3 s Staging + 34.5 s UAT). They burn **881 s per run** producing no signal beyond "still broken", most of it in 122 s Results-page navigation timeouts. | **881 s** | **M** — four separate root causes, three of them real product bugs | **Low** if fixed; quarantining trades coverage for time and should be time-boxed |
| 11 | **Resolve [229271](https://azure.devops.boltx.us/BoltCollection/Epos/_workitems/edit/229271)'s environment gating** — it is configured for no environment the suite runs in, so it never executes. | 0 s | **S** | **Low** — adds runtime once enabled |

### Suggested order

1. **Item 10 first** — 881 s/run, no framework risk, and it is the largest single win available. Three of the four are real bugs worth filing regardless of runtime.
2. **Item 1** — cheap, and everything from item 2 onward is guesswork without it.
3. **Items 4 and 7** — deterministic locator failures, ~104 s/run, low risk, small diffs.
4. **Items 2 and 3** — the real prize (~750 s/run) but they are product-side and need platform-team involvement.
5. **Items 5, 6, 8, 9** — framework sleeps and retries, ~225 s/run combined; item 6 carries the widest blast radius and should go last, behind a full multi-tenant regression.

Items 1, 4, 5, 7, 8 and 9 together are worth roughly **330 s/run** and are entirely inside this repository. Items 2, 3 and 10 are worth roughly **1630 s/run** and are not.
