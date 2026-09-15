using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("Journey")]
    public class Journey
    {
        [Column("Id", IsPrimaryKey = true)] public Guid Id { get; set; }

        [Column("Line"), NotNull] public short Line { get; set; }

        [Column("Lobs"), NotNull] public string Lobs { get; set; } = null!;

        [Column("D2CType"), NotNull] public string D2CType { get; set; } = null!;

        [Column("DateCreated"), NotNull] public DateTime DateCreated { get; set; }

        [Column("DateUpdated"), NotNull] public DateTime DateUpdated { get; set; }

        [Column("Description")] public string? Description { get; set; }

        [Column("RootGroupId")] public Guid? RootGroupId { get; set; }
    }
}