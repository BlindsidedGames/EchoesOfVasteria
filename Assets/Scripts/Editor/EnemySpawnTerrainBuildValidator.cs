using System;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Enemies;
using TimelessEchoes.MapGeneration;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TimelessEchoes.Editor
{
    public sealed class EnemySpawnTerrainBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            var errors = FindErrors();
            if (errors.Count > 0)
                throw new BuildFailedException("Enemy spawn terrain validation failed:\n" + string.Join("\n", errors));
        }

        [MenuItem("Tools/Validate Enemy Spawn Terrains")]
        private static void ValidateFromMenu()
        {
            var errors = FindErrors();
            if (errors.Count == 0) Debug.Log("Enemy spawn terrain validation passed.");
            else foreach (var error in errors) Debug.LogError(error);
        }

        private static List<string> FindErrors()
        {
            var errors = new List<string>();
            foreach (var terrain in AssetsOfType<TerrainSettings>())
            {
                var path = AssetDatabase.GetAssetPath(terrain);
                if (terrain.traversal == TerrainSettings.Traversal.Unspecified)
                    errors.Add($"{path}: classify the terrain as Walkable or NonWalkable.");
                else if ((terrain.name.EndsWith("_NonWalkable", StringComparison.Ordinal) && terrain.IsWalkable) ||
                         (terrain.name.EndsWith("_Walkable", StringComparison.Ordinal) && !terrain.IsWalkable))
                    errors.Add($"{path}: the name's walkability suffix disagrees with its Traversal setting.");
            }

            foreach (var enemy in AssetsOfType<EnemyData>())
            {
                var error = enemy.GetSpawnTerrainError();
                if (error != null) errors.Add($"{AssetDatabase.GetAssetPath(enemy)}: {error}");
            }

            foreach (var map in AssetsOfType<MapGenerationConfig>())
            {
                if (map.tilemapChunkSettings == null || map.taskGeneratorSettings == null)
                {
                    errors.Add($"{AssetDatabase.GetAssetPath(map)}: map generation settings are missing.");
                    continue;
                }
                var settings = map.tilemapChunkSettings;
                var terrains = new[] { settings.bottomTerrain, settings.middleTerrain, settings.topTerrain };
                if (map.taskGeneratorSettings.enemies == null) continue;
                foreach (var enemy in map.taskGeneratorSettings.enemies)
                {
                    if (!enemy)
                        errors.Add($"{AssetDatabase.GetAssetPath(map)}: the enemy roster contains a missing reference.");
                    else if (enemy.spawnTerrains != null && !enemy.spawnTerrains.Any(t => t && t.IsWalkable && terrains.Contains(t)))
                        errors.Add($"{AssetDatabase.GetAssetPath(map)}: enemy '{enemy.name}' has no Walkable spawn terrain in this map.");
                }
            }
            return errors;
        }

        private static IEnumerable<T> AssetsOfType<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null);
    }
}
