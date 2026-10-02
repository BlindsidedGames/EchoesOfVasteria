#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using Sirenix.Serialization;
using TimelessEchoes.Farming;

namespace Tests.EditMode
{
    public sealed class FarmCommandsTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly FarmTuning tuning = new FarmTuning();

        private FarmState Prepared() => FarmCommands.PrepareBeds(new FarmState { FormatVersion = 1 }, "build", Start, tuning).Candidate;
        private static FarmState Credit(FarmState state, string id) =>
            FarmCommands.RecordRadishAdventureCompletion(state, id, Start, true, true).Candidate;
        private FarmState Planted(string bed = FarmCommands.WestBedId)
        {
            var state = Credit(Prepared(), "return-one");
            return FarmCommands.Plant(state, bed, "plant-one", Start, tuning).Candidate;
        }

        [Test]
        public void FreshFarmHasNoSeedsOrDiscoveryAndBuildOnlyProposesPayment()
        {
            var source = new FarmState { FormatVersion = 1 };
            var build = FarmCommands.PrepareBeds(source, "build", Start, tuning);
            Assert.IsTrue(build.Accepted);
            Assert.AreEqual(-10, build.ResourceDeltas["Log"]);
            Assert.AreEqual(-20, build.ResourceDeltas["Stick"]);
            Assert.IsFalse(source.OriginalBedsPrepared);
            Assert.IsTrue(build.Candidate.OriginalBedsPrepared);
            Assert.IsEmpty(build.Candidate.Seeds);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.PrepareBeds(build.Candidate, "build", Start, tuning).Status);
            Assert.AreEqual(FarmCommandStatus.NoChange,
                FarmCommands.PrepareBeds(build.Candidate, "different-build", Start, tuning).Status);
        }

        [Test]
        public void EligibleHitCreditsOnceAndDiscoverySurvivesQuantityZeroAndReload()
        {
            var source = Prepared();
            var hit = FarmCommands.RecordRadishAdventureCompletion(source, "return", Start, true, true);
            Assert.IsEmpty(source.Seeds);
            Assert.AreEqual(1, hit.Candidate.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(1, hit.Candidate.Seeds[FarmCommands.RadishSeedId].LifetimeAcquired);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.RecordRadishAdventureCompletion(hit.Candidate, "return", Start, true, true).Status);
            var planted = FarmCommands.Plant(hit.Candidate, FarmCommands.WestBedId, "plant", Start, tuning).Candidate;
            var loaded = RoundTrip(planted);
            Assert.AreEqual(0, loaded.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.IsTrue(loaded.Seeds[FarmCommands.RadishSeedId].IsDiscovered);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.Plant(loaded, FarmCommands.WestBedId, "plant", Start, tuning).Status);
        }

        [Test]
        public void MissIsRecordedAndCannotBeRetriedAsHit()
        {
            var miss = FarmCommands.RecordRadishAdventureCompletion(new FarmState { FormatVersion = 1 }, "completed-work", Start, true, false);
            Assert.IsTrue(miss.Accepted);
            Assert.IsEmpty(miss.Candidate.Seeds);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.RecordRadishAdventureCompletion(miss.Candidate, "completed-work", Start, true, false).Status);
            Assert.AreEqual("OperationIdConflict",
                FarmCommands.RecordRadishAdventureCompletion(miss.Candidate, "completed-work", Start, true, true).Reason);
            Assert.AreEqual("IneligibleAdventureCompletion",
                FarmCommands.RecordRadishAdventureCompletion(miss.Candidate, "abandoned-work", Start, false, true).Reason);
        }

        [Test]
        public void PlantingRequiresPaidBedsAndOneSeedAndCannotDebitTwice()
        {
            var locked = Credit(new FarmState { FormatVersion = 1 }, "return");
            Assert.AreEqual("BedLocked", FarmCommands.Plant(locked, FarmCommands.WestBedId, "plant", Start, tuning).Reason);
            Assert.AreEqual(1, locked.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual("NoRadishSeeds", FarmCommands.Plant(Prepared(), FarmCommands.WestBedId, "plant", Start, tuning).Reason);
            var planted = Planted();
            Assert.AreEqual("BedOccupied", FarmCommands.Plant(planted, FarmCommands.WestBedId, "new-plant", Start, tuning).Reason);
            Assert.AreEqual(0, planted.Seeds[FarmCommands.RadishSeedId].Quantity);
        }

        [Test]
        public void ActiveAndOfflineGrowthAreEqualAndMonthOnlyCompletesOneBatchPerBed()
        {
            var state = Credit(Credit(Prepared(), "return-one"), "return-two");
            state = FarmCommands.Plant(state, FarmCommands.WestBedId, "plant-west", Start, tuning).Candidate;
            state = FarmCommands.Plant(state, FarmCommands.EastBedId, "plant-east", Start, tuning).Candidate;
            var offline = FarmCommands.AdvanceGrowth(state, Start.AddMinutes(12)).Candidate;
            var online = FarmCommands.AdvanceGrowth(state, Start.AddMinutes(12), 12 * 60).Candidate;
            foreach (var id in FarmCommands.OriginalBedIds)
                Assert.AreEqual(offline.Beds[id].ElapsedSeconds, online.Beds[id].ElapsedSeconds);
            var month = FarmCommands.HarvestReady(state, "harvest-month", Start.AddMonths(1));
            Assert.AreEqual(20, month.ResourceDeltas["Radish"]);
            Assert.AreEqual(2, month.HarvestedBeds.Count);
            Assert.AreEqual(2, month.Candidate.HarvestedBatchIds.Count);
            Assert.IsFalse(month.Candidate.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.AreEqual("NoReadyBeds", FarmCommands.HarvestReady(month.Candidate, "another-harvest", Start.AddMonths(2)).Reason);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.HarvestReady(RoundTrip(month.Candidate), "harvest-month", Start.AddMonths(2)).Status);
            Assert.AreEqual(2, month.Candidate.Seeds[FarmCommands.RadishSeedId].LifetimeAcquired);
            Assert.AreEqual(0, month.Candidate.Seeds[FarmCommands.RadishSeedId].Quantity);
        }

        [Test]
        public void FailedWriteDiscardsWholeHarvestProposalAndRetryHasSameYield()
        {
            var persisted = Planted();
            var firstAttempt = FarmCommands.HarvestReady(persisted, "harvest", Start.AddHours(1));
            Assert.IsTrue(firstAttempt.Accepted);
            // Simulated persistence failure: the owner retains its old snapshot and publishes nothing.
            Assert.IsTrue(persisted.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.IsEmpty(persisted.HarvestedBatchIds);
            Assert.IsFalse(persisted.Operations.ContainsKey("harvest"));
            var retry = FarmCommands.HarvestReady(persisted, "harvest", Start.AddHours(1));
            Assert.AreEqual(firstAttempt.ResourceDeltas["Radish"], retry.ResourceDeltas["Radish"]);
            persisted = RoundTrip(retry.Candidate); // Commit succeeded, then process crashed before UI publication.
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.HarvestReady(persisted, "harvest", Start.AddHours(1)).Status);
            Assert.AreEqual("NoReadyBeds", FarmCommands.HarvestReady(persisted, "other-id", Start.AddHours(1)).Reason);
        }

        [Test]
        public void InvalidSecondReadyBatchRejectsWholeHarvestWithoutPartialFirstBedPayout()
        {
            var state = Credit(Credit(Prepared(), "return-one"), "return-two");
            state = FarmCommands.Plant(state, FarmCommands.WestBedId, "plant-west", Start, tuning).Candidate;
            state = FarmCommands.Plant(state, FarmCommands.EastBedId, "plant-east", Start, tuning).Candidate;
            state.HarvestedBatchIds.Add(state.Beds[FarmCommands.EastBedId].BatchId);
            var harvest = FarmCommands.HarvestReady(state, "harvest", Start.AddHours(1));
            Assert.AreEqual("InvalidOrPreviouslyHarvestedBatch", harvest.Reason);
            Assert.IsNull(harvest.Candidate);
            Assert.IsEmpty(harvest.ResourceDeltas);
            Assert.IsTrue(state.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.IsFalse(state.HarvestedBatchIds.Contains(state.Beds[FarmCommands.WestBedId].BatchId));
            Assert.IsFalse(state.Operations.ContainsKey("harvest"));
        }

        [Test]
        public void FailedSeedAndPlantWritesCanRetryWithoutDuplicatingCreditOrDebit()
        {
            var persisted = Prepared();
            var credit = FarmCommands.RecordRadishAdventureCompletion(persisted, "return", Start, true, true);
            Assert.IsEmpty(persisted.Seeds);
            Assert.AreEqual(1, FarmCommands.RecordRadishAdventureCompletion(persisted, "return", Start, true, true)
                .Candidate.Seeds[FarmCommands.RadishSeedId].Quantity);
            persisted = RoundTrip(credit.Candidate);
            var plant = FarmCommands.Plant(persisted, FarmCommands.WestBedId, "plant", Start, tuning);
            Assert.AreEqual(1, persisted.Seeds[FarmCommands.RadishSeedId].Quantity);
            var retry = FarmCommands.Plant(persisted, FarmCommands.WestBedId, "plant", Start, tuning);
            Assert.AreEqual(plant.Candidate.Beds[FarmCommands.WestBedId].BatchId, retry.Candidate.Beds[FarmCommands.WestBedId].BatchId);
            Assert.AreEqual(0, retry.Candidate.Seeds[FarmCommands.RadishSeedId].Quantity);
        }

        [Test]
        public void RollbackKeepsEarnedGrowthAndRebasesFutureProgress()
        {
            var state = FarmCommands.AdvanceGrowth(Planted(), Start.AddMinutes(10)).Candidate;
            state = FarmCommands.AdvanceGrowth(state, Start.AddMinutes(3)).Candidate;
            Assert.AreEqual(600, state.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.AreEqual(Start.AddMinutes(3).Ticks, state.Beds[FarmCommands.WestBedId].LastGrowthUtcTicks);
            state = FarmCommands.AdvanceGrowth(state, Start.AddMinutes(4)).Candidate;
            Assert.AreEqual(660, state.Beds[FarmCommands.WestBedId].ElapsedSeconds);
        }

        [Test]
        public void LaterPlantGetsNoCarriedTimeButAnotherForwardClockJumpCanAcceleratePaidBatch()
        {
            var future = Start.AddMonths(1);
            var state = FarmCommands.HarvestReady(Planted(), "harvest-one", future).Candidate;
            state = Credit(state, "return-two");
            state = FarmCommands.Plant(state, FarmCommands.WestBedId, "plant-two", future, tuning).Candidate;
            Assert.AreEqual(0, state.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.AreEqual("NoReadyBeds", FarmCommands.HarvestReady(state, "too-soon", future).Reason);
            var second = FarmCommands.HarvestReady(state, "harvest-two", future.AddMonths(1));
            Assert.AreEqual(10, second.ResourceDeltas["Radish"]);
            Assert.AreEqual(0, second.Candidate.Seeds[FarmCommands.RadishSeedId].Quantity);
        }

        [Test]
        public void UnknownRecordsArePreservedAndNeverGrownOrHarvested()
        {
            var state = Planted();
            state.Beds["future.bed"] = new FarmBedState { Unlocked = true, BatchId = "future", RecipeId = FarmCommands.RadishRecipeId,
                DurationSeconds = 1, HarvestYield = 123, LastGrowthUtcTicks = Start.Ticks };
            state.Seeds["future.seed"] = new FarmSeedState { Quantity = 17, LifetimeAcquired = 21 };
            state.Operations["future.operation"] = new FarmOperationReceipt { Fingerprint = "unknown-operation" };
            var harvest = FarmCommands.HarvestReady(state, "harvest", Start.AddHours(1));
            var loaded = RoundTrip(harvest.Candidate);
            Assert.AreEqual(10, harvest.ResourceDeltas["Radish"]);
            Assert.AreEqual(0, loaded.Beds["future.bed"].ElapsedSeconds);
            Assert.AreEqual("future", loaded.Beds["future.bed"].BatchId);
            Assert.AreEqual(17, loaded.Seeds["future.seed"].Quantity);
            Assert.AreEqual("unknown-operation", loaded.Operations["future.operation"].Fingerprint);
        }

        [Test]
        public void InvalidClockInputsAndOperationReuseDoNotMutateSource()
        {
            var state = Planted();
            Assert.AreEqual("InvalidActiveElapsed", FarmCommands.AdvanceGrowth(state, Start, double.NaN).Reason);
            Assert.AreEqual("InvalidActiveElapsed", FarmCommands.AdvanceGrowth(state, Start, -1).Reason);
            Assert.AreEqual("OperationIdConflict",
                FarmCommands.Plant(state, FarmCommands.EastBedId, "plant-one", Start, tuning).Reason);
            Assert.AreEqual(0, state.Beds[FarmCommands.WestBedId].ElapsedSeconds);
        }

        [Test]
        public void PendingCreditCloneAndDifferentOwnersAreIndependent()
        {
            var firstOwner = new FarmState { FormatVersion = 1 };
            firstOwner.PendingCredits["return"] = new FarmPendingCredit { Rolled = true, CompletedAtUtcTicks = Start.Ticks };
            firstOwner.PendingCredits["unknown-null"] = null;
            var clone = firstOwner.DeepClone();
            clone.PendingCredits["return"].Rolled = false;
            clone.PendingCredits["return"].CompletedAtUtcTicks++;
            clone.PendingCredits.Remove("unknown-null");
            Assert.IsTrue(firstOwner.PendingCredits["return"].Rolled);
            Assert.AreEqual(Start.Ticks, firstOwner.PendingCredits["return"].CompletedAtUtcTicks);
            Assert.IsTrue(firstOwner.PendingCredits.ContainsKey("unknown-null"));
            var secondOwner = new FarmState { FormatVersion = 1 };
            Assert.IsEmpty(secondOwner.PendingCredits);
            var credited = FarmCommands.RecordRadishAdventureCompletion(firstOwner, "return", Start, true, true);
            Assert.IsEmpty(secondOwner.Seeds);
            Assert.IsEmpty(secondOwner.Operations);
            Assert.IsTrue(firstOwner.PendingCredits.ContainsKey("return"));
            Assert.IsFalse(credited.Candidate.PendingCredits.ContainsKey("return"));
            Assert.IsTrue(credited.Candidate.PendingCredits.ContainsKey("unknown-null"));
        }

        [Test]
        public void PendingMissConsumesOnlyMatchingIntentAndConflictPreservesSelectedResult()
        {
            var source = new FarmState { FormatVersion = 1 };
            source.PendingCredits["miss"] = new FarmPendingCredit { Rolled = false, CompletedAtUtcTicks = Start.Ticks };
            source.PendingCredits["other-hit"] = new FarmPendingCredit { Rolled = true, CompletedAtUtcTicks = Start.Ticks + 1 };
            Assert.AreEqual("PendingRollConflict",
                FarmCommands.RecordRadishAdventureCompletion(source, "miss", Start, true, true).Reason);
            Assert.IsFalse(source.PendingCredits["miss"].Rolled);
            Assert.IsEmpty(source.Operations);
            var miss = FarmCommands.RecordRadishAdventureCompletion(source, "miss", Start, true, false);
            Assert.IsTrue(miss.Accepted);
            Assert.IsFalse(miss.Candidate.PendingCredits.ContainsKey("miss"));
            Assert.IsTrue(miss.Candidate.PendingCredits.ContainsKey("other-hit"));
            Assert.IsEmpty(miss.Candidate.Seeds);
            Assert.AreEqual("adventure-radish:0", miss.Candidate.Operations["miss"].Fingerprint);
            Assert.IsTrue(source.PendingCredits.ContainsKey("miss"));
        }

        private static FarmState RoundTrip(FarmState state)
        {
            var writer = new SerializationContext();
            writer.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowOnErrors;
            var reader = new DeserializationContext();
            reader.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowOnErrors;
            return SerializationUtility.DeserializeValue<FarmState>(
                SerializationUtility.SerializeValue(state, DataFormat.Binary, writer), DataFormat.Binary, reader);
        }
    }
}
#endif
