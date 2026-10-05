using System;
using System.Linq;
using System.Collections.Generic;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Quests;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Farming
{
    /// <summary>Production Fields owner. Commands commit through the existing immutable farm lane.</summary>
    public sealed class FarmService : MonoBehaviour, IFarmPresentationSource
    {
        public static FarmService Instance { get; private set; }
        [SerializeField] private FarmTuning tuning = new();
        private GameData owner;
        private bool skipGrowthFrame = true;
        private FarmContent content;
        public FarmContent Content => content ? content : content = FarmContent.Load();
        private int gateSignature = int.MinValue;
        private double lastMonotonic;
        private bool suspended;
        private bool applicationPaused;
        private bool applicationFocused = true;
        private double nextCreditRetry;
        public FarmTuning Tuning => tuning;
        public FarmState State => Oracle.oracle?.saveData?.Farm;
        public bool Ready => Oracle.oracle != null && Oracle.oracle.HasLoadedCurrentSlotData;
        private string lastError;
        public string LastError => lastError == null ? null : ErrorText(lastError);
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
            // Fresh slots bypass migration; initialize the revision without importing old construction.
            owner.Farm.ProductionRevision = 1;
            gateSignature = int.MinValue;
            skipGrowthFrame = true;
            lastMonotonic = Time.realtimeSinceStartupAsDouble;
            Changed?.Invoke();
        }
        private void Update()
        {
            if (!Ready || suspended) return;
            if (!ReferenceEquals(owner, Oracle.oracle.saveData)) { SettleResume(); return; }
            var seconds = Time.unscaledDeltaTime;
            if (skipGrowthFrame) skipGrowthFrame = false;
            else if (seconds >= 0 && seconds <= Time.maximumDeltaTime) FarmCommands.TickFields(owner.Farm, seconds);
            if (owner.Farm.TwinsLevel >= 20 && owner.Farm.Beds.Any(b => FarmCommands.KnownBed(b.Key) && b.Value?.Repeat == true && b.Value.IsReady))
                Execute(state => FarmCommands.HarvestFields(owner, FarmJournal.NextOperation(state, "harvest-auto"), DateTime.UtcNow, Content, true), false);
            if (Time.realtimeSinceStartupAsDouble >= nextCreditRetry)
            {
                nextCreditRetry = Time.realtimeSinceStartupAsDouble + 1;
                RetryPendingCredit();
                RefreshProgression();
            }
        }
        private void CaptureGrowth()
        {
            if (!Ready || suspended) return;
            // A newly selected bank must be rebound before any command or snapshot capture.
            if (!ReferenceEquals(owner, Oracle.oracle.saveData)) { SettleResume(); return; }
            // Update already records only actual running frames. Save/load never settle a UTC gap.
        }
        private void OnApplicationPause(bool paused) { applicationPaused = paused; UpdateSuspension(); }
        private void OnApplicationFocus(bool focused) { applicationFocused = focused; UpdateSuspension(); }
        private void UpdateSuspension()
        {
            var value = applicationPaused;
            if (suspended == value) return;
            if (value) CaptureGrowth();
            suspended = value;
            if (!value) { SettleResume(); skipGrowthFrame = true; }
        }
        private static string QuestTitle(string questId, string fallback)
        {
            var quest = QuestManager.Instance?.GetQuestData(questId);
            return quest?.questName != null && !quest.questName.IsEmpty ? quest.questName.GetLocalizedString() : fallback;
        }
        private static string ErrorText(string code) => code switch
        {
            "TownRequired" => ToolkitLocalization.Text("fields.error.TownRequired", "Return to town to tend the beds."),
            "NoReadyBeds" => ToolkitLocalization.Text("fields.error.NoReadyBeds", "No beds are ready to harvest."),
            "NoMatchingSeeds" => ToolkitLocalization.Text("fields.error.NoMatchingSeeds", "You need 9 matching seed packs to sow a field."),
            "NoMatchingSapling" => ToolkitLocalization.Text("fields.error.NoMatchingSapling", "Find a matching sapling on an adventure first."),
            "NoRadishSeeds" => ToolkitLocalization.Text("fields.error.NoRadishSeeds", "Find a Radish seed pack on an adventure first."),
            "BedOccupied" => ToolkitLocalization.Text("fields.error.BedOccupied", "This bed already has a crop growing."),
            "BedLocked" => ToolkitLocalization.Text("fields.error.BedLocked", "Prepare the beds before planting."),
            _ => ToolkitLocalization.Text("fields.error.unavailable", "This Fields action is unavailable ({0}).", code)
        };
        private bool Execute(Func<FarmState, FarmCommandResult> command, bool townRequired)
        {
            if (!Ready || (townRequired && !InTown))
            { lastError = "TownRequired"; Changed?.Invoke(); return false; }
            CaptureGrowth();
            var ok = Oracle.oracle.TryCommitFarmCommand(command, out var error);
            lastError = ok ? null : error;
            owner = Oracle.oracle.saveData;
            if (ok) RefreshProgression();
            Changed?.Invoke();
            return ok;
        }
        private void RefreshProgression()
        {
            if (!Ready) return;
            var data = Oracle.oracle.saveData;
            var signature = HashCode.Combine(data.Farm.TwinsLevel, FarmContent.SkillLevel(data, "Farming"),
                FarmContent.SkillLevel(data, "Woodcutting"), FarmContent.SkillLevel(data, "Mining"), data.General.MaxRunDistance);
            if (signature == gateSignature) return;
            gateSignature = signature;
            QuestManager.Instance?.RefreshFieldsProgression();
            Changed?.Invoke();
        }
        // The noticeboard can acknowledge the narrative introduction on an adventure.
        // Construction and tending still require town; the introduction pays no resources.
        public bool BuildQuest(string questId) => Execute(state => FarmCommands.BuildFields(Oracle.oracle.saveData, questId,
            FarmJournal.NextOperation(state, "build:" + questId), DateTime.UtcNow, Content), questId != FarmContent.IntroductionId);
        public bool PrepareBeds() => BuildQuest("Farm.Garden.Build01.v1");
        public bool Plant(string bedId) => Plant(bedId, FarmCommands.RadishRecipeId);
        public bool Plant(string bedId, string recipeId) => Execute(state => FarmCommands.PlantRecipe(Oracle.oracle.saveData, bedId, recipeId,
            FarmJournal.NextOperation(state, FarmCommands.PlantFingerprint(bedId, recipeId)), DateTime.UtcNow, Content), true);
        public bool Water(string bedId) => Execute(state => FarmCommands.Water(state, bedId,
            FarmJournal.NextOperation(state, "water:" + bedId), DateTime.UtcNow), true);
        public bool SetRepeat(string bedId, bool repeat) => Execute(state => FarmCommands.SetRepeat(state, bedId, repeat,
            FarmJournal.NextOperation(state, "repeat:" + bedId + ":" + (repeat ? "1" : "0")), DateTime.UtcNow), true);
        public bool HarvestReady() => Execute(state => FarmCommands.HarvestFields(Oracle.oracle.saveData,
            FarmJournal.NextOperation(state, "harvest-fields"), DateTime.UtcNow, Content, false), true);

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
            if (!Ready || InTown || hero == null || hero != HeroController.Instance || hero.IsEcho || task == null || !Content ||
                !FarmContent.Completed(Oracle.oracle.saveData, FarmContent.IntroductionId)) return null;
            var recipe = Content.recipes.FirstOrDefault(r => !r.orchard && r.source == task);
            if (recipe == null || !Content.CanPlant(Oracle.oracle.saveData, recipe)) return null;
            var id = FarmJournal.StageSeed(State, recipe.seedId, UnityEngine.Random.value < Content.seedChance, DateTime.UtcNow);
            return id == null ? null : new StagedTaskCredit(this, Oracle.oracle.saveData, Oracle.oracle.CurrentSlot, id);
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
            if (!string.IsNullOrEmpty(pending.SeedId))
                return Execute(state => FarmCommands.CreditSeed(state, operationId,
                    new DateTime(pending.CompletedAtUtcTicks, DateTimeKind.Utc), pending.SeedId, pending.Rolled), false);
            return Execute(state => FarmCommands.RecordRadishAdventureCompletion(state, operationId,
                new DateTime(pending.CompletedAtUtcTicks, DateTimeKind.Utc), true, pending.Rolled), false);
        }
        private void RetryPendingCredit()
        {
            // The journal belongs to this loaded bank. No transient queue crosses owner/slot changes.
            var pending = State?.PendingCredits?.FirstOrDefault(x => x.Value != null && x.Value.CompletedAtUtcTicks > 0 &&
                x.Value.CompletedAtUtcTicks <= DateTime.MaxValue.Ticks &&
                FarmJournal.Inspect(State, x.Key, FarmJournal.CreditFingerprint(x.Value), out _) != FarmCommandStatus.Rejected);
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
            var garden = Array.IndexOf(FarmCommands.GardenBeds, bedId);
            var orchard = Array.IndexOf(FarmCommands.OrchardBeds, bedId);
            var point = garden >= 0 ? FieldsWorldView.GardenAnchors[garden] + Vector2.one :
                orchard >= 0 ? FieldsWorldView.OrchardAnchors[orchard] : new Vector2(-58,-12);
            TownWindowManager.Instance?.CloseAllWindows();
            foreach (var camera in FindObjectsByType<TownCameraPan>(FindObjectsInactive.Include))
                if (camera.gameObject.activeInHierarchy) { camera.Focus(point); break; }
        }
        public FarmPresentationSnapshot CapturePresentation()
        {
            var data = Oracle.oracle?.saveData;
            var state = State ?? new FarmState();
            var config = Content;
            if (!config) return new FarmPresentationSnapshot();
            double Amount(Resource resource) => ResourceManager.Instance ? ResourceManager.Instance.GetAmount(resource, data) :
                data?.Resources?.TryGetValue(resource.name, out var entry) == true ? entry.Amount : 0;
            var ready = QuestManager.Instance?.GetNoticeboardEntries()
                .Where(e => e.Category == QuestNoticeboardCategory.Ready).Select(e => e.Quest.questId).ToHashSet() ?? new HashSet<string>();
            var recipes = config.recipes.Where(r => r != null).Select(r =>
            {
                var quantity = r.paidInput ? Amount(r.paidInput) : 0;
                var discovered = r.paidInput ? ResourceManager.Instance ? ResourceManager.Instance.IsUnlocked(r.paidInput, data) :
                    data?.Resources?.TryGetValue(r.paidInput.name, out var input) == true && input.Earned : false;
                return new FarmRecipePresentation { Id=r.id, Title=r.paidInput ? ToolkitLocalization.Name(r.paidInput) : ToolkitLocalization.Text("fields.recipe-seeds", "{0} seeds", ToolkitLocalization.Name(r.output)),
                    SeedQuantity=quantity, Discovered=discovered, Eligible=config.CanPlant(data,r), Orchard=r.orchard,
                    Icon=r.packIcon, UnknownIcon=r.unknownIcon, RequiredHeroLevel=r.source ? r.source.requiredSkillLevel : 1 };
            }).ToArray();
            var builds = new List<FarmBuildPresentation>();
            var introComplete = FarmContent.Completed(data,FarmContent.IntroductionId);
            builds.Add(new FarmBuildPresentation { QuestId=FarmContent.IntroductionId, Title=QuestTitle(FarmContent.IntroductionId, "Fields of Our Own"), TwinsLevel=1,
                Completed=introComplete, CanTurnIn=InTown && ready.Contains(FarmContent.IntroductionId), Costs=ToolkitLocalization.Text("fields.meet-twins", "Meet Flora and Tillman"),
                Status=introComplete ? ToolkitLocalization.Text("fields.complete", "Complete") : data?.CompletedNpcTasks?.Contains("Farmers1") == true ? ToolkitLocalization.Text("fields.hear-introduction", "Hear Flora and Tillman's introduction") : ToolkitLocalization.Text("fields.meet-twins-adventure", "Meet Flora and Tillman on an adventure") });
            foreach (var build in config.builds)
            {
                var completed=FarmContent.Completed(data,build.questId);
                var gate=config.BuildGate(data,build);
                var costText=string.Join(" + ",build.costs.Select(c => ToolkitLocalization.Text("fields.material-cost", "{0:N0} {1}", c.amount, ToolkitLocalization.Name(c.resource))));
                var materials=build.costs.All(c=>Amount(c.resource)>=c.amount);
                builds.Add(new FarmBuildPresentation { QuestId=build.questId,Title=QuestTitle(build.questId, build.title),TwinsLevel=build.twinsLevel,Completed=completed,
                    Costs=costText,CanTurnIn=InTown && !completed && gate==null && materials && ready.Contains(build.questId),
                    Status=completed ? ToolkitLocalization.Text("fields.built", "Built") : gate ?? (materials ? ToolkitLocalization.Text("fields.ready-build", "Ready to build") : ToolkitLocalization.Text("fields.gather-materials", "Gather the construction materials")) });
            }
            var radish = recipes.FirstOrDefault(r => r.Id == FarmCommands.RadishRecipeId);
            return new FarmPresentationSnapshot
            {
                DisplayName=config.DisplayName,TwinsLevel=state.TwinsLevel,TwinsXp=state.TwinsXp,TwinsXpRequired=FarmContent.XpRequired(state.TwinsLevel),
                YieldMultiplier=FarmContent.YieldMultiplier(state.TwinsLevel),SpeedFactor=FarmContent.SpeedFactor(state.TwinsLevel),Recipes=recipes,Builds=builds.ToArray(),
                Prepared=state.GardenCapacity>0,TownActionsAllowed=InTown,Discovered=radish?.Discovered==true,SeedQuantity=radish?.SeedQuantity >= long.MaxValue ? long.MaxValue : (long)(radish?.SeedQuantity ?? 0),
                LogQuantity=data?.Resources?.TryGetValue("Log",out var log)==true?log.Amount:0,
                StickQuantity=data?.Resources?.TryGetValue("Stick",out var stick)==true?stick.Amount:0,
                BuildLogCost=10,BuildStickCost=20,
                Beds=FarmCommands.GardenBeds.Concat(FarmCommands.OrchardBeds).Select((id,index)=>
                {
                    state.Beds.TryGetValue(id,out var bed);
                    var recipe=config.Recipe(bed?.RecipeId ?? bed?.SelectedRecipeId);
                    var orchard=index>=6;
                    return new FarmBedPresentation { Id=id,Title=orchard ? ToolkitLocalization.Text("fields.orchard-number", "Orchard plot {0}", index-5) : ToolkitLocalization.Text("fields.bed-number", "Bed {0}", index+1),
                        Unlocked=FarmCommands.AccessibleBed(state,id),Orchard=orchard,RecipeId=recipe?.id,RecipeTitle=ToolkitLocalization.Name(recipe?.output),
                        Icon=bed?.IsPlanted==true?recipe?.output?.icon:null,Watered=bed?.Watered==true,Repeat=bed?.Repeat==true,Planted=bed?.IsPlanted==true,Ready=bed?.IsReady==true,
                        Progress01=bed?.IsPlanted==true && bed.ReadyAfterSeconds>0?Mathf.Clamp01((float)(bed.ElapsedSeconds/bed.ReadyAfterSeconds)):0,
                        RemainingSeconds=bed?.IsPlanted==true?Math.Max(0,bed.ReadyAfterSeconds-bed.ElapsedSeconds):0 };
                }).ToArray()
            };
        }
    }
}
