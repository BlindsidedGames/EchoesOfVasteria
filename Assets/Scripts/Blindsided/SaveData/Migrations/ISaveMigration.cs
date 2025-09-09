using Blindsided.SaveData;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>
    /// Contract for a single, idempotent save migration.
    /// - Version migrations run when TargetVersion is within (from, to].
    /// - Schema migrations run when TargetSchema is greater than current SchemaVersion.
    /// Implementations must be idempotent and validate data before changing it.
    /// </summary>
    public interface ISaveMigration
    {
        /// <summary>
        /// Optional target schema. If set and data.SchemaVersion &lt; TargetSchema, Apply will run.
        /// </summary>
        int? TargetSchema { get; }

        /// <summary>
        /// Optional target version gate in dotted number form (e.g., "0.1.0").
        /// If not null/empty, migration runs when fromVersion &lt; TargetVersion &lt;= toVersion.
        /// </summary>
        string TargetVersion { get; }

        /// <summary>
        /// Human-friendly ID for logging.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Perform the migration in-place on GameData. Must be idempotent.
        /// </summary>
        void Apply(GameData data);
    }
}

