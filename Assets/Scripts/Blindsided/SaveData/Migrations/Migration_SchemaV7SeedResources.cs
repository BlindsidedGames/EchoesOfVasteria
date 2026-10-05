using TimelessEchoes.Farming;

namespace Blindsided.SaveData.Migrations
{
    internal sealed class Migration_SchemaV7SeedResources : ISaveMigration, IConditionalRepairSaveMigration
    {
        public int? TargetSchema => 7;
        public string TargetVersion => null;
        public string Id => FarmSeedResources.MigrationId;
        public bool NeedsRepair(GameData data) => FarmSeedResources.NeedsRepair(data);
        public void Apply(GameData data)
        {
            new Migration_SchemaV6Fields().Apply(data);
            FarmSeedResources.Upgrade(data);
        }
    }
}
