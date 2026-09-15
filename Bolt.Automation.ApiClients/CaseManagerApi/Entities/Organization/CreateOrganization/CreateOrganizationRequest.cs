
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.CreateOrganization
{
    public class CreateOrganizationRequest
    {
        public string AgencyType { get; set; }
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
        public string PaymentCustomerId { get; set; }
        public string ProducerCode { get; set; }
        public string SubscriptionName { get; set; }
        public string SubscriptionNameAdditionalInfo { get; set; }
        public string SubscriptionTerm { get; set; }
    }
}
