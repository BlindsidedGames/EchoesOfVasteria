#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;

namespace Tests.EditMode
{
    public sealed class FieldsProductionTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);
        private FarmContent content;
        private const string Radish = "recipe.radish.v1";
        [SetUp] public void SetUp() { content = FarmContent.Load(); Assert.IsNotNull(content, "Production content must ship in Resources."); }
        private static GameData Fresh() => new GameData { Farm = new FarmState() };
        private static string Op(GameData data, string fingerprint) => FarmJournal.NextOperation(data.Farm, fingerprint);
        private static GameData Commit(GameData data, FarmCommandResult proposal)
        {
            Assert.IsTrue(proposal.Accepted, proposal.Reason);
            Assert.IsTrue(FarmTransaction.TryPrepare(data, proposal, out var candidate, out var error), error);
            return CurrentSaveCodec.Clone(candidate);
        }
        private GameData Garden(int seeds = 1, int level = 1)
        {
            var data = Fresh();
            data.CompletedNpcTasks.Add("Farmers1");
            data = Commit(data, FarmCommands.BuildFields(data, FarmContent.IntroductionId,
                Op(data, "build:" + FarmContent.IntroductionId), Now, content));
            var build = content.builds[0];
            data.General.MaxRunDistance = 100000;
            foreach (var source in build.sources) data.SkillData[source.associatedSkill.name] = new GameData.SkillProgress { Level = source.requiredSkillLevel };
            foreach (var cost in build.costs) data.Resources[cost.resource.name] = new GameData.ResourceEntry { Amount = cost.amount + 100, Earned = true };
            data = Commit(data, FarmCommands.BuildFields(data, build.questId, Op(data, "build:" + build.questId), Now, content));
            data.Farm.TwinsLevel = level;
            data.Resources[FarmCommands.SeedResourceName("seed.radish")] = new GameData.ResourceEntry { Amount = seeds, Earned = true, Tier = 1 };
            return data;
        }
        private GameData Plant(GameData data, string bed = FarmCommands.WestBedId) => Commit(data,
            FarmCommands.PlantRecipe(data, bed, Radish, Op(data, FarmCommands.PlantFingerprint(bed, Radish)), Now, content));
        private GameData Harvest(GameData data, bool automatic = false) => Commit(data,
            FarmCommands.HarvestFields(data, Op(data, automatic ? "harvest-auto" : "harvest-fields"), Now, content, automatic));

        [Test] public void IntroductionRequiresMeetingAndGrantsStarterPackOnlyOnce()
        {
            var data = Fresh(); var id = Op(data, "build:" + FarmContent.IntroductionId);
            Assert.IsFalse(FarmCommands.BuildFields(data, FarmContent.IntroductionId, id, Now, content).Accepted);
            data.CompletedNpcTasks.Add("Farmers1");
            data = Commit(data, FarmCommands.BuildFields(data, FarmContent.IntroductionId, id, Now, content));
            Assert.IsTrue(FarmContent.Completed(data, FarmContent.IntroductionId));
            Assert.AreEqual(1, data.Farm.TwinsLevel); Assert.AreEqual(0, data.Farm.TwinsXp);
            Assert.IsEmpty(data.Farm.Seeds); Assert.IsEmpty(data.Farm.Beds);
            Assert.AreEqual(1, data.Resources["Radish Seed Pack"].Amount);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.BuildFields(data, FarmContent.IntroductionId, id, Now, content).Status);
            Assert.AreEqual(1, data.Resources["Radish Seed Pack"].Amount);
            Assert.AreEqual(0, data.Farm.GardenCapacity); Assert.AreEqual(0, data.Farm.OrchardCapacity);
        }
        [Test] public void BuildPaymentAndCapacityAreOneProposalAndRetryCannotDoubleCharge()
        {
            var data = Fresh(); data.CompletedNpcTasks.Add("Farmers1");
            data = Commit(data, FarmCommands.BuildFields(data, FarmContent.IntroductionId, Op(data, "build:" + FarmContent.IntroductionId), Now, content));
            data.General.MaxRunDistance = 100000;
            var build = content.builds[0];
            foreach (var source in build.sources) data.SkillData[source.associatedSkill.name] = new GameData.SkillProgress { Level = source.requiredSkillLevel };
            foreach (var cost in build.costs) data.Resources[cost.resource.name] = new GameData.ResourceEntry { Amount = cost.amount };
            var id = Op(data, "build:" + build.questId);
            var proposal = FarmCommands.BuildFields(data, build.questId, id, Now, content);
            var shortResource = build.costs[0].resource.name; data.Resources[shortResource].Amount--;
            Assert.IsFalse(FarmTransaction.TryPrepare(data, proposal, out _, out _));
            Assert.AreEqual(0, data.Farm.GardenCapacity); Assert.IsFalse(FarmContent.Completed(data, build.questId));
            data.Resources[shortResource].Amount++;
            var committed = Commit(data, proposal);
            foreach (var cost in build.costs) Assert.AreEqual(0, committed.Resources[cost.resource.name].Amount);
            Assert.AreEqual(1, committed.Farm.GardenCapacity); Assert.IsTrue(FarmContent.Completed(committed, build.questId));
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.BuildFields(committed, build.questId, id, Now, content).Status);
            Assert.AreEqual(build.costs[0].amount, data.Resources[shortResource].Amount, "Failed/unpublished proposals preserve the source.");
        }
        [Test] public void LaterConstructionRechecksPreviousQuestTwinsAndCanonicalSourceAtHandIn()
        {
            var data = Garden(); var build = content.builds[2];
            data.Farm.TwinsLevel = build.twinsLevel;
            data.General.MaxRunDistance = 100000;
            foreach (var source in build.sources) data.SkillData[source.associatedSkill.name] = new GameData.SkillProgress { Level = source.requiredSkillLevel };
            var id = Op(data, "build:" + build.questId);
            Assert.IsFalse(FarmCommands.BuildFields(data, build.questId, id, Now, content).Accepted);
            data.Quests[build.previousQuestId] = new GameData.QuestRecord { Completed = true };
            data.Farm.TwinsLevel--;
            Assert.IsFalse(FarmCommands.BuildFields(data, build.questId, id, Now, content).Accepted);
            data.Farm.TwinsLevel++; var sourceTask = build.sources.First(s => s.requiredSkillLevel > 1);
            data.SkillData[sourceTask.associatedSkill.name].Level = sourceTask.requiredSkillLevel - 1;
            Assert.IsFalse(FarmCommands.BuildFields(data, build.questId, id, Now, content).Accepted);
            data.SkillData[sourceTask.associatedSkill.name].Level++;
            Assert.IsTrue(FarmCommands.BuildFields(data, build.questId, id, Now, content).Accepted);
            Assert.AreEqual(1, data.Farm.GardenCapacity, "Gate checks never publish a build.");
        }
        [Test] public void InterleavedConstructionPreservesBothPlotTypes()
        {
            var data = Garden(0);
            var steps = new[] {
                ("Farm.Orchard.Build01.v1", 15, 1, 2),
                ("Farm.Garden.Build02.v1", 25, 2, 2),
                ("Farm.Garden.Build03.v1", 65, 3, 2),
                ("Farm.Orchard.Build02.v1", 100, 3, 4),
                ("Farm.Garden.Build04.v1", 130, 4, 4),
                ("Farm.Garden.Build05.v1", 190, 5, 4),
                ("Farm.Orchard.Build03.v1", 225, 5, 6),
                ("Farm.Garden.Build06.v1", 250, 6, 6)
            };
            foreach (var (id, level, fields, orchard) in steps)
            {
                var build = content.Build(id);
                foreach (var source in build.sources)
                    data.SkillData[source.associatedSkill.name] = new GameData.SkillProgress { Level = source.requiredSkillLevel };
                foreach (var cost in build.costs)
                    data.Resources[cost.resource.name] = new GameData.ResourceEntry { Amount = cost.amount, Earned = true };
                data.Farm.TwinsLevel = level - 1;
                var op = Op(data, "build:" + id);
                Assert.False(FarmCommands.BuildFields(data, id, op, Now, content).Accepted, id + " must respect its level gate.");
                data.Farm.TwinsLevel = level;
                data = Commit(data, FarmCommands.BuildFields(data, id, op, Now, content));
                Assert.AreEqual(fields, data.Farm.GardenCapacity, id);
                Assert.AreEqual(orchard, data.Farm.OrchardCapacity, id);
            }
        }
        [Test] public void EarlyOrchardDoesNotRemovePreviouslyBuiltFields()
        {
            var data = Garden(0, 15);
            data.Farm.GardenCapacity = 6;
            var build = content.Build("Farm.Orchard.Build01.v1");
            foreach (var cost in build.costs)
                data.Resources[cost.resource.name] = new GameData.ResourceEntry { Amount = cost.amount, Earned = true };
            data = Commit(data, FarmCommands.BuildFields(data, build.questId, Op(data, "build:" + build.questId), Now, content));
            Assert.AreEqual(6, data.Farm.GardenCapacity);
            Assert.AreEqual(2, data.Farm.OrchardCapacity);
        }
        [Test] public void PlantFreezesTwinsYieldAndDurationAndConsumesOnlyMatchingOwnedSeed()
        {
            var data = Garden(2, 30); var original = data;
            data = Plant(data); var bed = data.Farm.Beds[FarmCommands.WestBedId];
            Assert.AreEqual(1, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount); Assert.AreEqual(2, original.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            Assert.AreEqual(content.baseDurationSeconds * .8, bed.DurationSeconds);
            Assert.AreEqual(content.baseYield * 1.29, bed.FrozenYield, .000001);
            data.Farm.TwinsLevel = 60;
            FarmCommands.TickFields(data.Farm, bed.DurationSeconds);
            var result = Harvest(data);
            Assert.AreEqual(content.baseYield * 1.29, result.Resources["Radish"].Amount, .000001);
            Assert.AreEqual(0, original.Farm.TwinsXp);
        }
        // Cost admission and exactly-once publication belong to the production command,
        // not a copied UI predicate. The rendered pointer test owns its distinct lifecycle risk.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CropSowingRequiresOneMatchingPackAtomicallyAndReplayNeverDebitsAgain(int packs)
        {
            var data = Garden(packs);
            var seed = FarmCommands.SeedResourceName("seed.radish");
            var token = Op(data, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, Radish));
            var proposal = FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, Radish, token, Now, content);
            Assert.AreEqual(packs, data.Resources[seed].Amount);
            Assert.False(data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            if (packs < 1)
            {
                Assert.False(proposal.Accepted); Assert.AreEqual("NoMatchingSeeds", proposal.Reason);
                Assert.IsEmpty(proposal.ResourceDeltas); return;
            }
            Assert.True(proposal.Accepted, proposal.Reason);
            // Recheck balances when publishing a proposal, even after admission succeeded.
            data.Resources[seed].Amount = 0;
            Assert.False(FarmTransaction.TryPrepare(data, proposal, out _, out _));
            Assert.False(data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            data.Resources[seed].Amount = packs;
            data = Commit(data, proposal);
            Assert.AreEqual(packs - 1, data.Resources[seed].Amount);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.PlantRecipe(data,
                FarmCommands.WestBedId, Radish, token, Now, content).Status);
            Assert.False(FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, Radish,
                Op(data, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, Radish)), Now, content).Accepted);
            Assert.AreEqual(packs - 1, data.Resources[seed].Amount);
        }
        [Test]
        public void ExistingGrowingBatchAndPendingCreditSurviveMigrationThenRepeatPaysOne()
        {
            var data = Plant(Garden(18, 20));
            // Model a batch already paid under the old cost. Loading must neither
            // refund that payment nor charge again when preserving the growing batch.
            data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount = 8;
            FarmCommands.TickFields(data.Farm, 123);
            var old = data.Farm.Beds[FarmCommands.WestBedId];
            var batch = old.BatchId; var yield = old.FrozenYield; var duration = old.DurationSeconds;
            var pending = FarmJournal.StageSeed(data.Farm, "seed.radish", true, Now);
            var migrated = Blindsided.SaveData.Migrations.SaveMigrationRunner.TryMigrate(CurrentSaveCodec.Clone(data), "9999.0.0");
            Assert.True(migrated.Succeeded, migrated.Error); data = migrated.Data;
            var bed = data.Farm.Beds[FarmCommands.WestBedId];
            Assert.AreEqual(batch, bed.BatchId); Assert.AreEqual(yield, bed.FrozenYield);
            Assert.AreEqual(duration, bed.DurationSeconds); Assert.AreEqual(123, bed.ElapsedSeconds);
            Assert.AreEqual(8, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            data = Commit(data, FarmCommands.CreditSeed(data.Farm, pending, Now, "seed.radish", true));
            Assert.AreEqual(9, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            data = Commit(data, FarmCommands.SetRepeat(data.Farm, FarmCommands.WestBedId, true,
                Op(data, "repeat:" + FarmCommands.WestBedId + ":1"), Now));
            FarmCommands.TickFields(data.Farm, duration);
            data = Harvest(data, true);
            Assert.True(data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.AreEqual(8, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            Assert.AreEqual(0, data.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.AreEqual(yield, data.Resources["Radish"].Amount, .000001);
        }
        [TestCase(120d, false)] [TestCase(400d, true)]
        public void WaterIsOncePerBatchPreservesElapsedAndLateWaterIsImmediatelyReady(double elapsed, bool ready)
        {
            var initial = Garden(); initial.Farm.FirstHarvestCompleted = true;
            var data = Plant(initial); FarmCommands.TickFields(data.Farm, elapsed);
            var id = Op(data, "water:" + FarmCommands.WestBedId);
            data = Commit(data, FarmCommands.Water(data.Farm, FarmCommands.WestBedId, id, Now));
            var bed = data.Farm.Beds[FarmCommands.WestBedId];
            Assert.AreEqual(elapsed, bed.ElapsedSeconds); Assert.AreEqual(ready, bed.IsReady);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.Water(data.Farm, FarmCommands.WestBedId, id, Now).Status);
            Assert.IsFalse(FarmCommands.Water(data.Farm, FarmCommands.WestBedId, Op(data, "water:" + FarmCommands.WestBedId), Now).Accepted);
            Assert.AreEqual(1, data.Farm.TwinsLevel); Assert.AreEqual(0, data.Farm.TwinsXp);
        }
        [Test] public void HarvestSurplusCarriesAndNeverAwardsHeroFarmingXp()
        {
            var data = Plant(Garden());
            data.SkillData["Farming"] = new GameData.SkillProgress { Level = 1, CurrentXP = 12.5f };
            FarmCommands.TickFields(data.Farm, 1800);
            var harvested = Harvest(data);
            Assert.AreEqual(2, harvested.Farm.TwinsLevel); Assert.AreEqual(5, harvested.Farm.TwinsXp);
            Assert.AreEqual(1, harvested.SkillData["Farming"].Level); Assert.AreEqual(12.5f, harvested.SkillData["Farming"].CurrentXP);
        }
        [Test] public void RepeatHarvestUsesStableBedOrderOnePackAndNoTimeOrWaterCarry()
        {
            var data = Garden(3, 25);
            var second = content.builds[1];
            foreach (var cost in second.costs) data.Resources[cost.resource.name] = new GameData.ResourceEntry { Amount = cost.amount, Earned = true };
            foreach (var source in second.sources) data.SkillData[source.associatedSkill.name] = new GameData.SkillProgress { Level = source.requiredSkillLevel };
            data = Commit(data, FarmCommands.BuildFields(data, second.questId, Op(data, "build:" + second.questId), Now, content));
            data.Farm.TwinsLevel = 20;
            data = Plant(data); data = Plant(data, FarmCommands.EastBedId);
            foreach (var bed in FarmCommands.OriginalBedIds)
                data = Commit(data, FarmCommands.SetRepeat(data.Farm, bed, true, Op(data, "repeat:" + bed + ":1"), Now));
            data = Commit(data, FarmCommands.Water(data.Farm, FarmCommands.WestBedId, Op(data, "water:" + FarmCommands.WestBedId), Now));
            FarmCommands.TickFields(data.Farm, 100000);
            data = Harvest(data, true);
            var west = data.Farm.Beds[FarmCommands.WestBedId]; var east = data.Farm.Beds[FarmCommands.EastBedId];
            Assert.IsTrue(west.IsPlanted); Assert.IsFalse(east.IsPlanted); Assert.IsFalse(east.Repeat);
            Assert.AreEqual(0, west.ElapsedSeconds); Assert.IsFalse(west.Watered);
            Assert.AreEqual(0, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            Assert.AreEqual(23.8, data.Resources["Radish"].Amount, .000001);
        }
        [Test] public void TargetedHarvestLeavesOtherReadyPlotUntouchedAndItsRepeatCanHarvestAgain()
        {
            var data = Garden(3, 25); var second = content.builds[1];
            foreach (var cost in second.costs) data.Resources[cost.resource.name] = new GameData.ResourceEntry { Amount = cost.amount, Earned = true };
            foreach (var source in second.sources) data.SkillData[source.associatedSkill.name] = new GameData.SkillProgress { Level = source.requiredSkillLevel };
            data = Commit(data, FarmCommands.BuildFields(data, second.questId, Op(data, "build:" + second.questId), Now, content));
            data.Farm.TwinsLevel = 20;
            data = Plant(data); data = Plant(data, FarmCommands.EastBedId);
            var west = FarmCommands.WestBedId;
            data = Commit(data, FarmCommands.SetRepeat(data.Farm, west, true, Op(data, "repeat:" + west + ":1"), Now));
            FarmCommands.TickFields(data.Farm, 100000);
            var token = Op(data, "harvest-bed:" + west);
            data = Commit(data, FarmCommands.HarvestFields(data, token, Now, content, false, west));
            Assert.True(data.Farm.Beds[FarmCommands.EastBedId].IsReady, "A plot's action must not harvest another plot.");
            Assert.True(data.Farm.Beds[west].IsPlanted);
            Assert.AreEqual(0, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            Assert.AreEqual(11.9, data.Resources["Radish"].Amount, .000001);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.HarvestFields(data, token, Now, content, false, west).Status);
            FarmCommands.TickFields(data.Farm, 100000);
            data = Commit(data, FarmCommands.HarvestFields(data, Op(data, "harvest-bed:" + west), Now, content, false, west));
            Assert.False(data.Farm.Beds[west].IsPlanted);
            Assert.True(data.Farm.Beds[FarmCommands.EastBedId].IsReady);
            Assert.AreEqual(23.8, data.Resources["Radish"].Amount, .000001);
        }
        [Test] public void RepeatRequiresTwinsTwentyAndDoesNotSpendOnEnable()
        {
            var data = Garden(1, 19); var bed = FarmCommands.WestBedId;
            Assert.IsFalse(FarmCommands.SetRepeat(data.Farm, bed, true, Op(data, "repeat:" + bed + ":1"), Now).Accepted);
            data.Farm.TwinsLevel++;
            data = Commit(data, FarmCommands.SetRepeat(data.Farm, bed, true, Op(data, "repeat:" + bed + ":1"), Now));
            Assert.AreEqual(1, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount); Assert.IsFalse(data.Farm.Beds[bed].IsPlanted);
        }
        [TestCase(49, false)]
        [TestCase(49, true)]
        [TestCase(50, false)]
        [TestCase(50, true)]
        public void PlantingWatersAtFiftyIndependentlyOfAutoReplant(int level, bool repeat)
        {
            var data = Garden(3, level);
            var id = FarmCommands.WestBedId;
            if (repeat) data = Commit(data, FarmCommands.SetRepeat(data.Farm, id, true, Op(data, "repeat:" + id + ":1"), Now));
            data = Plant(data);
            Assert.AreEqual(level >= 50, data.Farm.Beds[id].Watered, "Planting waters at level 50 even with Auto off.");
            FarmCommands.TickFields(data.Farm, 100000);
            data = Harvest(data, repeat);
            if (!repeat) data = Plant(data);
            Assert.True(data.Farm.Beds[id].IsPlanted);
            Assert.AreEqual(data.Farm.TwinsLevel >= 50, data.Farm.Beds[id].Watered, "Both manual and automatic replacements use the unlock.");
            Assert.AreEqual(1, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
        }
        [Test] public void PendingSeedSelectionSurvivesRetryCannotChangeRollAndCreditsOnce()
        {
            var data = Fresh(); var id = FarmJournal.StageSeed(data.Farm, "seed.radish", true, Now);
            Assert.IsNotNull(id);
            Assert.IsFalse(FarmCommands.CreditSeed(data.Farm, id, Now, "seed.radish", false).Accepted);
            Assert.IsFalse(FarmCommands.CreditSeed(data.Farm, id, Now, "seed.corn", true).Accepted);
            var proposal = FarmCommands.CreditSeed(data.Farm, id, Now, "seed.radish", true);
            Assert.IsEmpty(data.Farm.Seeds); Assert.IsTrue(data.Farm.PendingCredits.ContainsKey(id));
            data = Commit(data, proposal);
            Assert.AreEqual(1, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount); Assert.IsEmpty(data.Farm.PendingCredits);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.CreditSeed(data.Farm, id, Now, "seed.radish", true).Status);
        }
        [Test] public void CropHeroGateAndMatchingSeedAreBothRequiredEvenAtHighTwinsLevel()
        {
            var data = Garden(1, 225); var recipe = content.recipes.First(r => !r.orchard && r.source.requiredSkillLevel > 1);
            data.Resources[FarmCommands.SeedResourceName(recipe.seedId)] = new GameData.ResourceEntry { Amount = 1, Earned = true, Tier = 1 };
            var id = Op(data, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, recipe.id));
            Assert.IsFalse(FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, recipe.id, id, Now, content).Accepted);
            data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = recipe.source.requiredSkillLevel };
            data.Resources[FarmCommands.SeedResourceName(recipe.seedId)].Amount = 0;
            Assert.IsFalse(FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, recipe.id, id, Now, content).Accepted);
            data.Resources[FarmCommands.SeedResourceName(recipe.seedId)].Amount = 1;
            data = Commit(data, FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, recipe.id, id, Now, content));
            Assert.AreEqual(1, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            Assert.AreEqual(0, data.Resources[FarmCommands.SeedResourceName(recipe.seedId)].Amount);
        }
        [Test] public void OrchardSaplingDebitAndRegrowthAreOneDurableContract()
        {
            var data = Garden(0, 150); var recipe = content.Recipe("recipe.apple.v1");
            // This fixture models an already-built orchard bank; construction is covered separately.
            data.Farm.OrchardCapacity = 2;
            var bedId = FarmCommands.OrchardBeds[0]; data.Farm.Beds[bedId] = new FarmBedState { Unlocked = true };
            data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = recipe.source.requiredSkillLevel };
            data.Resources[recipe.paidInput.name] = new GameData.ResourceEntry { Amount = 1, Earned = true };
            var id = Op(data, FarmCommands.PlantFingerprint(bedId, recipe.id));
            var proposal = FarmCommands.PlantRecipe(data, bedId, recipe.id, id, Now, content);
            Assert.AreEqual(1, data.Resources[recipe.paidInput.name].Amount);
            data = Commit(data, proposal); Assert.AreEqual(0, data.Resources[recipe.paidInput.name].Amount);
            FarmCommands.TickFields(data.Farm, 100000); data = Harvest(data);
            Assert.IsFalse(data.Farm.Beds[bedId].IsPlanted);
            Assert.IsFalse(data.Farm.Beds[bedId].EstablishedTree);
            Assert.AreEqual(24.9, data.Resources[recipe.output.name].Amount, .000001);
            Assert.IsFalse(FarmCommands.HarvestFields(data, Op(data, "harvest-fields"), Now.AddYears(1), content, false).Accepted);
            Assert.IsFalse(FarmCommands.PlantRecipe(data, bedId, recipe.id, Op(data, FarmCommands.PlantFingerprint(bedId, recipe.id)), Now, content).Accepted);
        }
        [TestCase("oak", false)] [TestCase("birch", false)] [TestCase("spruce", false)]
        [TestCase("oak", true)] [TestCase("birch", true)] [TestCase("spruce", true)]
        public void TimberPaysBothResourcesAndAutoReplantsUntilSaplingsRunOut(string species, bool enableAtMedium)
        {
            var data = Garden(0, 150); var recipe = content.Recipe("recipe." + species + ".v1");
            var id = FarmCommands.OrchardBeds[0]; data.Farm.OrchardCapacity = 1;
            data.Farm.Beds[id] = new FarmBedState { Unlocked = true, Repeat = !enableAtMedium };
            data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = recipe.source.requiredSkillLevel };
            data.Resources[recipe.paidInput.name] = new GameData.ResourceEntry { Amount = 2, Earned = true };
            const int multiplier = 2;
            data.CauldronCardCounts[recipe.output.CardId] = 10000;
            data.CauldronCardCounts[recipe.secondaryOutput.CardId] = 100;
            var logBonus = TimelessEchoes.Upgrades.CauldronResourceYield.BonusPercent(data, recipe.output);
            var stickBonus = TimelessEchoes.Upgrades.CauldronResourceYield.BonusPercent(data, recipe.secondaryOutput);
            data = Commit(data, FarmCommands.PlantRecipe(data, id, recipe.id, Op(data, FarmCommands.PlantFingerprint(id, recipe.id)), Now, content));
            Assert.AreEqual(24.9, data.Farm.Beds[id].FrozenYield, .000001);
            Assert.AreEqual(12.45, data.Farm.Beds[id].FrozenSecondaryYield, .000001);
            Assert.True(data.Farm.Beds[id].Watered, "High-level Auto waters timber plots too.");
            if (!enableAtMedium)
            {
                data.Farm.Beds[id].GrowFully = false; // Existing Auto saves predate the per-tree decision.
                data = CurrentSaveCodec.Clone(data);
            }
            var planted = data.Farm.Beds[id];
            FarmCommands.TickFields(data.Farm, planted.DurationSeconds / 2);
            if (enableAtMedium)
            {
                Assert.True(planted.IsReady, "Manual trees can be chopped at medium size.");
                data = Commit(data, FarmCommands.SetRepeat(data.Farm, id, true, Op(data, "repeat:" + id + ":1"), Now));
                planted = data.Farm.Beds[id];
            }
            Assert.True(planted.FullGrowth, "Auto grows to full size, including after save/load.");
            Assert.False(planted.IsReady, "Auto waits beyond medium maturity.");
            FarmCommands.TickFields(data.Farm, 100000);
            var harvest = Op(data, "harvest-bed:" + id);
            data = Commit(data, FarmCommands.HarvestFields(data, harvest, Now, content, false, id));
            Assert.AreEqual(24.9 * multiplier * (1 + logBonus / 100d), data.Resources[recipe.output.name].Amount, .000001);
            Assert.AreEqual(12.45 * multiplier * (1 + stickBonus / 100d), data.Resources[recipe.secondaryOutput.name].Amount, .000001);
            Assert.AreEqual(0, data.Resources[recipe.paidInput.name].Amount);
            var next = data.Farm.Beds[id];
            Assert.True(next.IsPlanted); Assert.True(next.Watered); Assert.False(next.EstablishedTree);
            Assert.AreEqual(0, next.ElapsedSeconds);
            Assert.True(next.GrowFully, "Auto replants for a full-sized harvest.");
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.HarvestFields(data, harvest, Now, content, false, id).Status);
            FarmCommands.TickFields(data.Farm, 100000); data = Harvest(data, true);
            Assert.Greater(data.Resources[recipe.output.name].Amount, 24.9 * multiplier * (1 + logBonus / 100d));
            Assert.Greater(data.Resources[recipe.secondaryOutput.name].Amount, 12.45 * multiplier * (1 + stickBonus / 100d));
            Assert.AreEqual(0, data.Resources[recipe.paidInput.name].Amount);
            Assert.False(data.Farm.Beds[id].IsPlanted);
            Assert.False(data.Farm.Beds[id].Repeat);
        }
        [Test] public void SeedMissIsDurableAndCannotBeRerolledAfterReload()
        {
            var data = Fresh(); var id = FarmJournal.StageSeed(data.Farm, "seed.corn", false, Now);
            data = Commit(data, FarmCommands.CreditSeed(data.Farm, id, Now, "seed.corn", false));
            Assert.IsEmpty(data.Farm.Seeds); Assert.IsEmpty(data.Farm.PendingCredits);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.CreditSeed(data.Farm, id, Now.AddMonths(1), "seed.corn", false).Status);
            Assert.IsFalse(FarmCommands.CreditSeed(data.Farm, id, Now, "seed.corn", true).Accepted);
        }
        [Test] public void CommittedSeedReceiptAcknowledgesStaleIntentWithoutAnotherPack()
        {
            var data = Fresh(); var id = FarmJournal.StageSeed(data.Farm, "seed.radish", true, Now);
            var selected = data.Farm.PendingCredits[id].DeepClone();
            data = Commit(data, FarmCommands.CreditSeed(data.Farm, id, Now, "seed.radish", true));
            // Restore the pre-publication intent beside the already durable sequence prefix.
            data.Farm.PendingCredits[id] = selected;
            var acknowledgement = FarmCommands.CreditSeed(data.Farm, id, Now.AddDays(1), "seed.radish", true);
            Assert.IsTrue(acknowledgement.Accepted); Assert.IsEmpty(acknowledgement.ResourceDeltas);
            Assert.IsTrue(data.Farm.PendingCredits.ContainsKey(id), "Acknowledgement must publish only after persistence.");
            data = Commit(data, acknowledgement);
            Assert.IsEmpty(data.Farm.PendingCredits); Assert.AreEqual(1, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            Assert.AreEqual(1, data.ResourceStats["Radish Seed Pack"].TotalReceived);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.CreditSeed(data.Farm, id, Now, "seed.radish", true).Status);
        }
        [TestCase(1)] [TestCase(2)]
        public void PaidLegacyRadishKeepsContractButRequiresNewAccessAndPaysOnceWithoutTwinsXp(int journalFormat)
        {
            var data = Fresh(); data.Farm.FormatVersion = journalFormat;
            var tuning = new FarmTuning();
            data.Resources["Log"] = new GameData.ResourceEntry { Amount = 100, Earned = true };
            data.Resources["Stick"] = new GameData.ResourceEntry { Amount = 100, Earned = true };
            var prepareId = journalFormat == 1 ? "old-build" : Op(data, "prepare-original");
            data = Commit(data, FarmCommands.PrepareBeds(data.Farm, prepareId, Now, tuning));
            var seedId = journalFormat == 1 ? "old-seed" : FarmJournal.StageCredit(data.Farm, true, Now);
            data = Commit(data, FarmCommands.RecordRadishAdventureCompletion(data.Farm, seedId, Now, true, true));
            var plantId = journalFormat == 1 ? "old-plant" : Op(data, "plant-radish:" + FarmCommands.WestBedId);
            data = Commit(data, FarmCommands.Plant(data.Farm, FarmCommands.WestBedId, plantId, Now, tuning));
            FarmJournal.UpgradeLegacy(data.Farm);
            data.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds = 1800;
            Assert.AreEqual(0, data.Farm.GardenCapacity);
            Assert.IsFalse(FarmCommands.HarvestFields(data, Op(data, "harvest-fields"), Now, content, false).Accepted);
            Assert.IsFalse(FarmContent.Completed(data, FarmContent.IntroductionId));
            data.CompletedNpcTasks.Add("Farmers1");
            data = Commit(data, FarmCommands.BuildFields(data, FarmContent.IntroductionId, Op(data, "build:" + FarmContent.IntroductionId), Now, content));
            data.General.MaxRunDistance = 100000;
            var build = content.builds[0];
            foreach (var source in build.sources) data.SkillData[source.associatedSkill.name] = new GameData.SkillProgress { Level = source.requiredSkillLevel };
            var paidBatch = data.Farm.Beds[FarmCommands.WestBedId].BatchId;
            data = Commit(data, FarmCommands.BuildFields(data, build.questId, Op(data, "build:" + build.questId), Now, content));
            Assert.AreEqual(paidBatch, data.Farm.Beds[FarmCommands.WestBedId].BatchId, "New construction must preserve the old seed-paid batch.");
            data.Resources["Radish"] = new GameData.ResourceEntry { Amount = 0, Earned = true };
            data.CauldronCardCounts[content.Recipe(Radish).output.CardId] = 10000;
            var bonus = TimelessEchoes.Upgrades.CauldronResourceYield.BonusPercent(data, content.Recipe(Radish).output);
            Assert.Greater(bonus, 0, "Legacy harvest must include a real configured Cauldron bonus.");
            var harvestId = Op(data, "harvest-fields");
            data = Commit(data, FarmCommands.HarvestFields(data, harvestId, Now, content, false));
            Assert.AreEqual(10 * (1 + bonus / 100d), data.Resources["Radish"].Amount, .000001);
            Assert.AreEqual(1, data.Farm.TwinsLevel); Assert.AreEqual(0, data.Farm.TwinsXp);
            Assert.IsFalse(data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.HarvestFields(data, harvestId, Now, content, false).Status);
            Assert.IsFalse(FarmCommands.HarvestFields(data, Op(data, "harvest-fields"), Now.AddYears(1), content, false).Accepted);
        }
        [TestCase("recipe.radish.v1", 1, 600d)]
        [TestCase("recipe.corn.v1", 10, 608.1277064025601d)]
        [TestCase("recipe.turnip.v1", 30, 2796.7685300260973d)]
        [TestCase("recipe.apple.v1", 60, 501.93887882137057d)]
        [TestCase("recipe.corn.v1", 500, 60d)]
        [TestCase("recipe.turnip.v1", 4000, 127.10601231972099d)]
        [TestCase("recipe.radish.v1", 238, 60d)]
        [TestCase("recipe.apple.v1", 714, 60d)]
        [TestCase("recipe.turnip.v1", 5000, 60d)]
        [TestCase("recipe.turnip.v1", 10000, 60d)]
        public void GrowthScalesWithCropLevelAndFieldsSpeedAndWaterHalvesIt(string recipeId, int fieldsLevel, double seconds)
        {
            var data = Garden(2, fieldsLevel);
            var recipe = content.Recipe(recipeId);
            var bedId = recipe.orchard ? FarmCommands.OrchardBeds[0] : FarmCommands.WestBedId;
            if (recipe.orchard) { data.Farm.OrchardCapacity = 1; data.Farm.Beds[bedId] = new FarmBedState { Unlocked = true }; }
            data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = recipe.source.requiredSkillLevel };
            data.Resources[recipe.paidInput.name] = new GameData.ResourceEntry { Amount = 2, Earned = true };
            data = Commit(data, FarmCommands.PlantRecipe(data, bedId, recipeId, Op(data, FarmCommands.PlantFingerprint(bedId, recipeId)), Now, content));
            Assert.AreEqual(seconds, data.Farm.Beds[bedId].DurationSeconds, .000001);
            Assert.AreEqual(fieldsLevel >= 50, data.Farm.Beds[bedId].Watered);
            if (fieldsLevel < 50) data = Commit(data, FarmCommands.Water(data.Farm, bedId, Op(data, "water:" + bedId), Now));
            FarmCommands.TickFields(data.Farm, seconds / 2 - 1);
            Assert.IsFalse(data.Farm.Beds[bedId].IsReady);
            FarmCommands.TickFields(data.Farm, 1);
            Assert.IsTrue(data.Farm.Beds[bedId].IsReady);
        }
        [TestCase("recipe.radish.v1", 1, 10)]
        [TestCase("recipe.corn.v1", 1, 12)]
        [TestCase("recipe.apple.v1", 1, 14)]
        [TestCase("recipe.turnip.v1", 1, 141)]
        [TestCase("recipe.turnip.v1", 5000, 141)]
        public void HigherTierHarvestXpSurvivesWateringAndSaveReload(string recipeId, int fieldsLevel, int expectedXp)
        {
            var data = Garden(2, fieldsLevel);
            var recipe = content.Recipe(recipeId);
            var bedId = recipe.orchard ? FarmCommands.OrchardBeds[0] : FarmCommands.WestBedId;
            if (recipe.orchard) { data.Farm.OrchardCapacity = 1; data.Farm.Beds[bedId] = new FarmBedState { Unlocked = true }; }
            data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = recipe.source.requiredSkillLevel };
            data.Resources[recipe.paidInput.name] = new GameData.ResourceEntry { Amount = 2, Earned = true };
            data = Commit(data, FarmCommands.PlantRecipe(data, bedId, recipeId, Op(data, FarmCommands.PlantFingerprint(bedId, recipeId)), Now, content));
            Assert.AreEqual(expectedXp, data.Farm.Beds[bedId].HarvestXp);
            if (fieldsLevel < 50) data = Commit(data, FarmCommands.Water(data.Farm, bedId, Op(data, "water:" + bedId), Now));
            FarmCommands.TickFields(data.Farm, data.Farm.Beds[bedId].ReadyAfterSeconds);
            var beforeLevel = data.Farm.TwinsLevel;
            var beforeXp = data.Farm.TwinsXp;
            data = Harvest(data);
            var earned = data.Farm.TwinsXp - beforeXp;
            for (var level = beforeLevel; level < data.Farm.TwinsLevel; level++) earned += FarmContent.XpRequired(level);
            Assert.AreEqual(expectedXp, earned, "The saved batch must pay its full XP once, regardless of growth speed.");
        }
        [Test] public void EveryCropReachesThirtySecondWateredFloorAtItsEvenlySpacedTier()
        {
            var ordered = content.recipes.OrderBy(r => r.GrowthLevel).ThenBy(r => r.id, StringComparer.Ordinal).ToArray();
            var caps = new[] { 208, 417, 625, 833, 1042, 1250, 1458, 1667, 1875, 2083, 2292, 2500, 2708, 2917, 3125, 3333, 3542, 3750, 3958, 4167, 4375, 4583, 4792, 5000 };
            Assert.AreEqual(caps.Length, ordered.Length);
            for (var i = 0; i < ordered.Length; i++)
            {
                var recipe = ordered[i];
                var cap = caps[i];
                Assert.AreEqual(cap, content.GrowthCapLevel(recipe), recipe.id);
                Assert.Greater(content.GrowthDuration(recipe, cap - 1), 60, recipe.id + " must not reach the cap early");
                Assert.AreEqual(60, content.GrowthDuration(recipe, cap), recipe.id);
                Assert.AreEqual(60, content.GrowthDuration(recipe, 5000), recipe.id);
                Assert.AreEqual(60, content.GrowthDuration(recipe, int.MaxValue), recipe.id);
                var previous = content.GrowthDuration(recipe, 1);
                for (var level = 2; level <= 5000; level++)
                {
                    var duration = content.GrowthDuration(recipe, level);
                    Assert.LessOrEqual(duration, previous, recipe.id + " at Fields " + level);
                    previous = duration;
                }
            }
        }
        [Test] public void OrchardAutoReplantPaysForEachSaplingAfterReloadWithoutDuplicateHarvest()
        {
            var data = Garden(1, 150);
            var recipe = content.Recipe("recipe.apple.v1"); var bedId = FarmCommands.OrchardBeds[0];
            data.Farm.OrchardCapacity = 1; data.Farm.Beds[bedId] = new FarmBedState { Unlocked = true };
            data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = recipe.source.requiredSkillLevel };
            data.Resources[recipe.paidInput.name] = new GameData.ResourceEntry { Amount = 3, Earned = true };
            data = Commit(data, FarmCommands.PlantRecipe(data, bedId, recipe.id, Op(data, FarmCommands.PlantFingerprint(bedId, recipe.id)), Now, content));
            data.Farm.Beds[bedId].Repeat = true;
            FarmCommands.TickFields(data.Farm, 100000);
            var token = Op(data, "harvest-bed:" + bedId);
            data = Commit(data, FarmCommands.HarvestFields(data, token, Now, content, false, bedId));
            Assert.AreEqual(1, data.Resources[recipe.paidInput.name].Amount);
            Assert.True(data.Farm.Beds[bedId].IsPlanted); Assert.False(data.Farm.Beds[bedId].EstablishedTree);
            Assert.AreEqual(0, data.Farm.Beds[bedId].ElapsedSeconds);
            var fruit = data.Resources[recipe.output.name].Amount;
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.HarvestFields(data, token, Now, content, false, bedId).Status);
            FarmCommands.TickFields(data.Farm, 100000);
            data = Commit(data, FarmCommands.HarvestFields(data, Op(data, "harvest-bed:" + bedId), Now, content, false, bedId));
            Assert.Greater(data.Resources[recipe.output.name].Amount, fruit);
            Assert.AreEqual(0, data.Resources[recipe.paidInput.name].Amount);
        }
        [TestCase(false)] [TestCase(true)]
        public void ManualTreeCanBeChoppedAtMediumOrKeepGrowingOnce(bool keepGrowing)
        {
            var data = Garden(0, 150); var recipe = content.Recipe("recipe.oak.v1");
            var id = FarmCommands.OrchardBeds[0]; data.Farm.OrchardCapacity = 1;
            data.Farm.Beds[id] = new FarmBedState { Unlocked = true };
            data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = recipe.source.requiredSkillLevel };
            data.Resources[recipe.paidInput.name] = new GameData.ResourceEntry { Amount = 1, Earned = true };
            data = Commit(data, FarmCommands.PlantRecipe(data, id, recipe.id, Op(data, FarmCommands.PlantFingerprint(id, recipe.id)), Now, content));
            Assert.False(FarmCommands.KeepGrowing(data.Farm, id, Op(data, "grow-tree:" + id), Now).Accepted, "Not offered before medium maturity.");
            var normalDuration = data.Farm.Beds[id].ReadyAfterSeconds;
            FarmCommands.TickFields(data.Farm, normalDuration);
            Assert.True(data.Farm.Beds[id].IsReady);
            if (keepGrowing)
            {
                data = Commit(data, FarmCommands.KeepGrowing(data.Farm, id, Op(data, "grow-tree:" + id), Now));
                Assert.False(data.Farm.Beds[id].IsReady);
                Assert.AreEqual(normalDuration, data.Farm.Beds[id].ElapsedSeconds);
                Assert.AreEqual(0, data.Resources[recipe.paidInput.name].Amount);
                Assert.False(FarmCommands.KeepGrowing(data.Farm, id, Op(data, "grow-tree:" + id), Now).Accepted, "Cannot extend the same tree repeatedly.");
                Assert.False(FarmCommands.HarvestFields(data, Op(data, "harvest-fields"), Now, content, false).Accepted);
                FarmCommands.TickFields(data.Farm, normalDuration);
            }
            data = Harvest(data);
            Assert.AreEqual(keepGrowing ? 49.8 : 24.9, data.Resources[recipe.output.name].Amount, .000001);
            Assert.False(data.Farm.Beds[id].IsPlanted);
            Assert.False(data.Farm.Beds[id].GrowFully, "The decision ends with this harvest.");
        }
        [Test] public void UTCMonthJumpCannotGrowAndOnlyExplicitActiveTimeAdvancesBatch()
        {
            var data = Plant(Garden());
            var proposal = FarmCommands.HarvestFields(data, Op(data, "harvest-fields"), Now.AddMonths(1), content, false);
            Assert.IsFalse(proposal.Accepted); Assert.AreEqual(0, data.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            data = CurrentSaveCodec.Clone(data); FarmCommands.TickFields(data.Farm, 60);
            Assert.AreEqual(60, data.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds);
        }
        [TestCase(false)] [TestCase(true)] public void FabricatedBatchOrFutureReceiptCannotPay(bool future)
        {
            var data = Plant(Garden()); var bed = data.Farm.Beds[FarmCommands.WestBedId];
            FarmCommands.TickFields(data.Farm, 1800);
            if (future) bed.PlantSequence = data.Farm.LastIssuedSequence + 100;
            else bed.BatchId = "fields:unpaid-forgery:" + FarmCommands.WestBedId;
            var proposal = FarmCommands.HarvestFields(data, Op(data, "harvest-fields"), Now, content, false);
            Assert.IsFalse(proposal.Accepted); Assert.IsEmpty(proposal.ResourceDeltas);
            Assert.IsTrue(bed.IsPlanted); Assert.AreEqual(0, data.Farm.TwinsXp);
        }
    }
}
#endif
