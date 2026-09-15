using Bolt.Automation.ApiClients.AdbxApi.Entities.Case;

namespace Bolt.Automation.TestDataProvider.Providers.AdbxApiDataProvider
{
    public class CaseViewDataProvider
    {
        public static CreateCaseViewPayload GetCreateCaseViewPayload()
        {
            return new CreateCaseViewPayload
            {
                Referred = "autotest1",
                Clauses = new List<CaseViewQueryElement>
                {
                    new CaseViewQueryElement
                    {
                        Type = "condition",
                        FieldType = "caseStatus",
                        Operator = "eq",
                        Value = new
                        {
                            name = "Open",
                            value = "Open"
                        }
                    },
                    new CaseViewQueryElement
                    {
                        Type = "condition",
                        FieldType = "caseAssignedTo",
                        Operator = "eq",
                        Value = new
                        {
                            name = "",
                            value = ""
                        }
                    },
                    new CaseViewQueryElement
                    {
                        Type = "condition",
                        FieldType = "caseSeverity",
                        Operator = "eq",
                        Value = new
                        {
                            name = "Low",
                            value = "Low"
                        }
                    }
                },
                SaveQueryFor = "myview"
            };
        }
    }
}
