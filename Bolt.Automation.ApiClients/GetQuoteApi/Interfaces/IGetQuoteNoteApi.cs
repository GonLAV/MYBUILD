using Bolt.Automation.ApiClients.GetQuoteApi.Models.Note;
using Refit;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Interfaces
{
    public partial interface IGetQuoteNoteApi
    {
        [Get("/notes/{applicationId}")]
        Task<ApiResponse<List<NoteResponseModel>>> GetNotesByApplicationIdAsync(string applicationId);
    }
}
