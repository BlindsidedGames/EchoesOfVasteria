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
        public Sprite icon;
    }
}