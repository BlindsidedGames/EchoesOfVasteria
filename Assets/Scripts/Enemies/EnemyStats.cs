using UnityEngine;
using Sirenix.OdinInspector;
using Blindsided.Utilities;

namespace TimelessEchoes.Enemies
{
    [ManageableData]
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "SO/Enemy Stats")]
    public class EnemyStats : ScriptableObject
    {
        [TitleGroup("General")]
        public string enemyName;

        [TitleGroup("General")]
        [Tooltip("Lower numbers appear first in the stats panel")]
        public int displayOrder = 0;

        [TitleGroup("References")]
        public Sprite icon;

        [TitleGroup("Balance Data")]
        public int maxHealth = 10;

        [TitleGroup("Balance Data")]
        public int experience = 10;

        [TitleGroup("Balance Data")]
        public float defense = 0f;

        [TitleGroup("Balance Data")]
        public int damage = 1;

        [TitleGroup("Balance Data")]
        public float moveSpeed = 3f;

        [TitleGroup("Balance Data")]
        public float attackSpeed = 1f;

        /// <summary>
        /// Distance within which the enemy can perform attacks.
        /// </summary>
        [TitleGroup("Balance Data")]
        public float attackRange = 1f;

        [TitleGroup("Balance Data")]
        public float visionRange = 5f;

        /// <summary>
        /// Distance within which allies will join an engaged enemy.
        /// </summary>
        [TitleGroup("Balance Data")]
        public float assistRange = 8f;

        [TitleGroup("Balance Data")]
        public float wanderDistance = 2f;

        [TitleGroup("References")]
        public GameObject projectilePrefab;
    }
}
