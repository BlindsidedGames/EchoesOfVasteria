using System;
using System.Linq;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Farming
{
    public static partial class FarmCommands
    {
        public static readonly string[] GardenBeds = { WestBedId, EastBedId, "farm.field2.west", "farm.field2.east", "farm.field3.west", "farm.field3.east" };
        public static readonly string[] OrchardBeds = Enumerable.Range(1, 6).Select(i => "farm.orchard." + i).ToArray();
        public static readonly string[] CropNames = { "radish", "corn", "wheat", "watermelone", "carrot", "spud", "tomato", "lettuce", "cucumber", "leek", "parsnip", "pepper", "chillie", "pumking", "strawberry", "funion", "turnip" };
        public static bool KnownSeed(string id) => id != null && CropNames.Any(n => id == "seed." + n);
        public static string SeedResourceName(string id)
        {
            if (!KnownSeed(id)) throw new ArgumentException("Unknown seed", nameof(id));
            var crop = id.Substring(5);
            return char.ToUpperInvariant(crop[0]) + crop.Substring(1) + " Seed Pack";
        }
        public static bool KnownRecipe(string id) => id != null && CropNames.Concat(new[] { "apple", "pear", "peach", "cherry" }).Any(n => id == "recipe." + n + ".v1");
        public static bool KnownBuild(string id) => id == FarmContent.IntroductionId ||
            Enumerable.Range(1, 6).Any(i => id == "Farm.Garden.Build" + i.ToString("D2") + ".v1") ||
            Enumerable.Range(1, 3).Any(i => id == "Farm.Orchard.Build" + i.ToString("D2") + ".v1");
        public static bool KnownBed(string id) => GardenBeds.Contains(id) || OrchardBeds.Contains(id);
        public static string PlantFingerprint(string bedId, string recipeId) => "plant:" + bedId + ":" + recipeId;
        public static string ReplaceTreeFingerprint(string bedId, string recipeId) => "replace-tree:" + bedId + ":" + recipeId;
        public static FarmCommandResult BuildFields(GameData owner, string questId, string operationId, DateTime now, FarmContent content)
        {
            if (!content || owner?.Farm == null) return Reject("MissingFarmContent");
            var result = Begin(owner.Farm, operationId, "build:" + questId, now);
            if (!result.Accepted) return result;
            if (FarmContent.Completed(owner, questId)) return Reject("ConstructionAlreadyCompleted");
            if (questId == FarmContent.IntroductionId)
            {
                if (owner.CompletedNpcTasks?.Contains("Farmers1") != true) return Reject("MeetFarmersFirst");
                result.ResourceDeltas[SeedResourceName(RadishSeedId)] = 1;
                result.CompletedQuests.Add(questId); return result;
            }
            var build = content.Build(questId);
            var gate = content.BuildGate(owner, build);
            if (gate != null) return Reject(gate);
            if (build.gardenCapacity < 0 || build.gardenCapacity > GardenBeds.Length ||
                build.orchardCapacity < 0 || build.orchardCapacity > OrchardBeds.Length ||
                build.gardenCapacity + build.orchardCapacity == 0 || build.costs == null || build.costs.Count == 0) return Reject("InvalidConstruction");
            foreach (var cost in build.costs)
            {
                if (cost == null || !cost.resource || cost.amount <= 0) return Reject("InvalidBuildCost");
                result.ResourceDeltas.TryGetValue(cost.resource.name, out var total);
                result.ResourceDeltas[cost.resource.name] = total - cost.amount;
            }
            // Each expansion adds to its own plots without removing the other kind.
            result.Candidate.GardenCapacity = Math.Max(owner.Farm.GardenCapacity, build.gardenCapacity);
            result.Candidate.OrchardCapacity = Math.Max(owner.Farm.OrchardCapacity, build.orchardCapacity);
            for (int i = 0; i < build.gardenCapacity; i++) Unlock(result.Candidate, GardenBeds[i]);
            for (int i = 0; i < build.orchardCapacity; i++) Unlock(result.Candidate, OrchardBeds[i]);
            result.CompletedQuests.Add(questId);
            return result;
        }
        private static void Unlock(FarmState state, string id)
        {
            if (!state.Beds.TryGetValue(id, out var bed) || bed == null) state.Beds[id] = bed = new FarmBedState();
            bed.Unlocked = true;
        }
        public static bool AccessibleBed(FarmState state, string id)
        {
            if (state == null) return false;
            var garden = Array.IndexOf(GardenBeds, id);
            var orchard = Array.IndexOf(OrchardBeds, id);
            return garden >= 0 ? garden < state.GardenCapacity : orchard >= 0 && orchard < state.OrchardCapacity;
        }
        public static FarmCommandResult PlantRecipe(GameData owner, string id, string recipeId, string operationId, DateTime now, FarmContent content)
        {
            if (owner?.Farm == null || !content || !KnownRecipe(recipeId)) return Reject("MissingFarmContent");
            var result = Begin(owner.Farm, operationId, PlantFingerprint(id, recipeId), now);
            if (!result.Accepted) return result;
            var error = PlantInto(result, owner, id, content.Recipe(recipeId), operationId, content);
            return error == null ? result : Reject(error);
        }
        public static FarmCommandResult ReplaceTree(GameData owner, string id, string recipeId, string expectedBatchId, string operationId, DateTime now, FarmContent content)
        {
            if (owner?.Farm == null || !content || !KnownRecipe(recipeId)) return Reject("MissingFarmContent");
            var result = Begin(owner.Farm, operationId, ReplaceTreeFingerprint(id, recipeId), now);
            if (!result.Accepted) return result;
            if (!OrchardBeds.Contains(id) || !AccessibleBed(result.Candidate, id) ||
                !result.Candidate.Beds.TryGetValue(id, out var bed) || bed?.IsPlanted != true) return Reject("CannotReplaceTree");
            if (string.IsNullOrEmpty(expectedBatchId) || bed.BatchId != expectedBatchId) return Reject("TreeChanged");
            var recipe = content.Recipe(recipeId);
            if (recipe?.orchard != true || recipeId == bed.RecipeId) return Reject("CannotReplaceTree");
            // Clear only the unpublished candidate. Failed payment or validation leaves the old tree intact.
            bed.BatchId = null;
            var error = PlantInto(result, owner, id, recipe, operationId, content);
            return error == null ? result : Reject(error);
        }
        private static string PlantInto(FarmCommandResult result, GameData owner, string id, FarmRecipe recipe, string operationId, FarmContent content, bool regrowingTree = false)
        {
            var state = result.Candidate;
            if (!KnownBed(id) || !AccessibleBed(state, id) || !state.Beds.TryGetValue(id, out var bed) || bed == null) return "BedLocked";
            if (bed.IsPlanted) return "BedOccupied";
            if (!content.CanPlant(owner, recipe) || recipe.orchard != OrchardBeds.Contains(id) || !recipe.output) return "RecipeLocked";
            if (!Finite(content.baseDurationSeconds) || content.baseDurationSeconds <= 0 || !Finite(content.durationMultiplierPerCropLevel) || content.durationMultiplierPerCropLevel < 1 || !Finite(content.baseYield) || content.baseYield <= 0 || content.harvestXp <= 0) return "InvalidRecipeTuning";
            if (!Finite(content.minimumDurationSeconds) || content.minimumDurationSeconds <= 0 || content.minimumCropCapLevel <= 60 || content.lastCropCapLevel < content.minimumCropCapLevel) return "InvalidRecipeTuning";
            var duration = content.GrowthDuration(recipe, state.TwinsLevel);
            if (!Finite(duration) || duration <= 0) return "InvalidRecipeTuning";
            if (!recipe.paidInput || !recipe.orchard && recipe.paidInput.name != SeedResourceName(recipe.seedId)) return "MissingPaidInput";
            owner.Resources.TryGetValue(recipe.paidInput.name, out var input);
            result.ResourceDeltas.TryGetValue(recipe.paidInput.name, out var staged);
            var cost = FarmContent.PlantingCost(recipe.orchard);
            if (!regrowingTree && (input == null || !Finite(input.Amount) || input.Amount + staged < cost))
                return recipe.orchard ? "NoMatchingSapling" : "NoMatchingSeeds";
            if (!regrowingTree) result.ResourceDeltas[recipe.paidInput.name] = staged - cost;
            if (!FarmJournal.TrySequence(state, operationId, out var sequence, out _)) return "InvalidPlantReceipt";
            bed.PlantSequence = sequence;
            bed.BatchId = "fields:" + operationId + ":" + id;
            bed.SelectedRecipeId = bed.RecipeId = recipe.id;
            bed.DurationSeconds = duration;
            bed.EstablishedTree = regrowingTree;
            bed.FrozenYield = content.baseYield * FarmContent.YieldMultiplier(state.TwinsLevel);
            bed.HarvestXp = content.HarvestExperience(recipe);
            bed.ElapsedSeconds = 0; bed.Watered = false; bed.LastGrowthUtcTicks = 0;
            result.OperationReceipt.BatchIds.Add(bed.BatchId);
            return null;
        }
        public static void TickFields(FarmState state, double seconds)
        {
            if (state == null || !Finite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            foreach (var pair in state.Beds)
            {
                var bed = pair.Value;
                if (!KnownBed(pair.Key) || !AccessibleBed(state, pair.Key) || bed == null || !bed.IsPlanted || bed.IsReady) continue;
                if (!Finite(bed.DurationSeconds) || bed.DurationSeconds <= 0 || !Finite(bed.ElapsedSeconds) || bed.ElapsedSeconds < 0) throw new InvalidOperationException("Invalid growth state.");
                bed.ElapsedSeconds = Math.Min(bed.ReadyAfterSeconds, bed.ElapsedSeconds + seconds);
            }
        }
        public static FarmCommandResult Water(FarmState state, string id, string operationId, DateTime now)
        {
            var result = Begin(state, operationId, "water:" + id, now);
            if (!result.Accepted) return result;
            if (!KnownBed(id) || !AccessibleBed(state, id) || !result.Candidate.Beds.TryGetValue(id, out var bed) || bed == null || !bed.IsPlanted || bed.IsReady || bed.Watered) return Reject("CannotWater");
            bed.Watered = true; return result;
        }
        public static FarmCommandResult SetRepeat(FarmState state, string id, bool repeat, string operationId, DateTime now)
        {
            var result = Begin(state, operationId, "repeat:" + id + ":" + (repeat ? "1" : "0"), now);
            if (!result.Accepted) return result;
            if (!KnownBed(id) || !AccessibleBed(state, id) || repeat && state.TwinsLevel < 20 || !result.Candidate.Beds.TryGetValue(id, out var bed) || bed == null) return Reject("RepeatLocked");
            bed.Repeat = repeat; return result;
        }
        public static FarmCommandResult HarvestFields(GameData owner, string operationId, DateTime now, FarmContent content, bool automatic, string bedId = null)
        {
            if (owner?.Farm == null || !content) return Reject("MissingFarmContent");
            if (bedId != null && (!KnownBed(bedId) || automatic)) return Reject("UnknownBed");
            if (automatic && owner.Farm.TwinsLevel < 20) return Reject("RepeatLocked");
            var result = Begin(owner.Farm, operationId, bedId != null ? "harvest-bed:" + bedId : automatic ? "harvest-auto" : "harvest-fields", now);
            if (!result.Accepted) return result;
            foreach (var id in GardenBeds.Concat(OrchardBeds))
            {
                if (bedId != null && id != bedId) continue;
                if (!AccessibleBed(result.Candidate, id) || !result.Candidate.Beds.TryGetValue(id, out var bed) || bed == null || !bed.IsReady || automatic && !bed.Repeat) continue;
                var recipe = content.Recipe(bed.RecipeId);
                var legacy = bed.BatchId.StartsWith("radish:", StringComparison.Ordinal);
                if (recipe == null || !recipe.output || !Finite(bed.DurationSeconds) || !Finite(bed.ElapsedSeconds) ||
                    !ValidPaidBatch(owner.Farm, id, bed, legacy)) return Reject("InvalidHarvestBatch");
                result.ResourceDeltas.TryGetValue(recipe.output.name, out var total);
                var bonus = CauldronResourceYield.BonusPercent(owner, recipe.output);
                result.ResourceDeltas[recipe.output.name] = total + (legacy ? bed.HarvestYield : bed.FrozenYield) * (1 + bonus / 100d);
                result.OperationReceipt.BatchIds.Add(bed.BatchId); result.HarvestedBeds.Add(id);
                if (legacy)
                {
                    var legacyPlant = bed.BatchId.Substring(7);
                    if (FarmJournal.TrySequence(owner.Farm, legacyPlant, out var sequence, out _)) bed.LastHarvestedPlantSequence = sequence;
                    bed.LegacyBatchId = null;
                    result.Candidate.HarvestedBatchIds.Remove(bed.BatchId);
                }
                else
                {
                    bed.LastHarvestedPlantSequence = bed.PlantSequence;
                    FarmContent.AddXp(result.Candidate, bed.HarvestXp);
                    result.Candidate.FirstHarvestCompleted = true;
                }
                bed.BatchId = null; bed.RecipeId = null; bed.DurationSeconds = bed.ElapsedSeconds = bed.FrozenYield = 0; bed.HarvestXp = 0; bed.HarvestYield = 0; bed.LastGrowthUtcTicks = 0; bed.Watered = false;
                if (recipe.orchard || bed.Repeat && result.Candidate.TwinsLevel >= 20)
                {
                    // Begin one new cycle; never carry elapsed time or watering into it.
                    var error = PlantInto(result, owner, id, content.Recipe(bed.SelectedRecipeId), operationId, content, recipe.orchard);
                    if (error != null) bed.Repeat = false;
                }
            }
            return result.HarvestedBeds.Count == 0 ? Reject("NoReadyBeds") : result;
        }
        private static bool ValidPaidBatch(FarmState state, string id, FarmBedState bed, bool legacy)
        {
            if (legacy)
            {
                if (!OriginalBedIds.Contains(id) || bed.RecipeId != RadishRecipeId || bed.HarvestYield <= 0 || state.HarvestedBatchIds.Contains(bed.BatchId)) return false;
                var plantId = bed.BatchId.Substring(7);
                if (FarmJournal.TrySequence(state, plantId, out var sequence, out _))
                    return sequence > bed.LastHarvestedPlantSequence && FarmJournal.IsCommittedPlant(state, plantId, id);
                return bed.BatchId == bed.LegacyBatchId;
            }
            if (bed.PlantSequence <= bed.LastHarvestedPlantSequence || bed.PlantSequence > state.LastIssuedSequence ||
                !Finite(bed.FrozenYield) || bed.FrozenYield <= 0 || bed.HarvestXp <= 0 || !bed.BatchId.StartsWith("fields:", StringComparison.Ordinal) ||
                (!bed.BatchId.EndsWith(":" + id, StringComparison.Ordinal) || bed.BatchId.Length <= 8 + id.Length)) return false;
            var token = bed.BatchId.Substring(7, bed.BatchId.Length - 7 - id.Length - 1);
            if (!FarmJournal.TrySequence(state, token, out var plantSequence, out var fingerprint) || plantSequence != bed.PlantSequence ||
                FarmJournal.Inspect(state, token, fingerprint, out _) != FarmCommandStatus.AlreadyApplied) return false;
            if (fingerprint != PlantFingerprint(id, bed.RecipeId) && fingerprint != ReplaceTreeFingerprint(id, bed.RecipeId) && fingerprint != "harvest-auto" && fingerprint != "harvest-fields" && fingerprint != "harvest-bed:" + id) return false;
            return !state.CompletedSequences.TryGetValue(plantSequence, out var receipt) || receipt.BatchIds?.Contains(bed.BatchId) == true;
        }

        public static FarmCommandResult CreditSeed(FarmState state, string operationId, DateTime now, string seedId, bool rolled)
        {
            if (!KnownSeed(seedId) || state == null) return Reject("UnknownSeed");
            var fingerprint = "seed:" + seedId + ":" + (rolled ? "1" : "0");
            if (state.PendingCredits.TryGetValue(operationId, out var selected) && selected != null)
            {
                if (selected.SeedId != seedId || selected.Rolled != rolled) return Reject("PendingRollConflict");
                if (FarmJournal.Inspect(state, operationId, fingerprint, out _) == FarmCommandStatus.AlreadyApplied)
                {
                    var acknowledged = state.DeepClone(); acknowledged.PendingCredits.Remove(operationId);
                    return new FarmCommandResult { Status = FarmCommandStatus.Accepted, Candidate = acknowledged, Reason = "CommittedIntentAcknowledged" };
                }
            }
            var result = Begin(state, operationId, fingerprint, now);
            if (!result.Accepted) return result;
            if (!state.PendingCredits.TryGetValue(operationId, out var pending) || pending == null || pending.SeedId != seedId || pending.Rolled != rolled) return Reject("PendingRollConflict");
            result.Candidate.PendingCredits.Remove(operationId);
            if (!rolled) return result;
            result.ResourceDeltas[SeedResourceName(seedId)] = 1;
            return result;
        }
    }
}
