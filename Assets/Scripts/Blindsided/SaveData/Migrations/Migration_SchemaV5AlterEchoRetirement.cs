using System;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>Retirement receipt only. Historical production balances and quest progress stay inert.</summary>
    internal sealed class Migration_SchemaV5AlterEchoRetirement : ISaveMigration
    {
        public int? TargetSchema => 5;
        public string TargetVersion => null;
        public string Id => "SchemaV5AlterEchoRetirement";
        public void Apply(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            // Deliberately no payout, catch-up, compensation, balance deletion or quest replay.
            // Advancing the schema prevents older schema-four builds from resuming production.
        }
    }
}
