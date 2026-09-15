using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Popups
{
    /// <summary>
    /// Vehicle Selection Popup - Select or add vehicles for Auto policies.
    /// Use FieldRegistry with Vehicle fields (VehicleVIN, PLYear, PLMake, PLModel)
    /// and FillForm pattern for form interactions.
    /// </summary>
    public class Interview_VehicleSelectionPopup : InterviewPopupBase
    {
        public Interview_VehicleSelectionPopup(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PopupIdentifier => "vehicle-selection";
        protected override string PopupName => "Vehicle Selection Popup";
    }
}
