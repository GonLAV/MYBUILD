using System.Globalization;

namespace Bolt.Automation.TestDataProvider.TestData.D2CFarmers
{
    /// <summary>
    /// Expected field behaviour for the Farmers / Bristol West D2C 2.0 pages — one nested class per
    /// page, mirroring the page-by-page test cases. Holds only what the app is expected to do
    /// (messages, limits, value-in/value-out pairs); the assertions themselves stay in the tests.
    /// Page composition — selectors, labels and stacking order — needs the field registry and the
    /// layout helpers, so it lives beside them in the test project instead.
    /// </summary>
    public static class D2CFarmersExpectedData
    {
        /// <summary>Page 1 — "Let's get you started with a quote".</summary>
        public static class QuoteStart
        {
            // The app moved off the one-size-fits-all "This field is required" message — each field
            // now names itself in its own required-message.
            public const string FirstNameRequiredError = "Please provide your first name";
            public const string LastNameRequiredError = "Please provide your last name";
            public const string DateOfBirthRequiredError = "Please provide your date of birth";
            public const int FirstNameMaxLength = 50;
            public const int LastNameMaxLength = 30;
            public const string ValidLastName = "Smith-Garcia";
            public const string ValidDateOfBirth = "09/08/1999";

            private const string OverMaxFirstName = "QualityAssuranceTestDataValidationScenarioNameVxAlpha";
            private const string OverMaxLastName = "GarciaHernandezWilliamsonBrightmore";

            /// <summary>What each name field keeps when typed past its cap — truncated at the max length.</summary>
            public static readonly (string Typed, string Kept) FirstNameTruncationCase =
                (OverMaxFirstName, OverMaxFirstName[..FirstNameMaxLength]);

            public static readonly (string Typed, string Kept) LastNameTruncationCase =
                (OverMaxLastName, OverMaxLastName[..LastNameMaxLength]);

            /// <summary>First Name values that must raise a validation error, and the message each raises.</summary>
            public static readonly (string Value, string ExpectedError)[] FirstNameErrorCases =
            [
                ("", FirstNameRequiredError),                         // left empty
                ("   ", FirstNameRequiredError),                      // spaces only
                ("A", "First Name must exceed 1 character")           // single character
            ];

            /// <summary>Last Name values that must raise a validation error, and the message each raises.</summary>
            public static readonly (string Value, string ExpectedError)[] LastNameErrorCases =
            [
                ("", LastNameRequiredError),                          // left empty
                ("   ", LastNameRequiredError),                       // spaces only
                ("A", "Last Name must exceed 1 character")            // single character
            ];

            /// <summary>What First Name keeps for each typed value — rejection and filtering. Truncation lives in <see cref="FirstNameTruncationCase"/>.</summary>
            public static readonly (string Typed, string Kept)[] FirstNameValueCases =
            [
                ("12345", ""),                      // numeric only — rejected
                ("!@#$", ""),                       // special characters — rejected
                ("John123", "John"),                // digits stripped
                ("Anna-Maria", "Anna-Maria"),       // hyphen allowed
                ("Anna Maria", "Anna Maria"),       // space allowed
                ("O'Connor", "O'Connor")            // apostrophe allowed
            ];

            /// <summary>
            /// Date of birth values that must raise a validation error. The future and under-16 dates
            /// are derived from today so the cases never go stale; the impossible calendar date is literal.
            /// </summary>
            public static (string Value, string ExpectedError)[] DateOfBirthErrorCases =>
            [
                ("", DateOfBirthRequiredError),
                (ToDateValue(DateTime.Today.AddYears(5)), "Date of Birth must be in the past"),
                ("02/30/1990", "Date must be a valid date in the following format: MM/DD/YYYY"),
                (ToDateValue(DateTime.Today.AddYears(-10)), "Drivers can't be under the age of 16.")
            ];

            // "/" in a custom format string means "the culture's date separator", so the dates the app
            // is fed would change shape on a machine with a non-US culture. Invariant pins them to MM/DD/YYYY.
            private static string ToDateValue(DateTime date) => date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
        }
    }
}
