using Bolt.Automation.Common;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.TestDataProvider.Repositories.DataStores
{
    public static class TestSpecificDataStore
    {
        public static readonly Dictionary<(Tenant?, Environment?), Dictionary<string, string>> All = new()
        {
            // KRAFTLAKEX QA Environment
            [(Tenant.KRAFTLAKEX, Environment.Qa)] = new Dictionary<string, string>
            {
                ["CLQuoteExternalId"] = "6TYL-389B-R05L",
                ["CLLeadId"] = "beb0ffb4-fa95-48ed-8292-450dad077bb4",
                ["CLConsumerExternalId"] = "U883-4W56-HF86",
                ["PLAccountExternalId"] = "BCL4-2LBX-JFDP",
                ["PLCmCaseId"] = "4581195",
                ["CLAccountExternalId"] = "I3KC-HG82-H3EV",
                ["CLCmCaseId"] = "4581203",
                ["MessageCaseExternalId"] = "8QLE-21PS-EC3B",
                ["MessageCaseId"] = "9EB3D889-E6D0-40DD-A9A4-9B65762AB831",
                ["MessageLeadId"] = "7490A31F-329E-4F2D-A992-D7C0B87DACF4",
                ["DeeplinkCaseExternalId"] = "JVU5-SMA7-C412",
                ["DeeplinkCaseLeadId"] = "a431f7df-c2d8-4de7-be2b-ca32286808ab",
                ["PolicyExternalId"] = "GY58-O7N6-L4BN",
                ["PolicyId"] = "46aa1f29-5af4-4621-9853-6bbc68ee62b7",
                ["DeeplinkServiceCaseExternalId"] = "95K2-D7VN-DC63",
                ["DeeplinkServiceCasePolicyId"] = "46aa1f29-5af4-4621-9853-6bbc68ee62b7",
                ["MessageServiceCaseExternalId"] = "B54V-TUHR-LC09",
                ["MessageServiceCaseId"] = "4a126a44-98b2-4556-b91d-12f4bc80c27b"
            },

            // KRAFTLAKEX UAT Environment
            [(Tenant.KRAFTLAKEX, Environment.Uat)] = new Dictionary<string, string>
            {
                ["CLQuoteExternalId"] = "Y0W6-S6SQ-QCBT",
                ["CLLeadId"] = "E3F2203A-DE4F-432F-AD95-ED68ED99FC80",
                ["CLConsumerExternalId"] = "B1CA-PM8O-CD28",
                ["PLAccountExternalId"] = "MF2V-9ILG-R04A",
                ["PLCmCaseId"] = "4351516",
                ["CLAccountExternalId"] = "E6HD-PCDQ-QE3F",
                ["CLCmCaseId"] = "4351517",
                ["MessageCaseExternalId"] = "BFEA-RFY8-KD54",
                ["MessageCaseId"] = "A7503E09-2FC4-4157-B484-272B31E28A46",
                ["MessageLeadId"] = "B5ED5CC0-9810-4369-851C-75BFBE8CC2DF",
                ["DeeplinkCaseExternalId"] = "49XW-DGBS-RDEL",
                ["DeeplinkCaseLeadId"] = "e3f2203a-de4f-432f-ad95-ed68ed99fc80",
                ["DeeplinkServiceCasePolicyId"] = "a16da529-94e4-444a-a40d-be153ad5956e",
                ["PolicyExternalId"] = "0KPD-QPYZ-K57B",
                ["PolicyId"] = "A16DA529-94E4-444A-A40D-BE153AD5956E",
                ["DeeplinkServiceCaseExternalId"] = "FO1Q-5QFZ-L5AM",
                ["DeeplinkServiceCaseLeadId"] = "a16da529-94e4-444a-a40d-be153ad5956e",
                ["MessageServiceCaseExternalId"] = "02RI-N410-D6BM",
                ["MessageServiceCaseId"] = "ED286E9E-BC2A-440F-AA08-FE334CACA6E4"
            },

            // KRAFTLAKEX Development Environment
            [(Tenant.KRAFTLAKEX, Environment.Dev)] = new Dictionary<string, string>
            {
                ["PLQuoteExternalId"] = "DEV-1A2B-3C4D",
                ["CLQuoteExternalId"] = "DEV-9Z8Y-7X6W",
                ["CLConsumerExternalId"] = "DEV1-CONS-5678",
                ["PLAccountExternalId"] = "DEV2-ACCT-1234",
                ["PLCmCaseId"] = "9901001",
                ["CLAccountExternalId"] = "DEV3-ACCT-9876",
                ["CLCmCaseId"] = "9901002"
            },

            // BOLTAG QA Environment
            [(Tenant.BOLTAG, Environment.Qa)] = new Dictionary<string, string>
            {
                ["RenewalsAccountExternalId"] = "OABK-PVN6-SAFO",
                ["EmailTemplateLeadNumber"] = "09242-08T",
                ["SmsTemplateSalesLeadNumber"] = "02116-82H",
                ["SmsTemplateServiceLeadNumber"] = "04512-54D",
                ["SmsServiceCaseNumber"] = "49145-51T",
                ["SmsNotificationLeadNumber"] = "89829-25L",
                ["SmsNotificationLeadId"] = "7328f6bd-ddc5-4b7c-88ce-16ca9ab29f97",
                ["SmsNotificationServiceCaseNumber"] = "34558-79I",
                ["SmsNotificationServiceCaseId"] = "f1e5d8e0-701a-4527-8830-8c9469f13a8f",
                ["SmsFromPhoneNumber"] = "+972528988650",
                ["OpenLeadsQueueIdBoltag"] = "4497b52f-d113-f011-8541-120b9007799f",
            },

            // BOLTAG UAT Environment
            [(Tenant.BOLTAG, Environment.Uat)] = new Dictionary<string, string>
            {
                ["RenewalsAccountExternalId"] = "VVNI-ZSRQ-CBDH",
                ["EmailTemplateLeadNumber"] = "80510-20J",
                ["SmsTemplateSalesLeadNumber"] = "78368-11T",
                ["SmsTemplateServiceLeadNumber"] = "85799-78C",
                ["SmsServiceCaseNumber"] = "06132-40P",
                ["SmsNotificationLeadNumber"] = "07105-73Q",
                ["SmsNotificationLeadId"] = "3d4f0086-6541-41e4-83a0-0b09704bde0a",
                ["SmsNotificationServiceCaseNumber"] = "14966-19P",
                ["SmsNotificationServiceCaseId"] = "378342a7-02fe-49c1-abbb-186895fb7fd5",
                ["SmsFromPhoneNumber"] = "+972528988650",
                ["OpenLeadsQueueIdBoltag"] = "d69a337d-3bff-ef11-84d5-12ed88f27439",
            },

            // BOLTACCESS QA Environment
            [(Tenant.BOLTACCESS, Environment.Qa)] = new Dictionary<string, string>
            {
                ["DeeplinkServiceCaseExternalId"] = "A4XG-AFS3-G26Z",
                ["DeeplinkServiceCasePolicyId"] = "68112c3b-e7a5-48c0-a3ae-4f78e3656012",
                ["PolicyExternalId"] = "AKHB-DVM5-Q9C1",
                ["MessageCaseExternalId"] = "",
                ["MessageLeadId"] = "",
            },

            // BOLTACCESS UAT Environment
            [(Tenant.BOLTACCESS, Environment.Uat)] = new Dictionary<string, string>
            {
                ["DeeplinkServiceCaseExternalId"] = "X5IM-7KX4-J40I",
                ["DeeplinkServiceCasePolicyId"] = "857cceb3-81cf-4496-99bf-01f33515a8c9",
                ["PolicyExternalId"] = "NF9M-Z5N4-IB7E",
            },

            //UNIFY QA Environment
            [(Tenant.UNIFY, Environment.Qa)] = new Dictionary<string, string>
            {
                ["PLQuoteExternalId"] = "5JAG-HQJC-CFC2",
                ["PLQuoteId"] = "55f29ce1-608b-f111-8504-1291380b7509",
                ["PLLeadId"] = "28ce599e-067e-49c3-8de8-93f5539f486c",
            },

            // UNIFY UAT Environment
            [(Tenant.UNIFY, Environment.Uat)] = new Dictionary<string, string>
            {
                ["PLQuoteExternalId"] = "X89F-7I28-D554",
                ["PLQuoteId"] = "b0976762-2e8a-f111-84fd-1209e8972049",
                ["PLLeadId"] = "961d3818-e3ef-4dbd-be20-1418d24f6fc2",
            },
        };

        public static string? GetValue(Tenant? tenant, Environment? environment, string key)
        {
            if (All.TryGetValue((tenant, environment), out var dict) && dict.TryGetValue(key, out var value))
                return value;
            return null;
        }
    }
}