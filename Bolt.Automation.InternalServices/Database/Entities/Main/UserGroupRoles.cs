using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("UserGroupRoles")]
    public class UserGroupRoles
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("UserId"), NotNull]
        public Guid UserId { get; set; }

        [Column("GroupId"), NotNull]
        public Guid GroupId { get; set; }

        [Column("RoleId")]
        public Guid RoleId { get; set; }

        [Column("AdditionalDataXml"), NotNull]
        public string AdditionalDataXml { get; set; } = null!;

        [Column("DateCreated"), NotNull]
        public DateTime DateCreated { get; set; }

        [Column("DateUpdated"), NotNull]
        public DateTime DateUpdated { get; set; }

        [Column("Tenant"), NotNull]
        public string Tenant { get; set; } = null!;

        [Column("IsActive"), NotNull]
        public bool IsActive { get; set; }

        [Column("LinkType"), NotNull]
        public string LinkType { get; set; } = null!;
    }
}
