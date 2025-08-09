using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Enemies;
using Blindsided.Utilities;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Stats
{
    [DefaultExecutionOrder(-1)]
    public class EnemyKillTracker : MonoBehaviour
    {
        public static EnemyKillTracker Instance { get; private set; }
        public static readonly int[] Thresholds = { 10, 100, 1000, 10000 };

        public event System.Action<EnemyData> OnKillRegistered;

        private readonly Dictionary<EnemyData, double> kills = new();

        private void Awake()
        {
            Instance = this;
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        public void RegisterKill(EnemyData stats)
        {
            if (stats == null) return;
            if (kills.ContainsKey(stats))
                kills[stats] += 1;
            else
                kills[stats] = 1;
            OnKillRegistered?.Invoke(stats);
        }

        public double GetKills(EnemyData stats)
        {
            return stats != null && kills.TryGetValue(stats, out var c) ? c : 0;
        }

        public int GetRevealLevel(EnemyData stats)
        {
            double count = GetKills(stats);
            int level = 0;
            foreach (var t in Thresholds)
            {
                if (count >= t) level++;
            }
            return level;
        }

        public float GetDamageMultiplier(EnemyData stats)
        {
            return 1f + GetRevealLevel(stats) * 0.25f;
        }

        private void SaveState()
        {
            if (oracle == null) return;
            var dict = new Dictionary<string, double>();
            foreach (var pair in kills)
            {
                if (pair.Key != null)
                    dict[pair.Key.name] = pair.Value;
            }
            oracle.saveData.EnemyKills = dict;
        }

        private void LoadState()
        {
            if (oracle == null) return;
            oracle.saveData.EnemyKills ??= new Dictionary<string, double>();
            kills.Clear();
            foreach (var enemy in Blindsided.Utilities.AssetCache.GetAll<EnemyData>(""))
            {
                oracle.saveData.EnemyKills.TryGetValue(enemy.name, out var count);
                kills[enemy] = count;
            }
        }
    }
}
