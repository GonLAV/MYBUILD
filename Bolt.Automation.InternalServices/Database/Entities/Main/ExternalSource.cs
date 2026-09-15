using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("ExternalSource")]
    public class ExternalSource
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("DomainPrefix"), NotNull]
        public string DomainPrefix { get; set; } = null!;

        [Column("Name"), NotNull]
        public string Name { get; set; } = null!;

        [Column("AMSAccountExecShortNamePL")]
        public string? AMSAccountExecShortNamePL { get; set; }

        [Column("AMSAccountExecShortNameCL")]
        public string? AMSAccountExecShortNameCL { get; set; }

        [Column("Phone")]
        public string? Phone { get; set; }

        [Column("IsActive"), NotNull]
        public bool IsActive { get; set; }

        [Column("AMSBranch")]
        public string? AMSBranch { get; set; }

        [Column("AMSDepartment")]
        public string? AMSDepartment { get; set; }

        [Column("AMSDivision")]
        public string? AMSDivision { get; set; }

        [Column("IsVisible"), NotNull]
        public bool IsVisible { get; set; }

        [Column("ThemeId")]
        public Guid? ThemeId { get; set; }

        [Column("ThemeKey")]
        public Guid? ThemeKey { get; set; }

        [Column("LogoId")]
        public Guid? LogoId { get; set; }

        [Column("LogoKey")]
        public Guid? LogoKey { get; set; }

        [Column("D2CJourney")]
        public string? D2CJourney { get; set; }

        [Column("RootGroupId")]
        public Guid? RootGroupId { get; set; }

        [Column("InternalId")]
        public string? InternalId { get; set; }

        [Column("ExternalSourceType")]
        public string? ExternalSourceType { get; set; }

        [Column("IsConsumer"), NotNull]
        public bool IsConsumer { get; set; }

        [Column("IsDefault"), NotNull]
        public bool IsDefault { get; set; }

        [Column("AdditionalInformation")]
        public string? AdditionalInformation { get; set; }

        [Column("ContextGroupId")]
        public Guid? ContextGroupId { get; set; }

        [Column("SecretName")]
        public string? SecretName { get; set; }

        #region Associations
        // You can add associations here when needed
        #endregion
    }
}