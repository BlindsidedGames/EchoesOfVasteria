#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Blindsided.SaveData;
using NUnit.Framework;
using Sirenix.Serialization;
using TimelessEchoes.Farming;

namespace Tests.EditMode
{
    // Unity's NUnit runner executes this fixture sequentially; its singleton save override
    // must not be used concurrently with another save fixture or a live Play session.
    public sealed class FarmTransactionTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly FarmTuning tuning = new FarmTuning();
        private string testRoot;
        private string previousRoot;
        private MethodInfo setRoot;

        [SetUp]
        public void SetUp()
        {
            setRoot = typeof(SaveManager).GetMethod("SetRootPathForTests", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(setRoot, "Refusing disk tests without the explicit test save root API.");
            var rootField = typeof(SaveManager).GetField("rootPathOverride", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(rootField, "Cannot preserve the existing save root override.");
            previousRoot = (string)rootField.GetValue(null);
            testRoot = Path.Combine(Path.GetTempPath(), "EOVFarmTransactions_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);
            setRoot.Invoke(null, new object[] { testRoot });
        }

        [TearDown]
        public void TearDown()
        {
            if (setRoot != null) setRoot.Invoke(null, new object[] { previousRoot });
            if (testRoot != null && Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
        }

        [TestCase(9, 20)]
        [TestCase(10, 19)]
        [TestCase(0, 0)]
        public void InsufficientMaterialsLeaveBothBalancesFarmAndQuestReceiptsUntouched(double logs, double sticks)
        {
            var live = Fixture(logs, sticks);
            var proposal = FarmCommands.PrepareBeds(live.Farm, "build", Start, tuning);
            Assert.IsFalse(FarmTransaction.TryPrepare(live, proposal, out var candidate, out var error));
            Assert.IsNull(candidate);
            StringAssert.Contains("Insufficient", error);
            Assert.AreEqual(logs, live.Resources["Log"].Amount);
            Assert.AreEqual(sticks, live.Resources["Stick"].Amount);
            Assert.IsFalse(live.Farm.OriginalBedsPrepared);
            Assert.IsEmpty(live.Farm.Operations);
            Assert.IsFalse(live.Quests.ContainsKey("Farm.PrepareBeds"));
            Assert.AreEqual(7, live.ResourceStats["Log"].TotalSpent);
        }

        [Test]
        public void SuccessfulBuildProposesExactlyOnePaymentAndPreservesUnrelatedProgress()
        {
            var live = Fixture();
            var build = FarmCommands.PrepareBeds(live.Farm, "build", Start, tuning);
            Assert.IsTrue(FarmTransaction.TryPrepare(live, build, out var candidate, out var error), error);
            Assert.AreEqual(10, candidate.Resources["Log"].Amount);
            Assert.AreEqual(20, candidate.Resources["Stick"].Amount);
            Assert.AreEqual(17, candidate.ResourceStats["Log"].TotalSpent);
            Assert.AreEqual(500, candidate.ResourceStats["Log"].TotalReceived);
            Assert.IsTrue(candidate.Quests["Farm.PrepareBeds"].Completed);
            Assert.AreEqual("build", candidate.Farm.OriginalBedsBuildOperationId);
            AssertPreservedProgress(candidate);
            Assert.AreEqual(20, live.Resources["Log"].Amount);
            Assert.IsFalse(live.Farm.OriginalBedsPrepared);
            var retry = FarmCommands.PrepareBeds(candidate.Farm, "build", Start, tuning);
            Assert.IsFalse(FarmTransaction.TryPrepare(candidate, retry, out _, out _));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-1)]
        public void PositiveHarvestRejectsInvalidExistingBalance(double balance)
        {
            var live = PlantedBoth();
            live.Resources["Radish"].Amount = balance;
            var harvest = FarmCommands.HarvestReady(live.Farm, "harvest", Start.AddHours(1));
            Assert.IsFalse(FarmTransaction.TryPrepare(live, harvest, out var candidate, out var error));
            Assert.IsNull(candidate);
            StringAssert.Contains("invalid Radish", error);
            Assert.IsTrue(live.Farm.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.IsEmpty(live.Farm.HarvestedBatchIds);
        }

        [Test]
        public void HarvestUsesCapturedResourceStatsWithoutMultipliersAdventureStatsOrSeedRecursion()
        {
            var live = PlantedBoth();
            var proposal = FarmCommands.HarvestReady(live.Farm, "harvest", Start.AddHours(1));
            Assert.IsTrue(FarmTransaction.TryPrepare(live, proposal, out var candidate, out var error), error);
            Assert.AreEqual(27, candidate.Resources["Radish"].Amount);
            Assert.IsTrue(candidate.Resources["Radish"].Earned);
            Assert.AreEqual(3, candidate.Resources["Radish"].Tier);
            Assert.AreEqual(123.25, candidate.Resources["Radish"].BestPerMinute);
            Assert.AreEqual(320, candidate.ResourceStats["Radish"].TotalReceived);
            Assert.AreEqual(19, candidate.ResourceStats["Radish"].TotalSpent);
            Assert.AreEqual(44, candidate.General.TasksCompleted);
            Assert.AreEqual(900, candidate.General.TotalResourcesGathered);
            Assert.AreEqual(2, candidate.Farm.Seeds[FarmCommands.RadishSeedId].LifetimeAcquired);
            Assert.AreEqual(0, candidate.Farm.Seeds[FarmCommands.RadishSeedId].Quantity);
            AssertPreservedProgress(candidate);
        }

        [Test]
        public void CurrentWriteProjectionRoundTripsFarmAndMeaningfulProgressWithoutObsoletePurchases()
        {
            var source = PlantedBoth();
            source.UpgradeLevels["old purchased attack"] = 8;
            source.StatUpgradesMigratedToGear = true;
            source.DuckHelmetSanitized = true;
            var loaded = SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(source), DataFormat.Binary);
            Assert.AreEqual(1, loaded.Farm.Operations["plant-east"].BatchIds.Count);
            Assert.AreEqual(2, loaded.Farm.Seeds[FarmCommands.RadishSeedId].LifetimeAcquired);
            Assert.AreEqual(source.Farm.Beds[FarmCommands.WestBedId].BatchId, loaded.Farm.Beds[FarmCommands.WestBedId].BatchId);
            Assert.IsTrue(loaded.UpgradeLevels == null || loaded.UpgradeLevels.Count == 0);
            Assert.IsFalse(loaded.StatUpgradesMigratedToGear);
            Assert.IsFalse(loaded.DuckHelmetSanitized);
            Assert.AreEqual(-1, loaded.SkillData["Farming"].Milestones[0].TierIndex);
            AssertPreservedProgress(loaded);
        }

        [Test]
        public void ActualCancelledSaveLeavesLiveSeedCreditUnpublishedAndRetryCreditsOnce()
        {
            var live = Fixture();
            var proposal = FarmCommands.RecordRadishAdventureCompletion(live.Farm, "return", Start, true, true);
            Assert.IsTrue(FarmTransaction.TryPrepare(live, proposal, out var candidate, out var error), error);
            var failed = SaveManager.Instance.SaveDetailedAsync(candidate, "Save1", new CancellationToken(true)).GetAwaiter().GetResult();
            Assert.AreEqual(SaveWriteStatus.Cancelled, failed.Status);
            Assert.IsEmpty(live.Farm.Seeds);
            Assert.IsEmpty(live.Farm.Operations);
            Assert.IsFalse(Directory.Exists(Path.Combine(testRoot, "Saves")));
            var success = SaveManager.Instance.SaveDetailedAsync(candidate, "Save1").GetAwaiter().GetResult();
            Assert.IsTrue(success.Succeeded, success.Error);
            var loaded = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
            Assert.IsTrue(loaded.Succeeded, loaded.Diagnostic);
            Assert.AreEqual(1, loaded.Data.Farm.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.RecordRadishAdventureCompletion(loaded.Data.Farm, "return", Start, true, true).Status);
        }

        [Test]
        public void FailedCreditThenRoutineDiskSnapshotPreservesIntentForExactlyOnceRecovery()
        {
            var live = Fixture();
            live.Farm.PendingCredits["return"] = new FarmPendingCredit { Rolled = true, CompletedAtUtcTicks = Start.Ticks };
            live.Farm.PendingCredits["other-miss"] = new FarmPendingCredit { Rolled = false, CompletedAtUtcTicks = Start.Ticks + 1 };
            var attempt = FarmCommands.RecordRadishAdventureCompletion(live.Farm, "return", Start, true, true);
            Assert.IsTrue(FarmTransaction.TryPrepare(live, attempt, out var candidate, out var error), error);
            var failed = SaveManager.Instance.SaveDetailedAsync(candidate, "Save1", new CancellationToken(true)).GetAwaiter().GetResult();
            Assert.AreEqual(SaveWriteStatus.Cancelled, failed.Status);
            Assert.IsTrue(live.Farm.PendingCredits.ContainsKey("return"));
            Assert.IsEmpty(live.Farm.Seeds);
            Assert.IsEmpty(live.Farm.Operations);

            // The normal snapshot may succeed after the credit attempt failed, then the process
            // can exit. Its saved intent preserves the selected hit without granting it early.
            var routineWrite = SaveManager.Instance.SaveDetailedAsync(live, "Save1").GetAwaiter().GetResult();
            Assert.IsTrue(routineWrite.Succeeded, routineWrite.Error);
            var durable = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
            Assert.IsTrue(durable.Succeeded, durable.Diagnostic);
            var reloaded = durable.Data;
            var intent = reloaded.Farm.PendingCredits["return"];
            Assert.IsTrue(intent.Rolled);
            Assert.AreEqual(Start.Ticks, intent.CompletedAtUtcTicks);
            Assert.IsEmpty(reloaded.Farm.Seeds);
            Assert.IsEmpty(reloaded.Farm.Operations);

            var credited = CommitAndReload(reloaded, FarmCommands.RecordRadishAdventureCompletion(
                reloaded.Farm, "return", Start.AddMinutes(1), true, intent.Rolled));
            Assert.AreEqual(1, credited.Farm.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(1, credited.Farm.Seeds[FarmCommands.RadishSeedId].LifetimeAcquired);
            Assert.IsFalse(credited.Farm.PendingCredits.ContainsKey("return"));
            Assert.IsTrue(credited.Farm.PendingCredits.ContainsKey("other-miss"));
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.RecordRadishAdventureCompletion(credited.Farm, "return", Start.AddMinutes(2), true, intent.Rolled).Status);
            AssertPreservedProgress(credited);
        }

        [Test]
        public void ActualIoFailureLeavesBuildUnpublishedAndMaterialsUnspent()
        {
            var live = Fixture();
            var proposal = FarmCommands.PrepareBeds(live.Farm, "build", Start, tuning);
            Assert.IsTrue(FarmTransaction.TryPrepare(live, proposal, out var candidate, out var error), error);
            var blockedRoot = Path.Combine(testRoot, "root-is-a-file");
            File.WriteAllText(blockedRoot, "Disposable I/O failure fixture.");
            setRoot.Invoke(null, new object[] { blockedRoot });
            var result = SaveManager.Instance.SaveDetailedAsync(candidate, "Save1").GetAwaiter().GetResult();
            Assert.AreEqual(SaveWriteStatus.Failed, result.Status, result.Error);
            Assert.IsFalse(live.Farm.OriginalBedsPrepared);
            Assert.AreEqual(20, live.Resources["Log"].Amount);
            Assert.AreEqual(40, live.Resources["Stick"].Amount);
            Assert.IsFalse(live.Quests.ContainsKey("Farm.PrepareBeds"));
            Assert.AreEqual("Disposable I/O failure fixture.", File.ReadAllText(blockedRoot));
        }

        [Test]
        public void ActualDiskCommitReloadMakesPlantDebitsAndTwoBedHarvestExactlyOnce()
        {
            var state = Fixture();
            state = CommitAndReload(state, FarmCommands.PrepareBeds(state.Farm, "build", Start, tuning));
            state = CommitAndReload(state, FarmCommands.RecordRadishAdventureCompletion(state.Farm, "return-one", Start, true, true));
            state = CommitAndReload(state, FarmCommands.RecordRadishAdventureCompletion(state.Farm, "return-two", Start, true, true));
            state = CommitAndReload(state, FarmCommands.Plant(state.Farm, FarmCommands.WestBedId, "plant-west", Start, tuning));
            state = CommitAndReload(state, FarmCommands.Plant(state.Farm, FarmCommands.EastBedId, "plant-east", Start, tuning));
            Assert.AreEqual(0, state.Farm.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.Plant(state.Farm, FarmCommands.WestBedId, "plant-west", Start, tuning).Status);
            state = CommitAndReload(state, FarmCommands.HarvestReady(state.Farm, "harvest", Start.AddMonths(1)));
            Assert.AreEqual(27, state.Resources["Radish"].Amount);
            Assert.AreEqual(320, state.ResourceStats["Radish"].TotalReceived);
            Assert.AreEqual(2, state.Farm.HarvestedBatchIds.Count);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.HarvestReady(state.Farm, "harvest", Start.AddMonths(1)).Status);
            Assert.AreEqual("NoReadyBeds", FarmCommands.HarvestReady(state.Farm, "new-harvest", Start.AddMonths(2)).Reason);
            AssertPreservedProgress(state);
        }

        [Test]
        public void SequenceCancelledWriteRoutineSnapshotAndRecoveryRetainGapAndPayOnce()
        {
            var live = Fixture();
            live.Farm = new FarmState();
            var first = FarmJournal.StageCredit(live.Farm, true, Start);
            var second = FarmJournal.StageCredit(live.Farm, false, Start.AddSeconds(1));
            var attempt = FarmCommands.RecordRadishAdventureCompletion(live.Farm, second, Start, true, false);
            Assert.IsTrue(FarmTransaction.TryPrepare(live, attempt, out var candidate, out var error), error);
            var failed = SaveManager.Instance.SaveDetailedAsync(candidate, "Save1", new CancellationToken(true)).GetAwaiter().GetResult();
            Assert.AreEqual(SaveWriteStatus.Cancelled, failed.Status);
            Assert.AreEqual(0, live.Farm.CommittedThroughSequence);
            Assert.AreEqual(2, live.Farm.PendingCredits.Count);
            Assert.IsEmpty(live.Farm.Seeds);
            Assert.IsTrue(SaveManager.Instance.SaveDetailedAsync(live, "Save1").GetAwaiter().GetResult().Succeeded);
            var routine = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult().Data;
            var later = CommitAndReload(routine, FarmCommands.RecordRadishAdventureCompletion(routine.Farm, second, Start, true, false));
            Assert.AreEqual(0, later.Farm.CommittedThroughSequence);
            Assert.AreEqual(1, later.Farm.CompletedSequences.Count);
            Assert.IsTrue(later.Farm.PendingCredits[first].Rolled);
            var done = CommitAndReload(later, FarmCommands.RecordRadishAdventureCompletion(later.Farm, first, Start, true, true));
            Assert.AreEqual(2, done.Farm.CommittedThroughSequence);
            Assert.IsEmpty(done.Farm.PendingCredits);
            Assert.IsEmpty(done.Farm.CompletedSequences);
            Assert.AreEqual(1, done.Farm.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.RecordRadishAdventureCompletion(done.Farm, first, Start, true, true).Status);
            Assert.AreEqual("OperationIdConflict",
                FarmCommands.RecordRadishAdventureCompletion(done.Farm, second, Start, true, true).Reason);
            AssertPreservedProgress(done);
        }

        [Test]
        public void SequenceIoFailureDoesNotPublishBuildWatermarkOrMaterials()
        {
            var live = Fixture();
            live.Farm = new FarmState();
            var token = FarmJournal.NextOperation(live.Farm, "prepare-original");
            var proposal = FarmCommands.PrepareBeds(live.Farm, token, Start, tuning);
            Assert.IsTrue(FarmTransaction.TryPrepare(live, proposal, out var candidate, out var error), error);
            var blocked = Path.Combine(testRoot, "sequence-root-is-a-file");
            File.WriteAllText(blocked, "Disposable failure");
            setRoot.Invoke(null, new object[] { blocked });
            Assert.AreEqual(SaveWriteStatus.Failed, SaveManager.Instance.SaveDetailedAsync(candidate, "Save1").GetAwaiter().GetResult().Status);
            Assert.AreEqual(0, live.Farm.LastIssuedSequence);
            Assert.AreEqual(0, live.Farm.CommittedThroughSequence);
            Assert.AreEqual(20, live.Resources["Log"].Amount);
            Assert.AreEqual(40, live.Resources["Stick"].Amount);
            Assert.IsFalse(live.Farm.OriginalBedsPrepared);
            Assert.AreEqual(token, FarmJournal.NextOperation(live.Farm, "prepare-original"));
        }

        private GameData CommitAndReload(GameData state, FarmCommandResult proposal)
        {
            Assert.IsTrue(FarmTransaction.TryPrepare(state, proposal, out var candidate, out var error), error);
            var written = SaveManager.Instance.SaveDetailedAsync(candidate, "Save1").GetAwaiter().GetResult();
            Assert.IsTrue(written.Succeeded, written.Error);
            // Reload the durable bytes directly: no runtime publication is needed for recovery.
            var loaded = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
            Assert.IsTrue(loaded.Succeeded, loaded.Diagnostic);
            return loaded.Data;
        }

        private GameData PlantedBoth()
        {
            var data = Fixture();
            data = Prepare(data, FarmCommands.PrepareBeds(data.Farm, "build", Start, tuning));
            data = Prepare(data, FarmCommands.RecordRadishAdventureCompletion(data.Farm, "return-one", Start, true, true));
            data = Prepare(data, FarmCommands.RecordRadishAdventureCompletion(data.Farm, "return-two", Start, true, true));
            data = Prepare(data, FarmCommands.Plant(data.Farm, FarmCommands.WestBedId, "plant-west", Start, tuning));
            return Prepare(data, FarmCommands.Plant(data.Farm, FarmCommands.EastBedId, "plant-east", Start, tuning));
        }

        private static GameData Prepare(GameData data, FarmCommandResult proposal)
        {
            Assert.IsTrue(FarmTransaction.TryPrepare(data, proposal, out var candidate, out var error), error);
            return candidate;
        }

        private static GameData Fixture(double logs = 20, double sticks = 40)
        {
            var data = new GameData { Farm = new FarmState { FormatVersion = 1 } };
            data.Resources["Log"] = new GameData.ResourceEntry { Amount = logs, Earned = true, Tier = 2 };
            data.Resources["Stick"] = new GameData.ResourceEntry { Amount = sticks, Earned = true, Tier = 1 };
            data.Resources["Radish"] = new GameData.ResourceEntry { Amount = 7, Earned = true, Tier = 3, BestPerMinute = 123.25 };
            data.ResourceStats["Log"] = new GameData.ResourceRecord { TotalReceived = 500, TotalSpent = 7 };
            data.ResourceStats["Radish"] = new GameData.ResourceRecord { TotalReceived = 300, TotalSpent = 19 };
            data.SkillData["Farming"] = new GameData.SkillProgress { Level = 17, CurrentXP = 123.5f,
                Milestones = new System.Collections.Generic.List<GameData.MilestoneProgressRecord>
                { new GameData.MilestoneProgressRecord { Id = "active-choice", IsActive = true, TierIndex = 4 } } };
            data.Quests["legacy-completed"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 1234 };
            data.Quests["legacy-partial"] = new GameData.QuestRecord { DistanceTravelProgress = 93.5, TasksBaseline = 22 };
            data.General.TasksCompleted = 44;
            data.General.TotalResourcesGathered = 900;
            return data;
        }

        private static void AssertPreservedProgress(GameData data)
        {
            Assert.AreEqual(17, data.SkillData["Farming"].Level);
            Assert.AreEqual(123.5f, data.SkillData["Farming"].CurrentXP);
            Assert.AreEqual("active-choice", data.SkillData["Farming"].Milestones[0].Id);
            Assert.IsTrue(data.SkillData["Farming"].Milestones[0].IsActive);
            Assert.IsTrue(data.Quests["legacy-completed"].Completed);
            Assert.AreEqual(1234, data.Quests["legacy-completed"].CompletedTimestamp);
            Assert.AreEqual(93.5, data.Quests["legacy-partial"].DistanceTravelProgress);
            Assert.AreEqual(22, data.Quests["legacy-partial"].TasksBaseline);
        }
    }
}
#endif
