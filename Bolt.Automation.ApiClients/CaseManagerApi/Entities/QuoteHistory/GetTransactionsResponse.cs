namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.QuoteHistory
{
    public class GetTransactionsResponse
    {
        public string CaseExternalId { get; set; }
        public List<QouteTransactionHistoryModel> Results { get; set; }

        public GetTransactionsResponse()
        {
            Results = [];

        }

        public class QouteTransactionHistoryModel
        {
            public Guid TransactionId { get; set; }
            public string TransactionDate { get; set; }
        }

    }


}
