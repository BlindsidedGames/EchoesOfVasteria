using Blindsided.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace TimelessEchoes.Upgrades
{
    [ManageableData]
    [CreateAssetMenu(fileName = "Resource", menuName = "SO/Resource Item")]
    public class Resource : ScriptableObject
    {
        public enum CauldronCategory
        {
            Auto,
            Farming,
            Fishing,
            Mining,
            Woodcutting,
            Looting,
            Combat
        }

        [Tooltip("Lower numbers appear first in the item stats panel")]
        public int resourceID;

        [PreviewField(50, ObjectFieldAlignment.Left)]
        public Sprite icon;

        [PreviewField(50, ObjectFieldAlignment.Left)]
        public Sprite UnknownIcon;

        public double baseValue = 1.0;
        public double valueMultiplier = 1.0f;

        [Tooltip("If true, disciples will not generate this resource and Alter Echo data is ignored")]
        public bool DisableAlterEcho;

        [Tooltip("Override how this resource is categorized for the Cauldron. Auto uses inferred grouping.")]
        public CauldronCategory cauldronCategory = CauldronCategory.Auto;

        [HideInInspector] public double totalReceived;
        [HideInInspector] public double totalSpent;
    }
}