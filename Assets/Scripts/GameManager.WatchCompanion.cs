using System;
using System.Linq;
using System.Text;
using TimelessEchoes.Buffs;
using TimelessEchoes.MapGeneration;
using Blindsided;
using Newtonsoft.Json;
using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace TimelessEchoes
{
    public partial class GameManager
    {
        private float nextWatchSnapshot;
        private string lastWatchMode;

        private string WatchMode => runLoadingActive || backgroundSettling || resumeScheduled
            ? "loading" : IsInTown ? "town" : "run";

        private void UpdateWatchCompanion()
        {
            if (!WatchCompanionPrototype.Supported || backgroundSuspended || backgroundSettling) return;
            var refresh = WatchCompanionPrototype.ConsumeRefreshRequest();
            if (!refresh && WatchMode == lastWatchMode && Time.unscaledTime < nextWatchSnapshot) return;
            PublishWatchCompanionSnapshot();
        }

        private void PublishWatchCompanionSnapshot()
        {
            if (!WatchCompanionPrototype.Supported) return;
            var json = CaptureWatchCompanionSnapshot();
            if (json == null) return;
            WatchCompanionPrototype.Publish(json);
            lastWatchMode = WatchMode;
            nextWatchSnapshot = Time.unscaledTime + 15;
        }

        // Read the actual owners without checkpointing, awarding rewards or changing history.
        public string CaptureWatchCompanionSnapshot()
        {
            var oracle = Oracle.oracle;
            var data = oracle?.saveData;
            if (data == null || !oracle.HasLoadedCurrentSlotData) return null;
            var tracker = statTracker ? statTracker : GameplayStatTracker.Instance;
            var controller = SkillController.Instance;
            var mode = WatchMode;
            var map = CurrentGenerationConfig ? CurrentGenerationConfig.name : data.ActiveRun?.Map;
            var runStats = mode == "run" && tracker && tracker.RunInProgress ? tracker.CaptureRunState() : null;
            var health = hero ? hero.GetComponent<HeroHealth>() : null;
            var buff = BuffManager.Instance;
            var cap = tracker ? RunDistanceRules.EffectiveCap(tracker.MaxRunDistance,
                buff ? buff.MaxDistanceMultiplier : 1, buff ? buff.MaxDistanceFlatBonus : 0) : 0;
            if (oracle.demo) cap = Math.Min(cap, 300);
            var run = runStats == null || string.IsNullOrEmpty(map) ? null : new
            {
                mapId = WatchText(map, 80), mapTitle = WatchMapTitle(map),
                distance = WatchNumber(runStats.Distance), cap = WatchNumber(cap),
                distanceReaping = !IsKillScalingMode,
                healthFraction = Math.Clamp(WatchNumber(health && health.MaxHealth > 0
                    ? health.CurrentHealth / health.MaxHealth : data.ActiveRun?.HealthFraction ?? 1), 0, 1),
                dead = heroDead, reaped = runEndedByReaper,
                durationSeconds = WatchNumber(runStats.Duration), tasks = Math.Max(0, runStats.Tasks),
                kills = Math.Max(0, runStats.Kills), resources = WatchNumber(runStats.Resources)
            };
            var skills = controller ? controller.EnumerateSkills().Where(s => s).GroupBy(s => s.name)
                .Select(g => g.First()).Take(32).Select(s => new
                {
                    id = WatchText(s.name, 80), name = WatchText(ToolkitLocalization.Name(s, "skill"), 120),
                    level = Math.Max(1, controller.GetLevel(s, data))
                }).ToArray() : Array.Empty<object>();
            var history = (tracker ? tracker.RecentRuns : data.General.RecentRuns)
                .Where(r => r != null && !string.IsNullOrEmpty(r.MapType))
                .OrderByDescending(r => r.RunNumber).Take(10).Select(r => new
                {
                    runNumber = Math.Max(0, r.RunNumber), mapId = WatchText(r.MapType, 80), mapTitle = WatchMapTitle(r.MapType),
                    durationSeconds = WatchNumber(r.Duration), distance = WatchNumber(r.Distance),
                    tasks = Math.Max(0, r.TasksCompleted), kills = Math.Max(0, r.EnemiesKilled),
                    resources = WatchNumber(r.ResourcesCollected),
                    result = r.Reaped ? "reaped" : r.Died ? "died" : r.Abandoned ? "abandoned" : "completed",
                    estimated = r.Estimated
                }).ToArray();
            var timeline = run == null ? null : WatchCompanionPrototype.GetTimeline(map, data);
            string Serialize() => JsonConvert.SerializeObject(new
            {
                schemaVersion = 1, generatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), demo = BackgroundRunPrototype.Demo,
                state = mode, backgroundProgressEnabled = BackgroundRunPrototype.Enabled, run,
                forecast = run == null ? null : WatchCompanionPrototype.GetForecast(map, data), timeline, skills, recentRuns = history
            });
            var json = Serialize();
            while (Encoding.UTF8.GetByteCount(json) > 32768 && timeline != null)
            { timeline = timeline.LimitedTo(timeline.frames.Count / 2); json = Serialize(); }
            return Encoding.UTF8.GetByteCount(json) <= 32768 ? json : null;
        }

        private static string WatchMapTitle(string map)
        {
            return WatchText(ToolkitLocalization.Text("map." + map, map == "Haloween" ? "Hollow" : map), 120);
        }

        private static string WatchText(string value, int limit) => string.IsNullOrEmpty(value) ? "Unknown" : value.Length > limit ? value.Substring(0, limit) : value;
        private static double WatchNumber(double value) => double.IsFinite(value) ? Math.Max(0, value) : 0;
    }
}
