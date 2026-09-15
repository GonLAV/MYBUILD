using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("Groups")]
    public class Groups
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("Name"), NotNull]
        public string Name { get; set; } = null!;

        [Column("Type"), NotNull]
        public Guid Type { get; set; }

        [Column("ExternalId")]
        public string? ExternalId { get; set; }

        [Column("AdditionalDataXml"), NotNull]
        public string AdditionalDataXml { get; set; } = null!;

        [Column("UserType"), NotNull]
        public string UserType { get; set; } = null!;

        [Column("DateCreated"), NotNull]
        public DateTime DateCreated { get; set; }

        [Column("DateUpdated"), NotNull]
        public DateTime DateUpdated { get; set; }

        [Column("Tenant"), NotNull]
        public string Tenant { get; set; } = null!;

        [Column("IsActive"), NotNull]
        public bool IsActive { get; set; }

        [Column("SequenceId"), NotNull]
        public string SequenceId { get; set; } = null!;

        [Column("CrmRepShortName"), NotNull]
        public string CrmRepShortName { get; set; } = null!;

        [Column("IsVisible"), NotNull]
        public string IsVisible { get; set; } = null!;

        [Column("ExcludeFromAgencyAppetite"), NotNull]
        public bool ExcludeFromAgencyAppetite { get; set; }

        [Column("AdditionalInformation"), NotNull]
        public string AdditionalInformation { get; set; } = null!;
    }
}
