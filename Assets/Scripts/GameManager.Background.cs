using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using Newtonsoft.Json;
using TimelessEchoes.Buffs;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Stats;
using UnityEngine;

namespace TimelessEchoes
{
    public partial class GameManager
    {
        private bool backgroundSuspended, backgroundSettling, backgroundDemoStarted, activityVisible;
        private float backgroundTimeScale = 1, nextBackgroundForecast;

        private void EnterRunBackground()
        {
            if (!BackgroundRunPrototype.Enabled || IsInTown || backgroundSuspended || runLoadingActive) return;
            SaveActiveRun();
            RefreshBackgroundForecast();
            backgroundSuspended = true;
            backgroundTimeScale = Time.timeScale;
            Time.timeScale = 0;
        }

        private void ExitRunBackground()
        {
            if (!backgroundSuspended) return;
            backgroundSuspended = false;
            var saved = Oracle.oracle?.saveData?.ActiveRun;
            if (saved == null) { Time.timeScale = backgroundTimeScale; return; }
            StartCoroutine(ResumeBackgroundRun(saved));
        }

        private IEnumerator ResumeBackgroundRun(RunSaveData saved)
        {
            runLoadingActive = true;
            yield return null;
            var entry = generationButtons.FirstOrDefault(e => e?.Config && e.Config.name == saved.Map);
            if (entry != null && TrySettleBackground(saved, entry))
            {
                Time.timeScale = backgroundTimeScale;
                runLoadingActive = false;
                BeginAdventure(entry.Config, Oracle.oracle.saveData.ActiveRun);
            }
            else if (Oracle.oracle?.HasLoadedCurrentSlotData == true)
            { runLoadingActive = false; Time.timeScale = backgroundTimeScale; }
        }

        private BackgroundRunEstimator.Input BackgroundInput(RunSaveData run, MapGenerationButton entry, double seconds)
        {
            var data = Oracle.oracle.saveData;
            data.BackgroundSamples ??= new();
            if (!data.BackgroundSamples.TryGetValue(run.Map, out var samples))
            {
                samples = data.General.RecentRuns.Where(r => r.MapType == run.Map && !r.Estimated && !r.Abandoned && r.Duration >= 5)
                    .TakeLast(10).Select(BackgroundRunSample.From).ToList();
                // Once bootstrapped, estimated display history must never replace real calibration.
                data.BackgroundSamples[run.Map] = samples;
            }
            var snapshot = HeroStatSystem.GetSnapshot();
            var config = entry.Config;
            var content = new BackgroundRunContent(config, entry.ScalingMode == MapScalingMode.KillBased,
                entry.KillsPerLevel, KillLevelIncreasePerTier, run);
            var gate = double.PositiveInfinity;
            foreach (var npc in config.taskGeneratorSettings.npcTasks)
                if (npc != null && npc.spawnOnlyOnce && !data.CompletedNpcTasks.Contains(npc.id))
                    gate = Math.Min(gate, Math.Max(0, npc.localX - 2));
            if (IvanReturnPending || StaticReferences.ActiveNpcMeetings.Count > 0 || UI.Toolkit.BuffIntroductionGuidance.Active)
                gate = run.Distance;
            return new BackgroundRunEstimator.Input
            {
                Run = run, Samples = samples, Seconds = seconds, BaseCap = statTracker.MaxRunDistance,
                StartingDistance = config.entryIntroduction ? Math.Max(startingDistance, config.entryIntroduction.StartingInset) : startingDistance,
                MaxHealth = Math.Max(1, snapshot.maxHealth), Regen = snapshot.healthRegenPerSecond,
                RestartSeconds = SkipRestartCountdown ? 0 : deathWindowDuration,
                DistanceReaping = entry.ScalingMode != MapScalingMode.KillBased,
                CapGain = config.allowMaxDistanceReapGain && !Oracle.oracle.demo,
                GateDistance = gate, StopOnFirstReap = !data.General.FirstReapTownVisited, Pressure = content.Pressure,
                CapModifiers = content.CapModifiers,
                NextBuffExpiry = run.Buffs?.Active?.Where(b => !float.IsFinite(b.ExpireAtDistance) || b.ExpireAtDistance <= 0).Select(b => (double)b.Remaining)
                    .DefaultIfEmpty(double.PositiveInfinity).Min() ?? double.PositiveInfinity
            };
        }

