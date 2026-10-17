using System;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.EditorTools
{
    public static class ToolkitWorldBarRepair
    {
        // Process dependencies first so nested prefab instances see their repaired source.
        [MenuItem("Tools/UI Toolkit/Repair World Bars")]
        public static void Run() => Debug.Log(RepairAssets());

        public static string RepairAssets()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode before repairing prefabs.");
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)
                    .GetComponentsInChildren<ToolkitWorldAnchor>(true).Any(anchor => anchor.elements.Any(
                        element => element.value == ToolkitWorldAnchor.Value.HealthFill
                                   || element.value == ToolkitWorldAnchor.Value.TaskFill)))
                .OrderBy(path => AssetDatabase.GetDependencies(path, true).Length)
                .ThenBy(path => path, StringComparer.Ordinal).ToArray();
            int changed = 0, removed = 0;
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool dirty = false;
                    foreach (var group in root.GetComponentsInChildren<ToolkitWorldAnchor>(true)
                                 .GroupBy(anchor => anchor.gameObject))
                    {
                        // The complete inherited binding owns the fill. Added bindings from
                        // the old cutover contain only background/text and must not render.
                        var anchors = group.OrderByDescending(anchor => anchor.elements.Count(
                            element => element.value == ToolkitWorldAnchor.Value.HealthFill)).ToArray();
                        var owner = anchors[0];
                        if (!owner.elements.Any(element => element.value == ToolkitWorldAnchor.Value.HealthFill
                                                         || element.value == ToolkitWorldAnchor.Value.TaskFill))
                            continue;
                        foreach (var duplicate in anchors.Skip(1))
                        {
                            UnityEngine.Object.DestroyImmediate(duplicate);
                            removed++;
                            dirty = true;
                        }
                        foreach (var element in owner.elements)
                        {
                            if (!element.sliced || !element.anchor) continue;
                            var canvas = element.anchor.GetComponentInParent<Canvas>(true);
                            var fill = element.anchor.GetComponent<SlicedFilledImage>();
                            var image = element.anchor.GetComponent<Image>();
                            if (!canvas || (!fill && !image)) continue;
                            float multiplier = fill ? fill.pixelsPerUnitMultiplier : image.pixelsPerUnitMultiplier;
                            float reference = canvas.referencePixelsPerUnit / Mathf.Max(.01f, multiplier);
                            if (Mathf.Approximately(element.sliceReferencePixelsPerUnit, reference)) continue;
                            element.sliceReferencePixelsPerUnit = reference;
                            dirty = true;
                        }
                    }
                    if (dirty)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changed++;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return $"World bars: repaired {changed} prefabs, removed {removed} duplicate bindings.";
        }
    }
}
