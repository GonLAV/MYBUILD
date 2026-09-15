using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.Users;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.TestDataProvider.Repositories.DataStores
{
    // Structural user data only. Credential fields (Password, ApiKey, OAuthToken, AgentIdentity,
    // ApiSource) live in the secrets bundle under environments.<env>.userSecrets.<TENANT>.<Role>
    // and are overlaid at read-time by TestContextAccessor.CurrentUserCollection. See
    // Documentation/Secrets-Tier2-UserTwilio-Migration-Spec.md.
    public static class UserDataStore
    {
        public static readonly Dictionary<(Tenant?, Environment?), UserTestDataCollection> All = new()
        {
            #region BOLTAG
            // BOLTAG Dev Environment
            [(Tenant.BOLTAG, Environment.Dev)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    Username = "boltagagent1@test.com",
                    Role = UserRole.Agent,
                    Email = "boltagagent1@test.com"
                }
            },

            // BOLTAG QA Environment
            [(Tenant.BOLTAG, Environment.Qa)] = new UserTestDataCollection
            {
                Admin = new UserTestData
                {
                    Username = "boltagadmin@test.com",
                    Role = UserRole.Admin,
                    Email = "boltagadmin@test.com",
                    Id = "",
                    WorkSpaceGroupId = ""
                },
                Agent = new UserTestData
                {
                    Username = "boltagagent1@test.com",
                    Role = UserRole.Agent,
                    Email = "boltagagent1@test.com",
                    Id = "b20d1657-a42f-41e6-8f42-33ecdd286b41",
                    WorkSpaceGroupId = "c2bf90fd-3c2e-4040-b556-0e94d65b9746"
                },
                PartnerPortalAgent = new UserTestData
                {
                    Username = "automationpp.agent@boltinc.com",
                    Role = UserRole.Agent,
                    Email = "automationpp.agent@boltinc.com",
                    Source = "automationpp",
                    LoginUrl = "https://partnerportal-qa.boltqa.com/BOLTAG/automationpp/login"
                },
                PartnerPortalAdmin = new UserTestData
                {
                    Username = "automationpp.admin@boltinc.com",
                    Role = UserRole.Admin,
                    Email = "automationpp.admin@boltinc.com",
                    Source = "automationpp",
                    LoginUrl = "https://partnerportal-qa.boltqa.com/BOLTAG/automationpp/login"
                },
                ServiceAgent = new UserTestData
                {
                    FirstName = "Service",
                    LastName = "Agent",
                    Username = "Service@Agent.com",
                    Role = UserRole.ServiceAgent,
                    Email = "Service@Agent.com",
                    Id = "96C298DB-FD0D-4CDC-9C38-AFB700C24FF8",
                    WorkSpaceGroupId = "790B3A5F-C839-4675-93B4-AFB7001CD521"
                },
                ConsumerOrganicPL = new UserTestData // getquote api source
                {
                    Role = UserRole.Consumer
                },
                ConsumerKeller = new UserTestData // getquote api source
                {
                    Role = UserRole.Consumer
                },
                D2CAutomation = new UserTestData
                {
                    Role = UserRole.Consumer,
                    Source = "D2CAutomation",
                },
                BMW = new UserTestData
                {
                    Role = UserRole.Consumer
                },
                Underwriter = new UserTestData //casemanager user
                {
                    Role = UserRole.Underwriter,
                    UserExternalId = "Boltobolt123"
                },
                WFGAgent = new UserTestData
                {
                    UserExternalId = "WFGAGENT",
                    Username = "Automation@wfg.com",
                    FirstName = "WFGAgent",
                    LastName = "Consumer",
                    Phone = "5675675675",
                    Role = UserRole.WFGAgent,
                    Email = "WFGAgent@Agent.com",
                    Source = "WFGagent",
                    Sso = new SsoUserData
                    {
                        Issuer = "wfg:nonprod:transamerica:com:saml2",
                        Audience = "https://sts-qa-boltag.boltqa.com"
                    }
                },
                ServiceManager = new UserTestData
                {
                    Username = "boltautomation@boltinc.com",
                    Role = UserRole.ServiceManager,
                    Email = "boltautomation@boltinc.com",
                },

            },

            // BOLTAG UAT Environment
            [(Tenant.BOLTAG, Environment.Uat)] = new UserTestDataCollection
            {
                Admin = new UserTestData
                {
                    Username = "boltagadmin@test.com",
                    Role = UserRole.Admin,
                    Email = "boltagadmin@test.com",
                    Id = "",
                    WorkSpaceGroupId = ""
                },
                Agent = new UserTestData
                {
                    Username = "boltagagent1@test.com",
                    Role = UserRole.Agent,
                    Email = "boltagagent1@test.com"
                },
                PartnerPortalAgent = new UserTestData
                {
                    Username = "automationpp.agent@boltinc.com",
                    Role = UserRole.Agent,
                    Email = "automationpp.agent@boltinc.com",
                    Source = "automationpp",
                    LoginUrl = "https://partnerportal.bolttest.com/BOLTAG/automationpp/login"
                },
                PartnerPortalAdmin = new UserTestData
                {
                    Username = "automationpp.admin@boltinc.com",
                    Role = UserRole.Admin,
                    Email = "automationpp.admin@boltinc.com",
                    Source = "automationpp",
                    LoginUrl = "https://partnerportal.bolttest.com/BOLTAG/automationpp/login"
                },
                ServiceAgent = new UserTestData
                {
                    FirstName = "Service",
                    LastName = "Agent",
                    Username = "Service@Agent.com",
                    Role = UserRole.ServiceAgent,
                    Email = "Service@Agent.com",
                    Id = "6D0AD60F-9F23-4AB7-B2D5-AFCA00D69456",
                    WorkSpaceGroupId = "D091AF32-3667-4B9B-8318-AFCA00BE4C42"
                },
                ConsumerOrganicPL = new UserTestData // getquote api source
                {
                    Role = UserRole.Consumer
                },
                ConsumerKeller = new UserTestData // getquote api source
                {
                    Role = UserRole.Consumer
                },
                D2CAutomation = new UserTestData
                {
                    Role = UserRole.Consumer,
                    Source = "D2CAutomation",
                },
                automationfeatureoff = new UserTestData
                {
                    Role = UserRole.Consumer
                },
                Underwriter = new UserTestData //casemanager user
                {
                    Role = UserRole.Underwriter,
                    UserExternalId = "Boltobolt123"
                },
                WFGAgent = new UserTestData
                {
                    UserExternalId = "WFGAGENT",
                    Username = "Automation@wfg.com",
                    FirstName = "WFGAgent",
                    LastName = "Consumer",
                    Phone = "5675675675",
                    Role = UserRole.WFGAgent,
                    Email = "WFGAgent@Agent.com",
                    Source = "WFGagent",
                    Sso = new SsoUserData
                    {
                        Issuer = "wfg:nonprod:transamerica:com:saml2",
                        Audience = "https://sts-boltag.bolttest.com"
                    }
                },
                ServiceManager = new UserTestData
                {
                    Username = "boltautomation@boltinc.com",
                    Role = UserRole.ServiceManager,
                    Email = "boltautomation@boltinc.com",
                },
            },

            // BOLTAG Staging Environment
            [(Tenant.BOLTAG, Environment.Staging)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    Username = "boltagagent1@test.com",
                    Role = UserRole.Agent,
                    Email = "boltagagent1@test.com"
                },
                ServiceAgent = new UserTestData
                {
                    FirstName = "Service",
                    LastName = "Agent",
                    Username = "Service@Agent.com",
                    Role = UserRole.ServiceAgent,
                    Email = "Service@Agent.com",
                },
                PartnerPortalAgent = new UserTestData
                {
                    Username = "automationpp.agent@boltinc.com",
                    Role = UserRole.Agent,
                    Email = "automationpp.agent@boltinc.com",
                    Source = "automationpp",
                    LoginUrl = "https://partnerportal-stg.boltinc.com/BOLTAG/automationpp/login"
                },
                PartnerPortalAdmin = new UserTestData
                {
                    Username = "automationpp.admin@boltinc.com",
                    Role = UserRole.Admin,
                    Email = "automationpp.admin@boltinc.com",
                    Source = "automationpp",
                    LoginUrl = "https://partnerportal-stg.boltinc.com/BOLTAG/automationpp/login"
                },
                Admin = new UserTestData
                {
                    Username = "boltagadmin@test.com",
                    Role = UserRole.Admin,
                    Email = "boltagadmin@test.com"
                },
                ConsumerOrganicPL = new UserTestData // getquote api source
                {
                    Role = UserRole.Consumer
                },
                D2CAutomation = new UserTestData // D2C interview base-URL source
                {
                    Role = UserRole.Consumer,
                    Source = "D2CAutomation",
                }
            },

            // BOLTAG Production Environment
            [(Tenant.BOLTAG, Environment.Production)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    Username = "boltagagent1@test.com",
                    Role = UserRole.Agent,
                    Email = "boltagagent1@test.com"
                },
                Admin = new UserTestData
                {
                    Username = "boltagadmin@test.com",
                    Role = UserRole.Admin,
                    Email = "boltagadmin@test.com"
                },
                PartnerPortalAgent = new UserTestData
                {
                    Username = "automationpp.agent@boltinc.com",
                    Role = UserRole.Agent,
                    Email = "automationpp.agent@boltinc.com",
                    Source = "automationpp",
                    LoginUrl = "https://partnerportal.boltinc.com/BOLTAG/automationpp/login"
                },
                PartnerPortalAdmin = new UserTestData
                {
                    Username = "automationpp.admin@boltinc.com",
                    Role = UserRole.Admin,
                    Email = "automationpp.admin@boltinc.com",
                    Source = "automationpp",
                    LoginUrl = "https://partnerportal.boltinc.com/BOLTAG/automationpp/login"
                },
                WFGAgent = new UserTestData
                {
                    UserExternalId = "WFGAGENT",
                    Username = "Automation@wfg.com",
                    FirstName = "WFGAgent",
                    LastName = "Consumer",
                    Phone = "5675675675",
                    Role = UserRole.WFGAgent,
                    Email = "WFGAgent@Agent.com",
                    Source = "WFGagent",
                    Sso = new SsoUserData
                    {
                        Issuer = "wfg:prod:transamerica:com:saml2",
                        Audience = "https://sts-boltag.boltinc.com"
                    }
                },
                ServiceAgent = new UserTestData
                {
                    Username = "ServiceT@Agent.com",
                    Role = UserRole.ServiceAgent,
                    Id = "25D14123-FBEC-4CF4-9062-AFD800520FD5",
                    WorkSpaceGroupId = "2B293CF8-58C0-477C-BF05-AFD8002F2E88"
                },
                ConsumerOrganicPL = new UserTestData // getquote api source
                {
                    Role = UserRole.Consumer
                },
                ConsumerKeller = new UserTestData // getquote api source
                {
                    Role = UserRole.Consumer
                },
                D2CAutomation = new UserTestData // D2C interview base-URL source
                {
                    Role = UserRole.Consumer,
                    Source = "D2CAutomation",
                },
            },

            #endregion
            #region USAA
            // USAA QA Environment
            [(Tenant.USAA, Environment.Qa)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    Email = "usaaagent1@test.com",
                    Username = "usaaagent1@test.com",
                    UserExternalId = "601134",
                    Role = UserRole.Agent,
                    SendAgentIdentity = true,
                    Sso = new SsoUserData
                    {
                        Issuer = "http://usaa",
                        Audience = "https://sts-qa-usaa.boltqa.com"
                    }
                },
                OnlineQuote = new UserTestData
                {
                    Role = UserRole.D2C,
                    UserExternalId = "999079884",
                    Source = "MembersD2C",
                    Sso = new SsoUserData
                    {
                        Issuer = "http://www.qa",
                        Audience = "https://d2cinterview-qa.boltqa.com/MembersD2C"
                    },
                    LoginUrl = ""
                },
                D2CAgent = new UserTestData
                {
                    Username = "d2cagent1@test.com",
                    UserExternalId = "601134",
                    Role = UserRole.Agent,
                },
                D2CAgent2 = new UserTestData
                {
                    UserExternalId = "9991",
                    Role = UserRole.D2C,
                    Sso = new SsoUserData
                    {
                        Issuer = "http://www.qa",
                        Audience = "https://d2cinterview-qa.boltqa.com/MembersD2C"
                    }
                }
            },

            // USAA Uat Environment
            [(Tenant.USAA, Environment.Uat)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    UserExternalId = "601134",
                    Email = "agentone@test.com",
                    Username = "agentone@test.com",
                    Role = UserRole.Agent,
                    SendAgentIdentity = true,
                    Sso = new SsoUserData
                    {
                        Issuer = "http://www.okta.com/exk2nk4v2yg5wXpq80h8",
                        Audience = "https://sts-usaa.bolttest.com"
                    }
                },
                OnlineQuote = new UserTestData
                {
                    UserExternalId = "999041030",
                    Role = UserRole.D2C,
                    Source = "MembersD2C",
                    Sso = new SsoUserData
                    {
                        Issuer = "http://usaafed",
                        Audience = "https://d2cinterview.bolttest.com/MembersD2C"
                    }
                },
                D2CAgent = new UserTestData
                {
                    Username = "d2cagent1@test.com",
                    UserExternalId = "601134",
                    Role = UserRole.Agent,
                },
                D2CAgent2 = new UserTestData
                {
                    UserExternalId = "9991",
                    Role = UserRole.D2C,
                    Sso = new SsoUserData
                    {
                        Issuer = "http://usaafed",
                        Audience = "https://d2cinterview.bolttest.com/MembersD2C"
                    }
                }
            },

            // USAA Production Environment
            [(Tenant.USAA, Environment.Production)] = new UserTestDataCollection
            {
                Consumer = new UserTestData
                {
                    Role = UserRole.Consumer
                }
            },
            #endregion
            #region Kraftlakex
            //KRAFTLAKEX QA Environment
            [(Tenant.KRAFTLAKEX, Environment.Qa)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    Username = "lsp1_aortest@test.com",
                    Role = UserRole.Agent,
                    Email = "lsp1_aortest@test.com",
                    GroupExternalId = "AOR-TEST",
                    SendAgentIdentity = true,
                    LoginUrl = "https://sts-qa-kraftlakex.boltqa.com/Login/Login"
                },
                LSP1 = new UserTestData //LSP1
                {
                    Username = "LSP_B3Ag1Aor1@tes.com",
                    Role = UserRole.Agent,
                    Email = "LSP_B3Ag1Aor1@tes.com",
                    GroupExternalId = "07278M",
                    WorkSpaceGroupId = "AC4A78FC-1493-42A8-B524-B1A200A8E29D",
                    UserExternalId = "12345",
                    Id = "5bf62052-9721-4b7e-b234-b1a200a9e36a",
                    SendAgentIdentity = true,
                    LoginUrl = "https://sts-qa-kraftlakex.boltqa.com/Login/Login"

                },
                LSP1CL = new UserTestData //LSPCL
                {
                    Username = "Agent6@farmerstestinator.com",
                    Role = UserRole.Agent,
                    Email = "Agent6@farmerstestinator.com",
                    GroupExternalId = "0611H6",
                    UserExternalId = "1380351",
                    Id = "9CA27B94-68FD-418B-9CE7-B1A300A17D6B",
                    SendAgentIdentity = true,
                    LoginUrl = "https://sts-qa-kraftlakex.boltqa.com/Login/Login"

                },
                RootAdmin = new UserTestData
                {
                    Username = "root@boltinc.com",
                    Role = UserRole.RootAdmin,
                    Email = "root@boltinc.com",
                    GroupExternalId = "1"
                },
                Underwriter = new UserTestData //casemanager user
                {
                    Role = UserRole.Underwriter,
                    UserExternalId = "3ef1ab36-94ae-40de-a350-9696ba3bee94"
                },
                CasePortalUser = new UserTestData //casemanager Portal
                {
                    UserExternalId = "01751C0F-19AC-4C19-9FB0-9AC8DE9500B2"
                }
            },

            //KRAFTLAKEX UAT Environment
            [(Tenant.KRAFTLAKEX, Environment.Uat)] = new UserTestDataCollection
            {
                LSP1 = new UserTestData
                {
                    Username = "lsp1_aortest@test.com",
                    Role = UserRole.Agent,
                    Email = "lsp1_aortest@test.com",
                    GroupExternalId = "AOR-TEST",
                    SendAgentIdentity = true,
                    LoginUrl = "https://sts-kraftlakex.bolttest.com/login/login",
                },
                Agent = new UserTestData //LSP1
                {
                    Username = "libe519@farmersoktauser.com",
                    Role = UserRole.Agent,
                    Email = "libe519@farmersoktauser.com",
                    GroupExternalId = "222502",
                    UserExternalId = "100008",
                    Id = "73227DB0-820F-460F-8D1C-B1CA00C21E5A",
                    SendAgentIdentity = true
                },
                LSP1CL = new UserTestData //LSP1CL
                {
                    Username = "Agent6@farmerstestinator.com",
                    Role = UserRole.Agent,
                    GroupExternalId = "0611H6",
                    UserExternalId = "1380351",
                    Id = "9CA27B94-68FD-418B-9CE7-B1A300A17D6B",
                    SendAgentIdentity = true,
                    LoginUrl = "https://sts-kraftlakex.bolttest.com/login/login",

                },
                RootAdmin = new UserTestData
                {
                    Username = "root@boltinc.com",
                    Role = UserRole.RootAdmin,
                    Email = "root@boltinc.com",
                    GroupExternalId = "1"
                },
                Underwriter = new UserTestData //casemanager user
                {
                    Role = UserRole.Underwriter,
                    UserExternalId = "166a2f57-feee-4141-8b2b-b772c899f7a2"
                },
                CasePortalUser = new UserTestData //casemanager Portal
                {
                    UserExternalId = "166A2F57-FEEE-4141-8B2B-B772C899F7A2"
                }
            },

            // KRAFTLAKEX Staging Environment
            [(Tenant.KRAFTLAKEX, Environment.Staging)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    Username = "lsp1_aortest@test.com",
                    Role = UserRole.Agent,
                    Email = "lsp1_aortest@test.com",
                    GroupExternalId = "AOR-TEST",
                    SendAgentIdentity = true
                }
            },

            #endregion
            #region Unify
            //UNIFY Dev Environment
            [(Tenant.UNIFY, Environment.Dev)] = new UserTestDataCollection
            {
                TestAgent = new UserTestData
                {
                    Username = "externalagent1@test.com",
                    Role = UserRole.TestAgent,
                    Email = "externalagent1@test.com",
                    Subtenant = "EXTERNAL"
                },
                RootAdmin = new UserTestData
                {
                    Username = "root@boltinc.com",
                    Role = UserRole.RootAdmin,
                    Email = "root@boltinc.com"
                }
            },

            //UNIFY QA Environment
            [(Tenant.UNIFY, Environment.Qa)] = new UserTestDataCollection
            {
                TestAgent = new UserTestData
                {
                    Username = "externalagent1@test.com",
                    Role = UserRole.TestAgent,
                    Email = "externalagent1@test.com",
                    LoginUrl = "https://login-qa.boltqa.com/EXTERNAL/login/login",
                    Subtenant = "EXTERNAL",
                },
                Admin = new UserTestData
                {
                    Username = "externalAdmin@test.com",
                    Role = UserRole.Admin,
                    Email = "externalAdmin@test.com",
                    LoginUrl = "https://login-qa.boltqa.com/EXTERNAL/login/login",
                    Subtenant = "EXTERNAL",
                },
                FarmersAdmin = new UserTestData
                {
                    Username = "farmersadmin@test.com",
                    Role = UserRole.Admin,
                    Email = "farmersadmin@test.com",
                    LoginUrl = "https://login-qa.boltqa.com/farmersdhub/login/login",
                    Subtenant = "farmersdhub",
                },
                RootAdmin = new UserTestData
                {
                    Username = "root@boltinc.com",
                    Role = UserRole.RootAdmin,
                    Email = "root@boltinc.com",
                    LoginUrl = "https://login-qa.boltqa.com/login/login"
                },
                Underwriter = new UserTestData //casemanager user
                {
                    Role = UserRole.Underwriter,
                    UserExternalId = "2f1cc85f-d984-4d69-a4c0-447456bb3a8c"
                },
                CasePortalUser = new UserTestData //casemanager Portal
                {
                    UserExternalId = "43DE228B-DBA7-4BDB-A907-501D75F2B699"
                },
                LakeviewConsumer = new UserTestData
                {
                    Role = UserRole.D2C,
                    Source = "lakeviewconsumer",
                },
                Consumer = new UserTestData
                {
                    Role = UserRole.D2C
                },
                MarketLibAgent = new UserTestData
                {
                    Role = UserRole.TestAgent,
                    Username = "marketslibagent1@test.com",
                    SendAgentIdentity = true,
                    LoginUrl = "https://login-qa.boltqa.com/marketslib/login/login"
                },
                MarketLibAgentCL = new UserTestData
                {
                    Role = UserRole.D2C,
                    Username = "marketlibagentcl"
                }
            },

            //UNIFY UAT Environment
            [(Tenant.UNIFY, Environment.Uat)] = new UserTestDataCollection
            {
                TestAgent = new UserTestData
                {
                    Username = "automationagent@epos.com",
                    Role = UserRole.TestAgent,
                    Email = "automationagent@epos.com",
                    LoginUrl = "https://login.bolttest.com/TESTAPP/login/login",
                    Subtenant = "TESTAPP"
                },
                FarmersAdmin = new UserTestData
                {
                    Username = "FarmersDHub@test.com",
                    Role = UserRole.Admin,
                    Email = "FarmersDHub@test.com",
                    LoginUrl = "https://login.bolttest.com/farmersdhub/login/login",
                    Subtenant = "farmersdhub"
                },
                Underwriter = new UserTestData //casemanager user
                {
                    Role = UserRole.Underwriter,
                    UserExternalId = "30842D7C-C6CF-4F0F-8514-B032685EA123"
                },
                CasePortalUser = new UserTestData //casemanager Portal
                {
                    UserExternalId = "843E9C6D-1A6F-4026-8E83-7766AC0F2C85"
                },
                LakeviewConsumer = new UserTestData
                {
                    Role = UserRole.D2C,
                    Source = "fastlanebayviewtest",
                },
                MarketLibAgent = new UserTestData
                {
                    Role = UserRole.ServiceAgent,
                    Username = "mlagent1@test.com",
                    SendAgentIdentity = true,
                    LoginUrl = "https://login.bolttest.com/MARKETSLIB/login/login"
                },
                RootAdmin = new UserTestData
                {
                    Username = "root@boltinc.com",
                    Role = UserRole.RootAdmin,
                    Email = "root@boltinc.com",
                    LoginUrl = "https://login.bolttest.com/login/login"
                }
            },

            // UNIFY Staging Environment
            [(Tenant.UNIFY, Environment.Staging)] = new UserTestDataCollection
            {
                MarketLibAgent = new UserTestData
                {
                    Username = "mlagent1@test.com",
                    Role = UserRole.TestAgent,
                    Email = "mlagent1@test.com",
                    LoginUrl = "https://login-stg.boltinc.com/marketslib/Login/Login",
                    Subtenant = "marketslib"
                },
                SalesEnvironmentAdmin = new UserTestData
                {
                    FirstName = "SalesEnvironment",
                    LastName = "Admin",
                    Username = "SalesEnvironmentAdmin@test.com",
                    Role = UserRole.Admin,
                    Email = "SalesEnvironmentAdmin@test.com",
                    LoginUrl = "https://login-stg.boltinc.com/SALESENVIRONMENT/login/login",
                    Subtenant = "SALESENVIRONMENT"
                },
                PartnerPortalAdmin = new UserTestData
                {
                    Username = "agencytwo.admin@test.com",
                    Role = UserRole.Admin,
                    Email = "agencytwo.admin@test.com",
                    Source = "Agencytwo",
                    Subtenant = "SALESENVIRONMENT",
                    LoginUrl = "https://partnerportal-stg.boltinc.com/SALESENVIRONMENT/Agencytwo/login"
                }
            },
            #endregion
            #region Comparion
            //COMPARION QA Environment
            [(Tenant.COMPARION, Environment.Qa)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    GroupExternalId = "TESTCOMAgent",
                    UserExternalId = "N0169256",
                    // GetQuote API identifies the Comparion agent by X-Agent-Identity; the value itself
                    // is overlaid from the secrets bundle (qa.userSecrets.COMPARION.Agent).
                    SendAgentIdentity = true,
                    Sso = new SsoUserData
                    {
                        Issuer = "https://comparion",
                        Audience = "https://sts-qa-comparion.boltqa.com"
                    }
                },
                Admin = new UserTestData
                {
                    GroupExternalId = "COMOrganization",
                    UserExternalId = "COMPADMIN",
                    Sso = new SsoUserData
                    {
                        Issuer = "https://comparion",
                        Audience = "https://sts-qa-comparion.boltqa.com"
                    }
                }
            },

            //COMPARION UAT Environment
            [(Tenant.COMPARION, Environment.Uat)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    GroupExternalId = "TESTCOMAgent",
                    UserExternalId = "N0169256",
                    // GetQuote API identifies the Comparion agent by X-Agent-Identity; the value is
                    // overlaid from the secrets bundle (uat.userSecrets.COMPARION.Agent).
                    SendAgentIdentity = true,
                    Sso = new SsoUserData
                    {
                        Issuer = "https://test-lmidp.libertymutual.com",
                        Audience = "https://sts-comparion.bolttest.com"
                    }
                },
                Admin = new UserTestData
                {
                    GroupExternalId = "COMOrganization",
                    UserExternalId = "COMPADMIN",
                    Sso = new SsoUserData
                    {
                        Issuer = "https://test-lmidp.libertymutual.com",
                        Audience = "https://sts-comparion.bolttest.com"
                    }
                }
            },

            // COMPARION Production Environment
            [(Tenant.COMPARION, Environment.Production)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    GroupExternalId = "TESTCOMAgent",
                    UserExternalId = "testagent1",
                    Role = UserRole.Agent
                }
            },
            #endregion
            #region BoltAccess
            //BOLTACCESS QA Environment
            [(Tenant.BOLTACCESS, Environment.Qa)] = new UserTestDataCollection
            {
                Admin = new UserTestData
                {
                    Username = "testadmin@test.com",
                    Role = UserRole.Admin,
                    Email = "testadmin@test.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/TEST/login/login"
                },
                RootAdmin = new UserTestData
                {
                    Username = "root@boltinc.com",
                    Role = UserRole.RootAdmin,
                    Email = "root@boltinc.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/login/login"
                },
                Agent = new UserTestData
                {
                    Username = "automation@agent.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/AUTOMATION/Login/Login",
                    SendAgentIdentity = true
                },
                AgentCL = new UserTestData
                {
                    Username = "automation@agent.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/AUTOMATION/Login/Login",
                    SendAgentIdentity = true
                },
                Underwriter = new UserTestData //casemanager user
                {
                    Role = UserRole.Underwriter,
                    UserExternalId = "43de228b-dba7-4bdb-a907-501d75f2b699"
                },
                ConsumerAgentBoltAccess = new UserTestData
                {
                    Username = "CaseKING1@shalom.com",
                    Role = UserRole.Agent,
                    Email = "CaseKING1@shalom.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/ABITESTGON/login/login"
                },
                CasePortalUser = new UserTestData //casemanager Portal
                {
                    UserExternalId = "43DE228B-DBA7-4BDB-A907-501D75F2B699"
                },
                MfaPrincipal = new UserTestData
                {
                    Username = "boltautomation+1214@boltinc.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/mfatest/Login/Login",
                    SubtenantId = "70c8e4a8-397e-46bd-b836-98619d26526b",
                    Subtenant = "MFA TEST",
                    FirstName = "MFA",
                    Email = "boltautomation+1214@boltinc.com"
                },
                NoMfaPrincipal = new UserTestData
                {
                    Username = "boltautomation+4444@boltinc.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/mfalegalname/Login/Login",
                    SubtenantId = "e7bc4d56-3bfb-425f-867a-0bcbf34ff149",
                    Subtenant = "MFA Legal Name",
                    Email = "boltautomation+4444@boltinc.com"
                },
                GroupMfaAgent = new UserTestData
                {
                    Username = "boltautomation+2227@boltinc.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/mfaahtest/Login/Login",
                    FirstName = "AGENT MFA EN",
                    Email = "boltautomation+2227@boltinc.com"
                },
                GroupNoMfaAgent = new UserTestData
                {
                    Username = "boltautomation+3334@boltinc.com",
                    LoginUrl = "https://sts-qa-boltaccess.boltqa.com/mfaahtest/Login/Login",
                    FirstName = "AGENT MFA DIS",
                    Email = "boltautomation+3334@boltinc.com"
                }
            },

            //BOLTACCESS UAT Environment
            [(Tenant.BOLTACCESS, Environment.Uat)] = new UserTestDataCollection
            {
                Admin = new UserTestData
                {
                    Username = "testadmin@test.com",
                    Role = UserRole.Admin,
                    Email = "testadmin@test.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/TEST/login/login"
                },
                RootAdmin = new UserTestData
                {
                    Username = "root@boltinc.com",
                    Role = UserRole.RootAdmin,
                    Email = "root@boltinc.com",
                    UserExternalId = "root@boltinc.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/login/login"
                },
                Agent = new UserTestData
                {
                    Username = "automation@agent.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/AUTOMATION1/login/login",
                    SendAgentIdentity = true
                },
                AgentCL = new UserTestData
                {
                    Username = "automation@agent.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/AUTOMATION1/login/login",
                    SendAgentIdentity = true
                },
                Underwriter = new UserTestData //casemanager user
                {
                    Role = UserRole.Underwriter,
                    UserExternalId = "30842D7C-C6CF-4F0F-8514-B032685EA123"
                },
                CasePortalUser = new UserTestData //casemanager Portal
                {
                    UserExternalId = "30842D7C-C6CF-4F0F-8514-B032685EA123"
                },
                MfaPrincipal = new UserTestData
                {
                    Username = "boltautomation+1213@boltinc.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/mfatest/Login/Login",
                    SubtenantId = "606e1355-3431-4d8a-825f-48bfb95f00eb",
                    Subtenant = "MFATEST",
                    FirstName = "MFA TEST",
                    Email = "boltautomation+1213@boltinc.com"
                },
                NoMfaPrincipal = new UserTestData
                {
                    Username = "boltautomation+4444@boltinc.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/mfalegalname/Login/Login",
                    SubtenantId = "1033404b-0942-48b6-9d46-07b11fa987c8",
                    Subtenant = "MFA Legal Name",
                    Email = "boltautomation+4444@boltinc.com"
                },
                GroupMfaAgent = new UserTestData
                {
                    Username = "boltautomation+2226@boltinc.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/mfaahtest/Login/Login",
                    FirstName = "AGENT MFA EN UAT",
                    Email = "boltautomation+2226@boltinc.com"
                },
                GroupNoMfaAgent = new UserTestData
                {
                    Username = "boltautomation+3334@boltinc.com",
                    LoginUrl = "https://sts-boltaccess.bolttest.com/mfaahtest/Login/Login",
                    FirstName = "AGENT MFA DIS UAT",
                    Email = "boltautomation+3334@boltinc.com"
                }
            },

            #endregion
            #region Libertyx
            //LIBERTYX QA Environment
            [(Tenant.LIBERTYX, Environment.Qa)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    UserExternalId = "AUTE",
                    Sso = new SsoUserData
                    {
                        Issuer = "https://libertyx",
                        Audience = "https://sts-qa-libertyx.boltqa.com"
                    }
                },
                Admin = new UserTestData
                {
                    GroupExternalId = "LMOrganization",
                    UserExternalId = "LMGROUPADMIN",
                    Sso = new SsoUserData
                    {
                        Issuer = "https://libertyx",
                        Audience = "https://sts-qa-libertyx.boltqa.com"
                    }
                }
            },

            //LIBERTYX UAT Environment
            [(Tenant.LIBERTYX, Environment.Uat)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    UserExternalId = "AUTE",
                    Sso = new SsoUserData
                    {
                        Issuer = "https://test-lmidp.libertymutual.com",
                        Audience = "https://sts-libertyx.bolttest.com"
                    }
                },
                Admin = new UserTestData
                {
                    GroupExternalId = "TESTLMOrganization",
                    UserExternalId = "LMGROUPADMIN",
                    Sso = new SsoUserData
                    {
                        Issuer = "https://test-lmidp.libertymutual.com",
                        Audience = "https://sts-libertyx.bolttest.com"
                    }
                }
            },

            // LIBERTYX Production Environment
            [(Tenant.LIBERTYX, Environment.Production)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    UserExternalId = "LIB442454084",
                    Role = UserRole.Agent
                }
            },
            #endregion
            #region ProgressivePL
            //PROGRESSIVEPL Dev Environment
            [(Tenant.PROGRESSIVEPL, Environment.Dev)] = new UserTestDataCollection
            {
                Admin = new UserTestData
                {
                    Username = "AmitADMIN@gmail.com",
                    Role = UserRole.Admin,
                    Email = "",
                    SendAgentIdentity = true,
                    Sso = new SsoUserData
                    {
                        Issuer = "",
                        Audience = "",
                    }
                },
                Consumer = new UserTestData
                {
                    Role = UserRole.Consumer
                }
            },

            //PROGRESSIVEPL QA Environment
            [(Tenant.PROGRESSIVEPL, Environment.Qa)] = new UserTestDataCollection
            {
                Admin = new UserTestData
                {
                    Username = "QAauto1@ADMIN.com",
                    UserExternalId = "105199",
                    Role = UserRole.Admin,
                    Email = "",
                    SendAgentIdentity = true,
                    Sso = new SsoUserData
                    {
                        Issuer = "http://dev-pgrlogin.progressive.com/adfs/services/trust",
                        Audience = "https://uat01.ProgressiveBolt.com"
                    }

                },
                Consumer = new UserTestData //for platform api
                {
                    Role = UserRole.Consumer
                },
                DMP = new UserTestData // getquote source - DMP
                {
                    Role = UserRole.Consumer
                },
                DMPHQX = new UserTestData // getquote source - DMPHQX
                {
                    Role = UserRole.Consumer
                },
                Mortgage = new UserTestData // getquote source - Mortgage
                {
                    Role = UserRole.Consumer
                },
                MPQ3 = new UserTestData // getquote source - MPQ3
                {
                    Role = UserRole.Consumer
                },
                MPQ3Renters = new UserTestData // getquote source - MPQ3_Renters
                {
                    Role = UserRole.Consumer
                },
                ProgressiveWebsite = new UserTestData // getquote source - ProgressiveWebsite
                {
                    Role = UserRole.Consumer
                }
            },

            //PROGRESSIVEPL UAT Environment
            [(Tenant.PROGRESSIVEPL, Environment.Uat)] = new UserTestDataCollection
            {
                Admin = new UserTestData
                {
                    Username = "sanityadmin@epos.com",
                    UserExternalId = "A167934",
                    Role = UserRole.Admin,
                    Email = "",
                    SendAgentIdentity = true,
                    Sso = new SsoUserData
                    {
                        Issuer = "https://sts.windows.net/6c7ec574-5904-4d49-bd18-6ab3d432ad2d/",
                        Audience = "https://sts-progressivepl.bolttest.com"
                    }

                },
                Agent = new UserTestData
                {
                    Role = UserRole.Agent
                },
                Consumer = new UserTestData
                {
                    Role = UserRole.Consumer
                }
            },

            // PROGRESSIVEPL Staging Environment
            [(Tenant.PROGRESSIVEPL, Environment.Staging)] = new UserTestDataCollection
            {
                Agent = new UserTestData
                {
                    Username = "automationRep@epos.com",
                    Role = UserRole.Agent
                },
                Admin = new UserTestData
                {
                    Username = "automationAdmin@eops.com",
                    Role = UserRole.Admin
                }
            },

            //PROGRESSIVEPL Production Environment
            [(Tenant.PROGRESSIVEPL, Environment.Production)] = new UserTestDataCollection
            {
                Admin = new UserTestData
                {
                    Username = "talh@boltinc.com",
                    UserExternalId = "b999999",
                    Role = UserRole.Admin,
                    Email = "",
                    SendAgentIdentity = true,
                    Sso = new SsoUserData
                    {
                        Issuer = "https://sts.windows.net/6c7ec574-5904-4d49-bd18-6ab3d432ad2d/",
                        Audience = "https://sts-progressivepl.boltinc.com"
                    }
                },
                Consumer = new UserTestData
                {
                    Role = UserRole.Consumer
                }
            },
            #endregion

        };
    }
}