        public BackgroundRunEstimator.Result ForecastBackgroundRun(double seconds)
        {
            var run = Oracle.oracle?.saveData?.ActiveRun;
            if (run == null) return null;
            var entry = generationButtons.FirstOrDefault(e => e?.Config && e.Config.name == run.Map);
            return entry == null ? null : BackgroundRunEstimator.Forecast(BackgroundInput(run, entry, seconds));
        }

        private bool TrySettleBackground(RunSaveData run, MapGenerationButton entry)
        {
            if (!BackgroundRunPrototype.Enabled || run.CheckpointUtcTicks <= 0 || backgroundSettling) return true;
            var seconds = (DateTime.UtcNow.Ticks - run.CheckpointUtcTicks) / (double)TimeSpan.TicksPerSecond;
            if (seconds < 2) return true;
            var input = BackgroundInput(run, entry, seconds);
            var forecast = BackgroundRunEstimator.Forecast(input);
            var content = new BackgroundRunContent(entry.Config, entry.ScalingMode == MapScalingMode.KillBased,
                entry.KillsPerLevel, KillLevelIncreasePerTier, run, forecast.EchoTaskShare, forecast.EchoKillShare);
            backgroundSettling = true;
            loadingRun = run;
            var initialResources = statTracker.TotalResourcesGathered;
            try
            {
                var committed = Oracle.oracle.CommitBackgroundRun(() =>
                {
                    statTracker.RestoreRunState(run.Stats, entry.Config, entry.ScalingMode);
                    runResourceTracker?.RestoreRunState(run.Loot);
                    var oldRandom = UnityEngine.Random.state;
                    UnityEngine.Random.InitState(unchecked((int)run.CheckpointUtcTicks));
                    double elapsed = 0;
                    try
                    {
                        foreach (var step in forecast.Steps)
                        {
                            statTracker.AdvanceEstimatedRun(step); elapsed += step.Seconds;
                            content.Award(step, elapsed);
                            if (!step.EndsRun) continue;
                            content.EndRun();
                            if (step.Reaped)
                            {
                                statTracker.AddTimesReaped();
                                if (step.CapIncrease > 0) statTracker.IncreaseMaxRunDistance((float)step.CapIncrease);
                            }
                            else statTracker.AddDeath();
                            var restarts = !(step.Reaped && input.StopOnFirstReap) && step.RestartSeconds + .00001 >= input.RestartSeconds;
                            if (!restarts) continue;
                            statTracker.EndRun(true, step.Reaped, true);
                            statTracker.BeginRun(entry.Config, entry.ScalingMode);
                            statTracker.RecordHeroPosition(new Vector3((float)input.StartingDistance, 0, 0));
                            elapsed += step.RestartSeconds;
                        }
                    }
                    finally { UnityEngine.Random.state = oldRandom; }
                    statTracker.SetEstimatedRemainders(forecast);
                    var last = forecast.Steps.LastOrDefault();
                    var dead = last != null && last.EndsRun && ((last.Reaped && input.StopOnFirstReap) || last.RestartSeconds + .00001 < input.RestartSeconds);
                    run.Distance = (float)forecast.Distance; run.HealthFraction = dead ? 0 : (float)forecast.HealthFraction;
                    run.Dead = dead; run.Reaped = dead && last.Reaped;
                    run.Stats = statTracker.CaptureRunState();
                    run.Loot = runResourceTracker ? runResourceTracker.CaptureRunState() : run.Loot;
                    if (forecast.Reaps + forecast.Deaths > 0) run.Buffs = null;
                    else if (run.Buffs != null)
                    {
                        foreach (var buff in run.Buffs.Active) buff.Remaining -= (float)forecast.MovingSeconds;
                        run.Buffs.Active.RemoveAll(b => b.Remaining <= 0 || b.ExpireAtDistance > 0 && b.ExpireAtDistance <= run.Distance);
                        foreach (var key in run.Buffs.Cooldowns.Keys.ToArray()) run.Buffs.Cooldowns[key] = Math.Max(0, run.Buffs.Cooldowns[key] - (float)forecast.MovingSeconds);
                    }
                    Oracle.oracle.saveData.Shop.GatheringSeconds = Math.Max(0, Oracle.oracle.saveData.Shop.GatheringSeconds - forecast.Seconds);
                    run.CheckpointUtcTicks = DateTime.UtcNow.Ticks;
                    Oracle.oracle.saveData.ActiveRun = run;
                });
                if (!committed) return false;
                var report = new { map = run.Map, awaySeconds = seconds, forecast.SampleCount, forecast.Seconds,
                    forecast.Distance, forecast.Reaps, forecast.Deaths, forecast.Tasks, forecast.Kills,
                    forecast.StopReason, resources = statTracker.TotalResourcesGathered - initialResources,
                    checkpointUtcTicks = run.CheckpointUtcTicks, prototype = true };
                try { File.WriteAllText(Path.Combine(Platform.StartupDiagnostics.EditorScreenshotRoot ?? Application.persistentDataPath, "background-last-settlement.json"), JsonConvert.SerializeObject(report, Formatting.Indented)); }
                catch (IOException ex) { Debug.LogWarning("Could not write background prototype diagnostics: " + ex.Message); }
                Debug.Log("[Background prototype] " + JsonConvert.SerializeObject(report));
                BackgroundRunPrototype.End(); activityVisible = false;
                return true;
            }
            finally { backgroundSettling = false; }
        }

