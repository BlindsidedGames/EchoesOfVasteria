using System;
using System.Linq;
using System.Collections.Generic;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Farming
{
    /// <summary>Explicitly installed in the development scene. Live Main has no farm service.</summary>
    public sealed class FarmService : MonoBehaviour, IFarmPresentationSource
    {
        public static FarmService Instance { get; private set; }
        [SerializeField] private FarmTuning tuning = new();
        [SerializeField, Range(0, 1)] private float developmentRadishPackChance = .1f;
        private GameData owner;
        private double lastMonotonic;
        private bool suspended;
        private bool applicationPaused;
        private bool applicationFocused = true;
        private double nextCreditRetry;
        public FarmTuning Tuning => tuning;
        public FarmState State => Oracle.oracle?.saveData?.Farm;
        public bool Ready => Oracle.oracle != null && Oracle.oracle.HasLoadedCurrentSlotData;
        public string LastError { get; private set; }
        public event Action Changed;
        private bool InTown => Ready && GameManager.Instance != null && GameManager.Instance.IsInTown;
        private void Awake() { Instance = this; lastMonotonic = Time.realtimeSinceStartupAsDouble; }
        private void OnEnable() { Blindsided.EventHandler.OnLoadData += Loaded; Blindsided.EventHandler.OnSaveData += CaptureGrowth; }
        private void OnDisable() { Blindsided.EventHandler.OnLoadData -= Loaded; Blindsided.EventHandler.OnSaveData -= CaptureGrowth; }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Loaded() { owner = null; SettleResume(); }
        private void SettleResume()
        {
            if (!Ready) return;
            owner = Oracle.oracle.saveData;
            owner.Farm ??= new FarmState();
            FarmJournal.UpgradeLegacy(owner.Farm);
            FarmCommands.CaptureGrowthInPlace(owner.Farm, DateTime.UtcNow);
            lastMonotonic = Time.realtimeSinceStartupAsDouble;
            Changed?.Invoke();
        }
        private void Update()
        {
            if (!Ready || suspended) return;
            if (!ReferenceEquals(owner, Oracle.oracle.saveData)) { SettleResume(); return; }
            if (Time.realtimeSinceStartupAsDouble - lastMonotonic >= .25) CaptureGrowth();
            if (Time.realtimeSinceStartupAsDouble >= nextCreditRetry)
            {
                nextCreditRetry = Time.realtimeSinceStartupAsDouble + 1;
                RetryPendingCredit();
            }
        }
        private void CaptureGrowth()
        {
            if (!Ready || suspended) return;
            // Settle this bank's elapsed offline time before any command or early save
            // can adopt it and replace its historical clock baseline.
            if (!ReferenceEquals(owner, Oracle.oracle.saveData)) { SettleResume(); return; }
            var now = Time.realtimeSinceStartupAsDouble;
            FarmCommands.CaptureGrowthInPlace(owner.Farm, DateTime.UtcNow, Math.Max(0, now - lastMonotonic));
            lastMonotonic = now;
        }
        private void OnApplicationPause(bool paused) { applicationPaused = paused; UpdateSuspension(); }
        private void OnApplicationFocus(bool focused) { applicationFocused = focused; UpdateSuspension(); }
        private void UpdateSuspension()
        {
            var value = applicationPaused || !applicationFocused;
            if (suspended == value) return;
            if (value) CaptureGrowth();
            suspended = value;
            if (!value) SettleResume();
        }
        private bool Execute(Func<FarmState, FarmCommandResult> command, bool townRequired)
        {
            if (!Ready || (townRequired && !InTown))
            { LastError = "Return to town to tend the beds."; Changed?.Invoke(); return false; }
            CaptureGrowth();
            var ok = Oracle.oracle.TryCommitFarmCommand(command, out var error);
            LastError = ok ? null : error switch
            {
                "NoReadyBeds" => "No beds are ready to harvest.",
                "NoRadishSeeds" => "Find a Radish seed pack on an adventure first.",
                "BedOccupied" => "This bed already has a crop growing.",
                "BedLocked" => "Prepare the beds before planting.",
                _ => error
            };
            owner = Oracle.oracle.saveData;
            Changed?.Invoke();
            return ok;
        }
        public bool PrepareBeds() => Execute(state => FarmCommands.PrepareBeds(state,
            FarmJournal.NextOperation(state, "prepare-original"), DateTime.UtcNow, tuning), true);
        public bool Plant(string bedId) => Execute(state => FarmCommands.Plant(state, bedId,
            FarmJournal.NextOperation(state, "plant-radish:" + bedId), DateTime.UtcNow, tuning), true);
        public bool HarvestReady() => Execute(state => FarmCommands.HarvestReady(state,
            FarmJournal.NextOperation(state, "harvest-ready"), DateTime.UtcNow, activeElapsedSeconds: 0,
            harvestYieldBonusPercent: CauldronResourceYield.BonusPercent(Oracle.oracle.saveData, "Radish")), true);

        /// <summary>Eligibility and ownership are captured before ordinary completion callbacks.</summary>
        public sealed class StagedTaskCredit
        {
            internal readonly FarmService Source;
            internal readonly GameData Owner;
            internal readonly int Slot;
            internal readonly string OperationId;
            internal StagedTaskCredit(FarmService source, GameData owner, int slot, string operationId)
            { Source = source; Owner = owner; Slot = slot; OperationId = operationId; }
        }
        public StagedTaskCredit StageCompletedTask(StagedTaskCredit existing, TaskData task, HeroBase hero)
        {
            // Retain the completed task's token even after its individual receipt is compacted.
            // An old-owner handle is deliberately not reminted for a newly selected bank.
            if (existing != null) return existing;
            if (!Ready || InTown || hero == null || hero != HeroController.Instance || hero.IsEcho || task == null || task.taskID != 28) return null;
            return StageRadishCompletion(UnityEngine.Random.value < developmentRadishPackChance);
        }
        public StagedTaskCredit StageRadishCompletion(bool rolled)
        {
            if (!Ready) return null;
            var current = Oracle.oracle.saveData;
            var id = FarmJournal.StageCredit(current.Farm, rolled, DateTime.UtcNow);
            return id == null ? null : new StagedTaskCredit(this, current, Oracle.oracle.CurrentSlot, id);
        }
        public bool CommitStagedTask(StagedTaskCredit credit)
        {
            // A callback may end the run or switch slots. Never transfer old-bank credit to a new owner.
            if (credit == null || credit.Source != this || !Ready ||
                !ReferenceEquals(credit.Owner, Oracle.oracle.saveData) || credit.Slot != Oracle.oracle.CurrentSlot) return false;
            var state = credit.Owner.Farm;
            if (state.PendingCredits?.TryGetValue(credit.OperationId, out var pending) == true)
                return CommitPendingCredit(credit.OperationId, pending);
            return FarmJournal.IsCommittedCredit(state, credit.OperationId);
        }
        private bool CommitPendingCredit(string operationId, FarmPendingCredit pending)
        {
            if (!Ready || pending == null) return false;
            if (State.Operations.TryGetValue(operationId, out var receipt))
            {
                if (receipt?.Fingerprint != "adventure-radish:" + (pending.Rolled ? "1" : "0")) return false;
                State.PendingCredits?.Remove(operationId);
                return true;
            }
            return Execute(state => FarmCommands.RecordRadishAdventureCompletion(state, operationId,
                new DateTime(pending.CompletedAtUtcTicks, DateTimeKind.Utc), true, pending.Rolled), false);
        }
        private void RetryPendingCredit()
        {
            // The journal belongs to this loaded bank. No transient queue crosses owner/slot changes.
            var pending = State?.PendingCredits?.FirstOrDefault(x => x.Value != null && x.Value.CompletedAtUtcTicks > 0 &&
                x.Value.CompletedAtUtcTicks <= DateTime.MaxValue.Ticks &&
                FarmJournal.Inspect(State, x.Key, "adventure-radish:" + (x.Value.Rolled ? "1" : "0"), out _) != FarmCommandStatus.Rejected);
            if (pending.HasValue && pending.Value.Value != null)
                CommitPendingCredit(pending.Value.Key, pending.Value.Value);
        }
        public bool CreditRadishCompletion(string operationId, bool rolled)
        {
            if (!Ready || string.IsNullOrWhiteSpace(operationId)) return false;
            // Retry only; a caller cannot create a fresh legacy UUID or change its selected roll.
            if (State.PendingCredits?.TryGetValue(operationId, out var pending) == true)
                return pending != null && pending.Rolled == rolled && CommitPendingCredit(operationId, pending);
            return FarmJournal.TrySequence(State, operationId, out _, out var fingerprint) &&
                fingerprint == "adventure-radish:" + (rolled ? "1" : "0") && FarmJournal.IsCommittedCredit(State, operationId);
        }
        public void Focus(string bedId)
        {
            if (!InTown) return;
            var point = bedId == FarmCommands.WestBedId ? new Vector2(-62, 1) : new Vector2(-58, 1);
            TownWindowManager.Instance?.CloseAllWindows();
            foreach (var camera in FindObjectsByType<TownCameraPan>(FindObjectsInactive.Include))
                if (camera.gameObject.activeInHierarchy) { camera.Focus(point); break; }
        }
        public FarmPresentationSnapshot CapturePresentation()
        {
            var state = State ?? new FarmState();
            state.Seeds.TryGetValue(FarmCommands.RadishSeedId, out var seed);
            double Amount(string name) => Oracle.oracle?.saveData.Resources.TryGetValue(name, out var entry) == true ? entry.Amount : 0;
            return new FarmPresentationSnapshot
            {
                Prepared = state.OriginalBedsPrepared, TownActionsAllowed = InTown,
                Discovered = seed?.IsDiscovered == true, SeedQuantity = seed?.Quantity ?? 0,
                LogQuantity = Amount("Log"), StickQuantity = Amount("Stick"),
                BuildLogCost = tuning.OriginalBedsLogCost, BuildStickCost = tuning.OriginalBedsStickCost,
                Beds = FarmCommands.OriginalBedIds.Select((id, index) =>
                {
                    state.Beds.TryGetValue(id, out var bed);
                    return new FarmBedPresentation { Id = id, Title = index == 0 ? "West bed" : "East bed",
                        Planted = bed?.IsPlanted == true, Ready = bed?.IsReady == true,
                        Progress01 = bed?.IsPlanted == true && bed.DurationSeconds > 0 ? (float)(bed.ElapsedSeconds / bed.DurationSeconds) : 0,
                        RemainingSeconds = bed?.IsPlanted == true ? Math.Max(0, bed.DurationSeconds - bed.ElapsedSeconds) : 0 };
                }).ToArray()
            };
        }
    }
}
