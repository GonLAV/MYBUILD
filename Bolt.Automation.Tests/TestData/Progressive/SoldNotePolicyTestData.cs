using Bolt.Automation.Common.Enums;
using Bolt.Automation.TestDataProvider.TestData.SoldNoteTestData;
using NUnit.Framework;


namespace Bolt.Automation.Tests.TestData.Progressive;

/// <summary>
/// Test data and expected validation messages for Sold Note policy number validation.
/// </summary>
public static class SoldNotePolicyTestData
{
    // ── Sold Note form defaults ──────────────────────────────────────────────────

    public const string DefaultProduct = "Homeowners";
    public const string DefaultPremium = "1000";

    // ── Policy number generators ──────────────────────────────────────────────────

    /// <summary>
    /// Returns <paramref name="count"/> random decimal digits. Called at test execution
    /// time (not discovery time), so <see cref="Random.Shared"/> is safe to use here.
    /// </summary>
    public static string RandomDigits(int count)
        => string.Concat(Enumerable.Range(0, count).Select(_ => Random.Shared.Next(0, 10)));

    // ── NUnit TestCaseSource data ─────────────────────────────────────────────────

    /// <summary>
    /// One test case per carrier. Each case provides a fixed prefix and digit count;
    /// the test method appends <see cref="RandomDigits"/> at execution time so the
    /// policy number varies per run while test IDs stay stable across discovery passes.
    /// </summary>
    public static IEnumerable<TestCaseData> InvalidFormatCases
    {
        get
        {
            // AGH + 7 digits required; "XYZ" prefix is always wrong.
            yield return new TestCaseData("American Integrity", "XYZ", 6, SoldNotePolicyErrors.AmericanIntegrity, AddressKey.FL)
                .SetProperty("TestCaseId", "243380")
                .SetDescription("American Integrity — invalid prefix (not AGH + 7 digits)");

            // 9 digits required; 3 digits is always too short.
            yield return new TestCaseData("American Modern Insurance Group", "", 3, SoldNotePolicyErrors.AMIG, AddressKey.AZ_Tucson)
                .SetProperty("TestCaseId", "243381")
                .SetDescription("AMIG — too short (not 9 digits)");

            // 3 letters + numbers, min 7 chars required; "AB" + 2 digits = 4 chars is always too short.
            yield return new TestCaseData("ASI (American Strategic Insurance)", "AB", 2, SoldNotePolicyErrors.ASI, AddressKey.AZ_Tucson)
                .SetProperty("TestCaseId", "243382")
                .SetDescription("ASI — too short (not 3 letters + numbers, min 7 chars)");

            // PSM + 7 digits required; "ABC" prefix is always wrong.
            yield return new TestCaseData("Assurant Inc. Group", "ABC", 6, SoldNotePolicyErrors.Assurant, AddressKey.TN)
                .SetProperty("TestCaseId", "243383")
                .SetDescription("Assurant — invalid prefix (not PSM + 7 digits)");

            // 12 digits required; 10 digits is always too short.
            yield return new TestCaseData("Foremost Insurance Company", "", 10, SoldNotePolicyErrors.Foremost, AddressKey.AZ_Tucson)
                .SetProperty("TestCaseId", "243379")
                .SetDescription("Foremost — 10 digits (should be 12)");

            // 8 digits required; 7 digits is always too short.
            yield return new TestCaseData("Hippo", "", 7, SoldNotePolicyErrors.Hippo, AddressKey.FL)
                .SetProperty("TestCaseId", "243378")
                .SetDescription("Hippo — 7 digits (should be 8)");

            // 8 digits required; 7 digits is always too short.
            yield return new TestCaseData("Homesite Insurance", "", 7, SoldNotePolicyErrors.Homesite, AddressKey.OH)
                .SetProperty("TestCaseId", "243377")
                .SetDescription("Homesite — 7 digits (should be 8)");

            // 12 digits required; 8 digits is always too short.
            yield return new TestCaseData("National General Insurance", "", 8, SoldNotePolicyErrors.NationalGeneral, AddressKey.FL)
                .SetProperty("TestCaseId", "243384")
                .SetDescription("National General — 8 digits (should be 12)");

            // 10 alphanumeric chars required; 8 digits is always too short.
            yield return new TestCaseData("Nationwide Mutual Insurance Company", "", 8, SoldNotePolicyErrors.Nationwide, AddressKey.UT)
                .SetProperty("TestCaseId", "243385")
                .SetDescription("Nationwide — 8 digits (should be 10 alphanumeric)");

            // BQ01/TCPN/TRPN + 5-7 letters required; digits after the dash are always invalid.
            yield return new TestCaseData("Openly", "BQ01-", 5, SoldNotePolicyErrors.Openly, AddressKey.AZ_Tucson)
                .SetProperty("TestCaseId", "243386")
                .SetDescription("Openly — numeric suffix (needs 5–7 alpha-only letters after dash)");

            // 2 letters + 9 digits (11 chars) required; "AB" + 8 digits = 10 chars is always too short.
            yield return new TestCaseData("PURE", "AB", 8, SoldNotePolicyErrors.Pure, AddressKey.NY_Averill_Park)
                .SetProperty("TestCaseId", "243387")
                .SetDescription("PURE — 10 chars (should be 2 letters + 9 digits = 11)");

            // PUP + 7 digits (10 chars) required; "PUP" + 6 digits = 9 chars is always too short.
            yield return new TestCaseData("RLI Corporation", "PUP", 6, SoldNotePolicyErrors.RLI, AddressKey.FL)
                .SetProperty("TestCaseId", "243390")
                .SetDescription("RLI — 9 chars (should be PUP + 7 digits = 10)");

            // Letter + 9 digits required; digits-only is always missing the leading letter.
            yield return new TestCaseData("Tower Hill", "", 8, SoldNotePolicyErrors.TowerHill, AddressKey.FL)
                .SetProperty("TestCaseId", "243388")
                .SetDescription("Tower Hill — no leading letter");

            // State(2 letters) + LOB(1 letter) + 10 digits = 14 chars required.
            // "FL0" uses digit '0' as the LOB character — always invalid for LOB.
            yield return new TestCaseData("TRUE", "FL0", 11, SoldNotePolicyErrors.True, AddressKey.FL)
                .SetProperty("TestCaseId", "243389")
                .SetDescription("TRUE — digit LOB code '0' (should be a letter, e.g. FL031234567890 → FLO...)");
        }
    }

}
