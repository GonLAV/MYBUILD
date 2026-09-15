
namespace Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine
{
    public partial class CommercialLineData : BaseLineData
    {
        public string? WCWhoIsCovered { get; set; }
        public string? CurrentPremiumWC { get; set; }
        public string? CurrentWCCarrier { get; set; }
        public bool? DeclinedCanceledOrNonRenewed { get; set; }
        public bool WCContinousCoverage { get; set; }
        public string? WcExpirationDate { get; set; }
        public bool? CreditCheckPermission { get; set; }
    }
}
