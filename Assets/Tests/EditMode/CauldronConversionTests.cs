#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class CauldronConversionTests
    {
        private GameData State(double food = 100, double stew = 0) => new GameData
        {
            CauldronStew = stew,
            Resources = new Dictionary<string, GameData.ResourceEntry> { ["Food"] = new() { Amount = food, Earned = true, Tier = 3 } },
            ResourceStats = new Dictionary<string, GameData.ResourceRecord> { ["Food"] = new() { TotalReceived = food } },
            Quests = new Dictionary<string, GameData.QuestRecord>
            {
                ["active"] = new() { CauldronMixProgress = 7 },
                ["completed"] = new() { Completed = true, CauldronMixProgress = 100 },
                ["inactive"] = new() { CauldronMixProgress = 3 }
            }
        };
        private bool Convert(GameData state, double amount, long sequence, out GameData candidate, out bool replay) =>
            CauldronConversion.TryPrepare(state, "Food", amount, .014, sequence,
                new[] { "active", "active", "completed" }, out candidate, out replay, out _);

        [Test] public void PartialConversionIsOneCandidateAndPreservesOriginal()
        {
            var source = State(100, 10); Assert.IsTrue(Convert(source, 25, 1, out var next, out var replay));
            Assert.IsFalse(replay); Assert.AreEqual(75, next.Resources["Food"].Amount);
            Assert.AreEqual(10.35, next.CauldronStew, 1e-12); Assert.AreEqual(32, next.Quests["active"].CauldronMixProgress);
            Assert.AreEqual(100, next.Quests["completed"].CauldronMixProgress); Assert.AreEqual(3, next.Quests["inactive"].CauldronMixProgress);
            Assert.AreEqual(100, source.Resources["Food"].Amount); Assert.AreEqual(10, source.CauldronStew);
            Assert.AreEqual(25, next.ResourceStats["Food"].TotalSpent); Assert.AreEqual(3, next.Resources["Food"].Tier);
        }
        [Test] public void MaxBillionsAndFractionalRemainderDoNotUseIntOrFloat()
        {
            var source = State(5_000_000_000.25); Assert.IsTrue(Convert(source, 5_000_000_000.25, 1, out var next, out _));
            Assert.AreEqual(0, next.Resources["Food"].Amount); Assert.AreEqual(70_000_000.0035, next.CauldronStew, 1e-7);
            Assert.AreEqual(5_000_000_007.25, next.Quests["active"].CauldronMixProgress);
        }
        [Test] public void LastReceiptReloadIsReplayAndOlderReceiptCannotPayAgain()
        {
            Assert.IsTrue(Convert(State(100), 10, 1, out var one, out _));
            var loaded = CurrentSaveCodec.Clone(one);
            Assert.IsTrue(Convert(loaded, 10, 1, out var replayCandidate, out var replay)); Assert.IsTrue(replay); Assert.IsNull(replayCandidate);
            Assert.IsFalse(Convert(loaded, 11, 1, out _, out _));
            Assert.IsTrue(Convert(loaded, 10, 2, out var two, out _));
            Assert.IsFalse(Convert(two, 10, 1, out _, out _)); Assert.AreEqual(80, two.Resources["Food"].Amount);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(101)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidAmountsLeaveInputUntouched(double amount)
        { var source = State(); Assert.IsFalse(Convert(source, amount, 1, out var next, out _)); Assert.IsNull(next); Assert.AreEqual(100, source.Resources["Food"].Amount); }
        [Test] public void UnknownFoodCannotConvert() { var s = State(); s.Resources["Food"].Earned = false; Assert.IsFalse(Convert(s, 1, 1, out _, out _)); }
        [Test] public void OldSaveStartsSequenceWithoutChangingEvaOrCards()
        {
            var s = State(); s.CauldronEvaLevel = 77; s.CauldronEvaXp = 23; s.CauldronCardCounts["RES:Food"] = 999;
            Assert.IsTrue(Convert(s, 100, 1, out var n, out _)); Assert.AreEqual(77, n.CauldronEvaLevel);
            Assert.AreEqual(23, n.CauldronEvaXp); Assert.AreEqual(999, n.CauldronCardCounts["RES:Food"]);
        }
        [Test] public void NonRepresentableDebitAndOverflowCannotCreateStew()
        {
            Assert.IsFalse(Convert(State(double.MaxValue), 1, 1, out _, out _));
            Assert.IsFalse(Convert(State(100, double.MaxValue), 1, 1, out _, out _));
            Assert.IsFalse(CauldronConversion.TryPrepare(State(), "Food", 100, double.MaxValue, 1, null, out _, out _, out _));
        }
        [Test] public void ReusedFruitSpawnEmitsOneFreshCompletionEvent()
        {
            var root=new GameObject("Fruit lifecycle contract");root.SetActive(false);
            try
            {
                var task=root.AddComponent<FruitHarvestTask>();int completions=0;task.TaskCompleted+=_=>completions++;
                var activate=typeof(FruitHarvestTask).GetMethod("OnEnable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var complete=typeof(BaseTask).GetMethod("NotifyCompleted",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                activate.Invoke(task,null);complete.Invoke(task,null);complete.Invoke(task,null);Assert.AreEqual(1,completions);
                activate.Invoke(task,null);complete.Invoke(task,null);complete.Invoke(task,null);Assert.AreEqual(2,completions);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void CancelledDiskCommitDoesNotPublishAndDurableReceiptRecoversOnce()
        {
            var rootField = typeof(SaveManager).GetField("rootPathOverride", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var setRoot = typeof(SaveManager).GetMethod("SetRootPathForTests", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(setRoot, "Refusing disk test without explicit isolated-root API.");
            var previous = rootField.GetValue(null); var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EOVCauldron_" + Guid.NewGuid().ToString("N"));
            try
            {
                setRoot.Invoke(null,new object[]{root});
                var live = State(5_000_000_000.25, 10);
                Assert.IsTrue(Convert(live, 5_000_000_000.25, 1, out var candidate, out _));
                var cancelled = SaveManager.Instance.SaveDetailedAsync(candidate, "Save1", new System.Threading.CancellationToken(true)).GetAwaiter().GetResult();
                Assert.AreEqual(SaveWriteStatus.Cancelled,cancelled.Status);Assert.AreEqual(5_000_000_000.25,live.Resources["Food"].Amount);Assert.AreEqual(10,live.CauldronStew);
                var write = SaveManager.Instance.SaveDetailedAsync(candidate,"Save1");
                candidate.Resources["Food"].Amount=123;candidate.CauldronStew=999; // Capture is immutable even before background completion.
                var committed=write.GetAwaiter().GetResult();Assert.IsTrue(committed.Succeeded,committed.Error);
                var reloaded=SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();Assert.IsTrue(reloaded.Succeeded,reloaded.Diagnostic);
                Assert.AreEqual(0,reloaded.Data.Resources["Food"].Amount);Assert.AreEqual(70_000_010.0035,reloaded.Data.CauldronStew,1e-7);
                Assert.AreEqual(5_000_000_007.25,reloaded.Data.Quests["active"].CauldronMixProgress);
                Assert.IsTrue(Convert(reloaded.Data,5_000_000_000.25,1,out var repeated,out var replay));Assert.IsTrue(replay);Assert.IsNull(repeated);
            }
            finally
            {
                setRoot.Invoke(null,new[]{previous});
                if(System.IO.Directory.Exists(root))System.IO.Directory.Delete(root,true);
            }
        }
        [Test] public void SaplingBonusNeverReplacesFruitAndIsAnIndependentBoundedRoll()
        {
            var fruit = ScriptableObject.CreateInstance<Resource>(); var sapling = ScriptableObject.CreateInstance<Resource>();
            try
            {
                var primary = DropResolver.RollDrops(new[] { new ResourceDrop { resource = fruit, dropRange = new Vector2Int(2,6) } }, null, rand: () => .5f);
                Assert.AreEqual(fruit, primary[0].resource);
                var drops = new[] { new TaskData.BonusDrop { resource = sapling, chance = .1f, range = new Vector2Int(1,1) } };
                Assert.AreEqual(0, DropResolver.RollBonusDrops(drops, () => .1f).Count);
                var hit = DropResolver.RollBonusDrops(drops, () => .05f); Assert.AreEqual(1, hit.Count); Assert.AreEqual(1, hit[0].count);
                Assert.AreEqual(fruit, primary[0].resource);
            }
            finally { UnityEngine.Object.DestroyImmediate(fruit); UnityEngine.Object.DestroyImmediate(sapling); }
        }
    }
}
#endif
