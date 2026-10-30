using System;
using TimelessEchoes.Farming;

namespace Blindsided.SaveData.Migrations
{
    internal sealed class Migration_SchemaV9FarmProjects : ISaveMigration
    {
        public int? TargetSchema => 9;
        public string TargetVersion => null;
        public string Id => "SchemaV9FarmProjects";
        public void Apply(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.SchemaVersion < 9)
                FarmProjects.PreserveEarnedAutomation(data);
        }
    }
}
