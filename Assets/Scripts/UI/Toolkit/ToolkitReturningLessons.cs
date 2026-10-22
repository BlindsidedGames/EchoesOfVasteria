using System.Collections;
using Blindsided;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitOrinIntroduction
    {
        [SerializeField] private GameObject returningDialogPrefab;

        private static readonly (string route, string key, string text)[] ReturningTour =
        {
            ("quests", "quests", "Quests introduce the townsfolk and unlock new places to explore. Select a quest to pin it. Your completed quests stay completed."),
            ("buffs", "buffs", "Buffs help your hero gather, fight and summon Echoes. This panel shows each buff’s effect and cost. You don’t need to cast anything for this lesson."),
            ("inventory", "inventory", "Inventory holds your equipment and resources. You can compare items here. Keep your current equipment; this lesson doesn’t ask you to change it."),
            ("skills", "skills", "Skills show your levels, bonuses and upcoming unlocks. Select a skill to see its progress."),
            ("forge", "forge", "At the Forge, smelt materials into cores and use cores to craft equipment. You can also salvage unwanted items. There’s no need to craft, equip or spend anything during this refresher."),
            ("statistics/general", "maps", "Statistics tracks your adventures and discoveries. Choose a map to see its resources and your progress there."),
            ("statistics/graphs", "graphs", "Graphs show your recent runs. Use them to compare your results over time."),
            ("statistics/tasks", "tasks", "Tasks lets you choose which unlocked tasks can appear and adjust their frequency. Keep your current selections if you’re happy with them. That’s the end of the refresher.")
        };

        private static ToolkitBookDefinition.Text ReturningText(string key, string fallback) =>
            new() { key = key, fallback = fallback };

        private IEnumerator ReturningLessons()
        {
            var general = Oracle.oracle.saveData.General;
            if (!general.ReturningPlayerIntroductionComplete) yield break;
            while (GameManager.Instance && !GameManager.Instance.IsInTown) yield return null;
            const string marker = "ReturningLessons";
            Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Add(marker);
            try
            {
                if (!general.ReturningPlayerLessonsChoiceMade)
                {
                    if (!returningDialogPrefab) yield break;
                    var dialog = Instantiate(returningDialogPrefab).GetComponent<ToolkitDialogScreen>();
                    var chosen = false;
                    void Choose(bool replay)
                    {
                        if (chosen) return;
                        chosen = true;
                        general.ReturningPlayerWelcomeSeen = true;
                        general.ReturningPlayerLessonsChoiceMade = true;
                        general.ReturningPlayerLessonStage = replay ? 1 : 0;
                        Blindsided.EventHandler.SaveData();
                        dialog.Hide();
                    }
                    try
                    {
                        dialog.ShowLocalized(
                            ReturningText("intro.returning-title", "Welcome back"),
                            ReturningText("intro.ReturningPlayerWelcome", "Would you like a refresher on the opening lessons? We’ll show you the menus without changing your equipment, spending resources or repeating quests."),
                            ReturningText("intro.returning-show", "Show lessons"),
                            ReturningText("intro.returning-skip", "Skip lessons"),
                            () => Choose(true), () => Choose(false));
                        while (dialog && !chosen) yield return null;
                    }
                    finally { if (dialog) Destroy(dialog.gameObject); }
                    if (!chosen) yield break;
                    yield return null;
                }
                // This is a read-only tour, independent of the original interactive
                // tutorial flags. In particular, never restart the Forge quest.
                while (general.ReturningPlayerLessonStage > 0 && general.ReturningPlayerLessonStage <= ReturningTour.Length)
                {
                    var index = general.ReturningPlayerLessonStage - 1;
                    var lesson = ReturningTour[index];
                    if (lesson.route == "forge" && (Gear.ForgeFirstLesson.Stage >= 5 || Gear.ForgeFirstLesson.HasAdvancedSword))
                    {
                        general.ReturningPlayerLessonStage++;
                        Blindsided.EventHandler.SaveData();
                        continue;
                    }
                    var navigation = FindAnyObjectByType<ToolkitNavigationScreen>();
                    ToolkitMeetingScreen meeting = null;
                    var finished = false;
                    try
                    {
                        var guiding = navigation && navigation.BeginRefresherNavigation(lesson.route);
                        yield return null;
                        meeting = Instantiate(meetingPrefab).GetComponent<ToolkitMeetingScreen>();
                        meeting.Init(null, new[] { lesson.text }, () => finished = true,
                            new[] { "intro.refresher." + lesson.key }, index < ReturningTour.Length - 1);
                        if (guiding) meeting.GuideNavigation(navigation);
                        meeting.BeginDialogue();
                        while (meeting && !finished) yield return null;
                    }
                    finally
                    {
                        if (navigation) navigation.EndRefresherNavigation();
                        if (meeting) Destroy(meeting.gameObject);
                    }
                    if (!finished) yield break;
                    TownWindowManager.Instance?.CloseAllWindows();
                    general.ReturningPlayerLessonStage = index + 1 == ReturningTour.Length ? 0 : index + 2;
                    Blindsided.EventHandler.SaveData();
                    yield return null;
                }
            }
            finally { Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Remove(marker); }
        }
    }
}
