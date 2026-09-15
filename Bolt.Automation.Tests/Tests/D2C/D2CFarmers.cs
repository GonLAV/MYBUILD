using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestHelpers.Layout;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.Tests.TestHelpers.D2C.D2CFarmersLayoutData;
using static Bolt.Automation.TestDataProvider.TestData.D2CFarmers.D2CFarmersExpectedData;

namespace Bolt.Automation.Tests.Tests.D2C
{
    public class D2CFarmers : D2CTestBase
    {
        private const string QuoteStartUrl =
            "https://d2cfarmers-qa.boltqa.com/farmers/enter-info?tenant=BOLTAG&type=pl&v=d2cfarmers";

        [Test]
        [Ignore("D2C Farmers is currently not relevant")]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(UNIFY)]
        [Category("D2CFarmers")]
        [Category("D2C")]
        [TestCaseId(250107)]
        [Description("Verify First Name, Last Name max length and Date of birth validation rules on the Farmers Quote Start page")]
        public async Task UNIFY_D2CFarmers_Enter_Info_Page_Fields_Validation()
        {
            await _logger.ExecuteStepAsync("Open the Farmers Quote Start page", async () =>
            {
                await BrowserManager.NavigateAsync(QuoteStartUrl);
                PageFactory.CreatePage<D2CFarmers_EnterInfoPage>();
            });

            await _logger.ExecuteStepAsync("Cases 2, 3 & 8 — First Name values that raise a validation error", async () =>
            {
                foreach (var (value, expectedError) in QuoteStart.FirstNameErrorCases)
                {
                    var error = await _fieldValidation.EnterValueAndGetError(FieldNames.FirstName, value);
                    Assert.That(error, Is.EqualTo(expectedError), $"First Name [{value}] should raise its validation error");
                }
            });

            await _logger.ExecuteStepAsync("Cases 4-6 & 9-11 — the value First Name keeps for each input", async () =>
            {
                foreach (var (typed, expectedKept) in QuoteStart.FirstNameValueCases)
                {
                    var kept = await _fieldValidation.EnterValueAndLeaveField(FieldNames.FirstName, typed);
                    Assert.That(kept, Is.EqualTo(expectedKept), $"First Name given [{typed}] should keep [{expectedKept}], but kept [{kept}]");
                }
            });

            await _logger.ExecuteStepAsync("Cases 7 & 12 — both name fields cap input at their max length", async () =>
            {
                var (typedFirstName, expectedFirstName) = QuoteStart.FirstNameTruncationCase;
                var (typedLastName, expectedLastName) = QuoteStart.LastNameTruncationCase;
                var keptFirstName = await _fieldValidation.EnterValueAndLeaveField(FieldNames.FirstName, typedFirstName);
                var keptLastName = await _fieldValidation.EnterValueAndLeaveField(FieldNames.LastName, typedLastName);

                Assert.Multiple(() =>
                {
                    Assert.That(keptFirstName, Is.EqualTo(expectedFirstName),
                        $"First Name given {typedFirstName.Length} characters should keep only the first {QuoteStart.FirstNameMaxLength} [{expectedFirstName}], but kept [{keptFirstName}]");
                    Assert.That(keptLastName, Is.EqualTo(expectedLastName),
                        $"Last Name given {typedLastName.Length} characters should keep only the first {QuoteStart.LastNameMaxLength} [{expectedLastName}], but kept [{keptLastName}]");
                });
            });

            await _logger.ExecuteStepAsync("Last Name values that raise a validation error", async () =>
            {
                foreach (var (value, expectedError) in QuoteStart.LastNameErrorCases)
                {
                    var error = await _fieldValidation.EnterValueAndGetError(FieldNames.LastName, value);
                    Assert.That(error, Is.EqualTo(expectedError), $"Last Name [{value}] should raise its validation error");
                }
            });

            await _logger.ExecuteStepAsync("Give Last Name a valid value before moving to Date of birth", async () =>
            {
                var kept = await _fieldValidation.EnterValueAndLeaveField(FieldNames.LastName, QuoteStart.ValidLastName);
                Assert.That(kept, Is.EqualTo(QuoteStart.ValidLastName), $"Last Name should accept a valid name unchanged, but kept [{kept}]");
            });

            await _logger.ExecuteStepAsync("Cases 13-16 — Date of birth values that raise a validation error", async () =>
            {
                foreach (var (value, expectedError) in QuoteStart.DateOfBirthErrorCases)  

                {
                    var error = await _fieldValidation.EnterValueAndGetError(FieldNames.DateOfBirth, value);
                    Assert.That(error, Is.EqualTo(expectedError), $"Date of birth [{value}] should raise its validation error");
                }
            });

            await _logger.ExecuteStepAsync("Case 17 — a valid Date of birth is accepted", async () =>
            {
                var kept = await _fieldValidation.EnterValueAndLeaveField(FieldNames.DateOfBirth, QuoteStart.ValidDateOfBirth);
                var error = await _fieldValidation.GetFieldError(FieldNames.DateOfBirth);

                Assert.Multiple(() =>
                {
                    Assert.That(kept, Is.EqualTo(QuoteStart.ValidDateOfBirth), $"Date of birth should accept [{QuoteStart.ValidDateOfBirth}] unchanged, but kept [{kept}]");
                    Assert.That(error, Is.Empty, $"Date of birth should raise no validation error for [{QuoteStart.ValidDateOfBirth}], got [{error}]");
                });
            });
        }

