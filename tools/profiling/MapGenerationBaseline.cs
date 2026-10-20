// Copy into an isolated Editor-only profiling checkout. Never included in a player.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TimelessEchoes.MapGeneration;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class MapGenerationBaseline
{
    [Serializable] public sealed class Row
    {
        public string map, hash;
        public int seed, yields;
        public double milliseconds;
    }
    [Serializable] public sealed class Result { public List<Row> rows; }
    public static void Run(string output = null)
    {
        var results = new List<Row>();
        foreach (var map in new[] { "Farmlands", "River", "Woods", "Mines", "Haloween", "Beach" })
        foreach (var seed in new[] { 17, 9173, 54321 })
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Map.prefab");
            try
            {
                var generator = root.GetComponentInChildren<TilemapChunkGenerator>(true);
                var config = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>("Assets/Scriptables/MapSettings/" + map + ".asset");
                typeof(TilemapChunkGenerator).GetMethod("ApplyConfig", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(generator, new object[] { config });
                var data = new SerializedObject(generator);
                data.FindProperty("randomizeSeed").boolValue = false;
                data.FindProperty("seed").intValue = seed;
                data.ApplyModifiedPropertiesWithoutUndo();
                var text = new StringBuilder();
                int yields = 0;
                var watch = Stopwatch.StartNew();
                for (int segment = 0; segment < 4; segment++)
                {
                    if (segment == 3) generator.ClearSegment(Vector2Int.zero, new Vector2Int(64, 18));
                    var decor = new GameObject("probe-decor");
                    decor.transform.SetParent(root.transform);
                    var routine = generator.GenerateSegmentAsync(new Vector2Int(segment * 64, 0), new Vector2Int(64, 18), decor.transform);
                    while (routine.MoveNext()) yields++;
                    for (int x = segment * 64; x < (segment + 1) * 64; x++)
                    for (int y = 0; y < 18; y++)
                    {
                        var p = new Vector3Int(x, y);
                        text.Append(x).Append(',').Append(y).Append(':').Append(Guid(generator.TerrainMap.GetTile(p))).Append(':').Append(Guid(generator.DecorMap.GetTile(p))).Append(':').Append(generator.DecorMap.GetTransformMatrix(p).ToString("R")).AppendLine();
                    }
                    foreach (Transform child in decor.transform)
                    {
                        var sprite = child.GetComponentInChildren<SpriteRenderer>();
                        text.Append(child.name).Append(':').Append(child.position.ToString("R")).Append(':').Append(sprite ? Guid(sprite.sprite) : "").Append(':').Append(sprite && sprite.flipX).AppendLine();
                    }
                }
                results.Add(new Row { map = map, seed = seed, yields = yields, milliseconds = watch.Elapsed.TotalMilliseconds, hash = BitConverter.ToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))) });
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        // Ephemeral CLI assemblies are not Unity-serializable. Resolve the actual
        // Newtonsoft assembly explicitly (Localization also bundles a namesake).
        var serializer = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json", true)
            .GetMethod("SerializeObject", new[] { typeof(object) });
        File.WriteAllText(output ?? Environment.GetEnvironmentVariable("EOV_MAP_BASELINE_OUTPUT"),
            (string)serializer.Invoke(null, new object[] { results }));
    }
    static string Guid(Object value) => value ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) + "/" + value.name : "-";
}

