using System.Collections.Generic;
using Blindsided.Utilities;
using Sirenix.OdinInspector;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Enemies
{
    [ManageableData]
    [CreateAssetMenu(fileName = "EnemyData", menuName = "SO/Enemy Data")]
    public class EnemyData : ScriptableObject, IWeighted
    {
        [TitleGroup("General")] public string enemyName;

        [TitleGroup("General")] [Tooltip("Lower numbers appear first in the stats panel")]
        public int displayOrder;

        [TitleGroup("References")] public Sprite icon;

        [TitleGroup("Spawn Settings")] [Required]
        public GameObject prefab;

        [TitleGroup("Spawn Settings")] [MinValue(0)]
        public float weight = 1f;

        [TitleGroup("Spawn Settings")] public float minX;

        [TitleGroup("Spawn Settings")] public float maxX = float.PositiveInfinity;

        [TitleGroup("Spawn Settings")] public List<TerrainSettings> spawnTerrains = new();

        [TitleGroup("Spawn Settings")] public List<ResourceDrop> resourceDrops = new();

        [TitleGroup("Balance Data")] public int experience = 10;

        [PropertySpace(SpaceBefore = 5, SpaceAfter = 0)] [TitleGroup("Balance Data/Combat Stats")]
        public int maxHealth = 10;

        [TitleGroup("Balance Data/Combat Stats")]
        public float damage = 1;

        [TitleGroup("Balance Data/Combat Stats")]
        public float defense;

        [TitleGroup("Balance Data/Combat Stats")]
        public float attackSpeed = 1f;

        /// <summary>
        ///     Distance within which the enemy can perform attacks.
        /// </summary>
        [TitleGroup("Balance Data/Combat Stats")]
        public float attackRange = 1f;

        [TitleGroup("Balance Data/Movement Stats")]
        public float moveSpeed = 3f;

        [TitleGroup("Balance Data/Movement Stats")]
        public float visionRange = 5f;

        /// <summary>
        ///     Distance within which allies will join an engaged enemy.
        /// </summary>
        [TitleGroup("Balance Data/Movement Stats")]
        public float assistRange = 8f;

        [TitleGroup("Balance Data/Movement Stats")]
        public float wanderDistance = 2f;

        [TitleGroup("Level Scaling")] [MinValue(0)]
        public float damagePerLevel;

        [TitleGroup("Level Scaling")] [MinValue(0)]
        public int healthPerLevel;

        [TitleGroup("Level Scaling")] [MinValue(0f)]
        public float defensePerLevel;

        [TitleGroup("Level Scaling")] [MinValue(1f)]
        public float distancePerLevel = float.PositiveInfinity;

        [TitleGroup("References")] public GameObject projectilePrefab;

        public float GetWeight(float worldX)
        {
            if (prefab == null) return 0f;
            if (worldX < minX || worldX > maxX)
                return 0f;
            return Mathf.Max(0f, weight);
        }

        public int GetLevel(float worldX)
        {
            if (distancePerLevel <= 0f || float.IsInfinity(distancePerLevel))
                return 1;
            return Mathf.FloorToInt(Mathf.Max(0f, worldX) / distancePerLevel) + 1;
        }

        public int GetMaxHealthForLevel(int level)
        {
            return maxHealth + healthPerLevel * level;
        }

        public float GetDamageForLevel(int level)
        {
            return damage + damagePerLevel * level;
        }

        public float GetDefenseForLevel(int level)
        {
            return defense + defensePerLevel * level;
        }
    }
}