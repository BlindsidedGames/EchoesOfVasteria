using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using Blindsided.SaveData;

namespace Blindsided.EditorTools
{
    public class QuestSaveCleanupWindow : EditorWindow
    {
        private bool slot1 = true;
        private bool slot2 = true;
        private bool slot3 = true;
        private bool dryRun = true;

        private Vector2 scroll;
        private readonly List<string> logLines = new List<string>();

        [MenuItem("Tools/Timeless Echoes/Quest Save Cleanup")] 
        public static void ShowWindow()
        {
            var wnd = GetWindow<QuestSaveCleanupWindow>(utility: false, title: "Quest Save Cleanup");
            wnd.minSize = new Vector2(420, 320);
        }

        private void OnGUI()
        {
            GUILayout.Label("Clean up quest data in existing save slots", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Slots", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    slot1 = GUILayout.Toggle(slot1, "Save1", "Button");
                    slot2 = GUILayout.Toggle(slot2, "Save2", "Button");
                    slot3 = GUILayout.Toggle(slot3, "Save3", "Button");
                }

                dryRun = EditorGUILayout.ToggleLeft(new GUIContent("Dry Run (measure only)", "When enabled, computes potential size savings without writing files."), dryRun);

                if (GUILayout.Button(dryRun ? "Measure Selected Slots" : "Clean Selected Slots"))
                {
                    Run(dryRun);
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Results", EditorStyles.miniBoldLabel);
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = scrollView.scrollPosition;
                foreach (var line in logLines)
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            }
        }

        private void Run(bool measureOnly)
        {
            logLines.Clear();
            var any = false;
            if (slot1) { ProcessSlot("Save1", measureOnly); any = true; }
            if (slot2) { ProcessSlot("Save2", measureOnly); any = true; }
            if (slot3) { ProcessSlot("Save3", measureOnly); any = true; }
            if (!any) logLines.Add("No slots selected.");
        }

        private void ProcessSlot(string slotName, bool measureOnly)
        {
            try
            {
                SaveManager.Instance.SetCurrentSlot(slotName);
                var load = SaveManager.Instance.LoadAsync().GetAwaiter().GetResult();
                if (!load.ok || load.data == null)
                {
                    logLines.Add($"{slotName}: No valid save found.");
                    return;
                }

                var data = load.data;
                var before = GetPayloadSize(data);
                var (trimmedCount, trimmedAny) = TrimCompletedQuests(data);
                var after = GetPayloadSize(data);

                var delta = before - after;
                if (measureOnly)
                {
                    logLines.Add($"{slotName}: Quests trimmed: {trimmedCount}, Size {before} -> {after} bytes (Δ {delta}).");
                    return;
                }

                if (!trimmedAny)
                {
                    logLines.Add($"{slotName}: Nothing to clean. Size {before} bytes.");
                    return;
                }

                var ok = SaveManager.Instance.SaveAsync(data).GetAwaiter().GetResult();
                if (ok)
                {
                    logLines.Add($"{slotName}: Cleaned. Quests trimmed: {trimmedCount}, Size {before} -> {after} bytes (Δ {delta}).");
                }
                else
                {
                    logLines.Add($"{slotName}: Save failed after cleanup.");
                }
            }
            catch (Exception ex)
            {
                logLines.Add($"{slotName}: Error: {ex.Message}");
            }
        }

        private static int GetPayloadSize(GameData data)
        {
            try
            {
                byte[] payload = Sirenix.Serialization.SerializationUtility.SerializeValue(data, Sirenix.Serialization.DataFormat.Binary);
                return payload?.Length ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private static (int trimmedCount, bool trimmedAny) TrimCompletedQuests(GameData data)
        {
            if (data == null || data.Quests == null)
                return (0, false);

            var count = 0;
            var any = false;
            foreach (var kv in data.Quests)
            {
                var rec = kv.Value;
                if (rec == null || !rec.Completed)
                    continue;

                var changed = false;

                if (rec.KillProgress != null && rec.KillProgress.Count > 0)
                {
                    rec.KillProgress.Clear();
                    rec.KillProgress = null;
                    changed = true;
                }
                if (rec.BuffCastProgress != null && rec.BuffCastProgress.Count > 0)
                {
                    rec.BuffCastProgress.Clear();
                    rec.BuffCastProgress = null;
                    changed = true;
                }

                if (rec.DistanceTravelProgress != 0)
                {
                    rec.DistanceTravelProgress = 0;
                    changed = true;
                }
                if (Math.Abs(rec.CauldronMixProgress) > double.Epsilon)
                {
                    rec.CauldronMixProgress = 0;
                    changed = true;
                }

                // Reset baselines for completed quests (no longer needed)
                if (rec.BuffCastBaselineSet || rec.BuffCastBaseline != 0)
                {
                    rec.BuffCastBaseline = 0;
                    rec.BuffCastBaselineSet = false;
                    changed = true;
                }
                if (rec.CriticalBaselineSet || rec.CriticalBaseline != 0)
                {
                    rec.CriticalBaseline = 0;
                    rec.CriticalBaselineSet = false;
                    changed = true;
                }
                if (rec.ResourcesBaselineSet || Math.Abs(rec.ResourcesBaseline) > double.Epsilon)
                {
                    rec.ResourcesBaseline = 0;
                    rec.ResourcesBaselineSet = false;
                    changed = true;
                }
                if (rec.TasksBaselineSet || rec.TasksBaseline != 0)
                {
                    rec.TasksBaseline = 0;
                    rec.TasksBaselineSet = false;
                    changed = true;
                }

                if (changed)
                {
                    count++;
                    any = true;
                }
            }

            return (count, any);
        }
    }
}
