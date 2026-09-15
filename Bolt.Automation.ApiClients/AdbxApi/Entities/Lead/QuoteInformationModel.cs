
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Lead
{
    public class QuoteInformationModel
    {
        public Guid QuoteId { get; set; }
        public string? InsuredName { get; set; }
        public string? Product { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }
}
