using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Cases;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Policies;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.FrontEnds.Projects.STS;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.RegularExpressions;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;

namespace Bolt.Automation.Tests.TestHelpers.ADBX;

/// <summary>
/// Reusable helper for common ADBX UI test steps:
/// login, account creation via New Quote, and account lookup.
/// </summary>
public class AdbxTestHelper(
    IAutomationLogger logger,
    IBrowserManager browserManager,
    IPageFactory pageFactory,
    IPageHelper pageHelper,
    IScopeContext scopeContext)
{
    #region LoginMethods
    /// <summary>
    /// Sets the current user in scope, navigates to the login URL, and completes login.
    /// </summary>
    public async Task LoginAsync(UserTestData user, string url)
    {
        scopeContext.Set(ctx => ctx.CurrentUser, user);

        await logger.ExecuteStepAsync("Log in to ADBX", async () =>
        {
            await browserManager.NavigateAsync(url);
            var loginPage = pageFactory.CreatePage<STS_LoginPage>();
            await loginPage.Login();
        }, "Expected result: ADBX Home Page is displayed");
        // The post-login /?token= -> /home SPA redirect is waited out by ADBX_HomePage's own
        // page-readiness validation (it uses a longer budget for exactly this), so no wait here.
    }

    /// <summary>
    /// Sets the current user in scope, navigates to the login URL, and searches for the relevant quote and click on it.
    /// </summary>
    public async Task LoginAndNavigateToQuoteAsync(UserTestData user, string url, string friendlyId)
    {
        await LoginAsync(user, url);

        await logger.ExecuteStepAsync($"Navigate to quote {friendlyId}", async () =>
        {
            var homePage = pageFactory.CreatePage<ADBX_HomePage>();
            await homePage.SearchFor(friendlyId);
            await pageHelper.SelectTableRowAsync(1);
        }, $"Expected result: User is navigated to quote with FriendlyId '{friendlyId}'");
    }

    /// <summary>
    /// Logs in and returns the ADBX Home page. Use this when the caller needs the home page to
    /// navigate from; use <see cref="LoginAsync(UserTestData, string)"/> when the login does not
    /// land on home (MFA challenge, access denied) or the page is not needed.
    /// </summary>
    public async Task<ADBX_HomePage> LoginToHomeAsync(UserTestData user, string url)
    {
        await LoginAsync(user, url);
        return pageFactory.CreatePage<ADBX_HomePage>();
    }
    #endregion

    /// <summary>
    /// Creates a personal account via the New Quote button from the ADBX Home page.
    /// Generates a unique email, fills the account form, completes creation,
    /// verifies the Interview Start page is displayed, then navigates back home.
    /// </summary>
    /// <param name="additionalFormData">Optional field overrides merged on top of registry defaults.</param>
    /// <returns>The generated account email and the ADBX Home page.</returns>
    public async Task<(string Email, ADBX_HomePage HomePage)> CreateAccountViaNewQuoteAsync(
        Dictionary<string, string>? additionalFormData = null)
    {
        var (email, startPage) = await StartQuoteFromNewAccountAsync<Product_StartPage>(additionalFormData);

        var homePage = await logger.ExecuteStepAsync("Return to ADBX from the new quote", async () =>
            await startPage.ClickHome(),
            "Expected result: ADBX Home Page is displayed");

        scopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);

        return (email, homePage);
    }

    /// <summary>
    /// Creates an account via New Quote and stays on the quote it opens, in the new tab. Works for
    /// both Personal and Commercial (pass AccountBusinessLine="Commercial" in additionalFormData) -
    /// clicking Add on the "Enter Account Information" popup opens the interview directly either way.
    /// </summary>
    /// <param name="additionalFormData">Optional field overrides merged on top of registry defaults.</param>
    /// <returns>The generated account email and the Interview start page of the new quote.</returns>
    public async Task<(string Email, TPage StartPage)> StartQuoteFromNewAccountAsync<TPage>(
        Dictionary<string, string>? additionalFormData = null)
        where TPage : class, IInterview
    {
        var email = RandomManager.GetRandomEmail();
        var formData = new Dictionary<string, string>(additionalFormData ?? [])
        {
            [AccountEmail] = email
        };

        var accountPopup = await logger.ExecuteStepAsync("Click New Quote button", async () =>
        {
            var homePage = pageFactory.CreatePage<ADBX_HomePage>();
            return await homePage.ClickOnNewQuote();
        }, "Expected result: 'Enter Account Information' pop-up is displayed");

        var startPage = await logger.ExecuteStepAsync("Fill in account details and click Add", async () =>
        {
            await accountPopup.FillForm(formData);
            await accountPopup.ClickPopupAdd();
            await browserManager.SwitchToLastTabAsync();
            // The caller fills interview fields next, so the front end has to move off ADBX here —
            // that is what re-points field lookups at the interview registry.
            scopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
            return pageFactory.CreatePage<TPage>();
        }, "Expected result: Account is created successfully and a new quote opens on the Start page");

        return (email, startPage);
    }

    /// <summary>Logs in, creates a commercial account via New Quote, and stays on the quote it opens.</summary>
    /// <remarks>
    /// Forcing the business line is all that is needed - <c>AccountInsuredType</c> is registry-gated on it
    /// and defaults to Business.
    /// </remarks>
    public async Task<(string Email, ProductBusinessProfilePageCL BusinessProfilePage)> StartCommercialQuoteFromNewAccountAsync(
        UserTestData user, Dictionary<string, string>? accountFormData = null)
    {
        await LoginToDashboardAsync(user);

        var formData = new Dictionary<string, string>(accountFormData ?? [])
        {
            [AccountBusinessLine] = "Commercial"
        };

        return await StartQuoteFromNewAccountAsync<ProductBusinessProfilePageCL>(formData);
    }

    /// <summary>
    /// Records a Sold note on a quote's timeline and saves the policy details it asks for, which is what
    /// creates the policy binder. Leaves the caller on the policy summary the save lands on.
    /// </summary>
    /// <remarks>
    /// "Sold" is a note-subject option, not a control of its own, and it is the only subject that opens
    /// the Add Policy Information form. The term is re-applied after the fill - always, whether or not
    /// <paramref name="policyFormData"/> carries one - because <c>PolicyExpirationDate</c> follows
    /// <c>PolicyTerm</c> in the registry with a year-out default and overwrites the date the app derived
    /// from it - see kb framework:popups.
    /// </remarks>
    public async Task RecordSoldNoteAsync(ADBX_QuoteSummaryPage quoteSummaryPage, Dictionary<string, string> policyFormData) =>
        await logger.ExecuteStepAsync("Record a Sold note with the policy details", async () =>
        {
            var notePopup = await quoteSummaryPage.ClickOnNewNote();
            await notePopup.FillForm(new Dictionary<string, string> { [NoteSubject] = "Sold" });

            var addPolicyPopup = await notePopup.ClickAddAndOpenPolicyInformationAsync();
            await addPolicyPopup.FillForm(policyFormData);

            // Unconditional, not guarded on the caller having passed a term: FillForm fills every
            // registry field on the page, so it applies PolicyTerm's own default when the caller omits
            // the key, and PolicyExpirationDate overwrites the derived date just the same. Guarding
            // this would leave exactly those callers with the year-out date they never asked for.
            // A null value re-applies the registry default - the value the fill itself used.
            await pageHelper.InteractWithField(PolicyTerm, policyFormData.GetValueOrDefault(PolicyTerm));

            await addPolicyPopup.ClickPopupConfirm();
        }, "Expected result: Selecting Sold and Add opens the Add Policy Information form, and confirming it creates the policy binder");

    /// <summary>
    /// Navigates to the Accounts tab, searches by email, checks whether the account is found,
    /// and if so selects the first row and returns the Account Summary page.
    /// Returns (false, null) when the account is not found — the caller is responsible for asserting.
    /// </summary>
    public async Task<(bool IsFound, ADBX_AccountSummaryPage? Page)> OpenAccountFromAccountsTabAsync(
        ADBX_HomePage homePage, string email)
    {
        var accountsPage = await logger.ExecuteStepAsync("Navigate to Accounts tab", async () =>
        {
            await homePage.ClickOnMenuTab(NavigationType.Accounts);
            return pageFactory.CreatePage<ADBX_AccountsTabPage>();
        }, "Expected result: Accounts grid is displayed");

        var isFound = await logger.ExecuteStepAsync("Search for account by email", async () =>
        {
            await accountsPage.SearchRecentRecords(email);
            var found = await pageHelper.IsSpecificColumnHaveData(email, "Email");
            logger.LogBusinessRule("Account Existence Validation", found,
                $"Account with email '{email}' {(found ? "exists" : "does not exist")}");
            return found;
        }, "Expected result: Newly created account is found and displayed in the grid");

        if (!isFound)
            return (false, null);

        var summaryPage = await logger.ExecuteStepAsync("Select account", async () =>
        {
            await pageHelper.SelectTableRowAsync(1);
            return pageFactory.CreatePage<ADBX_AccountSummaryPage>();
        }, "Expected result: Account Summary page is displayed");

        return (true, summaryPage);
    }

    /// <summary>
    /// Searches the ADBX home page grid by the given value, checks whether a match exists
    /// in the specified column, and if so selects the first row and returns the Account Summary page.
    /// Returns (false, null) when no match is found — the caller is responsible for asserting.
    /// </summary>
    public async Task<(bool IsFound, ADBX_AccountSummaryPage? Page)> SearchAndOpenAccountFromHomeAsync(
        string searchValue, string column)
    {
        var isFound = await logger.ExecuteStepAsync($"Search home grid for '{searchValue}'", async () =>
        {
            var homePage = pageFactory.CreatePage<ADBX_HomePage>();
            await homePage.SearchFor(searchValue);
            var found = await pageHelper.IsSpecificColumnHaveData(searchValue, column);
            logger.LogBusinessRule("Account Existence Validation", found,
                $"Account with {column} '{searchValue}' {(found ? "exists" : "does not exist")}");
            return found;
        }, $"Expected result: Account with {column} '{searchValue}' is displayed in the grid");

        if (!isFound)
            return (false, null);

        var summaryPage = await logger.ExecuteStepAsync("Select account", async () =>
        {
            await pageHelper.SelectTableRowAsync(1);
            return pageFactory.CreatePage<ADBX_AccountSummaryPage>();
        }, "Expected result: Account Summary page is displayed");

        return (true, summaryPage);
    }

    /// <summary>
    /// Validates that the quote contains expected products
    /// </summary>
    /// <param name="actualProducts">Comma-separated string of products from API response</param>
    /// <param name="expectedProducts">Array of expected product names</param>
    /// <param name="exactMatch">If true, validates that ONLY expected products are present. Default is false.</param>
    public void ValidateQuoteProducts(string actualProducts, string[] expectedProducts, bool exactMatch = false)
    {
        var productList = actualProducts.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
        var missingProducts = expectedProducts.Except(productList).ToList();
        var extraProducts = exactMatch ? productList.Except(expectedProducts).ToList() : new List<string>();

        var validationName = exactMatch ? "Products Validation (Exact Match)" : "Products Validation";
        var validationMessage = exactMatch
            ? "Quote should contain exactly the expected products"
            : "Quote should contain all expected products";

        var isValid = missingProducts.Count == 0 && extraProducts.Count == 0;

        logger.LogDataValidation(validationName, isValid,
            string.Join(", ", expectedProducts), actualProducts, validationMessage);

        var errors = new List<string>();
        if (missingProducts.Any()) errors.Add($"Missing: {string.Join(", ", missingProducts)}");
        if (extraProducts.Any()) errors.Add($"Unexpected: {string.Join(", ", extraProducts)}");

        Assert.That(errors.Count == 0, Is.True,
            $"Products validation failed. Expected: {string.Join(", ", expectedProducts)}. Actual: {actualProducts}. {string.Join(". ", errors)}");
    }

    /// <summary>
    /// Validates that the timeline note matches expected LOB patterns
    /// </summary>
    /// <param name="note">Timeline note description</param>
    /// <param name="lobs">Dictionary of LOB name and regex pattern</param>
    public void ValidateTimelinePatterns(string note, Dictionary<string, string> lobs)
    {
        var failures = new List<string>();

        foreach (var (lobName, pattern) in lobs)
        {
            var isMatch = Regex.IsMatch(note, pattern);

            logger.LogDataValidation(
                $"{lobName} Pattern",
                isMatch,
                "Matches",
                isMatch.ToString(),
                $"Online Submission note should contain {lobName} pattern");

            if (!isMatch)
            {
                failures.Add($"{lobName} pattern not found");
            }
        }

        Assert.That(failures.Count == 0, Is.True,
            $"Timeline pattern validation failed:\n- {string.Join("\n- ", failures)}\n\nActual note: {note}");
    }

    /// <summary>
    /// Builds the expected account defaults dictionary from the field registry,
    /// excluding name and source fields, and pre-sets TCPAConsent and Since.
    /// The caller should patch in the email used during account creation.
    /// </summary>
    public static Dictionary<string, string> BuildExpectedAccountDefaults() =>
        new(FieldRegistryADBX.Fields
            .Where(field => field.Key.StartsWith("Account"))
            .Where(field => !new[] { "AccountFirstName", "AccountLastName", "AccountSource" }.Contains(field.Key))
            .ToDictionary(static field => field.Key["Account".Length..], field => field.Value.DefaultValue))
        {
            ["TCPAConsent"] = "No Promotional:Not SetTransactional:Not SetConversational:Not Set",
            ["Since"] = DateTime.Today.ToString("MM/dd/yyyy")
        };

    /// <summary>
    /// Navigates to the specified queue page and selects the requested queue tab.
    /// Use <see cref="NavigationType.Service"/> for cases and <see cref="NavigationType.Leads"/> for leads.
    /// </summary>
    public async Task<TPage> SelectQueueAsync<TPage>(NavigationType navigationType, string queueName)
        where TPage : ADBX_BasePage
    {
        var homePage = pageFactory.CreatePage<ADBX_HomePage>();
        var targetPage = await NavigateToMenuAsync<TPage>(homePage, navigationType);

        await pageHelper.InteractWithField(QueueTabs, queueName);

        return targetPage;
    }

    public async Task<T> SearchAndOpenSummaryAsync<T>(string recordNumber) where T : ADBX_BasePage
    {
        var homePage = pageFactory.CreatePage<ADBX_HomePage>();
        await homePage.SearchFor(recordNumber);
        await pageHelper.SelectTableRowAsync(1);
        return pageFactory.CreatePage<T>();
    }

    /// <summary>Logs in and opens a quote on an existing commercial account off the Accounts tab.</summary>
    /// <remarks>The caller owns the switch to Interview and the page it creates; the tests differ on both.</remarks>
    public async Task StartCommercialQuoteFromExistingAccountAsync(UserTestData user)
    {
        await LoginToDashboardAsync(user);

        await logger.ExecuteStepAsync("Start a quote on an existing commercial account",
            async () => await SelectCommercialAccountAndStartQuoteAsync(),
            "Expected result: The interview opens for a quote on the selected commercial account");
    }

    // FrontEnd is set before the login, not after, so that anything the login itself resolves through the
    // field registry resolves against ADBX. Switching it later is not the problem it looks like:
    // ScopeContext.Set drops the cached registry whenever FrontEnd changes, so a later switch to Interview
    // does re-point field lookups (which is what StartQuoteFromNewAccountAsync relies on).
    private async Task LoginToDashboardAsync(UserTestData user)
    {
        scopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
        await LoginAsync(user, user.LoginUrl);
    }

    /// <summary>Picks the first commercial account off the Accounts tab and opens a new quote on it.</summary>
    public async Task SelectCommercialAccountAndStartQuoteAsync()
    {
        var homePage = pageFactory.CreatePage<ADBX_HomePage>();
        var accountsPage = await NavigateToMenuAsync<ADBX_AccountsTabPage>(homePage, NavigationType.Accounts);
        await accountsPage.SelectCommercialLinesAccountAsync();
        await pageFactory.CreatePage<ADBX_AccountSummaryPage>().ClickOnNewQuoteFromExistingAccount();
    }

