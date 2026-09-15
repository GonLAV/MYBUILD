using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.D2C;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.Tests.Tests.D2C
{
    public abstract class D2CTestBase : UITestBase
    {
        protected IGetQuoteApi? _getQuoteApi;
        protected IMainQueries? _mainQueries;
        protected D2CTestHelpers _d2cHelper = null!;
        protected FieldValidationHelper _fieldValidation = null!;
        protected PageLayoutHelper _layout = null!;
        protected LinkHelper _links = null!;

        protected D2CTestBase() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.D2C);
            var user = TestContextAccessor.CurrentUserCollection.D2CAutomation;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            SetD2CInterviewUrl(user);
        }

        /// <summary>
        /// Sets <c>CurrentUrl</c> to the D2C interview entry URL for the given consumer user:
        /// the tenant/env base <c>D2CUrl</c> suffixed with the user's <see cref="UserTestData.Source"/>.
        /// The source therefore lives in the datastore next to the user (and varies per tenant/env)
        /// instead of being hardcoded here. Call this after switching consumer users when a test
        /// drives the interview from the base URL rather than an API-created questionnaire.
        /// </summary>
        protected void SetD2CInterviewUrl(UserTestData? user)
        {
            var baseUrl = ScopeContext.Data.UrlDataCollection.FrontEnd.D2CUrl;
            ScopeContext.Set(ctx => ctx.CurrentUrl, baseUrl + (user?.Source ?? string.Empty));
        }

        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetService<IGetQuoteApi>();
            if (Environment != Common.Environment.Production)
                _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
        }

        protected override void InitializeComponents()
        {
            _d2cHelper = new D2CTestHelpers(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext, _getQuoteApi!, _mainQueries);
            _fieldValidation = new FieldValidationHelper(_pageHelper!, _logger);
            _layout = new PageLayoutHelper(BrowserManager, ScopeContext, _logger);
            _links = new LinkHelper(BrowserManager, _logger);
        }
    }
}