        [Test]
        [Ignore("D2C Farmers is currently not relevant")]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(UNIFY)]
        [Category("D2CFarmers")]
        [Category("D2C")]
        [TestCaseId(250103)]
        [Description("Verify both privacy links on the Farmers Quote Start page — Privacy Policy and Personal information use — each open the Farmers privacy statement in a new tab")]
        public async Task UNIFY_D2CFarmers_Enter_Info_Privacy_Links()
        {
            await _logger.ExecuteStepAsync("Step 1 — the Quote Start page loads", async () =>
            {
                await BrowserManager.NavigateAsync(QuoteStartUrl);
                PageFactory.CreatePage<D2CFarmers_EnterInfoPage>();
            });

            await _logger.ExecuteStepAsync("Steps 2 & 3 — the Privacy Policy link opens the Farmers privacy statement in a new tab",
                () => ShouldOpenPrivacyStatementInNewTab(QuoteStartLayout.PrivacyPolicyLink, QuoteStartLayout.PrivacyPolicyLinkText));

            await _logger.ExecuteStepAsync("Steps 4 & 5 — the Personal information use link opens the Farmers privacy statement in a new tab",
                () => ShouldOpenPrivacyStatementInNewTab(QuoteStartLayout.PersonalInformationLink, QuoteStartLayout.PersonalInformationLinkText));
        }

        private async Task ShouldOpenPrivacyStatementInNewTab(string linkSelector, string linkName)
        {
            var text = await _layout.GetText(linkSelector);
            Assert.That(text, Is.EqualTo(linkName),
                $"[{linkSelector}] should be the {linkName} link, but reads [{text}]");

            var tab = await _links.OpenInNewTab(linkSelector);
            Assert.That(tab, Is.Not.Null, $"Clicking the {linkName} link should open a new tab");

            Assert.Multiple(() =>
            {
                Assert.That(tab!.Url.TrimEnd('/'), Is.EqualTo(QuoteStartLayout.PrivacyStatementUrl),
                    $"The tab the {linkName} link opened should land on [{QuoteStartLayout.PrivacyStatementUrl}], landed on [{tab.Url}]");
                Assert.That(tab.Title, Is.EqualTo(QuoteStartLayout.PrivacyStatementTitle),
                    $"The tab the {linkName} link opened should be titled [{QuoteStartLayout.PrivacyStatementTitle}], but was titled [{tab.Title}] at {tab.Url}");
            });
        }

        [Test]
        [Ignore("D2C Farmers is currently not relevant")]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(UNIFY)]
        [Category("D2CFarmers")]
        [Category("D2C")]
        [TestCaseId(250054)]
        [Description("Verify the Quote Start page composition: header and sub-header text, the three fields in order with their labels, the date format hint, the personal information link, the Agree button and the vehicle image placement")]
        public async Task UNIFY_D2CFarmers_Enter_Info_Page_Layout()
        {
            await _logger.ExecuteStepAsync("Step 1 — the Quote Start page loads", async () =>
            {
                await BrowserManager.NavigateAsync(QuoteStartUrl);
                PageFactory.CreatePage<D2CFarmers_EnterInfoPage>();
            });

            await _logger.ExecuteStepAsync("Steps 2-8 — headers, the three fields and their labels, the date hint, the link and the Agree button", async () =>
                await _layout.ShouldRender(QuoteStartLayout.Composition));

            await _logger.ExecuteStepAsync("Step 9 — the vehicle image sits right of the Agree button and above the footer", async () =>
            {
                var vehicle = await _layout.GetBox(QuoteStartLayout.VehicleImage);
                var button = await _layout.GetBox(QuoteStartLayout.AgreeButton);
                var footer = await _layout.GetBox(QuoteStartLayout.Footer);
                Assert.That(vehicle, Is.Not.Null, "The vehicle image should be displayed");
                Assert.That(button, Is.Not.Null, "The Agree button should be displayed");
                Assert.That(footer, Is.Not.Null, "The footer should be displayed");

                Assert.Multiple(() =>
                {
                    Assert.That(vehicle!.IsRightOf(button!), Is.True,
                        $"The vehicle image ({vehicle}) should render right of the Agree button ({button})");
                    Assert.That(vehicle.Bottom, Is.LessThanOrEqualTo(footer!.Y),
                        $"The vehicle image ({vehicle}) should end above the footer (top {footer.Y:0})");
                });
            });
        }
    }
}
