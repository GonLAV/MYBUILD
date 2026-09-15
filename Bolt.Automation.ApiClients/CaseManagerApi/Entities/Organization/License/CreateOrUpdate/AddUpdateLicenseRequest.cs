
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.License.CreateOrUpdate
{
    public class AddUpdateLicenseRequest
    {
        public string OperationType { get; set; }
        public string LicenseExternalId { get; set; }
        public string LicenseType { get; set; }
        public string ResidentLicenseType { get; set; }
        public DateTime LicenseExpirationDate { get; set; }
        public string LicenseNumber { get; set; }
        public string LicenseState { get; set; }
        public string LicenseStatus { get; set; }
        public string LicenseHolder { get; set; }
    }
}
