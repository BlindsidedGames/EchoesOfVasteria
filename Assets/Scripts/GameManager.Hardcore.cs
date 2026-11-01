using Blindsided;
using TimelessEchoes.Buffs;
using TimelessEchoes.Gear;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes
{
    public partial class GameManager
    {
        private bool hardcorePaused;
        private float hardcorePreviousTimeScale = 1;
        public static bool HardcoreFallen => Oracle.oracle?.saveData?.HardcoreFallen == true;

        private void ApplyHardcoreState()
        {
            if (!HardcoreFallen)
            {
                if (hardcorePaused) { Time.timeScale = hardcorePreviousTimeScale; heroDead = false; runEndedByDeath = false; }
                hardcorePaused = false;
                return;
            }
            if (!hardcorePaused) hardcorePreviousTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
            hardcorePaused = true;
            heroDead = true; runEndedByDeath = true; runEndedByReaper = false;
            Time.timeScale = 0;
            StopAllCoroutines();
            runLoadingActive = false;
            loadingRun = null; resumeScheduled = false;
            backgroundSuspended = false;
            DeathPromptVisible = false;
            if (deathWindow) deathWindow.SetActive(false);
            deathUiFailsafeCheckAt = -1;
            BuffManager.Instance?.Pause();
            CauldronManager.Instance?.StopTasting();
            foreach (var forge in FindObjectsByType<ForgeSession>(FindObjectsInactive.Include, FindObjectsSortMode.None)) forge.StopAutoCrafting();
            BackgroundRunPrototype.End();
            Oracle.oracle.saveData.ActiveRun = null;
        }

        private bool FinishHardcoreDeath()
        {
            if (Oracle.oracle?.saveData?.Hardcore != true) return false;
            var distance = statTracker ? statTracker.CurrentRunDistance : hero ? hero.transform.position.x : 0;
            statTracker?.EndRun(true);
            Oracle.oracle.FinishHardcoreRun(CurrentGenerationConfig ? CurrentGenerationConfig.name : "", distance);
            ApplyHardcoreState();
            return true;
        }
    }
}
