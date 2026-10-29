#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using TimelessEchoes.Editor;
using TimelessEchoes.Enemies;
using TimelessEchoes.MapGeneration;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class EnemySpawnTerrainBuildValidationTests
    {
        private string folder;
        private TerrainSettings ground, other;
        private EnemyData enemy;
        private MapGenerationConfig map;

        [SetUp]
        public void CreateIsolatedAssets()
        {
            var name = "__SpawnTerrainValidation_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name);
            folder = "Assets/" + name;
            ground = ScriptableObject.CreateInstance<TerrainSettings>();
            ground.traversal = TerrainSettings.Traversal.Walkable;
            AssetDatabase.CreateAsset(ground, folder + "/Ground_Walkable.asset");
            other = ScriptableObject.CreateInstance<TerrainSettings>();
            other.traversal = TerrainSettings.Traversal.Walkable;
            AssetDatabase.CreateAsset(other, folder + "/Other_Walkable.asset");
            enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.spawnTerrains = new List<TerrainSettings> { ground };
            AssetDatabase.CreateAsset(enemy, folder + "/Enemy.asset");
            map = ScriptableObject.CreateInstance<MapGenerationConfig>();
            map.tilemapChunkSettings.bottomTerrain = ground;
            map.tilemapChunkSettings.middleTerrain = ground;
            map.tilemapChunkSettings.topTerrain = ground;
            map.taskGeneratorSettings.enemies = new List<EnemyData> { enemy };
            AssetDatabase.CreateAsset(map, folder + "/Map.asset");
        }

        [TearDown]
        public void RemoveIsolatedAssets()
        {
            if (!string.IsNullOrEmpty(folder) && folder.StartsWith("Assets/__SpawnTerrainValidation_", StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(folder);
        }

        [TestCase("empty")]
        [TestCase("missing")]
        [TestCase("nonWalkable")]
        [TestCase("unclassified")]
        [TestCase("misleadingName")]
        [TestCase("wrongMap")]
        public void BuildRejectsInvalidEnemyAssignmentsAndAcceptsTheirRepair(string mistake)
        {
            switch (mistake)
            {
                case "empty": enemy.spawnTerrains.Clear(); break;
                case "missing": enemy.spawnTerrains[0] = null; break;
                case "nonWalkable":
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(other), "Other_NonWalkable");
                    other.traversal = TerrainSettings.Traversal.NonWalkable;
                    enemy.spawnTerrains[0] = other;
                    break;
                case "unclassified":
                    other.traversal = TerrainSettings.Traversal.Unspecified;
                    enemy.spawnTerrains[0] = other;
                    break;
                case "misleadingName":
                    // The suffix still says Walkable; the data must take precedence.
                    other.traversal = TerrainSettings.Traversal.NonWalkable;
                    enemy.spawnTerrains[0] = other;
                    break;
                case "wrongMap": enemy.spawnTerrains[0] = other; break;
            }

            var validator = new EnemySpawnTerrainBuildValidator();
            var failure = Assert.Throws<BuildFailedException>(() => validator.OnPreprocessBuild(null));
            StringAssert.Contains(folder + (mistake == "wrongMap" ? "/Map.asset" : "/Enemy.asset"), failure.Message);

            enemy.spawnTerrains = new List<TerrainSettings> { ground };
            other.traversal = TerrainSettings.Traversal.Walkable;
            if (mistake == "nonWalkable")
                Assert.That(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(other), "Other_Walkable"), Is.Empty);
            Assert.DoesNotThrow(() => validator.OnPreprocessBuild(null));
        }
    }
}
#endif
