using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Enemies;
using Blindsided.Utilities;
using static Blindsided.EventHandler;
using static Blindsided.Oracle;

namespace TimelessEchoes.Stats
{
    public class EnemyKillTracker : MonoBehaviour
    {
        public static readonly int[] Thresholds = { 10, 100, 1000, 10000 };

        private readonly Dictionary<EnemyStats, double> kills = new();

        private void Awake()
        {
            LoadState();
            OnSaveData += SaveState;
            OnLoadData += LoadState;
        }

        private void OnDestroy()
        {
            OnSaveData -= SaveState;
            OnLoadData -= LoadState;
        }

        public void RegisterKill(EnemyStats stats)
        {
            if (stats == null) return;
            if (kills.ContainsKey(stats))
                kills[stats] += 1;
            else
                kills[stats] = 1;
        }

        public double GetKills(EnemyStats stats)
        {
            return stats != null && kills.TryGetValue(stats, out var c) ? c : 0;
        }

        public int GetRevealLevel(EnemyStats stats)
        {
            double count = GetKills(stats);
            int level = 0;
            foreach (var t in Thresholds)
            {
                if (count >= t) level++;
            }
            return level;
        }

        public float GetDamageMultiplier(EnemyStats stats)
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
            foreach (var enemy in Resources.LoadAll<EnemyStats>(""))
            {
                oracle.saveData.EnemyKills.TryGetValue(enemy.name, out var count);
                kills[enemy] = count;
            }
        }
    }
}
