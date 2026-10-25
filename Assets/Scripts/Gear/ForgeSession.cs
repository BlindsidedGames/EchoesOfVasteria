using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Gear.UI;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.SaveData.StaticReferences;
namespace TimelessEchoes.Gear
{
    /// <summary>Forge selection, pending craft and automation lifetime, independent of either UI renderer.</summary>
    public sealed class ForgeSession : MonoBehaviour
    {
        [SerializeField] private ForgeCatalog catalog;
        private CraftingService crafting;
        private EquipmentController equipment;
        private ResourceManager resources;
        private CoreSO selectedCore;
        private string selectedSlot;
        private GearItem lastCrafted;
        private bool isAutoCrafting, resultVisible, initialized;
        private Coroutine autoCraftCoroutine;
        private readonly Dictionary<ConversionType, ConversionPipeline> conversions = new();
        public event Action Changed;
        public event Action AutomationStopped;
        public ForgeCatalog Catalog => catalog;
        public CoreSO Core => selectedCore;
        public string Slot => selectedSlot;
        public GearItem Pending => lastCrafted;
        public const int AutomationUnlockLevel = 3;
        public bool AutomationUnlocked => (Oracle.oracle?.saveData?.CraftingMasteryLevel ?? 0) >= AutomationUnlockLevel;
        public bool IsAutoCrafting => isAutoCrafting;
        public bool HasResult => resultVisible && lastCrafted != null;
        public bool CanReplace => HasResult && !isAutoCrafting;
        public void Initialize(ForgeCatalog value)
        {
            if (initialized) { RestoreFirstLesson(); return; }
            catalog = value; crafting = CraftingService.Instance; equipment = EquipmentController.Instance; resources = ResourceManager.Instance;
            if (!catalog || !crafting || !resources || !equipment) throw new InvalidOperationException("Forge services must be ready before opening");
            conversions[ConversionType.Ingot] = ConversionPipelineFactory.CreateIngotPipeline();
            conversions[ConversionType.Crystal] = ConversionPipelineFactory.CreateCrystalPipeline(catalog.slime);
            conversions[ConversionType.Chunk] = ConversionPipelineFactory.CreateChunkPipeline(catalog.stone);
            conversions[ConversionType.Core] = ConversionPipelineFactory.CreateCorePipeline(catalog.ConversionResources);
            initialized = true;
            Blindsided.EventHandler.OnLoadData += Loaded;
            RestoreSelections();
            RestoreFirstLesson();
        }
        private void RestoreFirstLesson()
        {
            if (!ForgeFirstLesson.Active) return;
            selectedCore = catalog.cores[0].core;
            if (ForgeFirstLesson.Stage >= 3) selectedSlot = "Weapon";
            conversions[ConversionType.Ingot].DesiredAmount = System.Math.Max(1, selectedCore.ingotCost);
            if (ForgeFirstLesson.Stage == 4 && lastCrafted == null)
            {
                lastCrafted = ForgeFirstLesson.Restore(selectedCore);
                resultVisible = lastCrafted != null;
            }
        }
        private void RestoreSelections()
        {
            var preferences = Oracle.oracle?.saveData?.SavedPreferences;
            selectedCore = catalog.cores.FirstOrDefault(x => x.core && x.core.name == preferences?.LastSelectedForgeCore)?.core ?? catalog.cores.FirstOrDefault()?.core;
            selectedSlot = !string.IsNullOrWhiteSpace(preferences?.LastSelectedForgeSlot) ? preferences.LastSelectedForgeSlot : equipment.Slots.FirstOrDefault() ?? "Weapon";
            if (preferences != null) { preferences.LastSelectedForgeCore = selectedCore ? selectedCore.name : null; preferences.LastSelectedForgeSlot = selectedSlot; }
            conversions[ConversionType.Ingot].DesiredAmount = ForgeIngotCraftAmount;
            conversions[ConversionType.Crystal].DesiredAmount = ForgeCrystalCraftAmount;
            conversions[ConversionType.Chunk].DesiredAmount = ForgeChunkCraftAmount;
            conversions[ConversionType.Core].DesiredAmount = ForgeCoreCraftAmount;
            Notify();
        }
        private void Loaded()
        {
            // Never continue a pending batch against a newly loaded profile.
            if (autoCraftCoroutine != null) StopCoroutine(autoCraftCoroutine);
            autoCraftCoroutine = null; isAutoCrafting = false; lastCrafted = null; resultVisible = false;
            RestoreSelections();
            RestoreFirstLesson();
        }
        public void SelectCore(CoreSO value)
        {
            if (ForgeFirstLesson.Active)
            {
                if (value != catalog.cores[0].core || ForgeFirstLesson.Stage != 1) return;
                ForgeFirstLesson.Advance(2); Save("first-lesson-core");
            }
            if (catalog.Find(value) == null) return;
            bool changed = value != selectedCore;
            if (changed) StopAutoCrafting();
            selectedCore = value;
            if (Oracle.oracle?.saveData?.SavedPreferences != null) Oracle.oracle.saveData.SavedPreferences.LastSelectedForgeCore = value.name;
            if (changed) resultVisible = false;
            Notify();
        }
        public void SelectSlot(string value)
        {
            if (ForgeFirstLesson.Active)
            {
                if (value != "Weapon" || ForgeFirstLesson.Stage != 2) return;
                ForgeFirstLesson.Advance(3); Save("first-lesson-weapon");
            }
            if (!equipment.Slots.Contains(value)) return;
            if (lastCrafted != null && lastCrafted.slot != value) { SalvageService.Instance?.Salvage(lastCrafted, isAuto: true); lastCrafted = null; resultVisible = false; }
            bool changed = selectedSlot != value;
            if (changed) StopAutoCrafting();
            selectedSlot = value;
            if (changed) resultVisible = false;
            if (Oracle.oracle?.saveData?.SavedPreferences != null) Oracle.oracle.saveData.SavedPreferences.LastSelectedForgeSlot = value;
            Notify();
        }
        public bool CanCraft()
        {
            if (ForgeFirstLesson.Active && ForgeFirstLesson.Stage != 3) return false;
            if (!initialized || !selectedCore) return false;
            var binding = catalog.Find(selectedCore); var ingot = binding?.ingotResource ? binding.ingotResource : selectedCore.requiredIngot;
            return binding?.coreResource && ingot && resources.GetAmount(ingot) >= Math.Max(0, selectedCore.ingotCost) && resources.GetAmount(binding.coreResource) >= 1;
        }
        public double MaxCrafts
        {
            get
            {
                var binding = catalog.Find(selectedCore); if (binding == null || !selectedCore || !binding.coreResource) return 0;
                var ingot = binding.ingotResource ? binding.ingotResource : selectedCore.requiredIngot;
                if (!ingot) return 0;
                return Math.Max(0, Math.Min(Math.Floor(resources.GetAmount(binding.coreResource)), Math.Floor(resources.GetAmount(ingot) / Math.Max(1, selectedCore.ingotCost))));
            }
        }
        public void Craft()
        {
            if (isAutoCrafting) { StopAutoCrafting(); return; }
            if (!CanCraft()) { Notify(); return; }
            if (lastCrafted != null) { SalvageService.Instance?.Salvage(lastCrafted, isAuto: false); lastCrafted = null; }
            lastCrafted = crafting.Craft(selectedCore, selectedSlot, null, catalog.Find(selectedCore).coreResource);
            if (lastCrafted == null) { Notify(); return; }
            if (ForgeFirstLesson.Active)
            {
                Oracle.oracle.saveData.General.ForgeFirstLessonItem = ForgeFirstLesson.Record(lastCrafted);
                ForgeFirstLesson.Advance(4); resultVisible = true; Notify(); Save("first-lesson-craft"); return;
            }
            if (equipment.GetEquipped(lastCrafted.slot) == null)
            {
                if (!equipment.Equip(lastCrafted)) { resultVisible = true; Notify(); return; }

                lastCrafted = null; resultVisible = false;
            }
            else resultVisible = true;
            Notify(); Save("craft");
        }
        public void Replace()
        {
            if (ForgeFirstLesson.ReconcileExistingWeapon()) { Notify(); return; }
            var completingFirstLesson = ForgeFirstLesson.Active;
            if (!CanReplace || !equipment.Equip(lastCrafted)) return;
            if (completingFirstLesson)
            {
                ForgeFirstLesson.Advance(5);
                Oracle.oracle.saveData.General.ForgeFirstLessonItem = null;
                Oracle.oracle.saveData.General.ForgeIntroductionSeen = true;
                TimelessEchoes.Quests.QuestManager.Instance?.TryTurnInQuest(ForgeFirstLesson.QuestId);
            }
            lastCrafted = null; resultVisible = false; Notify(); Save("equip");
        }
        public static double ClampAmount(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 1 : Math.Max(1, Math.Floor(value));
        public double Desired(ConversionType type) => conversions[type].DesiredAmount;
        public void SetAmount(ConversionType type, double value) { conversions[type].DesiredAmount = ForgeFirstLesson.Active ? System.Math.Max(1, selectedCore.ingotCost) : ClampAmount(value); Notify(); }
        public bool CanConvert(ConversionType type) => (!ForgeFirstLesson.Active || (ForgeFirstLesson.Stage == 0 && type == ConversionType.Ingot)) && !isAutoCrafting && conversions[type].CanPerform(resources, selectedCore);
        public double ConversionMaximum(ConversionType type) => conversions[type].GetCalculatedMaxAmount(resources, selectedCore);
        public double ConversionAmount(ConversionType type) => conversions[type].GetMaxAmount(resources, selectedCore, Desired(type));
        public void Convert(ConversionType type)
        {
            if (!CanConvert(type) || !conversions[type].PerformConversion(resources, selectedCore)) return;
            if (ForgeFirstLesson.Active) ForgeFirstLesson.Advance(1);
            var amount = ClampAmount(Desired(type)); conversions[type].DesiredAmount = amount;
            switch (type) { case ConversionType.Ingot: ForgeIngotCraftAmount = amount; break; case ConversionType.Crystal: ForgeCrystalCraftAmount = amount; break; case ConversionType.Chunk: ForgeChunkCraftAmount = amount; break; case ConversionType.Core: ForgeCoreCraftAmount = amount; break; }
            Notify(); Save("conversion");
        }
        public void StartAutoCrafting()
        {
            if (ForgeFirstLesson.Active || !AutomationUnlocked) return;
            if (isAutoCrafting) { StopAutoCrafting(); return; }
            if (!CanCraft()) return;
            isAutoCrafting = true;
            autoCraftCoroutine = StartCoroutine(CraftUntilUpgradeCoroutine()); Notify();
        }
        public void StopAutoCrafting()
        {
            if (!isAutoCrafting) return;
            isAutoCrafting = false;
            if (autoCraftCoroutine != null) StopCoroutine(autoCraftCoroutine);
            autoCraftCoroutine = null; AutomationStopped?.Invoke(); Notify();
        }

        private void Notify() => Changed?.Invoke();
        private static void Save(string operation) { try { Blindsided.EventHandler.SaveData(); } catch (Exception e) { Debug.LogError($"SaveData after {operation} failed: {e}"); } }
        private void OnDestroy()
        {
            if (!initialized) return;
            StopAutoCrafting(); Blindsided.EventHandler.OnLoadData -= Loaded;
        }
        // Automation below retains the existing batching, salvage and upgrade-stop rules.
        private int GetCurrentCraftsPerSecond()
        {
            var config = crafting?.Config;
            if (config == null) return 10;

            int baseSpeed = Mathf.Max(1, config.baseCraftsPerSecond);
            int maxSpeed = config.maxCraftsPerSecond;

            // Get milestone bonus from skill controller
            float milestoneBonus = 0f;
            var controller = SkillController.Instance;
            if (controller != null)
                milestoneBonus = controller.Aggregator.GetForgeCraftSpeedBonus();

            int total = baseSpeed + Mathf.RoundToInt(milestoneBonus);

            // Apply cap if configured
            if (maxSpeed > 0)
                total = Mathf.Min(total, maxSpeed);

            return Mathf.Max(1, total);
        }

        private IEnumerator CraftUntilUpgradeCoroutine()
        {
            // Calculate dynamic speed from config + milestone bonuses
            var config = crafting?.Config;
            float batchInterval = config != null ? config.batchInterval : 0.1f;
            int craftsPerSec = GetCurrentCraftsPerSecond();

            // For low speeds (≤ batchInterval rate), use longer wait times
            // For high speeds (> batchInterval rate), batch multiple crafts per frame
            int craftsPerBatch = Mathf.Max(1, Mathf.RoundToInt(craftsPerSec * batchInterval));
            float waitTime = craftsPerSec > 0 ? Mathf.Max(batchInterval, 1.0f / craftsPerSec) : batchInterval;
            var wait = new WaitForSecondsRealtime(waitTime);

            // Turbo mode: batch salvage yields when processing multiple crafts (>1) for maximum speed
            // Uses expected value calculation instead of rolling each item individually
            bool turboMode = craftsPerBatch > 1;
            int turboSalvageCount = 0; // Accumulated items to batch salvage

            // Hot-reload config once per second
            float lastConfigCheck = Time.unscaledTime;
            const float configCheckInterval = 1.0f;

            // Throttle timers for UI updates during autocraft
            float lastVisualUpdate = Time.unscaledTime;
            float lastStatsUpdate = Time.unscaledTime;
            float lastResourceUpdate = Time.unscaledTime - 0.5f; // Stagger by 0.5s from stats
            const float visualUpdateInterval = 0.1f;   // 10 Hz
            const float statsUpdateInterval = 1.0f;    // 1 Hz
            const float resourceUpdateInterval = 1.0f; // 1 Hz

            // Capture baseline affix stat set at the start of the session if Lock Stats is enabled
            HashSet<StatDefSO> baselineSet = null;
            if (StaticReferences.LockAutocraftStatSet)
            {
                var baseline = equipment?.GetEquipped(selectedSlot);
                if (baseline != null)
                    baselineSet = BuildAffixStatSet(baseline);
            }

            var pendingAutoSalvage = lastCrafted != null;
            bool shouldBreak = false;

            while (isAutoCrafting && !shouldBreak)
            {
                // Hot-reload config check (once per second)
                if (Time.unscaledTime - lastConfigCheck >= configCheckInterval)
                {
                    lastConfigCheck = Time.unscaledTime;
                    config = crafting?.Config;
                    batchInterval = config != null ? config.batchInterval : 0.1f;
                    int newCraftsPerSec = GetCurrentCraftsPerSecond();
                    if (newCraftsPerSec != craftsPerSec)
                    {
                        craftsPerSec = newCraftsPerSec;
                        craftsPerBatch = Mathf.Max(1, Mathf.RoundToInt(craftsPerSec * batchInterval));
                        waitTime = craftsPerSec > 0 ? Mathf.Max(batchInterval, 1.0f / craftsPerSec) : batchInterval;
                        wait = new WaitForSecondsRealtime(waitTime);
                        turboMode = craftsPerBatch > 1;
                    }
                }

                // Batch multiple crafts per frame for high speeds
                // Wrap in ResourceManager batch to defer OnInventoryChanged until batch ends
                var rm = ResourceManager.Instance;
                rm?.BeginBatch();
                try
                {
                for (int batch = 0; batch < craftsPerBatch && isAutoCrafting; batch++)
                {
                    bool isLastInBatch = (batch == craftsPerBatch - 1);

                    // Discard previous craft - turbo mode accumulates for batch salvage
                    if (pendingAutoSalvage && lastCrafted != null)
                    {
                        if (turboMode)
                        {
                            // Fast path: release to pool and count for batch salvage
                            GearObjectPool.ReleaseItem(lastCrafted);
                            turboSalvageCount++;
                        }
                        else
                        {
                            // Normal path: salvage for resources
                            SalvageService.Instance?.Salvage(lastCrafted, isAuto: true);
                        }
                        lastCrafted = null;
                        pendingAutoSalvage = false;
                    }

                    if (!CanCraft())
                    {

                        shouldBreak = true;
                        break;
                    }

                    if (selectedCore == null || crafting == null)
                    {
                        shouldBreak = true;
                        break;
                    }

                    var coreRes = catalog.Find(selectedCore)?.coreResource;
                    var craftedItem = crafting.Craft(selectedCore, selectedSlot, null, coreRes);
                    if (craftedItem == null)
                    {
                        Notify();
                        shouldBreak = true;
                        break;
                    }

                    lastCrafted = craftedItem;

                    var eq = equipment?.GetEquipped(lastCrafted.slot);

                    // Check for stop conditions (always check, may stop early)
                    bool isUpgrade = !StaticReferences.NeverStopAutocraft && UpgradeEvaluator.IsPotentialUpgrade(crafting, lastCrafted, eq);
                    bool isVastium = !StaticReferences.NeverStopAutocraft && StaticReferences.StopAutocraftOnVastium &&
                                     lastCrafted?.rarity?.name == "Vastium";
                    bool isStopping = isUpgrade || isVastium;

                    // Throttled UI updates during autocraft
                    float now = Time.unscaledTime;

                    // Visual preview: 10 Hz or immediate on stop
                    bool doVisualUpdate = isStopping || (isLastInBatch && (now - lastVisualUpdate) >= visualUpdateInterval);
                    if (doVisualUpdate)
                    {
                        lastVisualUpdate = now;
                        resultVisible = true;
                        Notify();
                    }

                    // Stats rebuild: 1 Hz or immediate on stop
                    bool doStatsUpdate = isStopping || (isLastInBatch && (now - lastStatsUpdate) >= statsUpdateInterval);
                    if (doStatsUpdate)
                    {
                        lastStatsUpdate = now;
                        Notify();

                    }

                    // Resource display: 1 Hz staggered or immediate on stop
                    bool doResourceUpdate = isStopping || (isLastInBatch && (now - lastResourceUpdate) >= resourceUpdateInterval);
                    if (doResourceUpdate)
                    {
                        lastResourceUpdate = now;
                        Notify();
                    }

                    if (isUpgrade)
                    {
                        bool passesLock = true;
                        if (StaticReferences.LockAutocraftStatSet)
                        {
                            if (baselineSet != null)
                            {
                                var rolledSet = BuildAffixStatSet(lastCrafted);
                                passesLock = AffixSetsEqual(baselineSet, rolledSet);
                            }
                            else
                            {
                                // No baseline equipped; allow any upgrade to stop
                                passesLock = true;
                            }
                        }

                        if (passesLock)
                        {

                            shouldBreak = true;
                            break; // leave lastCrafted for player to review/replace/salvage
                        }
                    }

                    if (isVastium)
                    {

                        shouldBreak = true;
                        break;
                    }

                    pendingAutoSalvage = true;
                }

                // Batch salvage turbo mode items INSIDE the batch to avoid double OnInventoryChanged
                if (turboSalvageCount > 0 && selectedCore != null)
                {
                    SalvageService.Instance?.BatchSalvageWithinBatch(selectedCore, turboSalvageCount);
                    turboSalvageCount = 0;
                }
                }
                finally
                {
                    rm?.EndBatch();
                }

                if (!shouldBreak)
                {
                    Notify();
                    yield return wait;
                }
            }

            // Final batch salvage for any remaining turbo items
            if (turboSalvageCount > 0 && selectedCore != null)
            {
                SalvageService.Instance?.BatchSalvage(selectedCore, turboSalvageCount);
                turboSalvageCount = 0;
            }

            isAutoCrafting = false;
            autoCraftCoroutine = null;
            Notify();

            AutomationStopped?.Invoke();
        }

        private static HashSet<StatDefSO> BuildAffixStatSet(GearItem item)
        {
            var set = new HashSet<StatDefSO>();
            if (item?.affixes != null)
            {
                for (int i = 0; i < item.affixes.Count; i++)
                {
                    var a = item.affixes[i];
                    if (a?.stat != null)
                        set.Add(a.stat);
                }
            }
            return set;
        }

        private static bool AffixSetsEqual(HashSet<StatDefSO> a, HashSet<StatDefSO> b)
        {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            return a.SetEquals(b);
        }

    }
}
