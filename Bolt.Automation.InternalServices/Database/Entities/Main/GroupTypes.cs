
using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("GroupTypes")]
    public class GroupTypes
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("Name"), NotNull]
        public string Name { get; set; } = null!;

        [Column("AdditionalData"), NotNull]
        public string AdditionalData { get; set; } = null!;

        [Column("HierarchyType")]
        public string? HierarchyType { get; set; }
    }
}
