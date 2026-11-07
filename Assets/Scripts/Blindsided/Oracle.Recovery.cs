using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace Blindsided
{
    public partial class Oracle
    {
        private const string ReportQueueKey = "SaveRecovery.Diagnostics.v1";
        private readonly HashSet<string> sendingRecoveryReports = new();
        private RecoveryReports recoveryReports;
        private bool retryInterruptedRun;
        private bool restoringRun;
        private bool townRecoveryDiscardedRun;

        [Serializable]
        private sealed class RecoveryReports
        {
            public List<RecoveryReport> items = new();
        }

        [Serializable]
        private sealed class RecoveryReport
        {
            public int slot;
            public string id, phase, details;
            public bool sent, resolved;
        }

        private RecoveryReports Reports
        {
            get
            {
                if (recoveryReports != null) return recoveryReports;
                try { recoveryReports = JsonUtility.FromJson<RecoveryReports>(PlayerPrefs.GetString(ReportQueueKey)); }
                catch (ArgumentException) { }
                recoveryReports ??= new RecoveryReports();
                recoveryReports.items ??= new List<RecoveryReport>();
                return recoveryReports;
            }
        }

        private RecoveryReport CurrentRecoveryReport =>
            Reports.items.LastOrDefault(report => report.slot == CurrentSlot && !report.resolved);

        private void StoreRecoveryReports()
        {
            PlayerPrefs.SetString(ReportQueueKey, JsonUtility.ToJson(Reports));
            PlayerPrefs.Save();
        }

        private void RecordRecoveryReport(string phase, string outcome, Exception exception = null)
        {
            try
            {
                // A retry belongs to the same incident. Keep its original failing phase.
                if (CurrentRecoveryReport != null) return;
                var id = "EOV-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
                var report = new RecoveryReport
                {
                    slot = CurrentSlot, id = id, phase = phase,
                    details = FeedbackForm.FormatReport($"ID: {id}\nPhase: {phase}\nOutcome: {outcome}", exception)
                };
                var reports = Reports.items;
                if (reports.Count >= 12)
                {
                    var old = reports.FirstOrDefault(r => r.resolved && r.sent);
                    if (old != null) reports.Remove(old);
                }
                reports.Add(report);
                StoreRecoveryReports();
                SendRecoveryReport(report);
            }
            catch (Exception ex) { Debug.LogWarning($"Could not queue recovery diagnostics ({ex.GetType().Name})."); }
        }

        private void SendRecoveryReport(RecoveryReport report)
        {
            if (report == null || report.sent || !sendingRecoveryReports.Add(report.id)) return;
            FeedbackForm.Submit("Save.Recovery." + report.phase, report.details, sent =>
            {
                sendingRecoveryReports.Remove(report.id);
                if (sent)
                {
                    report.sent = true;
                    try { StoreRecoveryReports(); }
                    catch (Exception ex) { Debug.LogWarning($"Could not store the diagnostic receipt ({ex.GetType().Name})."); }
                }
                if (_recoveryRequired && CurrentRecoveryReport == report) ShowRecoveryUi();
            });
        }

        private void ResolveRecoveryReport()
        {
            var report = CurrentRecoveryReport;
            if (report == null) return;
            report.resolved = true;
            try { StoreRecoveryReports(); }
            catch (Exception ex) { Debug.LogWarning($"Could not close recovery diagnostics ({ex.GetType().Name})."); }
        }

        private IEnumerable<ToolkitDialogScreen.Choice> RecoveryActions(bool showSaveFolder)
        {
            foreach (var choice in RecoveryChoices()) yield return choice;
            if (showSaveFolder)
                yield return new ToolkitDialogScreen.Choice
                {
                    Text = ToolkitLocalization.Text("save.recovery.folder", "Open Save Folder"),
                    Select = OpenSaveFolder
                };
            var report = CurrentRecoveryReport;
            if (report == null) yield break;
            if (!report.sent)
            {
                yield return new ToolkitDialogScreen.Choice
                {
                    Text = sendingRecoveryReports.Contains(report.id)
                        ? ToolkitLocalization.Text("save.recovery.sending", "Sending diagnostics…")
                        : ToolkitLocalization.Text("save.recovery.send", "Send Diagnostics"),
                    Select = () => { SendRecoveryReport(report); ShowRecoveryUi(); }
                };
            }
            yield return new ToolkitDialogScreen.Choice
            {
                Text = ToolkitLocalization.Text("save.recovery.copy-id", "Copy ID · {0}", report.id),
                Select = () => GUIUtility.systemCopyBuffer = report.id
            };
        }

        private string RunRestoreKey => GetSlotPlayerPrefsKey(CurrentSlot, "RestoringRun");

        internal bool TryBeginRunRestore(RunSaveData saved, bool resumedInSameProcess)
        {
            var retry = retryInterruptedRun;
            retryInterruptedRun = false;
            if (!resumedInSameProcess && !retry && PlayerPrefs.GetString(RunRestoreKey) == RestoreCheckpointId(saved))
            {
                ReportRunRestoreFailure("restore-interrupted", null);
                return false;
            }
            restoringRun = true;
            TrackRunRestoreCheckpoint(saved);
            return true;
        }

        private static string RestoreCheckpointId(RunSaveData saved) => $"{saved.Map}\n{saved.CheckpointUtcTicks}";

        internal void TrackRunRestoreCheckpoint(RunSaveData saved)
        {
            try
            {
                PlayerPrefs.SetString(RunRestoreKey, RestoreCheckpointId(saved));
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException ex) { Debug.LogWarning($"Could not record the restore attempt: {ex.Message}"); }
        }

        internal void CompleteRunRestore()
        {
            restoringRun = false;
            retryInterruptedRun = false;
            try
            {
                if (PlayerPrefs.HasKey(RunRestoreKey))
                {
                    PlayerPrefs.DeleteKey(RunRestoreKey);
                    PlayerPrefs.Save();
                }
            }
            catch (PlayerPrefsException ex) { Debug.LogWarning($"Could not clear the restore attempt: {ex.Message}"); }
            ResolveRecoveryReport();
        }

        internal void ReportOfflineEstimateFailure(Exception exception)
        {
            Debug.LogException(exception);
            RecordRecoveryReport("offline-estimate", "Offline rewards not applied", exception);
        }

        internal void ReportRunRestoreFailure(string phase, Exception exception, bool offlineRewardsCommitted = false)
        {
            if (_recoveryRequired) return;
            RecordRecoveryReport(phase, offlineRewardsCommitted ? "Offline rewards committed" : "Offline rewards not confirmed", exception);
            RequireRuntimeReloadRecovery("The active adventure could not be restored",
                exception ?? new InvalidOperationException(phase), phase: phase);
        }

        private void RetryPendingRecoveryReports()
        {
            foreach (var report in Reports.items.Where(report => !report.sent))
                SendRecoveryReport(report);
        }
    }
}
