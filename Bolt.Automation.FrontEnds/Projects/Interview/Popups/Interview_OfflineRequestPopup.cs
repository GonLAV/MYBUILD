using System.Reflection;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Popups
{
    public class Interview_OfflineRequestPopup : InterviewPopupBase
    {
        public Interview_OfflineRequestPopup(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PopupIdentifier => "Offline Request";
        protected override string PopupName => "Offline Request Popup";

        private const string FileLocator = "input[type='file']";
        private const string FreeTextlocator = "//div/textarea";

        public async Task AddText(string text)
        {
            await PageHelper.InteractWithElement(
            LocatorType.XPath,
            FreeTextlocator,
            ElementAction.Fill,
            new ElementInteractionOptions { Value = text, PressTab = false }
            );
        }

        public async Task AddFileAsync()
        {
            // Get the base directory of the executing assembly
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            // Build the full path to the attachment
            string attachmentPath = Path.Combine(baseDir, "Models","TestData","ExternalDataFile", "Attachment.pdf");

            // Ensure the file exists
            if (!File.Exists(attachmentPath))
                throw new FileNotFoundException($"Attachment file not found: {attachmentPath}");
            // Find the file input and set the file
            var fileInput = Page.Locator(FileLocator);
            await fileInput.SetInputFilesAsync(attachmentPath);
            // Example: Wait for the submit button to become enabled
            await Page.WaitForSelectorAsync("button.Request.Submit:not([disabled])");
        }

    }
}
