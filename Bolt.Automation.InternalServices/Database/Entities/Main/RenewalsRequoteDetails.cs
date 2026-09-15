using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("RenewalsRequoteDetails")]
    public class RenewalsRequoteDetails
    {
        [Column("Id"), PrimaryKey] public Guid Id { get; set; }
        [Column("PolicyBinderId")] public Guid PolicyBinderId { get; set; }
        [Column("OriginalQuoteId")] public Guid OriginalQuoteId { get; set; }
        [Column("NewQuoteId")] public Guid NewQuoteId { get; set; }
        [Column("Status")] public string? Status { get; set; }
    }
}
