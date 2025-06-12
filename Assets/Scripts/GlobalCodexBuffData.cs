using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GlobalCodexBuffData", menuName = "SO/Global Codex Buff Data")]
public class GlobalCodexBuffData : ScriptableObject
{
    [System.Serializable]
    public class Threshold
    {
        public int killsRequired;
        public int damageBonus;
        public int healthBonus;
    }

    public List<Threshold> thresholds = new();
}
