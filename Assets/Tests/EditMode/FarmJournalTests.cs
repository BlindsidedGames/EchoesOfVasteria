#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sirenix.Serialization;
using TimelessEchoes.Farming;

namespace Tests.EditMode
{
    public sealed class FarmJournalTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly FarmTuning Tuning = new FarmTuning();
        private static FarmState Reload(FarmState state) => SerializationUtility.DeserializeValue<FarmState>(
            SerializationUtility.SerializeValue(state, DataFormat.Binary), DataFormat.Binary);
        private static FarmState Credit(FarmState state, bool hit = true)
        {
            var id = FarmJournal.StageCredit(state, hit, Start);
            var result = FarmCommands.RecordRadishAdventureCompletion(state, id, Start, true, hit);
            Assert.IsTrue(result.Accepted, result.Reason);
            return result.Candidate;
        }
        private static FarmState Prepared()
        {
            var source = new FarmState();
            return FarmCommands.PrepareBeds(source, FarmJournal.NextOperation(source, "prepare-original"), Start, Tuning).Candidate;
        }
        private static FarmState Plant(FarmState state, string bed = FarmCommands.WestBedId)
        {
            return FarmCommands.Plant(state, bed, FarmJournal.NextOperation(state, "plant-radish:" + bed), Start, Tuning).Candidate;
        }
        private static FarmCommandResult Harvest(FarmState state, out string id)
        {
            id = FarmJournal.NextOperation(state, "harvest-ready");
            return FarmCommands.HarvestReady(state, id, Start.AddMonths(1), activeElapsedSeconds: 1800);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FixedCreditCompactsButCannotReplayOrChangeRollAfterReload(bool hit)
        {
            var state = new FarmState();
            var id = FarmJournal.StageCredit(state, hit, Start);
            var result = FarmCommands.RecordRadishAdventureCompletion(state, id, Start, true, hit);
            Assert.IsTrue(result.Accepted);
            Assert.IsTrue(state.PendingCredits.ContainsKey(id));
            Assert.AreEqual(0, state.CommittedThroughSequence);
            var loaded = Reload(result.Candidate);
            Assert.AreEqual(1, loaded.CommittedThroughSequence);
            Assert.IsEmpty(loaded.CompletedSequences);
            Assert.IsEmpty(loaded.PendingCredits);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.RecordRadishAdventureCompletion(loaded, id, Start, true, hit).Status);
            Assert.AreEqual("OperationIdConflict",
                FarmCommands.RecordRadishAdventureCompletion(loaded, id, Start, true, !hit).Reason);
            Assert.AreEqual(hit ? 1 : 0, loaded.Seeds.TryGetValue(FarmCommands.RadishSeedId, out var seed) ? seed.Quantity : 0);
        }

