using LinqToDB.Mapping;

namespace Bolt.Automation.InternalServices.Database.Entities.Main
{
    [Table("Provisioning_Action")]
    public class ProvisioningAction
    {
        [Column("Id", IsPrimaryKey = true)] public Guid Id { get; set; }

        [Column("Action")] public string? Action { get; set; }

        [Column("EntityId"), NotNull] public Guid EntityId { get; set; }

        [Column("EntityType"), NotNull] public string EntityType { get; set; } = null!;

        [Column("Category"), NotNull] public string Category { get; set; } = null!;

        [Column("EntityData")] public string? EntityData { get; set; }

        [Column("DateUpdated"), NotNull] public DateTime DateUpdated { get; set; }

        [Column("Status"), NotNull] public string Status { get; set; } = null!;

        [Column("Attempts")] public int? Attempts { get; set; }

        [Column("NextAttemptDate"), NotNull] public DateTime NextAttemptDate { get; set; }

        [Column("RetryData")] public string? RetryData { get; set; }
    }
}