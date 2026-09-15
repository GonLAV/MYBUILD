
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.CreateOrganizationLetter
{
    public class CreateOrganizationLetterRequest
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime DateSent { get; set; }
        public bool SendLetterNotification { get; set; } = true;
    }
}
