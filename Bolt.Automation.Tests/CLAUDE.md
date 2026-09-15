# Bolt.Automation.Tests — CLAUDE.md

The main test project. Tests **orchestrate**; implementation logic lives in page objects, API clients, and helpers.

## Base classes

- **TestBase** (API): provides `IScopeContext`, `IAutomationLogger`, `TestContextAccessor`; auto-detects tenant from the `[Tenant]` attribute and creates a scoped service provider per test.
- **UITestBase** (UI): extends TestBase with Playwright (`BrowserManager`, `PageFactory`, `PageHelper`).

## Test organization

```csharp
[Test]
[Tenant(USAA)]         // tenant for test-data resolution (mandatory for tenant-specific tests)
[Category("SSO")]      // category for filtering
[TestCaseId(212329)]   // Azure DevOps test case ID
[Author(Author.Helen)] // REQUIRED on every test — who owns it
public async Task MyTest() { }
```

Categories: ADBX, API, CRM, D2C, devops, FullQuote, GetQuoteApi, PartnerPortal, Payment, Quoting, Sanity, SSO.

## Conventions

- Inherit `TestBase` (API) or `UITestBase` (UI).
- **Always** specify `[Tenant]` for tenant-specific tests.
- **Every test carries `[Author(Author.<name>)]`** — a required attribute (values in the `Author` enum). If the owner is unknown, ask rather than omit it. In a file that also `using NUnit.Framework;`, add `using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;` — otherwise `[Author(Author.X)]` is ambiguous with NUnit's own `AuthorAttribute` (CS0104).
- Tests stay clean: high-level step orchestration + business validations only. **No logging in test bodies** — log in the implementation classes the test calls. See `philosophy/tests-stay-clean.md`.
- API responses: validate with `.EnsureSuccessContent()`.

## NUnit Parameterized Test Conventions

When writing parameterized NUnit tests, follow these rules to ensure proper discovery and distributed execution by the orchestrator:

### Use `TestCaseSource` + `TestCaseData` (preferred)

```csharp
[Test]
[Tenant(Tenant.BOLTAG)]
[Category("MyCategory")]
[TestCaseSource(nameof(MyTestData))]
public async Task MyTest(CarrierEnums carrier, AddressKey address) { }

private static IEnumerable<TestCaseData> MyTestData()
{
    yield return new TestCaseData(CarrierEnums.Bamboo, AddressKey.CA)
        .SetProperty("TestCaseId", "228954"); // string, never int — see warning below
}
```

### Use only NUnit-serializable types as TestCaseData arguments

**Allowed:** `string`, `int`, `double`, `bool`, `enum` — these produce unique, serializable FQNs.

**Forbidden as arguments:**
- `bool?` (nullable value types) — `null` cannot be serialized into FQN
- `string[]`, `List<T>`, arrays — `.ToString()` produces `System.String[]`, non-unique FQN
- Complex objects — produce type name instead of value in FQN

When a test needs a nullable or collection parameter, pass it as a `string` and parse inside the test:

```csharp
// WRONG — NUnit can't generate unique FQNs for these:
[TestCase(null, "expected")]           // bool? null
[TestCase(new[] { "a", "b" }, "...")]  // string[] array

// RIGHT — use string representation, parse in test method:
private static IEnumerable<TestCaseData> MyTestData()
{
    yield return new TestCaseData("null", "expected")    // string "null"
        .SetProperty("TestCaseId", "123");
    yield return new TestCaseData("a,b,c", "expected")   // comma-delimited
        .SetProperty("TestCaseId", "456");
}

public async Task MyTest(string nullableBoolStr, string expected)
{
    bool? value = nullableBoolStr == "null" ? null : bool.Parse(nullableBoolStr);
}
```

### Use `.SetProperty("TestCaseId", id)` for per-variant test case IDs

Do NOT pass TestCaseId as a method parameter. Use `.SetProperty()` on `TestCaseData` — consistent with the dominant pattern and keeps the method signature clean.

**The value MUST be a string** (`.SetProperty("TestCaseId", "228954")`, never an int). NUnit stores property values verbatim: an int value makes the orchestrator's discovery drop the id (the test shows no TestCaseId and blocks job creation), and NUnit's `PropertyFilter` throws at runtime for **every** test in a `TestCaseId=`-filtered run — which is how all orchestrator work items execute.

### Do not use `.SetName()` on `TestCaseData`

A renamed case cannot be matched by the orchestrator's discovery, which then copies a neighbouring test's `TestCaseId` onto it — the dashboard reports a false "TestCaseId N is shared by 3 tests" (VAL002) and the real ids vanish from the manifest. Keep the NUnit-generated name and pass **short keys** as arguments (`("HO3", "TX")`), resolving the full data inside the test. Short arguments are also what keeps the name within the 260-character Windows path limit that the artifact capture nests it into — which is what `SetName` used to be reached for.

### Inline `[TestCase]` is acceptable for simple all-string/all-primitive cases

```csharp
// OK — all strings, NUnit generates proper FQNs:
[TestCase("UsersRelayState", "301", "150825")]
[TestCase("DummyAdbxRelayState", "203", "200973")]
public async Task SsoKickoutTest(string relayState, string errorCode, string testCaseId) { }
```

### Why this matters

The orchestrator's discovery tool uses `dotnet vstest --ListFullyQualifiedTests` to get canonical FQNs. When NUnit can't serialize parameter values, the test appears as a **single non-parameterized FQN** — variants can't be split across workers or retried individually.

### Injected-parameter pattern (Professional Services, multi-axis: state x LOB)

Professional Services / runtime-injected tests (new tenants/environments, `INJECTED_*` env vars, `InjectionTestBase`/`UIInjectionTestBase`, multi-axis fan-out via `[InjectedParameter]`) have their own KB leaf — `kb lookup --topic framework:injection-tests` or read `Documentation/agent-knowledge/framework/injection-tests.md` directly.

A test may carry **one `[InjectedParameter]` marker per fan-out axis** (e.g. `INJECTED_PS_STATE` + `INJECTED_PS_LOB`); the orchestrator runs the cross-product of the values chosen for each. Adding an axis means adding its env var to `InjectedTestConfig` **and** an entry to `InjectedParameterCatalog` (`TestExtension/Helpers/`) — that catalog is the one type the orchestrator's discovery tool reflects for parameter values, so an axis missing from it gets no picker. `INJECTED_PS_STATE` is the exception: it predates the catalog and keeps its own `AddressData.SupportedStateInputs()` path, so a new state needs nothing here. See the KB leaf's "Adding a new fan-out axis".

## Deeper context

- `kb lookup --topic framework:test-class` — base-class mechanics + test-data system.
- Agent skills: `nexus-test-author` (implement a TC end-to-end), `nexus-test-review` (review against siblings + philosophy).
