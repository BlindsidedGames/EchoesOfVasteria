using Blindsided.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace TimelessEchoes.Upgrades
{
    [ManageableData]
    [CreateAssetMenu(fileName = "Resource", menuName = "SO/Upgrade Resource")]
    public class Resource : ScriptableObject
    {
        [Tooltip("Lower numbers appear first in the item stats panel")]
        public int resourceID;

        [PreviewField(50, ObjectFieldAlignment.Left)]
        public Sprite icon;

        [PreviewField(50, ObjectFieldAlignment.Left)]
        public Sprite UnknownIcon;

        [HideInInspector] public int totalReceived;
        [HideInInspector] public int totalSpent;
    }
}