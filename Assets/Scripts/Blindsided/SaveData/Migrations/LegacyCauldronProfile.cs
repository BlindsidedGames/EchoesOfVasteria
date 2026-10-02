using System;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>
    /// Immutable source economy, pinned to inspected released artifacts. Unknown producers are
    /// deliberately unsupported for redistribution; their counts remain recoverable unchanged.
    /// </summary>
    internal readonly struct LegacyCauldronProfile
    {
        internal const string DeferredReceipt = "CauldronProfileDeferred_v1";
        internal const string Steam143Receipt = "CauldronProfileSteam143_v1";
        private LegacyCauldronProfile(int resourceMaximum, int buffMaximum)
        {
            ResourceCardMaximum = resourceMaximum;
            BuffCardMaximum = buffMaximum;
        }

        public int ResourceCardMaximum { get; }
        public int BuffCardMaximum { get; }

        public static bool TryResolve(GameData data, out LegacyCauldronProfile profile)
        {
            // LastGameVersion changes after successful migration. Keep the source decision
            // durable so a later reload cannot mistake an unknown old source for this writer.
            if (data?.AppliedMigrationIds?.Contains(DeferredReceipt) == true)
            {
                profile = default;
                return false;
            }
            if (data?.AppliedMigrationIds?.Contains(Steam143Receipt) == true)
            {
                profile = new LegacyCauldronProfile(10000, 3000);
                return true;
            }

            var producer = !string.IsNullOrWhiteSpace(data?.LastGameVersion)
                ? data.LastGameVersion.Trim()
                : data?.GameVersionCreated?.Trim();

            // Steam public build 20851856, resources.assets SHA256
            // 86d4304cf93c5181b7c69f5ef7fefafceb5ad79fcaaafd4e964b65b5ee9338bd.
            // This version match selects its card thresholds; it does not certify mobile saves.
            if (string.Equals(producer, "1.4.3", StringComparison.Ordinal))
            {
                profile = new LegacyCauldronProfile(10000, 3000);
                return true;
            }

            profile = default;
            return false;
        }
    }
}
