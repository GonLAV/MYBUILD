
using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("GroupRelations")]
    public class GroupRelations
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("SourceId"), NotNull]
        public Guid SourceId { get; set; }

        [Column("DestinationId"), NotNull]
        public Guid DestinationId { get; set; }

        [Column("RelationType")]
        public int? RelationType { get; set; }
    }
}
