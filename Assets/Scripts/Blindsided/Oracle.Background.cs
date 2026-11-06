using System;

namespace Blindsided
{
    public partial class Oracle
    {
        // Normal save contributors observe the complete published owners and projected
        // loadingRun together. A failed publication/commit blocks further saves; it must
        // never retry rewards against an already-mutated live inventory.
        public bool CommitBackgroundProgress(Action publish)
        {
            if (!CanSave(allowDuringRestore: true) || saveData.Hardcore || _economicTransactionActive) return false;
            _economicTransactionActive = true;
            try { publish(); }
            catch (Exception ex)
            {
                RequireRuntimeReloadRecovery("Background progression failed", ex, checkpointCommitted: false,
                    phase: "offline-award");
                return false;
            }
            finally { _economicTransactionActive = false; }
            try
            {
                if (SaveCheckpointBlocking(CurrentSlot, false, "offline-save")) return true;
            }
            catch (Exception ex)
            {
                if (!_recoveryRequired)
                    RequireRuntimeReloadRecovery("Background progression could not be saved", ex,
                        checkpointCommitted: false, phase: "offline-save");
                return false;
            }
            if (!_recoveryRequired)
                RequireRuntimeReloadRecovery("Background progression could not be saved",
                    new InvalidOperationException("Checkpoint rejected"), checkpointCommitted: false, phase: "offline-save");
            return false;
        }
    }
}
