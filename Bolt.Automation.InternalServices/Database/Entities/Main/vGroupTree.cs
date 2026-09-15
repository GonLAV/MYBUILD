using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("vGroupTree")]
    public class vGroupTree
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("GroupName")]
        public string GroupName { get; set; } = null!;

        [Column("GRP_PATH")]
        public string GRP_PATH { get; set; } = null!;

        [Column("LEVEL_ID")]
        public int LEVEL_ID { get; set; }

        [Column("GRP_PATH_KEYS")]
        public string GRP_PATH_KEYS { get; set; } = null!;

        [Column("GroupType")]
        public string GroupType { get; set; } = null!;

        [Column("HierarchyType")]
        public string HierarchyType { get; set; } = null!;
    }
}
