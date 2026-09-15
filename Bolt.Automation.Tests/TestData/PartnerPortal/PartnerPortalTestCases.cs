using Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData;
using NUnit.Framework;

namespace Bolt.Automation.Tests.TestData.PartnerPortal
{
    public static class PartnerPortalTestCases
    {
        public static IEnumerable<TestCaseData> ClickableActivities
        {
            get
            {
                yield return new TestCaseData("Referred", PartnerPortal_FieldNames.ReferralsButton)
                    .SetProperty("TestCaseId", "231602");
                yield return new TestCaseData("Quoted", PartnerPortal_FieldNames.QuotesButton)
                    .SetProperty("TestCaseId", "231603");
                yield return new TestCaseData("Issued", PartnerPortal_FieldNames.PoliciesButton)
                  .SetProperty("TestCaseId", "231604");
            }
        }
    }
}
