using Bolt.Automation.Common;
using Bolt.Automation.Common.Models.Urls;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.TestDataProvider.Repositories.DataStores
{
    public static class UrlDataStore
    {
        public static readonly Dictionary<(Tenant, Environment), UrlTestDataCollection> All = new()
        {
            #region BOLTAG
            // BOLTAG Dev Environment
            [(Tenant.BOLTAG, Environment.Dev)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-dev-boltag.boltqa.com",
                    LoginUrl = "https://sts-dev-boltag.boltqa.com/Login/Login",
                },
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-dev-boltag.boltqa.com/Login/Login",
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-dev-boltag.boltqa.com/SSO/index"
                }
            },
            // BOLTAG QA Environment
            [(Tenant.BOLTAG, Environment.Qa)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-qa-boltag.boltqa.com",
                    LoginUrl = "https://sts-qa-boltag.boltqa.com/login/login",
                },
                FrontEnd = new UrlTestData
                {
                    BaseUrl = "https://adbx-qa-boltag.boltqa.com",
                    D2CUrl = "https://d2cinterview-qa.boltqa.com/",
                    LoginUrl = "https://sts-qa-boltag.boltqa.com/Login/Login",
                    AdditionalUrls = new Dictionary<string, string>
                    {
                        ["ConsumerInterviewCL"] = "https://d2capi-qa.boltqa.com/init?v=product&t=BOLTAG&source=Organic-CL&type=cl",
                        ["ConsumerFullInterviewPL"] = "https://d2capi-qa.boltqa.com/init?v=product&t=BOLTAG&source=automationthree&type=pl",
                        ["ConsumerShortInterviewPL"] = "https://d2capi-qa.boltqa.com/init?v=d2c&t=BOLTAG&source=D2CAutomation&type=pl"
                    }
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-qa-boltag.boltqa.com/SSO/index",
                    EndpointSso = "https://sts-qa-boltag.boltqa.com/SSO/index",
                    Recipient = "https://sts-qa-boltag.boltqa.com/SSO/index"
                }
            },
            // BOLTAG UAT Environment
            [(Tenant.BOLTAG, Environment.Uat)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-boltag.bolttest.com",
                    LoginUrl = "https://sts-boltag.bolttest.com/login/login",
                },
                FrontEnd = new UrlTestData
                {
                    BaseUrl = "https://adbx-boltag.bolttest.com",
                    D2CUrl = "https://d2cinterview.bolttest.com/",
                    LoginUrl = "https://sts-boltag.bolttest.com/login/login",
                    AdditionalUrls = new Dictionary<string, string>
                    {
                        ["ConsumerInterviewCL"] = "https://d2capi.bolttest.com/init?v=product&t=BOLTAG&source=Organic-CL&type=cl",
                        ["ConsumerFullInterviewPL"] = "https://d2capi.bolttest.com/init?v=product&t=BOLTAG&source=automationthree&type=pl",
                        ["ConsumerShortInterviewPL"] = "https://d2capi.bolttest.com/init?v=d2c&t=BOLTAG&source=D2CAutomation&type=pl"
                    }
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-boltag.bolttest.com/SSO/index",
                    EndpointSso = "https://sts-boltag.bolttest.com/SSO/index",
                    Recipient = "https://sts-boltag.bolttest.com/SSO/index"
                }
            },
            // BOLTAG Production Environment
            [(Tenant.BOLTAG, Environment.Production)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-boltag.boltinc.com",
                    LoginUrl = "https://sts-boltag.boltinc.com/Login/Login",
                },
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-boltag.boltinc.com/login/login",
                    D2CUrl = "https://quote.boltinsurance.com/",
                    AdditionalUrls = new Dictionary<string, string>
                    {
                        ["ConsumerInterviewCL"] = "https://d2capi.boltinc.com/init?v=product&t=BOLTAG&source=Organic-CL&type=cl",
                        ["ConsumerFullInterviewPL"] = "https://d2capi.boltinc.com/init?v=product&t=BOLTAG&source=automationthree&type=pl",
                        ["ConsumerShortInterviewPL"] = "https://d2capi.boltinc.com/init?v=d2c&t=BOLTAG&source=D2CAutomation&type=pl"
                    }
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-boltag.boltinc.com/SSO/index",
                    EndpointSso = "https://sts-boltag.boltinc.com/SSO/index",
                    Recipient = "https://sts-boltag.boltinc.com/SSO/index"
                }
            },
            // BOLTAG Staging Environment
            [(Tenant.BOLTAG, Environment.Staging)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-boltag.boltinc.com",
                    LoginUrl = "https://sts-boltag.boltinc.com/Login/Login",
                },
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-stg-boltag.boltinc.com/login/login",
                    D2CUrl = "https://d2cinterview-stg.boltinc.com/",
                    AdditionalUrls = new Dictionary<string, string>
                    {
                        ["ConsumerDashboard"] = "https://quote-stg-boltag.boltinc.com/tg6djyqksd-ocl",
                        ["AgencyOneAddress"] = "https://d2cinterview-stg.boltinc.com/agencyone/your-address"
                    }
                }
            },
            #endregion

            #region UNIFY
            // UNIFY Dev Environment
            [(Tenant.UNIFY, Environment.Dev)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-dev-unify.boltqa.com"
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-dev-unify.boltqa.com/SSO/index"
                }
            },
            // Unify Qa Environment
            [(Tenant.UNIFY, Environment.Qa)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-qa-unify.boltqa.com",
                    LoginUrl = "https://sts-qa-unify.boltqa.com/login/login",
                },
                FrontEnd = new UrlTestData
                {
                    D2CUrl = "https://d2cinterview.boltqa.com/",
                    MarketsLib = "https://login-qa.boltqa.com/marketslib/login/login",
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-qa-unify.boltqa.com/SSO/index"
                },
                CasePortalApi = new UrlTestData
                {
                    BaseUrl = "https://portaldev.superioraccessqa.com/BoltX"
                }
            },
            // Unify UAT Environment
            [(Tenant.UNIFY, Environment.Uat)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-unify.bolttest.com",
                    LoginUrl = "https://sts-unify.bolttest.com/login/login",
                },
                FrontEnd = new UrlTestData
                {
                    D2CUrl = "https://d2cinterview.bolttest.com/",
                    MarketsLib = "https://login.bolttest.com/marketslib/login/login",

                },
                CasePortalApi = new UrlTestData
                {
                    BaseUrl = "https://saisportaluat.bolttest.com/BoltX"
                }
            },
            // UNIFY Staging Environment
            [(Tenant.UNIFY, Environment.Staging)] = new UrlTestDataCollection
            {
                FrontEnd = new UrlTestData
                {
                    MarketsLib = "https://login-stg.boltinc.com/marketslib/Login/Login",
                    D2CUrl = "https://d2cinterview-stg.boltinc.com/",
                    AdditionalUrls = new Dictionary<string, string>
                    {
                        // Staging fronts every tenant from the one D2C host, so the tenant rides in as a
                        // query parameter here instead of being part of the host as in the other envs.
                        ["AgencyOneAddress"] = "https://d2cinterview-stg.boltinc.com/agencyone/your-address?tenant=UNIFY&type=pl&v=d2c"
                    }
                }
            },
            #endregion

            #region KRAFTLAKEX
            // KRAFTLAKEX UAT Environment
            [(Tenant.KRAFTLAKEX, Environment.Uat)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-kraftlakex.bolttest.com",
                    LoginUrl = "https://sts-kraftlakex.bolttest.com/login/login",
                },
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-kraftlakex.bolttest.com/login/login",
                },
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://platformapi-kraftlakex.bolttest.com"
                },
                CasePortalApi = new UrlTestData
                {
                    BaseUrl = "https://kluat.boltinc.com/KLX"
                }
            },
            // KRAFTLAKEX QA Environment
            [(Tenant.KRAFTLAKEX, Environment.Qa)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-qa-kraftlakex.boltqa.com",
                    LoginUrl = "https://sts-qa-kraftlakex.boltqa.com/Login/Login",
                },
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-qa-kraftlakex.boltqa.com/Login/Login",
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-qa-kraftlakex.boltqa.com/SSO/index"
                },
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://platformapi-qa-kraftlakex.boltqa.com"
                },
                CasePortalApi = new UrlTestData
                {
                    BaseUrl = "https://klportal.boltqa.com/KLX"
                }
            },
            // KRAFTLAKEX Dev Environment
            [(Tenant.KRAFTLAKEX, Environment.Dev)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-dev-kraftlakex.boltqa.com",
                    LoginUrl = "https://sts-dev-kraftlakex.boltqa.com/Login/Login",
                },
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-dev-kraftlakex.boltqa.com/Login/Login",
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-dev-kraftlakex.boltqa.com/SSO/index"
                },
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://platformapi-dev-kraftlakex.boltqa.com"
                }
            },
            // KRAFTLAKEX Staging Environment
            [(Tenant.KRAFTLAKEX, Environment.Staging)] = new UrlTestDataCollection
            {
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-stg-kraftlakex.boltinc.com/login/login",
                },
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://platformapi-stg-kraftlakex.boltinc.com"
                }
            },
            #endregion

            #region PROGRESSIVEPL
            // PROGRESSIVEPL Qa Environment
            [(Tenant.PROGRESSIVEPL, Environment.Dev)] = new UrlTestDataCollection
            {
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://platformapi-dev.progressive.boltqa.com/auth",
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "",
                    EndpointSso = "",
                    Recipient = ""
                }
            },
            // PROGRESSIVEPL Qa Environment
            [(Tenant.PROGRESSIVEPL, Environment.Qa)] = new UrlTestDataCollection
            {
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://platformapi-qa.progressive.boltqa.com",
                },
                CustomUrls = new UrlTestData
                {
                    CCPAProgressive = "https://60-www.qa.progressive.com/privacy/do-not-sell-my-information/?src=boltHQXQA",
                    CCPARedirect = "https://interviewprefill-qa-progressive.boltqa.com/do-not-sell-my-information?persistSuccessful=0&PgrOptOutPref=0",
                    CANotice = "https://www.progressive.com/privacy/privacy-data-request/",
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-qa-progressivepl.boltqa.com/SSO/index",
                    EndpointSso = "https://sts-qa-progressivepl.boltqa.com/SSO/index",
                    Recipient = "https://uat01.ProgressiveBolt.com/singlesignon/SamlConsumer"
                }
            },
            // PROGRESSIVEPL Uat Environment
            [(Tenant.PROGRESSIVEPL, Environment.Uat)] = new UrlTestDataCollection
            {
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://uat-api.boltinc.com"
                },
                CustomUrls = new UrlTestData
                {
                    CCPAProgressive = "https://60-www.qa.progressive.com/privacy/do-not-sell-my-information/?src=boltHQX",
                    CCPARedirect = "https://interviewprefill-progressive.bolttest.com/do-not-sell-my-information?persistSuccessful=0&PgrOptOutPref=0",
                    CANotice = "https://www.progressive.com/privacy/privacy-data-request/",
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-progressivepl.bolttest.com/SSO/Index",
                    EndpointSso = "https://sts-progressivepl.bolttest.com/SSO/Index",
                    Recipient = "https://sts-progressivepl.bolttest.com/SSO/Index"
                }
            },
            // PROGRESSIVEPL Production Environment
            [(Tenant.PROGRESSIVEPL, Environment.Production)] = new UrlTestDataCollection
            {
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://Api.boltinc.com/"
                },
                CustomUrls = new UrlTestData
                {
                    CCPAProgressive = "https://www.progressive.com/privacy/do-not-sell-my-information/?src=boltHQX",
                    CCPARedirect = "https://progressive.boltinc.com/do-not-sell-my-information?persistSuccessful=0&PgrOptOutPref=0",
                    CANotice = "https://www.progressive.com/privacy/privacy-data-request/",
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-progressivepl.boltinc.com/SSO/Index",
                    EndpointSso = "https://sts-progressivepl.boltinc.com/SSO/Index",
                    Recipient = "https://sts-progressivepl.boltinc.com/SSO/Index"
                }
            },
            // PROGRESSIVEPL Staging Environment
            [(Tenant.PROGRESSIVEPL, Environment.Staging)] = new UrlTestDataCollection
            {
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-stg-progressivepl.boltinc.com/Login/Login",
                    AdditionalUrls = new Dictionary<string, string>
                    {
                        ["HQX2Interview"] = "https://interviewprefill-stg-progressive.boltinc.com",
                        ["LegacyInterview"] = "https://interview-stg-progressive.boltinc.com",
                        ["DesktopService"] = "https://desktopserver-stg-progressive.boltinc.com"
                    }
                },
                PlatformApi = new UrlTestData
                {
                    BaseUrl = "https://PlatformApi-stg-progressive.boltinc.com"
                }
            },
            #endregion

            #region COMPARION
            // COMPARION Qa Environment
            [(Tenant.COMPARION, Environment.Qa)] = new UrlTestDataCollection
            {
                FrontEnd = new UrlTestData
                {
                    AdditionalUrls = new Dictionary<string, string>
                    {
                        ["HomeDashboard"] = "https://adbx-qa-comparion.boltqa.com/home"
                    }
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-qa-comparion.boltqa.com/SSO/index",
                    Recipient = "https://sts-qa-comparion.boltqa.com/SSO/index",
                    EndpointSso = "https://sts-qa-comparion.boltqa.com/SSO/index"
                }
            },
            // COMPARION UAT Environment
            [(Tenant.COMPARION, Environment.Uat)] = new UrlTestDataCollection
            {
                FrontEnd = new UrlTestData
                {
                    AdditionalUrls = new Dictionary<string, string>
                    {
                        ["HomeDashboard"] = "https://adbx-comparion.bolttest.com/home"
                    }
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-comparion.bolttest.com/SSO/index",
                    Recipient = "https://sts-comparion.bolttest.com/SSO/index",
                    EndpointSso = "https://sts-comparion.bolttest.com/SSO/index"
                }
            },
            #endregion

            #region USAA
            // USAA Qa Environment
            [(Tenant.USAA, Environment.Qa)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-qa-usaa.boltqa.com",
                    LoginUrl = "https://sts-qa-usaa.boltqa.com/login/login",
                },
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-qa-usaa.boltqa.com/login/login",
                    D2CUrl = "https://d2cinterview-qa.boltqa.com/"
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-qa-usaa.boltqa.com/SSO/index",
                    Recipient = "https://sts-qa-usaa.boltqa.com/SSO/index",
                    EndpointSso = "https://sts-qa-usaa.boltqa.com/SSO/index",
                }
            },
            // USAA Uat Environment
            [(Tenant.USAA, Environment.Uat)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-usaa.bolttest.com",
                    LoginUrl = "https://sts-usaa.bolttest.com/login/login",
                },
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-usaa.bolttest.com/login/login",
                    D2CUrl = "https://d2cinterview.bolttest.com/"
                },
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-usaa.bolttest.com/SSO/index",
                    Recipient = "https://sts-usaa.bolttest.com/SSO/index",
                    EndpointSso = "https://sts-usaa.bolttest.com/SSO/index",
                }
            },
            // USAA Production Environment
            [(Tenant.USAA, Environment.Production)] = new UrlTestDataCollection
            {
                FrontEnd = new UrlTestData
                {
                    LoginUrl = "https://sts-usaa.bolttest.com/login/login",
                },
            },
            #endregion

            #region LIBERTYX
            // LIBERTYX Qa Environment
            [(Tenant.LIBERTYX, Environment.Qa)] = new UrlTestDataCollection
            {
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-qa-libertyx.boltqa.com/SSO/index",
                    Recipient = "https://sts-qa-libertyx.boltqa.com/SSO/index",
                    EndpointSso = "https://sts-qa-libertyx.boltqa.com/SSO/index"
                }
            },
            // LIBERTYX UAT Environment
            [(Tenant.LIBERTYX, Environment.Uat)] = new UrlTestDataCollection
            {
                SsoApi = new UrlTestData
                {
                    BaseUrl = "https://sts-libertyx.bolttest.com/SSO/index",
                    Recipient = "https://sts-libertyx.bolttest.com/SSO/index",
                    EndpointSso = "https://sts-libertyx.bolttest.com/SSO/index"
                }
            },
            #endregion

            #region BOLTACCESS
            // BoltAccess Qa Environment
            [(Tenant.BOLTACCESS, Environment.Qa)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-qa-boltaccess.boltqa.com/"
                },
                FrontEnd = new UrlTestData
                {
                },
                CasePortalApi = new UrlTestData
                {
                    BaseUrl = "https://portaldev.superioraccessqa.com/BOLTX"
                }
            },
            // BOLTACCESS UAT Environment
            [(Tenant.BOLTACCESS, Environment.Uat)] = new UrlTestDataCollection
            {
                AdbxApi = new UrlTestData
                {
                    BaseUrl = "https://adbxapi-boltaccess.bolttest.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/login/login",
                },
                FrontEnd = new UrlTestData
                {
                },
                CasePortalApi = new UrlTestData
                {
                    BaseUrl = "https://saisportaluat.bolttest.com/BOLTX"
                }
            },
            #endregion
        };
    }
}