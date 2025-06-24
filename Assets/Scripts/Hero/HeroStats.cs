using UnityEngine;
using Blindsided.Utilities;

namespace TimelessEchoes.Hero
{
    [ManageableData]
    [CreateAssetMenu(fileName = "HeroStats", menuName = "SO/Hero Stats")]
    public class HeroStats : ScriptableObject
    {
        public float visionRange = 5f;
        public GameObject projectilePrefab;
    }
}
