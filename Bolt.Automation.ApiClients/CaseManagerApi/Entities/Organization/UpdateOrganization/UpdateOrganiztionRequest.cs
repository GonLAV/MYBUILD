
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.UpdateOrganization
{
    public class UpdateOrganiztionRequest
    {
        public string AgencyPrincipalFirstName { get; set; }
        public string AgencyPrincipalLastName { get; set; }
        public string AgencyName { get; set; }
        public string AgencyPhoneNumber { get; set; }
        public string AgencyMailingAddress { get; set; }
        public string AgencyCity { get; set; }
        public string AgencyZipCode { get; set; }
        public string AgencyState { get; set; }
        public string AgencyEmailAddress { get; set; }
        public string EzLynxUserName { get; set; }
        public string Status { get; set; }
        public bool BookTransferred { get; set; }
        public bool SurplusAllowed { get; set; }
    }
}
