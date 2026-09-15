using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
	[Table("Policy")]
	public class Policy
	{
		[Column("Id"                         , IsPrimaryKey = true )] public Guid     Id                          { get; set; } // uniqueidentifier
		[Column("DateCreated"                                      )] public DateTime DateCreated                 { get; set; } // datetime
		[Column("DateUpdated"                                      )] public DateTime DateUpdated                 { get; set; } // datetime
		[Column("ConsumerId"                                       )] public Guid     ConsumerId                  { get; set; } // uniqueidentifier
		[Column("PolicyData"                 , CanBeNull    = false)] public string   PolicyData                  { get; set; } = null!; // xml
		[Column("IsActive"                                         )] public bool     IsActive                    { get; set; } // bit
		[Column("CurrentStage"                                     )] public short    CurrentStage                { get; set; } // smallint
		[Column("Partner"                                          )] public short    Partner                     { get; set; } // smallint
		[Column("FriendlyId"                 , CanBeNull    = false)] public string   FriendlyId                  { get; set; } = null!; // varchar(15)
		[Column("DateExpired"                                      )] public DateTime DateExpired                 { get; set; } // datetime
		[Column("BundleCart"                                       )] public bool?    BundleCart                  { get; set; } // bit
		[Column("CartApproved"                                     )] public bool     CartApproved                { get; set; } // bit
		[Column("LastVisitedStage"                                 )] public short    LastVisitedStage            { get; set; } // smallint
		[Column("Purchased"                                        )] public bool     Purchased                   { get; set; } // bit
		[Column("SourceMedia"                                      )] public short    SourceMedia                 { get; set; } // smallint
		[Column("SourceKeyword"                                    )] public string?  SourceKeyword               { get; set; } // nvarchar(50)
		[Column("RemindersAdded"                                   )] public bool     RemindersAdded              { get; set; } // bit
		[Column("SourceIP"                                         )] public string?  SourceIp                    { get; set; } // varchar(40)
		[Column("CurrentFlow"                                      )] public short    CurrentFlow                 { get; set; } // smallint
		[Column("GroupId"                                          )] public Guid?    GroupId                     { get; set; } // uniqueidentifier
		[Column("QuoteType"                                        )] public short    QuoteType                   { get; set; } // smallint
		[Column("CurrentQuoteStage"                                )] public string?  CurrentQuoteStage           { get; set; } // nvarchar(50)
		[Column("CurrentQuoteMaxVisitedStage"                      )] public string?  CurrentQuoteMaxVisitedStage { get; set; } // nvarchar(50)
		[Column("ExternalId"                                       )] public string?  ExternalId                  { get; set; } // varchar(14)
		[Column("DeviceIndicator"                                  )] public string?  DeviceIndicator             { get; set; } // nvarchar(50)
		[Column("IsBridged"                                        )] public bool?    IsBridged                   { get; set; } // bit
		[Column("CreatedByGroupId"                                 )] public Guid?    CreatedByGroupId            { get; set; } // uniqueidentifier
		[Column("OwnedByGroupId"                                   )] public Guid?    OwnedByGroupId              { get; set; } // uniqueidentifier
		[Column("CreatedByUserId"                                  )] public Guid?    CreatedByUserId             { get; set; } // uniqueidentifier
		[Column("AssignedToUserId"                                 )] public Guid?    AssignedToUserId            { get; set; } // uniqueidentifier
		[Column("UpdatedByUserId"                                  )] public Guid?    UpdatedByUserId             { get; set; } // uniqueidentifier
		[Column("Tenant"                     , CanBeNull    = false)] public string   Tenant                      { get; set; } = null!; // varchar(100)
		[Column("LeadId"                                           )] public Guid?    LeadId                      { get; set; } // uniqueidentifier
		[Column("IsCompleted"                                      )] public bool     IsCompleted                 { get; set; } // bit

	}
}
