using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Buffs;
using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes
{
    public partial class GameManager
    {
        private RunSaveData loadingRun;
        private bool resumeScheduled;
        private bool runRestoreChecked;
        private bool backgroundSettlementCommitted;

        // Main can render before OnLoadData. Cover that gap without setting
        // runLoadingActive, which would prevent BeginAdventure from restoring it.
        public bool RunRestorePending => Oracle.oracle?.HasLoadedCurrentSlotData == true && !HardcoreFallen &&
            (resumeScheduled || !runRestoreChecked && IsInTown && Oracle.oracle.saveData.ActiveRun != null);

        private void SaveActiveRun()
        {
            var data = Oracle.oracle?.saveData;
            if (data == null || resumeScheduled) return;
            if (data.HardcoreFallen) { data.ActiveRun = null; return; }
            // Suspension and generation retain their continuation checkpoint until
            // the world is rebuilt. A save must not replace its departure timestamp.
            if (loadingRun != null) { data.ActiveRun = loadingRun; return; }
            if (IsInTown || !CurrentMap || !hero || statTracker?.RunInProgress != true)
            {
                data.ActiveRun = null;
                return;
            }
            var health = hero.GetComponent<HeroHealth>();
            var distance = Mathf.Max(0, hero.transform.position.x);
            // An unfinished conversation restarts on arrival. Keep its authored NPC ahead
            // of the resumed player so culling cannot remove the encounter.
            foreach (var npc in CurrentGenerationConfig.taskGeneratorSettings.npcTasks)
                if (npc != null && StaticReferences.ActiveNpcMeetings.Contains(npc.id))
                    distance = Mathf.Min(distance, Mathf.Max(0, npc.localX - 2));
            data.ActiveRun = CaptureRun(distance, health && health.MaxHealth > 0 ? health.CurrentHealth / health.MaxHealth : 1f);
        }

        private RunSaveData CaptureRun(float distance, float healthFraction) => new()
        {
            CheckpointUtcTicks = System.DateTime.UtcNow.Ticks,
            Map = CurrentGenerationConfig.name, Distance = distance,
            HealthFraction = Mathf.Clamp01(healthFraction), Dead = heroDead,
            Reaped = runEndedByReaper, ReturnQueued = retreatQueued,
            Stats = statTracker.CaptureRunState(), Loot = runResourceTracker ? runResourceTracker.CaptureRunState() : new(),
            Buffs = BuffManager.Instance?.CaptureRunState()
        };

        private void LoadActiveRun()
        {
            runRestoreChecked = true;
            ApplyHardcoreState();
            if (HardcoreFallen) return;
            var saved = Oracle.oracle?.saveData?.ActiveRun;
            if (saved == null || resumeScheduled || !IsInTown || runLoadingActive) return;
            resumeScheduled = true;
            StartCoroutine(ResumeAfterProgressLoads(saved));
        }

        private IEnumerator ResumeAfterProgressLoads(RunSaveData saved)
        {
            // OnLoadData also loads inventory, skills and buff assignments. All subscribers
            // must finish before this same run-loading flow creates scaled enemies.
            yield return null;
            resumeScheduled = false;
            if (Oracle.oracle?.saveData?.ActiveRun != saved || !Oracle.oracle.HasLoadedCurrentSlotData) yield break;
            if (Oracle.oracle.saveData.Hardcore && saved.Dead && !saved.Reaped)
            {
                Oracle.oracle.FinishHardcoreRun(saved.Map, saved.Distance);
                ApplyHardcoreState();
                yield break;
            }
            RestoreSavedRun(saved, resumedInSameProcess: false);
        }

        private void RestoreSavedRun(RunSaveData saved, bool resumedInSameProcess)
        {
            var oracle = Oracle.oracle;
            if (oracle?.HasLoadedCurrentSlotData != true) return;
            backgroundSettlementCommitted = false;
            try
            {
                if (!oracle.TryBeginRunRestore(saved, resumedInSameProcess)) return;
                var entry = generationButtons.FirstOrDefault(e => e?.Config && e.Config.name == saved.Map);
                if (!saved.IsValid || entry == null)
                {
                    oracle.ReportRunRestoreFailure("run-checkpoint", null);
                    return;
                }

                StaticReferences.ActiveNpcMeetings.Clear();
                if (!TrySettleBackground(saved, entry))
                {
                    oracle.ReportRunRestoreFailure("offline-settlement", null);
                    return;
                }
                var checkpoint = oracle.saveData.ActiveRun;
                if (backgroundSettlementCommitted) oracle.TrackRunRestoreCheckpoint(checkpoint);
                if (!BeginAdventure(entry.Config, checkpoint))
                    oracle.ReportRunRestoreFailure("run-start", null, backgroundSettlementCommitted);
            }
            catch (Exception ex)
            {
                oracle.ReportRunRestoreFailure("run-start", ex, backgroundSettlementCommitted);
            }
        }

        // Unity executes nested enumerators outside their parent's MoveNext. Drive the
        // restore stack here so that failures in cleanup, map waits and hero setup agree.
        private IEnumerator RunRestoreRoutine(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            var activeSeconds = 0f;
            try
            {
                while (stack.Count > 0)
                {
                    var current = stack.Peek();
                    bool moved;
                    object next = null;
                    Exception failure = null;
                    try
                    {
                        moved = current.MoveNext();
                        if (moved) next = current.Current;
                    }
                    catch (Exception ex)
                    {
                        moved = false;
                        failure = ex;
                    }
                    if (failure != null)
                    {
                        if (loadingRun != null)
                            Oracle.oracle?.ReportRunRestoreFailure("map-rebuild", failure, backgroundSettlementCommitted);
                        else Debug.LogException(failure);
                        yield break;
                    }
                    if (!moved)
                    {
                        (stack.Pop() as IDisposable)?.Dispose();
                        continue;
                    }
                    if (next is IEnumerator nested)
                    {
                        stack.Push(nested);
                        continue;
                    }

                    yield return next;
                    // Count active frames, so suspension itself cannot time out a restore.
                    if (loadingRun != null && Application.isFocused)
                        activeSeconds += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
                    if (loadingRun != null && activeSeconds >= 60f)
                    {
                        Oracle.oracle?.ReportRunRestoreFailure("map-timeout", null, backgroundSettlementCommitted);
                        yield break;
                    }
                }
                if (loadingRun != null)
                    Oracle.oracle?.ReportRunRestoreFailure("map-incomplete", null, backgroundSettlementCommitted);
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            }
        }
    }
}
