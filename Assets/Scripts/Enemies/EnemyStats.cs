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
        public int defense = 0;
        public int damage = 1;
        public float moveSpeed = 3f;
        public float attackSpeed = 1f;
        /// <summary>
        /// Distance within which the enemy can perform attacks.
        /// </summary>
        public float attackRange = 1f;
        public float visionRange = 5f;
        /// <summary>
        /// Distance within which allies will join an engaged enemy.
        /// </summary>
        public float assistRange = 8f;
        public float wanderDistance = 2f;
        public GameObject projectilePrefab;
    }
}
