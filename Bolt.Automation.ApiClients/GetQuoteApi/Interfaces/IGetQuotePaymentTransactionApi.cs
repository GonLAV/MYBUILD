using Bolt.Automation.ApiClients.GetQuoteApi.Models.PaymentTransaction;
using Refit;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Interfaces
{
    public partial interface IGetQuotePaymentTransactionApi
    {
        [Get("/paymenttransaction/{paymenttransactionid}")]
        Task<ApiResponse<PaymentTransactionResponseModel>> GetPaymentTransactionAsync(string paymenttransactionid);
    }
}