        [Test]
        public void FailedCandidateAndRoutineReloadRetainAllFixedPendingAndGap()
        {
            var state = new FarmState();
            var first = FarmJournal.StageCredit(state, true, Start);
            var second = FarmJournal.StageCredit(state, false, Start.AddSeconds(1));
            var third = FarmJournal.StageCredit(state, true, Start.AddSeconds(2));
            var failed = FarmCommands.RecordRadishAdventureCompletion(state, third, Start, true, true);
            Assert.IsTrue(failed.Accepted); // write failed: discard candidate, keep owner/selected intents.
            var routine = Reload(state);
            Assert.AreEqual(3, routine.PendingCredits.Count);
            Assert.AreEqual(0, routine.CommittedThroughSequence);
            Assert.IsEmpty(routine.Seeds);
            var later = FarmCommands.RecordRadishAdventureCompletion(routine, third, Start, true, true).Candidate;
            later = Reload(FarmCommands.RecordRadishAdventureCompletion(later, second, Start, true, false).Candidate);
            Assert.AreEqual(0, later.CommittedThroughSequence);
            Assert.AreEqual(2, later.CompletedSequences.Count);
            Assert.AreEqual(1, later.PendingCredits.Count);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.RecordRadishAdventureCompletion(later, third, Start, true, true).Status);
            var done = Reload(FarmCommands.RecordRadishAdventureCompletion(later, first, Start, true, true).Candidate);
            Assert.AreEqual(3, done.CommittedThroughSequence);
            Assert.IsEmpty(done.CompletedSequences);
            Assert.IsEmpty(done.PendingCredits);
            Assert.AreEqual(2, done.Seeds[FarmCommands.RadishSeedId].Quantity);
        }

        [Test]
        public void StoredPendingRollConflictCannotConsumeIntentOrAdvanceWatermark()
        {
            var source = new FarmState();
            var id = FarmJournal.StageCredit(source, false, Start);
            source.PendingCredits[id].Rolled = true; // inconsistent snapshot: do not trust token alone.
            var result = FarmCommands.RecordRadishAdventureCompletion(source, id, Start, true, false);
            Assert.AreEqual("PendingRollConflict", result.Reason);
            Assert.IsNull(result.Candidate);
            Assert.AreEqual(1, source.PendingCredits.Count);
            Assert.AreEqual(0, source.CommittedThroughSequence);
        }

        [Test]
        public void LegacyPendingHitAndMissDrainOnceWhileKnownHistoryAndUnseenUuidStaySealed()
        {
            var legacy = new FarmState { FormatVersion = 1 };
            legacy.Operations["paid"] = new FarmOperationReceipt { Fingerprint = "adventure-radish:1" };
            legacy.Operations["opaque"] = new FarmOperationReceipt { Fingerprint = "future.reward" };
            legacy.PendingCredits["hit"] = new FarmPendingCredit { Rolled = true, CompletedAtUtcTicks = Start.Ticks };
            legacy.PendingCredits["miss"] = new FarmPendingCredit { Rolled = false, CompletedAtUtcTicks = Start.Ticks };
            legacy.PendingCredits["opaque-null"] = null;
            FarmJournal.UpgradeLegacy(legacy);
            Assert.IsFalse(legacy.Operations.ContainsKey("paid"));
            Assert.IsTrue(legacy.Operations.ContainsKey("opaque"));
            var state = Reload(legacy);
            foreach (var id in new[] { "paid", "never-seen" })
                Assert.AreEqual("LegacyOperationSealed", FarmCommands.RecordRadishAdventureCompletion(state, id, Start, true, true).Reason);
            state = FarmCommands.RecordRadishAdventureCompletion(state, "hit", Start, true, true).Candidate;
            state = Reload(FarmCommands.RecordRadishAdventureCompletion(state, "miss", Start, true, false).Candidate);
            Assert.AreEqual(1, state.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(1, state.PendingCredits.Count);
            Assert.IsTrue(state.PendingCredits.ContainsKey("opaque-null"));
            Assert.AreEqual("LegacyOperationSealed", FarmCommands.RecordRadishAdventureCompletion(state, "hit", Start, true, true).Reason);
            Assert.IsEmpty(state.CompletedSequences);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyAlreadyPaidStalePendingIsRemovedWithoutAnotherPayout(bool hit)
        {
            var state = new FarmState { FormatVersion = 1 };
            state.PendingCredits["paid"] = new FarmPendingCredit { Rolled = hit, CompletedAtUtcTicks = Start.Ticks };
            state.Operations["paid"] = new FarmOperationReceipt { Fingerprint = "adventure-radish:" + (hit ? "1" : "0") };
            state.Seeds[FarmCommands.RadishSeedId] = new FarmSeedState { Quantity = 7, LifetimeAcquired = 9 };
            FarmJournal.UpgradeLegacy(state);
            Assert.IsEmpty(state.PendingCredits);
            Assert.IsEmpty(state.Operations);
            Assert.AreEqual(7, state.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(9, state.Seeds[FarmCommands.RadishSeedId].LifetimeAcquired);
            Assert.AreEqual("LegacyOperationSealed", FarmCommands.RecordRadishAdventureCompletion(Reload(state), "paid", Start, true, hit).Reason);
        }

        [Test]
        public void ConflictingLegacyPendingAndUnknownHistoryAreNotDiscardedOrPaid()
        {
            var state = new FarmState { FormatVersion = 1 };
            state.PendingCredits["conflict"] = new FarmPendingCredit { Rolled = true, CompletedAtUtcTicks = Start.Ticks };
            state.Operations["conflict"] = new FarmOperationReceipt { Fingerprint = "adventure-radish:0" };
            state.Operations["unknown"] = null;
            FarmJournal.UpgradeLegacy(state);
            Assert.AreEqual(1, state.PendingCredits.Count);
            Assert.AreEqual(2, state.Operations.Count);
            Assert.AreEqual("OperationIdConflict", FarmCommands.RecordRadishAdventureCompletion(state, "conflict", Start, true, true).Reason);
            Assert.IsEmpty(state.Seeds);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StaleCurrentIntentAcknowledgesDurableProofWithoutAnotherPayout(bool sparse)
        {
            var source = new FarmState();
            if (sparse) FarmJournal.StageCredit(source, false, Start);
            var id = FarmJournal.StageCredit(source, true, Start);
            var paid = FarmCommands.RecordRadishAdventureCompletion(source, id, Start, true, true).Candidate;
            paid.PendingCredits[id] = new FarmPendingCredit { Rolled = true, CompletedAtUtcTicks = Start.Ticks };
            var acknowledged = FarmCommands.RecordRadishAdventureCompletion(paid, id, Start, true, true);
            Assert.IsTrue(acknowledged.Accepted);
            Assert.AreEqual("CommittedIntentAcknowledged", acknowledged.Reason);
            Assert.IsTrue(paid.PendingCredits.ContainsKey(id)); // failed cleanup write retains source.
            Assert.IsFalse(acknowledged.Candidate.PendingCredits.ContainsKey(id));
            Assert.AreEqual(1, acknowledged.Candidate.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied,
                FarmCommands.RecordRadishAdventureCompletion(Reload(acknowledged.Candidate), id, Start, true, true).Status);
        }

        [Test]
        public void InvalidUiActionsAndDiscardedWriteDoNotIssuePermanentHoles()
        {
            var state = new FarmState();
            var token = FarmJournal.NextOperation(state, "plant-radish:" + FarmCommands.WestBedId);
            Assert.AreEqual("BedLocked", FarmCommands.Plant(state, FarmCommands.WestBedId, token, Start, Tuning).Reason);
            Assert.AreEqual(0, state.LastIssuedSequence);
            var buildId = FarmJournal.NextOperation(state, "prepare-original");
            var candidate = FarmCommands.PrepareBeds(state, buildId, Start, Tuning).Candidate;
            Assert.AreEqual(0, state.LastIssuedSequence); // failed/cancelled write has published nothing.
            Assert.AreEqual(buildId, FarmJournal.NextOperation(state, "prepare-original"));
            Assert.AreEqual(1, candidate.CommittedThroughSequence);
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, FarmCommands.PrepareBeds(Reload(candidate), buildId, Start, Tuning).Status);
        }

        [Test]
        public void StaleCompactedMissWithConflictingHitIntentIsPreservedAndRejected()
        {
            var source = new FarmState();
            var id = FarmJournal.StageCredit(source, false, Start);
            var settled = FarmCommands.RecordRadishAdventureCompletion(source, id, Start, true, false).Candidate;
            settled.PendingCredits[id] = new FarmPendingCredit { Rolled = true, CompletedAtUtcTicks = Start.Ticks };
            var result = FarmCommands.RecordRadishAdventureCompletion(settled, id, Start, true, true);
            Assert.AreEqual("OperationIdConflict", result.Reason);
            Assert.IsNull(result.Candidate);
            Assert.IsTrue(settled.PendingCredits[id].Rolled);
            Assert.IsEmpty(settled.Seeds);
            Assert.AreEqual(1, settled.CommittedThroughSequence);
        }

        [Test]
        public void ForeignOwnerFutureSequenceAndUnreservedAdventureCannotGrant()
        {
            var owner = new FarmState();
            var other = new FarmState();
            var id = FarmJournal.StageCredit(owner, true, Start);
            Assert.AreEqual("ForeignOrInvalidOperation", FarmCommands.RecordRadishAdventureCompletion(other, id, Start, true, true).Reason);
            var unreserved = FarmJournal.NextOperation(other, "adventure-radish:1");
            Assert.AreEqual("UnreservedOperation", FarmCommands.RecordRadishAdventureCompletion(other, unreserved, Start, true, true).Reason);
            Assert.IsEmpty(other.Seeds);
            Assert.IsEmpty(other.PendingCredits);
            Assert.AreEqual(0, other.LastIssuedSequence);
        }

        [Test]
        public void HarvestReplayAfterReplantCannotPayNewBatchAndKnownHistoryStaysConstant()
        {
            var state = Plant(Credit(Prepared()));
            var first = Harvest(state, out var harvestId);
            Assert.AreEqual(10, first.ResourceDeltas["Radish"]);
            state = Reload(first.Candidate); // committed; process exits before publication.
            Assert.IsEmpty(state.HarvestedBatchIds);
            var oldBatch = state.Beds[FarmCommands.WestBedId].LastHarvestedPlantSequence;
            state = Plant(Credit(state));
            var replay = FarmCommands.HarvestReady(state, harvestId, Start.AddMonths(1));
            Assert.AreEqual(FarmCommandStatus.AlreadyApplied, replay.Status);
            Assert.IsEmpty(replay.ResourceDeltas);
            Assert.IsTrue(state.Beds[FarmCommands.WestBedId].IsPlanted);
            Assert.AreEqual(oldBatch, state.Beds[FarmCommands.WestBedId].LastHarvestedPlantSequence);
            Assert.IsEmpty(state.Operations);
            Assert.IsEmpty(state.CompletedSequences);
            Assert.IsEmpty(state.HarvestedBatchIds);
        }

        [Test]
        public void ReintroducedAlreadyHarvestedOrUncommittedBatchIsRejectedAtomically()
        {
            var source = Plant(Credit(Prepared()));
            var oldBed = source.Beds[FarmCommands.WestBedId].DeepClone();
            var done = Harvest(source, out _).Candidate;
            done.Beds[FarmCommands.WestBedId].BatchId = oldBed.BatchId;
            done.Beds[FarmCommands.WestBedId].RecipeId = oldBed.RecipeId;
            done.Beds[FarmCommands.WestBedId].DurationSeconds = oldBed.DurationSeconds;
            done.Beds[FarmCommands.WestBedId].HarvestYield = oldBed.HarvestYield;
            done.Beds[FarmCommands.WestBedId].ElapsedSeconds = oldBed.DurationSeconds;
            Assert.AreEqual("InvalidOrPreviouslyHarvestedBatch", Harvest(done, out _).Reason);
            var uncommitted = Credit(Prepared());
            var forgedPlant = FarmJournal.NextOperation(uncommitted, "plant-radish:" + FarmCommands.WestBedId);
            uncommitted.Beds[FarmCommands.WestBedId] = oldBed;
            oldBed.BatchId = "radish:" + forgedPlant;
            Assert.AreEqual("InvalidOrPreviouslyHarvestedBatch", Harvest(uncommitted, out _).Reason);
        }

        [Test]
        public void LegacyLivePaidMarkerAndUnknownMarkersSurviveMigration()
        {
            var source = new FarmState { FormatVersion = 1 };
            var legacyBatch = "radish:legacy-plant";
            source.Beds[FarmCommands.WestBedId] = new FarmBedState { Unlocked = true, BatchId = legacyBatch,
                RecipeId = FarmCommands.RadishRecipeId, DurationSeconds = 1, HarvestYield = 10, LastGrowthUtcTicks = Start.Ticks };
            source.Operations["harvest"] = new FarmOperationReceipt { Fingerprint = "harvest-ready", BatchIds = new List<string> { legacyBatch, "radish:old-done" } };
            source.HarvestedBatchIds.UnionWith(new[] { legacyBatch, "radish:old-done", "opaque.future" });
            FarmJournal.UpgradeLegacy(source);
            Assert.IsTrue(source.HarvestedBatchIds.Contains(legacyBatch));
            Assert.IsFalse(source.HarvestedBatchIds.Contains("radish:old-done"));
            Assert.IsTrue(source.HarvestedBatchIds.Contains("opaque.future"));
            Assert.AreEqual("InvalidOrPreviouslyHarvestedBatch", Harvest(source, out _).Reason);
        }

        [Test]
        public void LegacyLiveBatchPaysOnceThenItsWhitelistCloses()
        {
            var source = new FarmState { FormatVersion = 1 };
            source.Beds[FarmCommands.WestBedId] = new FarmBedState { Unlocked = true, BatchId = "radish:old",
                RecipeId = FarmCommands.RadishRecipeId, DurationSeconds = 1, HarvestYield = 10, LastGrowthUtcTicks = Start.Ticks };
            FarmJournal.UpgradeLegacy(source);
            var old = source.Beds[FarmCommands.WestBedId].DeepClone();
            var done = Harvest(source, out _).Candidate;
            Assert.IsNull(done.Beds[FarmCommands.WestBedId].LegacyBatchId);
            done.Beds[FarmCommands.WestBedId].BatchId = old.BatchId;
            done.Beds[FarmCommands.WestBedId].RecipeId = old.RecipeId;
            done.Beds[FarmCommands.WestBedId].DurationSeconds = old.DurationSeconds;
            done.Beds[FarmCommands.WestBedId].HarvestYield = old.HarvestYield;
            done.Beds[FarmCommands.WestBedId].ElapsedSeconds = old.DurationSeconds;
            Assert.AreEqual("InvalidOrPreviouslyHarvestedBatch", Harvest(done, out _).Reason);
        }

        [Test]
        public void ClockCaptureTouchesOnlyGrowingOriginalBedsAndReadyBedsStopWork()
        {
            var state = Plant(Credit(Prepared()));
            state.Operations["opaque"] = new FarmOperationReceipt { Fingerprint = "future" };
            var operations = state.Operations;
            var pending = state.PendingCredits;
            state.Beds["future"] = new FarmBedState { Unlocked = true, BatchId = "future", RecipeId = FarmCommands.RadishRecipeId,
                DurationSeconds = 1, HarvestYield = 10, LastGrowthUtcTicks = Start.Ticks };
            Assert.IsTrue(FarmCommands.CaptureGrowthInPlace(state, Start.AddMinutes(10), 600));
            Assert.AreEqual(600, state.Beds[FarmCommands.WestBedId].ElapsedSeconds);
            Assert.AreSame(operations, state.Operations);
            Assert.AreSame(pending, state.PendingCredits);
            Assert.AreEqual(0, state.Beds["future"].ElapsedSeconds);
            Assert.IsTrue(FarmCommands.CaptureGrowthInPlace(state, Start.AddMonths(1)));
            Assert.AreEqual(600, state.Beds[FarmCommands.WestBedId].ElapsedSeconds, "UTC capture must not grow a batch.");
            Assert.IsTrue(FarmCommands.CaptureGrowthInPlace(state, Start.AddMonths(1), 1200));
            Assert.IsTrue(state.Beds[FarmCommands.WestBedId].IsReady);
            var baseline = state.Beds[FarmCommands.WestBedId].LastGrowthUtcTicks;
            Assert.IsFalse(FarmCommands.CaptureGrowthInPlace(state, Start.AddMonths(2)));
            Assert.AreEqual(FarmCommandStatus.NoChange, FarmCommands.AdvanceGrowth(state, Start.AddMonths(2)).Status);
            Assert.AreEqual(baseline, state.Beds[FarmCommands.WestBedId].LastGrowthUtcTicks);
        }

        [Test]
        public void LegacyFiftyThousandSettledReceiptsCompactAndNewHistoryRemainsConstant()
        {
            var source = new FarmState { FormatVersion = 1 };
            for (int i = 0; i < 50000; i++) source.Operations["old-" + i] = new FarmOperationReceipt { Fingerprint = "adventure-radish:0" };
            FarmJournal.UpgradeLegacy(source);
            Assert.IsEmpty(source.Operations);
            for (int i = 0; i < 2000; i++) source = Credit(source, i % 10 == 0);
            Assert.AreEqual(2000, source.CommittedThroughSequence);
            Assert.IsEmpty(source.Operations);
            Assert.IsEmpty(source.CompletedSequences);
            Assert.IsEmpty(source.PendingCredits);
            Assert.AreEqual(200, source.Seeds[FarmCommands.RadishSeedId].Quantity);
            Assert.AreEqual("LegacyOperationSealed", FarmCommands.RecordRadishAdventureCompletion(source, "old-1", Start, true, true).Reason);
        }

        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(-1, -1)]
        public void InvalidBoundsFailClosedWithoutResettingLineage(long issued, long committed)
        {
            var state = new FarmState { LastIssuedSequence = issued, CommittedThroughSequence = committed };
            var lineage = state.JournalLineage;
            Assert.IsNull(FarmJournal.NextOperation(state, "prepare-original"));
            Assert.Throws<InvalidOperationException>(() => FarmJournal.UpgradeLegacy(state));
            Assert.AreEqual(lineage, state.JournalLineage);
            Assert.AreEqual(issued, state.LastIssuedSequence);
        }

        [Test]
        public void UnknownFormatAndSequenceExhaustionFailClosed()
        {
            var state = new FarmState { FormatVersion = 3 };
            Assert.IsNull(FarmJournal.NextOperation(state, "prepare-original"));
            Assert.Throws<InvalidOperationException>(() => FarmJournal.UpgradeLegacy(state));
            state = new FarmState { LastIssuedSequence = long.MaxValue, CommittedThroughSequence = long.MaxValue };
            Assert.IsNull(FarmJournal.NextOperation(state, "prepare-original"));
            Assert.IsNull(FarmJournal.StageCredit(state, true, Start));
            Assert.IsEmpty(state.PendingCredits);
        }
    }
}
#endif
