using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using Object = UnityEngine.Object;

namespace TimelessEchoes.Upgrades
{
    [CustomEditor(typeof(Resource))]
    public class ResourceEditor : OdinEditor
    {
        private Resource item => target as Resource;

        [MenuItem("Tools/Resources/Assign Unknown Icons From Shadow")]
        private static void AssignUnknownIconsFromShadow()
        {
            // Load all resources
            var resources = Blindsided.Utilities.AssetCache.GetAll<Resource>("");

            // Load all sliced sprites from the shadow sheet via AssetDatabase (Editor-only)
            const string shadowPngPath = "Assets/Fonts/FloatingTextIconsShadow.png";
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(shadowPngPath);
            if (subAssets == null || subAssets.Length == 0)
            {
                Debug.LogError($"Could not load sprites from {shadowPngPath}");
                return;
            }

            // Build index -> sprite lookup using the naming pattern: FloatingTextIconsShadow_<index>
            var indexToSprite = new System.Collections.Generic.Dictionary<int, Sprite>();
            foreach (var obj in subAssets)
            {
                if (obj is not Sprite s) continue;
                var name = s.name; // e.g., FloatingTextIconsShadow_42
                var us = name.LastIndexOf('_');
                if (us < 0) continue;
                if (int.TryParse(name[(us + 1)..], out var idx))
                    indexToSprite[idx] = s;
            }

            var modified = 0;
            foreach (var res in resources)
            {
                if (res == null) continue;
                if (!ResourceIconLookup.TryGetIconIndex(res.resourceID, out var index))
                    continue;
                if (!indexToSprite.TryGetValue(index, out var sprite) || sprite == null)
                    continue;

                if (res.UnknownIcon != sprite)
                {
                    res.UnknownIcon = sprite;
                    EditorUtility.SetDirty(res);
                    modified++;
                }
            }

            if (modified > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"Updated UnknownIcon on {modified} Resources from FloatingTextIconsShadow");
            }
            else
            {
                Debug.Log("No Resource UnknownIcon changes were necessary.");
            }
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (item.icon != null)
            {
                var t = GetType("UnityEditor.SpriteUtility");
                if (t != null)
                {
                    var method = t.GetMethod("RenderStaticPreview",
                        new[] { typeof(Sprite), typeof(Color), typeof(int), typeof(int) });
                    if (method != null)
                    {
                        var ret = method.Invoke("RenderStaticPreview",
                            new object[] { item.icon, Color.white, width, height });
                        if (ret is Texture2D tex)
                            return tex;
                    }
                }
            }

            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies)
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        return type;
                }
            }

            return null;
        }
    }
}