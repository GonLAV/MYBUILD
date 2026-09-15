namespace Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common
{
    public class JourneyDto
    {
        public Guid Id { get; set; }

        public short Line { get; set; }

        public string Lobs { get; set; }

        public string D2CType { get; set; }

        public string Description { get; set; }

        public Guid? RootGroupId { get; set; }
    }
}
