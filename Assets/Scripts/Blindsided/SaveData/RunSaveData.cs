using System;
using System.Collections.Generic;
using TimelessEchoes.Buffs;

namespace Blindsided.SaveData
{
    // A continuation of the outing, not a snapshot of generated objects or combat.
    [Serializable]
    public sealed class RunSaveData
    {
        public int Version = 1;
        public string Map;
        public float Distance;
        public float HealthFraction = 1f;
        public bool Dead, Reaped, ReturnQueued;
        public Statistics Stats = new();
        public LootTotals Loot = new();
        public BuffManager.BuffRunState Buffs;

        public bool IsValid => Version == 1 && !string.IsNullOrEmpty(Map) &&
            float.IsFinite(Distance) && Distance >= 0 &&
            float.IsFinite(HealthFraction) && HealthFraction >= 0 && HealthFraction <= 1 &&
            Stats != null && Loot != null;

        [Serializable]
        public sealed class Statistics
        {
            public float Distance, Steps, Duration, ScaledDuration;
            public int Tasks, Kills;
            public double Resources, BonusResources, DamageDealt, DamageTaken;
            public Dictionary<string, double> ResourceAmounts = new();
            public int SessionDeaths, SessionKills, SessionReaps;
            public double SessionDamageDealt;
            public float SessionSteps, SessionDuration;
        }

        [Serializable]
        public sealed class LootTotals
        {
            public Dictionary<string, double> Amounts = new();
            public Dictionary<string, double> BonusAmounts = new();
            public float StartingReapDistance;
        }
    }
}
