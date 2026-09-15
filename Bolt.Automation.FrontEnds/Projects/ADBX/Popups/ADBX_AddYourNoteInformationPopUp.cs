using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Popups
{
    public class ADBX_AddYourNoteInformationPopUp(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePopup(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PopupIdentifier => "quotes";
        protected override string PopupName => "Account Information";

        /// <summary>
        /// Saves the note and hands back the Add Policy Information form. Only a note whose subject is
        /// "Sold" opens that form - every other subject just closes the popup, so the caller is
        /// responsible for having set the subject first.
        /// </summary>
        public async Task<ADBX_AddPolicyInformationPopUp> ClickAddAndOpenPolicyInformationAsync()
        {
            // No second WaitForLoaderToDisappear here: ClickPopupAdd -> ClickRequiredAsync already ends
            // with one, and a repeat would spend the full LoaderAppearTimeoutMs waiting for an overlay
            // the first call watched out. The form itself is gated by the popup below, which validates
            // page-ready on construction.
            await ClickPopupAdd();
            _logger?.Info("Saved the Sold note - the Add Policy Information form should now be open");

            return new ADBX_AddPolicyInformationPopUp(BrowserManager, PageHelper, ScopeContext, logger: _logger);
        }
    }
}
