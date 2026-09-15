using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common;
using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Invite
{
    public class SendInviteRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PrimaryPhoneNumber { get; set; }
        public string Email { get; set; }
        public Address PropertyAddress { get; set; }
        public bool IAgreeToReceiveEmailsByBolt { get; set; }
        public List<JourneyDto> PersonalLine { get; set; }
        public List<JourneyDto> CommercialLine { get; set; }
        public string LogoHeight { get; set; }
        public string LogoWidth { get; set; }
        public bool SendAgentEmailCopy { get; set; }
    }
}
