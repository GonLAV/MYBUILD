using LinqToDB.Mapping;

#nullable enable

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("Case")]
    public class Case
    {
        [Column("Id", IsPrimaryKey = true)] public Guid Id { get; set; } // uniqueidentifier
        [Column("TStamp")] public byte[] TStamp { get; set; } = null!; // timestamp
        [Column("DateCreated")] public DateTime DateCreated { get; set; } // datetime
        [Column("DateUpdated")] public DateTime DateUpdated { get; set; } // datetime
        [Column("Tenant")] public string? Tenant { get; set; } // varchar(100)
        [Column("EntityData")] public string? EntityData { get; set; } // xml
        [Column("IsActive")] public bool IsActive { get; set; } // bit
        [Column("EX_CaseNumber")] public string? EX_CaseNumber { get; set; } // nvarchar(15)
        [Column("EX_Status")] public string? EX_Status { get; set; } // nvarchar(30)
        [Column("CreatedByUserId")] public Guid? CreatedByUserId { get; set; } // uniqueidentifier
        [Column("UpdatedByUserId")] public Guid? UpdatedByUserId { get; set; } // uniqueidentifier
        [Column("AssignedToUserId")] public Guid? AssignedToUserId { get; set; } // uniqueidentifier
        [Column("CreatedByGroupId")] public Guid? CreatedByGroupId { get; set; } // uniqueidentifier
        [Column("OwnedByGroupId")] public Guid? OwnedByGroupId { get; set; } // uniqueidentifier
        [Column("IsSLAMet")] public bool? IsSLAMet { get; set; } // bit
        [Column("BusinessFlowStatus")] public string? BusinessFlowStatus { get; set; } // varchar(50)
        [Column("DueDate")] public DateTime? DueDate { get; set; } // datetime
        [Column("Product")] public string? Product { get; set; } // varchar(250)
        [Column("BusinessType")] public string? BusinessType { get; set; } // varchar(30)
        [Column("Originated")] public string? Originated { get; set; } // varchar(30)
        [Column("NeedHelp")] public bool? NeedHelp { get; set; } // bit
        [Column("Source")] public string? Source { get; set; } // varchar(50)
        [Column("CaseType")] public string? CaseType { get; set; } // varchar(50)
        [Column("SubType")] public string? SubType { get; set; } // varchar(30)
        [Column("Severity")] public string? Severity { get; set; } // varchar(30)
        [Column("ChangeReason")] public string? ChangeReason { get; set; } // varchar(50)
        [Column("AddressTimeZone")] public string? AddressTimeZone { get; set; } // varchar(15)
        [Column("SubCaseData")] public string? SubCaseData { get; set; } // nvarchar(max)
        [Column("SubCaseType")] public string? SubCaseType { get; set; } // varchar(50)
        [Column("ConsumerId")] public Guid? ConsumerId { get; set; } // uniqueidentifier
        [Column("ApplicationId")] public Guid? ApplicationId { get; set; } // uniqueidentifier
        [Column("PolicyBinderId")] public Guid? PolicyBinderId { get; set; } // uniqueidentifier
        [Column("ExternalId")] public string? ExternalId { get; set; } // varchar(14)
        [Column("IsSubmitted")] public bool? IsSubmitted { get; set; } // bit
        [Column("CmCaseId")] public string? CmCaseId { get; set; } // nvarchar(250)
        [Column("Bundle")] public bool Bundle { get; set; } // bit

        // #region Associations
        // /// <summary>
        // /// FK_Case_ConsumerId_Consumer
        // /// </summary>
        // [Association(ThisKey = nameof(ConsumerId), OtherKey = nameof(Consumer.Id))]
        // public Consumer? Consumer { get; set; }
        //
        // /// <summary>
        // /// FK_Case_ApplicationId_Policy
        // /// </summary>
        // [Association(ThisKey = nameof(ApplicationId), OtherKey = nameof(Policy.Id))]
        // public Policy? Application { get; set; }
        //
        // /// <summary>
        // /// FK_Case_PolicyBinderId_PolicyBinder
        // /// </summary>
        // [Association(ThisKey = nameof(PolicyBinderId), OtherKey = nameof(PolicyBinder.Id))]
        // public PolicyBinder? PolicyBinder { get; set; }
        // #endregion
    }
}