using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.IntegrationHubApi.Interfaces;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.Twilio;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Cases;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class NotificationTests : AdbxUITestBase
    {
        private IPollyRetryService _pollyRetryService = null!;

        protected override void ResolveServices()
        {
            _pollyRetryService = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("SMS")]
        [Category("Notification")]
        [TestCaseId(229764)]
        [Author(Author.Sandy)]
        [Description("Notifications: envelope icon badge count updates after receiving an SMS reply via Twilio webhook")]
        public async Task BOLTAG_Lead_SMS_Notification_Badge_Count()
        {
            var user = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Admin
                    : TestContextAccessor.CurrentUserCollection.ServiceAgent;

            await AdbxHelper.LoginAsync(
              user,
              ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var leadNumber = TestContextAccessor.GetTestSpecificValue("SmsNotificationLeadNumber");
            var leadSummaryPage = await _logger.ExecuteStepAsync($"Global search and open lead = {leadNumber}", async () =>
            {
                return await AdbxHelper.SearchAndOpenSummaryAsync<ADBX_LeadSummaryPage>(leadNumber!);
            }, "Expected result: Lead Summary page is displayed");

            var notificationsCountBeforeReply = await _logger.ExecuteStepAsync("Capture notifications badge count from dashboard", async () =>
            {
                var number = await leadSummaryPage.GetSmsNotificationsCountAsync();
                return number;
            }, "Expected result: Notifications badge count is captured");

            var topNotificationTimestampBeforeReply = await _logger.ExecuteStepAsync("Open notifications dropdown and capture top notification timestamp", async () =>
            {
                await leadSummaryPage.OpenSmsNotificationsDropdownAsync();
                return await leadSummaryPage.GetTopSmsNotificationTimestampAsync();
            }, "Expected result: Top notification timestamp is captured");

            var replyContent = "AutoNotification " + RandomManager.GetRandomString(8);

            var twilio = TestContextAccessor.CurrentTwilioData!;
            ScopeContext.Set(ctx => ctx.TwilioData, twilio);

            var fromPhone = "+972523717214";

            await _logger.ExecuteStepAsync("Send SMS reply via Twilio webhook", async () =>
            {
                var api = _uiTestScope.ServiceProvider.GetRequiredService<ITwilioWebhookApi>();
                var fields = twilio.BuildSmsReplyPayload(replyContent, SmsContext.Lead, fromPhone);
                var response = await api.SendMessageReplyAsync(nameof(BOLTAG), fields);
                response.EnsureSuccessStatusCode();
            });

            await _logger.ExecuteStepAsync("Poll notifications badge count until it increases above baseline, then assert", async () =>
            {
                var countAfterReply = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var count = await leadSummaryPage.GetSmsNotificationsCountAsync();
                    if (count <= notificationsCountBeforeReply)
                        throw new InvalidOperationException($"Notifications badge count '{count}' has not yet increased above baseline '{notificationsCountBeforeReply}' — retrying.");
                    return count;
                });

                Assert.That(countAfterReply, Is.GreaterThan(notificationsCountBeforeReply), $"Notifications badge count did not increase above baseline '{notificationsCountBeforeReply}' after SMS reply");
            }, "Expected result: Notifications badge count increased above baseline after SMS reply");

            await _logger.ExecuteStepAsync("Poll notifications dropdown until top notification timestamp changes, then assert", async () =>
            {
                var (timestampAfterReply, subtitleAfterReply) = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    await _pageHelper!.RefreshPageAsync();
                    await leadSummaryPage.OpenSmsNotificationsDropdownAsync();
                    var timestamp = await leadSummaryPage.GetTopSmsNotificationTimestampAsync();
                    if (timestamp == topNotificationTimestampBeforeReply)
                        throw new InvalidOperationException($"Top notification timestamp has not changed from baseline '{topNotificationTimestampBeforeReply}' — retrying.");
                    var subtitle = await leadSummaryPage.GetTopSmsNotificationSubtitleAsync();
                    return (timestamp, subtitle);
                });

                Assert.Multiple(() =>
                {
                    Assert.That(timestampAfterReply, Is.Not.EqualTo(topNotificationTimestampBeforeReply), $"Top notification timestamp did not change from baseline '{topNotificationTimestampBeforeReply}' after SMS reply");
                    Assert.That(subtitleAfterReply, Does.Contain(leadNumber).IgnoreCase, $"Top notification does not reference lead '{leadNumber}' after SMS reply");
                });
            }, "Expected result: Top notification timestamp changed from baseline and references the lead");

            var notificationsPage = await _logger.ExecuteStepAsync("Click See all notifications from dropdown", async () =>
            {
                return await leadSummaryPage.ClickSeeAllSmsNotificationsAsync<ADBX_NotificationsPage>(PageFactory);
            }, "Expected result: Notifications page is displayed");

            await _logger.ExecuteStepAsync("Click first notification in the list and verify it opens the correct lead", async () =>
            {
                await _pageHelper!.SelectTableRowAsync(1);
                var openedLeadSummaryPage = PageFactory.CreatePage<ADBX_LeadSummaryPage>();
                var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                var leadId = TestContextAccessor.GetTestSpecificValue("SmsNotificationLeadId");
                Assert.That(currentUrl, Does.Contain(leadId));
            }, "Expected result: Lead Summary page is displayed for the lead referenced by the notification");
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("SMS")]
        [Category("Notification")]
        [TestCaseId(229772)]
        [Author(Author.Sandy)]
        [Description("Notifications: envelope icon badge count updates after receiving an SMS reply via Twilio webhook (Service Case)")]
        public async Task BOLTAG_CaseService_SMS_Notification_Badge_Count()
        {
            var user = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Admin
                    : TestContextAccessor.CurrentUserCollection.ServiceAgent;

            await AdbxHelper.LoginAsync(
              user,
              ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var serviceCaseNumber = TestContextAccessor.GetTestSpecificValue("SmsNotificationServiceCaseNumber");
            var caseSummaryPage = await _logger.ExecuteStepAsync($"Global search and open service case = {serviceCaseNumber}", async () =>
            {
                return await AdbxHelper.SearchAndOpenSummaryAsync<ADBX_CaseSummaryPage>(serviceCaseNumber!);
            }, "Expected result: Case Summary page is displayed");

            var notificationsCountBeforeReply = await _logger.ExecuteStepAsync("Capture notifications badge count from dashboard", async () =>
            {
                var number = await caseSummaryPage.GetSmsNotificationsCountAsync();
                return number;
            }, "Expected result: Notifications badge count is captured");

            var topNotificationTimestampBeforeReply = await _logger.ExecuteStepAsync("Open notifications dropdown and capture top notification timestamp", async () =>
            {
                await caseSummaryPage.OpenSmsNotificationsDropdownAsync();
                return await caseSummaryPage.GetTopSmsNotificationTimestampAsync();
            }, "Expected result: Top notification timestamp is captured");

            var replyContent = "AutoNotification " + RandomManager.GetRandomString(8);

            var twilio = TestContextAccessor.CurrentTwilioData!;
            ScopeContext.Set(ctx => ctx.TwilioData, twilio);

            var fromPhone = "+972523717214";

            await _logger.ExecuteStepAsync("Send SMS reply via Twilio webhook", async () =>
            {
                var api = _uiTestScope.ServiceProvider.GetRequiredService<ITwilioWebhookApi>();
                var fields = twilio.BuildSmsReplyPayload(replyContent, SmsContext.Case, fromPhone);
                var response = await api.SendMessageReplyAsync(nameof(BOLTAG), fields);
                response.EnsureSuccessStatusCode();
            });

            await _logger.ExecuteStepAsync("Poll notifications badge count until it increases above baseline, then assert", async () =>
            {
                var countAfterReply = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var count = await caseSummaryPage.GetSmsNotificationsCountAsync();
                    if (count <= notificationsCountBeforeReply)
                        throw new InvalidOperationException($"Notifications badge count '{count}' has not yet increased above baseline '{notificationsCountBeforeReply}' — retrying.");
                    return count;
                });

                Assert.That(countAfterReply, Is.GreaterThan(notificationsCountBeforeReply), $"Notifications badge count did not increase above baseline '{notificationsCountBeforeReply}' after SMS reply");
            }, "Expected result: Notifications badge count increased above baseline after SMS reply");

            await _logger.ExecuteStepAsync("Poll notifications dropdown until top notification timestamp changes, then assert", async () =>
            {
                var (timestampAfterReply, subtitleAfterReply) = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    await _pageHelper!.RefreshPageAsync();
                    await caseSummaryPage.OpenSmsNotificationsDropdownAsync();
                    var timestamp = await caseSummaryPage.GetTopSmsNotificationTimestampAsync();
                    if (timestamp == topNotificationTimestampBeforeReply)
                        throw new InvalidOperationException($"Top notification timestamp has not changed from baseline '{topNotificationTimestampBeforeReply}' — retrying.");
                    var subtitle = await caseSummaryPage.GetTopSmsNotificationSubtitleAsync();
                    return (timestamp, subtitle);
                });

                Assert.Multiple(() =>
                {
                    Assert.That(timestampAfterReply, Is.Not.EqualTo(topNotificationTimestampBeforeReply), $"Top notification timestamp did not change from baseline '{topNotificationTimestampBeforeReply}' after SMS reply");
                    Assert.That(subtitleAfterReply, Does.Contain(serviceCaseNumber).IgnoreCase, $"Top notification does not reference service case '{serviceCaseNumber}' after SMS reply");
                });
            }, "Expected result: Top notification timestamp changed from baseline and references the service case");

            var notificationsPage = await _logger.ExecuteStepAsync("Click See all notifications from dropdown", async () =>
            {
                return await caseSummaryPage.ClickSeeAllSmsNotificationsAsync<ADBX_NotificationsPage>(PageFactory);
            }, "Expected result: Notifications page is displayed");

            await _logger.ExecuteStepAsync("Click first notification in the list and verify it opens the correct service case", async () =>
            {
                await _pageHelper!.SelectTableRowAsync(1);
                var openedCaseSummaryPage = PageFactory.CreatePage<ADBX_CaseSummaryPage>();
                var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                var ServiceCaseId = TestContextAccessor.GetTestSpecificValue("SmsNotificationServiceCaseId");
                Assert.That(currentUrl, Does.Contain(ServiceCaseId));
            }, "Expected result: Case Summary page is displayed for the service case referenced by the notification");
        }
    }
}
