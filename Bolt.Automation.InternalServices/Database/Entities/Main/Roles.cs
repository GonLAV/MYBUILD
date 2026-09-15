
using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("Roles")]
    public class Roles
    {
        [Column("Id", IsPrimaryKey = true)]
        public Guid Id { get; set; }

        [Column("Name"), NotNull]
        public string Name { get; set; } = null!;

        [Column("Description"), NotNull]
        public string Description { get; set; } = null!;

        [Column("Status")]
        public string? Status { get; set; }

        [Column("IsSystem"), NotNull]
        public bool IsSystem { get; set; }
    }
}
