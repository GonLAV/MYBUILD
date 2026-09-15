using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("Users")]
    public class Users
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("UserName"), NotNull]
        public string UserName { get; set; } = null!;

        [Column("FirstName"), NotNull]
        public string FirstName { get; set; } = null!;

        [Column("MiddleName")]
        public string? MiddleName { get; set; }

        [Column("LastName"), NotNull]
        public string LastName { get; set; } = null!;

        [Column("CrmId")]
        public Guid? CrmId { get; set; }

        [Column("Email"), NotNull]
        public string Email { get; set; } = null!;

        [Column("ExternalId"), NotNull]
        public string ExternalId { get; set; } = null!;

        [Column("Status"), NotNull]
        public string Status { get; set; } = null!;

        [Column("Password")]
        public byte[]? Password { get; set; }

        [Column("MemberNumber")]
        public string? MemberNumber { get; set; }

        [Column("UserType"), NotNull]
        public string UserType { get; set; } = null!;

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

        [Column("SourceKeyword"), NotNull]
        public string SourceKeyword { get; set; } = null!;

        [Column("CrmRepShortName")]
        public string? CrmRepShortName { get; set; }

        [Column("CrmExecShortName")]
        public string? CrmExecShortName { get; set; }

        [Column("DateDeactivated")]
        public DateTime? DateDeactivated { get; set; }

        [Column("CmUserId")]
        public string? CmUserId { get; set; }

        #region Associations
        // You can add associations here if needed
        #endregion
    }
}