using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Gear
{
    [Serializable]
    public class GearAffix
    {
        public StatDefSO stat;
        public float value;
    }

    [Serializable]
    public class GearItem
    {
        public string slot; // e.g., Weapon, Helmet, Chest, Boots
        public RaritySO rarity;
			public CoreSO core;
        public List<GearAffix> affixes = new();

        public float GetStatValue(StatDefSO stat)
        {
            if (stat == null) return 0f;
            float sum = 0f;
            foreach (var a in affixes)
                if (a != null && a.stat == stat)
                    sum += a.value;
            return sum;
        }
    }
}