        private void RefreshBackgroundForecast()
        {
            if (!BackgroundRunPrototype.Enabled || IsInTown || runLoadingActive || backgroundSettling || !statTracker || !hero) return;
            SaveActiveRun();
            var run = Oracle.oracle?.saveData?.ActiveRun;
            if (run == null) return;
            var forecast = ForecastBackgroundRun(900);
            if (forecast == null) return;
            var entry = generationButtons.First(e => e?.Config && e.Config.name == run.Map);
            var content = new BackgroundRunContent(entry.Config, entry.ScalingMode == MapScalingMode.KillBased,
                entry.KillsPerLevel, KillLevelIncreasePerTier, run);
            var resources = forecast.Tasks * content.EstimateResourcesPerWork(false, run.Distance) +
                forecast.Kills * content.EstimateResourcesPerWork(true, run.Distance);
            BackgroundRunPrototype.Publish(run.Map, forecast, run.Distance, resources);
            activityVisible = true;
        }

        private void UpdateBackgroundPrototype()
        {
            if (!BackgroundRunPrototype.Enabled) return;
            if (BackgroundRunPrototype.Demo && !backgroundDemoStarted && IsInTown && Oracle.oracle?.HasLoadedCurrentSlotData == true)
                StartBackgroundDemo();
            if (backgroundSuspended || backgroundSettling) return;
            if (IsInTown && activityVisible) { BackgroundRunPrototype.End(); activityVisible = false; }
            if (!IsInTown && Time.unscaledTime >= nextBackgroundForecast)
            { nextBackgroundForecast = Time.unscaledTime + 15; RefreshBackgroundForecast(); }
        }

        private void StartBackgroundDemo()
        {
            // Demo input may only modify an isolated prototype app/Editor screenshot bank.
            if (!Application.identifier.EndsWith(".prototype", StringComparison.Ordinal) && !Platform.StartupDiagnostics.EditorScreenshotSession) return;
            backgroundDemoStarted = true;
            var entry = generationButtons.FirstOrDefault(e => e?.Config && e.Config.name == "River");
            if (entry == null) return;
            var data = Oracle.oracle.saveData;
            data.General.FirstReapTownVisited = true;
            data.General.BuffTutorialStage = 4;
            data.Character = Characters.CharacterProfile.Legacy();
            data.Quests["Meeting Ivan"] = new GameData.QuestRecord { Completed = true };
            if (entry.Config.entryIntroduction) data.CompletedNpcTasks.Add(entry.Config.entryIntroduction.npcId);
            foreach (var npc in entry.Config.taskGeneratorSettings.npcTasks) if (npc != null) data.CompletedNpcTasks.Add(npc.id);
            foreach (var category in new[] { entry.Config.taskGeneratorSettings.fishing, entry.Config.taskGeneratorSettings.farming,
                entry.Config.taskGeneratorSettings.mining, entry.Config.taskGeneratorSettings.woodcutting, entry.Config.taskGeneratorSettings.looting })
                if (!string.IsNullOrEmpty(category.requiredNpcId)) data.CompletedNpcTasks.Add(category.requiredNpcId);
            data.BackgroundSamples["River"] = Enumerable.Range(0, 10).Select(i => new BackgroundRunSample
            { Seconds = 300 + i * 3, Distance = 180 + i * 2, EndDistance = 180 + i * 2, Tasks = 80 + i, Kills = 18 + i, DamageTaken = 1, Reaped = true }).ToList();
            statTracker.IncreaseMaxRunDistance(Mathf.Max(0, 200 - statTracker.MaxRunDistance));
            var run = new RunSaveData { Map = "River", Distance = 30, HealthFraction = 1, CheckpointUtcTicks = DateTime.UtcNow.Ticks };
            data.ActiveRun = run;
            BeginAdventure(entry.Config, run);
        }
    }
}
