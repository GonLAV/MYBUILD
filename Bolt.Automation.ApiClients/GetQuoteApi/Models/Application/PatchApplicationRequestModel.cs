using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class PatchApplicationRequestModel<T>
    {
        public Context? Context { get; set; }
        public T? Data { get; set; }
    }
}
