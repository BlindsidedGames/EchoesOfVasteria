using UnityEngine;
using Blindsided.Utilities;

namespace TimelessEchoes.Hero
{
    [ManageableData]
    [CreateAssetMenu(fileName = "HeroStats", menuName = "SO/Hero Stats")]
    public class HeroStats : ScriptableObject
    {
        public int maxHealth = 10;
        public int damage = 1;
        public float moveSpeed = 3f;
        public float attackSpeed = 1f;
        public float visionRange = 5f;
        public GameObject projectilePrefab;
    }
}
