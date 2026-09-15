using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Pages;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Popups;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.Tests.TestHelpers.PartnerPortal;

/// <summary>
/// Reusable helper for common Partner Portal UI test steps:
/// sending invites from the home page or via the envelope icon.
/// </summary>
public class PartnerPortalTestHelper(
    IAutomationLogger logger,
    IPageFactory pageFactory,
    IPageHelper pageHelper)
{
    /// <summary>
    /// Fills the home page contact form, clicks Continue to navigate to the invite page,
    /// fills and submits the invite (waiting for the sendinvite API 200 response),
    /// and returns the InviteSent confirmation popup.
    /// </summary>
    /// <param name="homePage">The already-resolved Partner Portal home page.</param>
    /// <param name="formData">Optional field overrides merged on top of registry defaults for the invite form.</param>
    public async Task<PartnerPortal_InviteSentPopup> SendInviteFromHomePageAsync(
        PartnerPortal_HomePage homePage,
        Dictionary<string, string>? formData = null)
    {
        await homePage.FillForm();
        await homePage.ClickContinue();

        var invitePage = pageFactory.CreatePage<PartnerPortal_InvitePage>();
        await invitePage.FillForm(formData);

        logger.Info("Invite sent via home page form — waiting for confirmation popup");
        return pageFactory.CreatePage<PartnerPortal_InviteSentPopup>();
    }

    /// <summary>
    /// Clicks the envelope icon on the current page, fills the first and last name,
    /// fills and submits the remaining invite fields (waiting for the sendinvite API 200 response),
    /// and returns the InviteSent confirmation popup.
    /// </summary>
    /// <param name="firstName">First name to enter on the invite form.</param>
    /// <param name="lastName">Last name to enter on the invite form.</param>
    /// <param name="formData">Optional field overrides merged on top of registry defaults for the invite form.</param>
    public async Task<PartnerPortal_InviteSentPopup> SendInviteViaEnvelopeIconAsync(
        string firstName,
        string lastName,
        Dictionary<string, string>? formData = null)
    {
        await pageHelper.InteractWithField(PartnerPortal_FieldNames.EnvelopeIcon);
        var invitePage = pageFactory.CreatePage<PartnerPortal_InvitePage>();
        await pageHelper.InteractWithField(FirstName, firstName);
        await pageHelper.InteractWithField(LastName, lastName);
        await invitePage.FillForm(formData);

        logger.Info($"Invite sent via envelope icon for '{firstName} {lastName}' — waiting for confirmation popup");
        return pageFactory.CreatePage<PartnerPortal_InviteSentPopup>();
    }

    /// <summary>
    /// Closes the invite sent popup and waits for the getJourneys API response,
    /// which signals the invite page has fully reset to its empty state.
    /// </summary>
    public async Task ClosePopupAndWaitForPageResetAsync(PartnerPortal_InviteSentPopup popup)
    {
        await pageHelper.WaitForApiResponseAsync(
            popup.ClosePopup,
            "invite/getJourneys",
            200,
            30000);
        logger.Info("Popup closed — invite page reset confirmed via getJourneys response");
    }

}
