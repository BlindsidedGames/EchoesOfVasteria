using System.Collections;
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

        private void SaveActiveRun()
        {
            var data = Oracle.oracle?.saveData;
            if (data == null || resumeScheduled) return;
            // Generation spans frames. Save its starting checkpoint, never a half-loaded world.
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
            Map = CurrentGenerationConfig.name, Distance = distance,
            HealthFraction = Mathf.Clamp01(healthFraction), Dead = heroDead,
            Reaped = runEndedByReaper, ReturnQueued = retreatQueued,
            Stats = statTracker.CaptureRunState(), Loot = runResourceTracker ? runResourceTracker.CaptureRunState() : new(),
            Buffs = BuffManager.Instance?.CaptureRunState()
        };

        private void LoadActiveRun()
        {
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
            if (saved.IsValid)
                foreach (var entry in generationButtons)
                    if (entry?.Config && entry.Config.name == saved.Map)
                    {
                        StaticReferences.ActiveNpcMeetings.Clear();
                        if (BeginAdventure(entry.Config, saved)) yield break;
                        break;
                    }
            Debug.LogWarning("The saved adventure could not be resumed. Player progression has been retained.");
            Oracle.oracle.saveData.ActiveRun = null;
        }
    }
}
