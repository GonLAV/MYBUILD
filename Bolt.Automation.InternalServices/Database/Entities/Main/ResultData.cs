using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("ResultData")]
    public class ResultData
    {
        [Column("Id", IsPrimaryKey = true)] public Guid Id { get; set; }

        [Column("PolicyId"), NotNull] public Guid PolicyId { get; set; }

        [Column("Carrier"), NotNull] public short Carrier { get; set; }

        [Column("Lob"), NotNull] public short Lob { get; set; }

        [Column("PackageType"), NotNull] public short PackageType { get; set; }

        [Column("Premium"), NotNull] public decimal Premium { get; set; }

        [Column("DateCreated"), NotNull] public DateTime DateCreated { get; set; }

        [Column("IsActive"), NotNull] public bool IsActive { get; set; }

        [Column("Status"), NotNull] public short Status { get; set; }

        [Column("Source")] public short? Source { get; set; }

        [Column("IsPublished")] public bool? IsPublished { get; set; }

        [Column("QuoteNumber")] public string? QuoteNumber { get; set; }

        [Column("CacheKey")] public string? CacheKey { get; set; }

        [Column("ResultDataSnapshotId"), NotNull]
        public Guid ResultDataSnapshotId { get; set; }

        [Column("IsBind")] public bool? IsBind { get; set; }

        [Column("Term"), NotNull] public short Term { get; set; }

        [Column("IsBlocked"), NotNull] public bool IsBlocked { get; set; }

        [Column("PremiumRecievedDate")] public DateTime? PremiumRecievedDate { get; set; }

        [Column("IsNonAdmitted"), NotNull] public bool IsNonAdmitted { get; set; }

        [Column("IsBridged"), NotNull] public bool IsBridged { get; set; }

        [Column("CredentialSource")] public short? CredentialSource { get; set; }

        [Column("Type"), NotNull] public short Type { get; set; }

        [Column("ResultAdditionalDetails")] public string? ResultAdditionalDetails { get; set; }

        [Column("ExternalId")] public string? ExternalId { get; set; }

        [Column("DateUpdated"), NotNull] public DateTime DateUpdated { get; set; }

        [Column("Selected")] public bool? Selected { get; set; }

        [Column("SelectedForExternalBind")] public bool? SelectedForExternalBind { get; set; }

        [Column("Discount")] public decimal? Discount { get; set; }

        [Column("IsBasedOnCache"), NotNull] public bool IsBasedOnCache { get; set; }

        [Column("IsBundle"), NotNull] public bool IsBundle { get; set; }


        [Table("ResultAttachment")]
        public class ResultAttachment
        {
            [Column("Id"), PrimaryKey] public Guid Id { get; set; }
            [Column("ResultId"), NotNull] public Guid ResultId { get; set; }
            [Column("AttachmentType"), NotNull] public int AttachmentType { get; set; }
        }

        [Table("ResultAttachmentContent")]
        public class ResultAttachmentContent
        {
            [Column("Id"), PrimaryKey] public Guid Id { get; set; }
            [Column("ResultAttachmentId"), NotNull] public Guid ResultAttachmentId { get; set; }
            [Column("TextAttachment")] public string? TextAttachment { get; set; }
            [Column("BinaryAttachment")] public byte[]? BinaryAttachment { get; set; }
        }
    }
}