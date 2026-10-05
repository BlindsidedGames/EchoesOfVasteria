using System;
using TimelessEchoes.Farming;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>Initializes Fields progression without replaying prototype construction or growth.</summary>
    internal sealed class Migration_SchemaV6Fields : ISaveMigration
    {
        public int? TargetSchema => 6;
        public string TargetVersion => null;
        public string Id => "SchemaV6Fields";

        public void Apply(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Farm ??= new FarmState();
            if (data.Farm.ProductionRevision != 0) return;
            data.Farm.ProductionRevision = 1;
            data.Farm.TwinsLevel = 1;
            data.Farm.TwinsXp = 0;
            data.Farm.GardenCapacity = 0;
            data.Farm.OrchardCapacity = 0;
            // Legacy prepared flags are historical receipts, never free Fields construction.
            // Preserve paid batches, seeds, pending credits, journal lineage, opaque entries
            // and elapsed growth exactly. LastGrowthUtcTicks is inert in the Fields service.
        }
    }
}
