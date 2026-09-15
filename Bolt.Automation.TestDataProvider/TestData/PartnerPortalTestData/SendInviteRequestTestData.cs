using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common;
using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Invite;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;

namespace Bolt.Automation.TestDataProvider.TestData.PartnerPortalTestData
{
    public static class SendInviteRequestTestData
    {

        public static readonly JourneyDto CLCommercialJourney = new()
        {
            Id = Guid.Parse("35f519ad-9d7b-4154-b489-08db17e64e2a"),
            Line = 0,
            Lobs = "BOP",
            D2CType = "Bolt consumer",
            Description = "BOP",
            RootGroupId = null
        };

        public static readonly JourneyDto PLCommercialJourney = new()
        {
            Id = Guid.Parse("5084d257-c1ea-4547-e0d0-08dbc037f960"),
            Line = 0,
            Lobs = "PersonalHome",
            D2CType = "Internal",
            Description = "PersonalHome",
            RootGroupId = null
        };


        public static readonly SendInviteRequest SendCLInviteRequest = new()
        {
            FirstName = NameSelector.GetFirstName(),
            LastName = NameSelector.GetLastName(),
            PrimaryPhoneNumber = 52 + RandomManager.GetRandomDigits(8),
            Email = "boltautomation@boltinc.com",
            PropertyAddress = AddressData.IL,
            IAgreeToReceiveEmailsByBolt = true,
            PersonalLine = new List<JourneyDto> { },
            CommercialLine = new List<JourneyDto> { CLCommercialJourney },
            LogoHeight = "1577",
            LogoWidth = "4659",
            SendAgentEmailCopy = false
        };

        public static readonly SendInviteRequest SendPLInviteRequest = new()
        {
            FirstName = NameSelector.GetFirstName(),
            LastName = NameSelector.GetLastName(),
            PrimaryPhoneNumber = 52 + RandomManager.GetRandomDigits(8),
            Email = "boltautomation@boltinc.com",
            PropertyAddress = AddressData.IL,
            IAgreeToReceiveEmailsByBolt = true,
            PersonalLine = new List<JourneyDto> { PLCommercialJourney },
            CommercialLine = new List<JourneyDto> { },
            LogoHeight = "1577",
            LogoWidth = "4659",
            SendAgentEmailCopy = false
        };

    }
}
