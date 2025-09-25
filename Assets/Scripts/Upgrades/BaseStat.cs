using Blindsided.Utilities;
using UnityEngine;
using TimelessEchoes.Gear;

namespace TimelessEchoes.Upgrades
{
    [ManageableData]
    [CreateAssetMenu(fileName = "BaseStat", menuName = "SO/Base Stat")]
    public class BaseStat : ScriptableObject
    {
        [SerializeField] private StatDefSO associatedStat;
        [SerializeField] private float baseValue = 0f;

        public StatDefSO AssociatedStat => associatedStat;
        public float BaseValue => baseValue;
    }
}
