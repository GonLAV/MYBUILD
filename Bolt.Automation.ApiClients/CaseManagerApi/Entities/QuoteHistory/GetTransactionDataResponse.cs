
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.QuoteHistory
{
    public class GetTransactionDataResponse
    {
        public Guid TransactionId { get; set; }
        public QouteTransactionDataModel Results { get; set; }

        public GetTransactionDataResponse()
        {
            Results = new QouteTransactionDataModel();
        }

        public class QouteTransactionDataModel
        {
            public string TransactionDataJson { get; set; }
            public string TransactionDataHex { get; set; }
            public string TransactionDataFormat { get; set; }
        }
    }
}
