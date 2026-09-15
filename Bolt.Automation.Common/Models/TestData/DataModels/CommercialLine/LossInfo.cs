namespace Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine
{
    public partial class CommercialLineData : BaseLineData
    {
        public bool? PLHaveAnyLosses { get; set; }
        public List<CommercialLoss>? Losses { get; set; }
    }

    public class CommercialLoss
    {
        public Guid Id { get; set; }
        public int SequenceNum { get; set; }
        public string? LossDate { get; set; }
        public string? TotalPaidAmount { get; set; }

        /// <summary>
        /// Commercial loss type, e.g. WC_IncidentOnly, WC_LostTime, WC_MedOnly.
        /// </summary>
        public string? LossType { get; set; }
    }
}