public async Task<bool> IsSmsTemplateAvailableAsync(string leadNumber, string templateName)
    {
        var leadSummaryPage = await SearchAndOpenSummaryAsync<ADBX_LeadSummaryPage>(leadNumber);
        await leadSummaryPage.NavigateCommnicationTab("sms");
        var values = await pageHelper.GetFieldDropdownListValues(ADBX_FieldNames.SmsTemplateList);

        return values.Any(v => v.Equals(templateName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(ADBX_PolicySummaryPage PolicySummaryPage, string PolicyNumber)> CreatePolicyAsync()
    {
        var quoteSummaryPage = pageFactory.CreatePage<ADBX_QuoteSummaryPage>();
        var quotePoliciesPage = await NavigateInnerTabAsync<ADBX_QuoteSummaryPoliciesTab>(quoteSummaryPage, NavigationType.Policies);

        var addPolicyPopup = await quotePoliciesPage.ClickOnAddPolicy();

        // Own the policy number here rather than reading it back out of the registry after the fact.
        // FieldRegistryADBX.Fields is a shared static and its PolicyNumber default is computed once
        // per process, so the old read-back returned whatever the last caller left behind - and every
        // policy in a run carried the same number.
        var policyNumber = "RenewTest" + RandomManager.GetRandomString(4) + RandomManager.GetRandomDigits(3);
        await addPolicyPopup.FillForm(new Dictionary<string, string> { [PolicyNumber] = policyNumber });
        await addPolicyPopup.ClickPopupConfirm();

        var policySummaryPage = pageFactory.CreatePage<ADBX_PolicySummaryPage>();
        logger.Info($"Policy created - # {policyNumber}");

        return (policySummaryPage, policyNumber);
    }

    public async Task<(ADBX_PolicySummaryPage PolicySummaryPage, string SecondPolicyNumber)> CreateRenewalPolicyAsync(string policyNumber)
    {
        var quoteSummaryPage = pageFactory.CreatePage<ADBX_QuoteSummaryPage>();
        var quotePoliciesPage = await NavigateInnerTabAsync<ADBX_QuoteSummaryPoliciesTab>(quoteSummaryPage, NavigationType.Policies);

        var addPolicyPopup = await quotePoliciesPage.ClickOnAddPolicy();
        var secondPolicyNumber = "Renew" + RandomManager.GetRandomString(4) + RandomManager.GetRandomDigits(3);

        // Per-call overrides, NOT registry mutation. FieldRegistryADBX.Fields is a shared static and
        // the suite runs Parallelizable(ParallelScope.All) with LevelOfParallelism(50), so writing
        // these five defaults let concurrent tests read each other's policy numbers and dates.
        await addPolicyPopup.FillForm(new Dictionary<string, string>
        {
            [PolicyRenewal] = policyNumber,
            [PolicyNumber] = secondPolicyNumber,
            [PolicyPremium] = "4000",
            [PolicyEffectiveDate] = DateTime.Today.AddDays(2).ToString("MM-dd-yyyy"),
            [PolicyExpirationDate] = DateTime.Today.AddDays(2).AddYears(1).ToString("MM-dd-yyyy"),
        });

        // The Renewal field is an ng-select. When the value written to it matches no loaded option it
        // keeps an empty selection and the popup still submits, producing a policy with NO renewal
        // relationship — which only surfaces three steps later as a grid timeout on the Renewals tab.
        // Fail here instead, where the cause is visible. See bug 253459.
        var selectedRenewal = await pageHelper.GetFieldValue(PolicyRenewal);
        logger.Info($"Renewal dropdown holds '{selectedRenewal}' before submitting (parent policy: {policyNumber})");

        if (string.IsNullOrWhiteSpace(selectedRenewal))
        {
            var offered = await pageHelper.GetFieldDropdownListValues(PolicyRenewal);
            logger.Warning($"Renewal dropdown selected nothing. Options offered: [{string.Join(" | ", offered)}]");
        }

        Assert.That(selectedRenewal, Does.Contain(policyNumber),
            $"The Renewal dropdown does not hold parent policy '{policyNumber}' (actual: '{selectedRenewal}'). " +
            "Submitting now would create a regular policy with no renewal relationship.");

        await addPolicyPopup.ClickPopupConfirm();

        var policySummaryPage = pageFactory.CreatePage<ADBX_PolicySummaryPage>();
        logger.Info($"Renewal Policy created - # {secondPolicyNumber}");
        return (policySummaryPage, secondPolicyNumber);
    }

    public async Task RequoteAsync(string secondPolicyNumber)
    {
        var quoteSummaryPage = pageFactory.CreatePage<ADBX_QuoteSummaryPage>();
        await quoteSummaryPage.ClickOnMenuTab(NavigationType.Renewals);
        var renewalsPage = pageFactory.CreatePage<ADBX_RenewalsTabPage>();
        await renewalsPage.SearchRecentRecords(secondPolicyNumber);
        var isSecondPolicyExist = await pageHelper.IsSpecificColumnHaveData(secondPolicyNumber, "Policy Number");
        Assert.That(isSecondPolicyExist, Is.True, $"Policy with number {secondPolicyNumber} is not found in Renewals tab");
        await pageHelper.ClickTableRowCheckboxAsync("policyNumber", secondPolicyNumber);
        await pageHelper.InteractWithField(PolicyRequoteButton);
    }

    /// <summary>
    /// Navigates to the specified ADBX menu tab from any ADBX page and returns the target page.
    /// When <paramref name="page"/> is null (SSO login), skips navigation and creates the target page directly.
    /// </summary>
    public async Task<T> NavigateToMenuAsync<T>(ADBX_BasePage? page, NavigationType navigationType)
        where T : ADBX_BasePage
    {
        if (page != null)
            return await page.NavigateToMenuAsync<T>(navigationType, pageFactory);

        return pageFactory.CreatePage<T>();
    }

    /// <summary>
    /// Navigates to the specified ADBX inner tab from any ADBX page and returns the target page.
    /// When <paramref name="page"/> is null (SSO login), skips navigation and creates the target page directly.
    /// </summary>
    public async Task<T> NavigateInnerTabAsync<T>(ADBX_BasePage? page, NavigationType navigationType)
    where T : ADBX_BasePage
    {
        if (page != null)
            return await page.NavigateInnerTabAsync<T>(navigationType, pageFactory);

        return pageFactory.CreatePage<T>();
    }

    /// <summary>
/// Navigates to the specified ADBX admin menu subtab and returns the target page.
/// When <paramref name="page"/> is null (SSO login), skips navigation and creates the target page directly.
/// </summary>
public async Task<T> NavigateToAdminMenuAsync<T>(ADBX_BasePage? page, string subtabName)
    where T : ADBX_BasePage
{
    if (page != null)
        await page.ClickOnAdminMenuTab("Admin", subtabName);

    return pageFactory.CreatePage<T>();
}
    #region Sorting functionality methods
    public async Task<IResponse> EnsureGridSortAsync(
    ADBX_BasePage page,
    string columnName,
    string sortProperty,
    string endpoint,
    GridSortDirection direction)
    {
        var header = page.Page.Locator(
            "datatable-header-cell[role='columnheader']",
            new() { HasText = columnName });

        var sortButton = header.Locator("span.sort-btn");
        var sortOrder = direction == GridSortDirection.Asc ? "asc" : "desc";

        logger.Info($"Sorting '{columnName}' ({sortProperty}) {sortOrder} on '{endpoint}'.");

        try
        {
            return await pageHelper.WaitForApiResponseAsync(
                () => sortButton.ClickAsync(),
                endpoint,
                200,
                30000,
                r => RequestMatchesSort(r.Request.Url, endpoint, sortProperty, sortOrder));
        }
        // A sort can fail either way — no response at all, or one with the wrong status — and both
        // need the column/direction context to be diagnosable, so both funnel into one type.
        catch (Exception ex) when (ex is TimeoutException or ApiResponseException)
        {
            throw new ApiResponseException(
                $"Sort response failed. Column='{columnName}', SortProperty='{sortProperty}', SortOrder='{sortOrder}', Endpoint='{endpoint}'. {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Verifies only that clicking sort ascending then descending sends the correctly-parameterized
    /// request and gets a 200 back (via <see cref="EnsureGridSortAsync"/>, which throws otherwise).
    /// Use for columns whose displayed values can't reliably prove reordering (e.g. a column that's
    /// frequently tied across all rows in current test data, like a computed Due Date).
    /// </summary>
    public async Task EnsureGridSortRequestsAsync(
        ADBX_BasePage page,
        string columnDisplayName,
        string sortKey,
        string route)
    {
        logger.Info($"'{columnDisplayName}' values can't prove reordering — checking the sort request only.");
        await EnsureGridSortAsync(page, columnDisplayName, sortKey, route, GridSortDirection.Asc);
        await EnsureGridSortAsync(page, columnDisplayName, sortKey, route, GridSortDirection.Desc);
    }

    private static bool RequestMatchesSort(
        string url,
        string endpoint,
        string sortProperty,
        string sortOrder)
    {
        if (!url.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
            return false;

        var query = ParseQuery(url);

        return query.TryGetValue("sortProperty", out var prop) &&
               prop.Equals(sortProperty, StringComparison.OrdinalIgnoreCase) &&
               query.TryGetValue("sortOrder", out var order) &&
               order.Equals(sortOrder, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string url)
    {
        var uri = new Uri(url);
        var queryString = uri.Query.TrimStart('?');

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(queryString))
            return result;

        foreach (var pair in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            result[key] = value;
        }

        return result;
    }

    public enum GridSortDirection
    {
        Asc,
        Desc
    }
    #endregion
}
