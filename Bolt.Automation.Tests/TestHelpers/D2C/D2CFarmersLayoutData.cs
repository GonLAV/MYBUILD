using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.Tests.TestHelpers.Layout;

namespace Bolt.Automation.Tests.TestHelpers.D2C
{
    /// <summary>
    /// Expected page composition for the Farmers / Bristol West D2C 2.0 pages — one nested class per
    /// page. This stays in the test project because it is addressed through the field registry
    /// (<see cref="FieldNames"/>) and read by the layout helpers; the value-level expectations live in
    /// <c>Bolt.Automation.TestDataProvider.TestData.D2CFarmers.D2CFarmersExpectedData</c>.
    /// </summary>
    internal static class D2CFarmersLayoutData
    {
        /// <summary>
        /// Page 1 composition — the furniture around the three fields, and the text it must show.
        /// These are page-level elements rather than form fields, so they are addressed by CSS
        /// selector instead of through the field registry.
        /// </summary>
        internal static class QuoteStartLayout
        {
            public const string Header = "h1";
            public const string SubHeader = "p.subtitle";
            public const string DateOfBirthFormatHint = ".optional-text .text";
            public const string PersonalInformationLink = "a.info-link";
            public const string PrivacyPolicyLink = ".disclaimer a";
            public const string AgreeButton = "button[type=\"button\"][data-automation-element=\"next-button-element\"]";
            public const string VehicleImage = "img.car";
            public const string Footer = "footer.footer";

            public const string HeaderText = "Let's get you started with a quote";
            public const string SubHeaderText = "Get started with just a few personal details.";
            public const string DateOfBirthFormatHintText = "mm/dd/yyyy";
            public const string PersonalInformationLinkText = "Personal information use";
            public const string PrivacyPolicyLinkText = "Privacy Policy";
            public const string AgreeButtonText = "Agree";

            /// <summary>
            /// The three fields in the order the page must present them, and the label each shows.
            /// The app renders these as floating labels rather than placeholder attributes, so the
            /// label is what the user reads inside an empty field.
            /// </summary>
            public static readonly (string FieldName, string LabelSelector, string LabelText)[] FieldsInOrder =
            [
                (FieldNames.FirstName,   "label[for='FirstName']",   "First name"),
                (FieldNames.LastName,    "label[for='LastName']",    "Last name"),
                (FieldNames.DateOfBirth, "label[for='DateOfBirth']", "Date of birth")
            ];

            /// <summary>
            /// The order the page must read in, top down — the furniture with the field labels
            /// standing in for the fields themselves. Declared once here so the test asserts the
            /// whole composition in a single call instead of pair by pair.
            /// </summary>
            private static readonly string[] StackedTopToBottom =
            [
                Header,
                SubHeader,
                ..FieldsInOrder.Select(f => f.LabelSelector),
                DateOfBirthFormatHint,
                PersonalInformationLink,
                AgreeButton
            ];

            /// <summary>
            /// The whole page composition in one declaration — what the shared composition check reads.
            /// Every part of it is derived from the selectors and texts above, so nothing is stated twice.
            /// </summary>
            public static readonly PageComposition Composition = new()
            {
                PageName = "Farmers Quote Start page",
                Texts = BuildExpectedTexts(),
                StackedTopToBottom = StackedTopToBottom,
                FieldsInOrder = [.. FieldsInOrder.Select(f => f.FieldName)]
            };

            /// <summary>
            /// Where both privacy links must take the user. Stated without a trailing slash, which the
            /// site adds and the test case does not care about — the comparison trims it off either
            /// side. Everything else is matched whole, so a link pointing anywhere but the Farmers
            /// privacy statement fails rather than passing on a partial match.
            /// </summary>
            public const string PrivacyStatementUrl = "https://www.farmers.com/privacy-statement";

            /// <summary>
            /// The title the Farmers privacy statement must come up under — the expected result the
            /// test case states for the tab each link opens. Note the space before the colon: that is
            /// how the Farmers page titles itself.
            /// </summary>
            public const string PrivacyStatementTitle = "Privacy Statement : Farmers Insurance";

            private static Dictionary<string, string> BuildExpectedTexts()
            {
                var texts = new Dictionary<string, string>
                {
                    [Header] = HeaderText,
                    [SubHeader] = SubHeaderText,
                    [DateOfBirthFormatHint] = DateOfBirthFormatHintText,
                    [PersonalInformationLink] = PersonalInformationLinkText,
                    [AgreeButton] = AgreeButtonText
                };

                foreach (var (_, labelSelector, labelText) in FieldsInOrder)
                    texts[labelSelector] = labelText;

                return texts;
            }
        }
    }
}
