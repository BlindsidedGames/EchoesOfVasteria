using Blindsided.Utilities;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace TimelessEchoes.Upgrades
{
    [ManageableData]
    [CreateAssetMenu(fileName = "Resource", menuName = "SO/Upgrade Resource")]
    public class Resource : ScriptableObject
    {
        [Tooltip("Lower numbers appear first in the item stats panel")] public int resourceID = 0;
        public Sprite icon;

        [HideInInspector] public int totalReceived;
        [HideInInspector] public int totalSpent;
    }
}