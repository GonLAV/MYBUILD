using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Popups
{
    /// <summary>
    /// Additional Driver Popup - Add or edit additional drivers/operators.
    /// Use FieldRegistry with Operator fields (OperatorGender, OperatorRelationship, etc.)
    /// and FillForm pattern for form interactions.
    /// </summary>
    public class Interview_AdditionalDriverPopup : InterviewPopupBase
    {
        public Interview_AdditionalDriverPopup(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PopupIdentifier => "add-driver";
        protected override string PopupName => "Additional Driver Popup";
    }
}
