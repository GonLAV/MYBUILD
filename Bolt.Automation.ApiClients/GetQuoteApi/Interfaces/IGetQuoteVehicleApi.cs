using Bolt.Automation.ApiClients.GetQuoteApi.Models.Vehicles;
using Refit;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Interfaces
{
    public partial interface IGetQuoteVehicleApi
    {
        [Get("/vehicles/recreational/makes/{year}")]
        Task<ApiResponse<List<string>>> GetRVMakesAsync(string year);

        [Get("/vehicles/recreational/models/{year}/{make}")]
        Task<ApiResponse<List<string>>> GetRVModelsAsync(string year, string make);

        [Get("/vehicles/recreational/details/{year}/{make}/{model}")]
        Task<ApiResponse<List<RVDetail>>> GetRVDetailsAsync(string year, string make, string model);
    }
}
