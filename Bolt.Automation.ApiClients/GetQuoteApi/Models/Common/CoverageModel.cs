
namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Common
{
    public class CoverageModel
    {
        public IEnumerable<CoverageEntry>? PolicyLevelCoverages { get; set; }
        public Dictionary<string, Dictionary<string, IEnumerable<CoverageEntry>>>? EntityCoverages { get; set; }
    }
}
