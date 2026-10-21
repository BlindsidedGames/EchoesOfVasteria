using System;
using System.Linq;

namespace Blindsided.SaveData.Migrations
{
    // Run after legacy quest/buff migrations, on the detached save candidate only.
    internal sealed class Migration_SchemaV8ReturningPlayers : IPostVersionSaveMigration
    {
        public int? TargetSchema => 8;
        public string TargetVersion => null;
        public string Id => "SchemaV8ReturningPlayerGuidance";

        public void Apply(GameData data)
        {
            var general = data.General;
            if (general == null) return;
            bool Completed(string id) => data.Quests != null && data.Quests.TryGetValue(id, out var quest) && quest?.Completed == true;
            if (Completed("The names Gill"))
            {
                general.InventoryTutorialStage = Math.Max(general.InventoryTutorialStage, 3);
                general.HubTutorialStage = Math.Max(general.HubTutorialStage, 3);
            }
            if (Completed("Echo Casting") || data.BuffSlots?.Any(s => s == "Echo Tasks" || s == "Echo Combat") == true)
                general.BuffTutorialStage = Math.Max(general.BuffTutorialStage, 4);

            // These are the original opening's milestones, not a playtime threshold.
            // Do not mark GraphsViewed or modify any equipment, choices or rewards.
            if (!Completed("The names Gill") || !Completed("Into The Multiverse")) return;
            general.ReturningPlayerIntroductionComplete = true;
            general.MapStatisticsTutorialStage = Math.Max(general.MapStatisticsTutorialStage, 9);
            general.TaskBoostTutorialStage = Math.Max(general.TaskBoostTutorialStage, 2);
            general.GraphsTutorialStage = Math.Max(general.GraphsTutorialStage, 2);
            general.RunSummaryIntroductionSeen = true;
            general.FirstReapTownVisited = general.TimesReaped > 0 || general.FirstReapTownVisited;
        }
    }
}
