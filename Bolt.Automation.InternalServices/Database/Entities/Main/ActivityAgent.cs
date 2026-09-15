using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
	[Table("ActivityAgent")]
	public class ActivityAgent
	{
		[Column("ActivityID"   , IsPrimaryKey = true)] public Guid      ActivityId    { get; set; } // uniqueidentifier
		[Column("CreatedBy"                         )] public Guid      CreatedBy     { get; set; } // uniqueidentifier
		[Column("AssignedTo"                        )] public Guid      AssignedTo    { get; set; } // uniqueidentifier
		[Column("DateCreated"                       )] public DateTime  DateCreated   { get; set; } // datetime
		[Column("PolicyId"                          )] public Guid?     PolicyId      { get; set; } // uniqueidentifier
		[Column("Carrier"                           )] public string?   Carrier       { get; set; } // nvarchar(50)
		[Column("ActivityCrmId"                     )] public Guid?     ActivityCrmId { get; set; } // uniqueidentifier
		[Column("DateUpdated"                       )] public DateTime? DateUpdated   { get; set; } // datetime
		[Column("DBAction"                          )] public string?   DbAction      { get; set; } // varchar(35)
		[Column("CommentTran"                       )] public string?   CommentTran   { get; set; } // varchar(max)
		[Column("EffDate"                           )] public DateTime? EffDate       { get; set; } // datetime
		[Column("ExpDate"                           )] public DateTime? ExpDate       { get; set; } // datetime
		[Column("ConsumerId"                        )] public Guid?     ConsumerId    { get; set; } // uniqueidentifier
	}
}
