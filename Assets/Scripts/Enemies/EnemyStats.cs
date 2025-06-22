using UnityEngine;
using Blindsided.Utilities;

namespace TimelessEchoes.Enemies
{
    [ManageableData]
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "SO/Enemy Stats")]
    public class EnemyStats : ScriptableObject
    {
        public int maxHealth = 10;
        public int experience = 10;
        [Range(0f, 1f)] public float gearDropChance = 0.1f;
        public int defense = 0;
        public int damage = 1;
        public float moveSpeed = 3f;
        public float attackSpeed = 1f;
        public float visionRange = 5f;
        public float wanderDistance = 2f;
        public GameObject projectilePrefab;
    }
}
