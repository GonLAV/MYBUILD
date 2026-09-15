
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.License.Delete
{
    public class DeleteLicensesResponse
    {
        public List<DeleteLicensesResultItemModel> Result { get; set; }
    }

    public class DeleteLicensesResultItemModel
    {
        public bool Success { get; set; }
        public string LicenseExternalId { get; set; }
    }
}
