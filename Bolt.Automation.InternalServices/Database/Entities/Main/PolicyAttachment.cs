
using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
	[Table("PolicyAttachment")]
	public class PolicyAttachment
	{
		[Column("PolicyId"                            )] public Guid     PolicyId       { get; set; } // uniqueidentifier
		[Column("AttachmentType"                      )] public short    AttachmentType { get; set; } // smallint
		[Column("Content"       , CanBeNull    = false)] public string   Content        { get; set; } = null!; // xml
		[Column("datecreated"                         )] public DateTime Datecreated    { get; set; } // datetime
		[Column("Id"            , IsPrimaryKey = true )] public Guid     Id             { get; set; } // uniqueidentifier
		[Column("IsActive"                            )] public bool     IsActive       { get; set; } // bit
		[Column("DateUpdated"                         )] public DateTime DateUpdated    { get; set; } // datetime

		// #region Associations
		// /// <summary>
		// /// FK_PolicyAttachment_PolicyId_Policy
		// /// </summary>
		// [Association(CanBeNull = false, ThisKey = nameof(PolicyId), OtherKey = nameof(DataModel.Policy.Id))]
		// public Policy Policy { get; set; } = null!;
		// #endregion
	}
}
