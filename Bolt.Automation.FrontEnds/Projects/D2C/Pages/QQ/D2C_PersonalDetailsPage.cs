using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_PersonalDetailsPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null) 
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {

        #region Page Properties
        protected override string PageIdentifier => "personal-details";
        protected override string PageName => "Personal Details Page";
        #endregion

        private bool _secondFormProcessed = false;

        public override async Task ClickContinue()
        {
            await base.ClickContinue();

            const int pollIntervalMs = 300;
            const int maxWaitMs = 10_000;
            int elapsed = 0;

            while (elapsed < maxWaitMs && Page.Url.Contains(PageIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(pollIntervalMs);
                elapsed += pollIntervalMs;
            }

            var stillOnPersonalDetails = Page.Url.Contains(PageIdentifier, StringComparison.OrdinalIgnoreCase);

            if (stillOnPersonalDetails && !_secondFormProcessed)
            {
                _logger?.Info($"{PageName} detected second form in multi-LOB flow. Filling and continuing.");
                _secondFormProcessed = true;

                await base.FillForm();
                await base.ClickContinue();
            }
            else if (!stillOnPersonalDetails)
            {
                _logger?.Debug($"{PageName} navigated away successfully (single-LOB flow).");
            }
            else
            {
                _logger?.Debug($"{PageName} second form already processed.");
            }
        }

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            await base.FillForm(formData);
        }


        public async Task SelectDeSelectIAgreeToReceiveEmailsByBoltSelected(bool isToSelect = true)
        {
            try
            {
                await PageHelper.InteractWithField(
                    FieldNames.AgreeToReceiveEmail, 
                    isToSelect.ToString().ToLower()
                );
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to select/deselect IAgreeToReceiveEmailsByBolt");
                throw;
            }
        }
    }
}
