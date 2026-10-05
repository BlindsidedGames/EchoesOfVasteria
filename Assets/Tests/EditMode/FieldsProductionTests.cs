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
        private GameData Garden(int seeds = 9, int level = 1)
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

        [Test] public void IntroductionRequiresActualFarmerMeetingAndGrantsOnlyCompletion()
        {
            var data = Fresh(); var id = Op(data, "build:" + FarmContent.IntroductionId);
            Assert.IsFalse(FarmCommands.BuildFields(data, FarmContent.IntroductionId, id, Now, content).Accepted);
            data.CompletedNpcTasks.Add("Farmers1");
            data = Commit(data, FarmCommands.BuildFields(data, FarmContent.IntroductionId, id, Now, content));
            Assert.IsTrue(FarmContent.Completed(data, FarmContent.IntroductionId));
            Assert.AreEqual(1, data.Farm.TwinsLevel); Assert.AreEqual(0, data.Farm.TwinsXp);
            Assert.IsEmpty(data.Farm.Seeds); Assert.IsEmpty(data.Farm.Beds);
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
        [Test] public void PlantFreezesTwinsYieldAndDurationAndConsumesOnlyMatchingOwnedSeed()
        {
            var data = Garden(18, 30); var original = data;
            data = Plant(data); var bed = data.Farm.Beds[FarmCommands.WestBedId];
            Assert.AreEqual(9, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount); Assert.AreEqual(18, original.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
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
        [TestCase(0)] [TestCase(8)] [TestCase(9)] [TestCase(18)]
        public void CropSowingRequiresNineMatchingPacksAtomicallyAndReplayNeverDebitsAgain(int packs)
        {
            var data = Garden(packs);
            var seed = FarmCommands.SeedResourceName("seed.radish");
            var token = Op(data, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, Radish));
            var proposal = FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, Radish, token, Now, content);
            Assert.AreEqual(packs, data.Resources[seed].Amount);
            Assert.False(data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            if (packs < 9)
            {
                Assert.False(proposal.Accepted); Assert.AreEqual("NoMatchingSeeds", proposal.Reason);
                Assert.IsEmpty(proposal.ResourceDeltas); return;
            }
            Assert.True(proposal.Accepted, proposal.Reason);
            // Recheck balances when publishing a proposal, even after admission succeeded.
            data.Resources[seed].Amount = 8;
            Assert.False(FarmTransaction.TryPrepare(data, proposal, out _, out _));
            Assert.False(data.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            data.Resources[seed].Amount = packs;
            data = Commit(data, proposal);
            Assert.AreEqual(packs - 9, data.Resources[seed].Amount);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.PlantRecipe(data,
                FarmCommands.WestBedId, Radish, token, Now, content).Status);
            Assert.False(FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, Radish,
                Op(data, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, Radish)), Now, content).Accepted);
            Assert.AreEqual(packs - 9, data.Resources[seed].Amount);
        }
        [Test]
        public void ExistingGrowingBatchAndPendingCreditSurviveMigrationThenRepeatPaysNine()
        {
            var data = Plant(Garden(18, 20));
            // Model a batch that was already paid under the old cost. Loading must
            // preserve that contract rather than debit the extra eight packs.
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
            Assert.AreEqual(0, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            Assert.AreEqual(0, data.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.AreEqual(yield, data.Resources["Radish"].Amount, .000001);
        }
        [TestCase(300d, false)] [TestCase(1200d, true)]
        public void WaterIsOncePerBatchPreservesElapsedAndLateWaterIsImmediatelyReady(double elapsed, bool ready)
        {
            var data = Plant(Garden()); FarmCommands.TickFields(data.Farm, elapsed);
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
        [Test] public void RepeatHarvestUsesStableBedOrderNinePacksAndNoTimeOrWaterCarry()
        {
            var data = Garden(27, 20);
            var second = content.builds[1];
            foreach (var cost in second.costs) data.Resources[cost.resource.name].Amount += cost.amount;
            data = Commit(data, FarmCommands.BuildFields(data, second.questId, Op(data, "build:" + second.questId), Now, content));
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
        [Test] public void RepeatRequiresTwinsTwentyAndDoesNotSpendOnEnable()
        {
            var data = Garden(1, 19); var bed = FarmCommands.WestBedId;
            Assert.IsFalse(FarmCommands.SetRepeat(data.Farm, bed, true, Op(data, "repeat:" + bed + ":1"), Now).Accepted);
            data.Farm.TwinsLevel++;
            data = Commit(data, FarmCommands.SetRepeat(data.Farm, bed, true, Op(data, "repeat:" + bed + ":1"), Now));
            Assert.AreEqual(1, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount); Assert.IsFalse(data.Farm.Beds[bed].IsPlanted);
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
            var data = Garden(9, 225); var recipe = content.recipes.First(r => !r.orchard && r.source.requiredSkillLevel > 1);
            data.Resources[FarmCommands.SeedResourceName(recipe.seedId)] = new GameData.ResourceEntry { Amount = 9, Earned = true, Tier = 1 };
            var id = Op(data, FarmCommands.PlantFingerprint(FarmCommands.WestBedId, recipe.id));
            Assert.IsFalse(FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, recipe.id, id, Now, content).Accepted);
            data.SkillData[recipe.source.associatedSkill.name] = new GameData.SkillProgress { Level = recipe.source.requiredSkillLevel };
            data.Resources[FarmCommands.SeedResourceName(recipe.seedId)].Amount = 0;
            Assert.IsFalse(FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, recipe.id, id, Now, content).Accepted);
            data.Resources[FarmCommands.SeedResourceName(recipe.seedId)].Amount = 9;
            data = Commit(data, FarmCommands.PlantRecipe(data, FarmCommands.WestBedId, recipe.id, id, Now, content));
            Assert.AreEqual(9, data.Resources[FarmCommands.SeedResourceName("seed.radish")].Amount);
            Assert.AreEqual(0, data.Resources[FarmCommands.SeedResourceName(recipe.seedId)].Amount);
        }
        [Test] public void OrchardSaplingDebitAndFiniteHarvestAreOneDurableContract()
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
            Assert.AreEqual(24.9, data.Resources[recipe.output.name].Amount, .000001);
            Assert.IsFalse(FarmCommands.HarvestFields(data, Op(data, "harvest-fields"), Now.AddYears(1), content, false).Accepted);
            Assert.IsFalse(FarmCommands.PlantRecipe(data, bedId, recipe.id, Op(data, FarmCommands.PlantFingerprint(bedId, recipe.id)), Now, content).Accepted);
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
