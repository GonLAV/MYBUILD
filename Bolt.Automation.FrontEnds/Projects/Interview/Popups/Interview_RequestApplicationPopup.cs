using System.Reflection;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Popups
{
    /// <summary>
    /// Represents the "Request Application" popup shown on the Results page
    /// for carrier-specific application requests (e.g. Travelers BOP).
    /// </summary>
    public class Interview_RequestApplicationPopup : InterviewPopupBase
    {
        public Interview_RequestApplicationPopup(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PopupIdentifier => "Request Application";
        protected override string PopupName => "Request Application Popup";

        private const string FreeTextLocator = "//div[@class='request-application-popup']//textarea | //app-custom-textarea//textarea | //div[contains(@class,'modal')]//textarea";
        private const string FileInputLocator = "input[type='file']";
        private const string SubmitRequestButtonLocator = "//button[contains(normalize-space(.),'Submit Request')]";
        private const string ConfirmButtonLocator = "//button[contains(normalize-space(.),'CONFIRM') or contains(normalize-space(.),'Confirm')]";
        private const string RequestSubmittedGreyedLocator = "//button[contains(@class,'Request Submitted')][@disabled] | //button[contains(normalize-space(.),'Request Submitted')][@disabled]";

        /// <summary>Adds free-text notes to the request textarea.</summary>
        public async Task AddText(string text)
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                FreeTextLocator,
                ElementAction.Fill,
                new ElementInteractionOptions { Value = text, PressTab = false });
            _logger?.Info($"Added text to Request Application popup: {text}");
        }

        /// <summary>Uploads a file attachment to the request.</summary>
        public async Task AddFileAsync()
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string attachmentPath = Path.Combine(baseDir, "Models", "TestData", "ExternalDataFile", "Attachment.pdf");

            if (!File.Exists(attachmentPath))
                throw new FileNotFoundException($"Attachment file not found: {attachmentPath}");

            var fileInput = Page.Locator(FileInputLocator);
            await fileInput.SetInputFilesAsync(attachmentPath);
            _logger?.Info($"Attached file: {attachmentPath}");
        }

        /// <summary>
        /// Clicks the "Submit Request" button inside the popup.
        /// The popup stays open and shows a confirmation message.
        /// </summary>
        public async Task ClickSubmitRequest()
        {
            var locator = Page.Locator($"xpath={SubmitRequestButtonLocator}");
            await locator.ClickAsync();
            _logger?.Info("Clicked Submit Request button");
            await Task.Delay(500);
        }

        /// <summary>
        /// Clicks the "CONFIRM" button on the confirmation message
        /// ("Your request was submitted").
        /// </summary>
        public async Task ClickConfirm()
        {
            var locator = Page.Locator($"xpath={ConfirmButtonLocator}");
            await locator.ClickAsync(new Microsoft.Playwright.LocatorClickOptions { Timeout = 10000 });
            _logger?.Info("Clicked Confirm button on request submission");
            await Task.Delay(500);
        }

        /// <summary>
        /// Returns true when the "Request Application" button on the Results page
        /// has transitioned to the disabled "Request Submitted" state.
        /// </summary>
        public new async Task<bool> IsRequestSubmittedButtonExists()
        {
            return await PageHelper.ElementExists(LocatorType.XPath, RequestSubmittedGreyedLocator);
        }
    }
}
