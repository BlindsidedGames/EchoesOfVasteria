using System;

namespace Blindsided
{
    public partial class Oracle
    {
        // Normal save contributors observe the complete published owners and projected
        // loadingRun together. A failed publication/commit blocks further saves; it must
        // never retry rewards against an already-mutated live inventory.
        public bool CommitBackgroundRun(Action publish)
        {
            if (!CanBeginSaveMutation || !CanSave() || _economicTransactionActive) return false;
            _economicTransactionActive = true;
            try { publish(); }
            catch (Exception ex)
            {
                RequireRuntimeReloadRecovery("Background progression failed", ex, checkpointCommitted: false);
                return false;
            }
            finally { _economicTransactionActive = false; }
            if (SaveNowBlocking(CurrentSlot)) return true;
            RequireRuntimeReloadRecovery("Background progression could not be saved", new InvalidOperationException("Checkpoint rejected"), checkpointCommitted: false);
            return false;
        }
    }
}
