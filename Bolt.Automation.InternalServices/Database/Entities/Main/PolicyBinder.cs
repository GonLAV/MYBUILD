using LinqToDB.Mapping;

#nullable enable

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("PolicyBinder")]
    public class PolicyBinder
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("TStamp"), NotNull]
        public byte[] TStamp { get; set; } = null!; // timestamp

        [Column("DateCreated"), NotNull]
        public DateTime DateCreated { get; set; }

        [Column("DateUpdated"), NotNull]
        public DateTime DateUpdated { get; set; }

        [Column("Tenant")]
        public string? Tenant { get; set; }

        [Column("EntityData"), NotNull]
        public string EntityData { get; set; } = null!; // xml

        [Column("IsActive"), NotNull]
        public bool IsActive { get; set; }

        [Column("EX_PolicyNumber")]
        public string? EX_PolicyNumber { get; set; }

        [Column("EX_EffectiveDate")]
        public DateTime? EX_EffectiveDate { get; set; }

        [Column("EX_AccountId")]
        public Guid? EX_AccountId { get; set; }

        [Column("PriorPolicyNumber")]
        public string? PriorPolicyNumber { get; set; }

        [Column("PriorPolicyId")]
        public Guid? PriorPolicyId { get; set; }

        [Column("ExternalId")]
        public string? ExternalId { get; set; }

        [Column("EX_ApplicationId")]
        public Guid? EX_ApplicationId { get; set; }

        [Column("CreatedByGroupId")]
        public Guid? CreatedByGroupId { get; set; }

        [Column("OwnedByGroupId")]
        public Guid? OwnedByGroupId { get; set; }

        [Column("CreatedByUserId")]
        public Guid? CreatedByUserId { get; set; }

        [Column("AssignedToUserId")]
        public Guid? AssignedToUserId { get; set; }

        [Column("UpdatedByUserId")]
        public Guid? UpdatedByUserId { get; set; }

        [Column("ResultDataId")]
        public Guid? ResultDataId { get; set; }

        [Column("AdditionalData")]
        public string? AdditionalData { get; set; }

        #region Associations
        // Inverse association to Case
        [Association(ThisKey = nameof(Id), OtherKey = nameof(Case.PolicyBinderId))]
        public IEnumerable<Case>? Cases { get; set; }
        #endregion
    }
}