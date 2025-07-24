using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Reflection;
using TimelessEchoes.Tasks;
using TimelessEchoes.MapGeneration;

namespace TimelessEchoes.Tests
{
    public class ValidateTerrainRulesTests
    {
        private GameObject mapObj;
        private Tilemap tilemap;
        private GameObject generatorObj;
        private ProceduralTaskGenerator generator;
        private Tile tileA;
        private Tile tileB;
        private TerrainSettings settings;

        [SetUp]
        public void SetUp()
        {
            mapObj = new GameObject("Map", typeof(Grid));
            var tmObj = new GameObject("Tilemap");
            tmObj.transform.parent = mapObj.transform;
            tilemap = tmObj.AddComponent<Tilemap>();
            tmObj.AddComponent<TilemapRenderer>();

            tileA = ScriptableObject.CreateInstance<Tile>();
            tileB = ScriptableObject.CreateInstance<Tile>();

            for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (x == 0 || x == 4 || y == 0 || y == 4)
                    tilemap.SetTile(pos, tileB);
                else
                    tilemap.SetTile(pos, tileA);
            }

            generatorObj = new GameObject("Generator");
            generator = generatorObj.AddComponent<ProceduralTaskGenerator>();

            typeof(ProceduralTaskGenerator).GetField("terrainMap", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(generator, tilemap);
            typeof(ProceduralTaskGenerator).GetField("bottomBuffer", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(generator, 0);

            settings = ScriptableObject.CreateInstance<TerrainSettings>();
            settings.tile = tileA;
            settings.taskSettings = new TerrainSettings.TaskSettings();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(generatorObj);
            Object.DestroyImmediate(mapObj);
            Object.DestroyImmediate(tileA);
            Object.DestroyImmediate(tileB);
            Object.DestroyImmediate(settings);
        }

        private bool Validate(Vector3Int cell)
        {
            var method = typeof(ProceduralTaskGenerator).GetMethod("ValidateTerrainRules", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)method.Invoke(generator, new object[] { settings, cell });
        }

        [Test]
        public void InteriorAllowedBorderRejected_WhenBorderOnlyFalse()
        {
            settings.taskSettings.borderOnly = false;
            settings.taskSettings.topBorderOffset = 0;
            settings.taskSettings.bottomBorderOffset = 0;
            settings.taskSettings.leftBorderOffset = 0;
            settings.taskSettings.rightBorderOffset = 0;

            Assert.IsTrue(Validate(new Vector3Int(2, 2, 0)));
            Assert.IsFalse(Validate(new Vector3Int(1, 2, 0)));
        }

        [Test]
        public void BorderAllowedInteriorRejected_WhenBorderOnlyTrue()
        {
            settings.taskSettings.borderOnly = true;
            settings.taskSettings.topBorderOffset = 0;
            settings.taskSettings.bottomBorderOffset = 0;
            settings.taskSettings.leftBorderOffset = 0;
            settings.taskSettings.rightBorderOffset = 0;

            Assert.IsFalse(Validate(new Vector3Int(2, 2, 0)));
            Assert.IsTrue(Validate(new Vector3Int(1, 2, 0)));
        }

        [Test]
        public void OffsetsExpandBorderCheck()
        {
            settings.taskSettings.borderOnly = false;
            settings.taskSettings.topBorderOffset = 1;
            settings.taskSettings.bottomBorderOffset = 1;
            settings.taskSettings.leftBorderOffset = 1;
            settings.taskSettings.rightBorderOffset = 1;

            Assert.IsFalse(Validate(new Vector3Int(2, 2, 0)));
        }
    }
}
