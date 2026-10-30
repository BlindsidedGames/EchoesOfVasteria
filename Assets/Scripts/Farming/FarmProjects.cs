using System;
using Blindsided.SaveData;

namespace TimelessEchoes.Farming
{
    [Flags]
    public enum FarmProject { None = 0, MillBody = 1, MillSails = 2, Well = 4 }

    public static class FarmProjects
    {
        public const string MillBody = "Farm.Project.MillBody.v1";
        public const string MillSails = "Farm.Project.MillSails.v1";
        public const string Well = "Farm.Project.Well.v1";

        public static FarmProject Kind(string id) => id switch
        {
            MillBody => FarmProject.MillBody, MillSails => FarmProject.MillSails,
            Well => FarmProject.Well, _ => FarmProject.None
        };
        public static bool Complete(FarmState state, FarmProject project) => state != null && (state.Projects & project) == project;
        public static bool AutoReplant(FarmState state) => Complete(state, FarmProject.MillBody | FarmProject.MillSails);
        public static bool AutoWater(FarmState state) => Complete(state, FarmProject.Well);

        // Called by the detached save migration, never by the presentation layer.
        public static void PreserveEarnedAutomation(GameData data)
        {
            data.Farm ??= new FarmState();
            data.Quests ??= new();
            var farm = data.Farm;
            if (farm.TotalHarvests > 0) farm.FirstHarvestCompleted = true;
            void Grant(string id, FarmProject project)
            {
                farm.Projects |= project;
                if (!data.Quests.TryGetValue(id, out var record) || record == null)
                    data.Quests[id] = record = new GameData.QuestRecord();
                record.Completed = true;
            }
            if (farm.TwinsLevel >= 20)
            {
                Grant(MillBody, FarmProject.MillBody);
                Grant(MillSails, FarmProject.MillSails);
            }
            if (farm.TwinsLevel >= 50) Grant(Well, FarmProject.Well);
        }
    }
}
