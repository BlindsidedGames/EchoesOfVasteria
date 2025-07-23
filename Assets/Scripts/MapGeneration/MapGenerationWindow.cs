#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using TimelessEchoes.MapGeneration;

namespace TimelessEchoes.MapGeneration
{
    public class MapGenerationWindow : OdinMenuEditorWindow
    {
        [MenuItem("Tools/Map Generation Config")]
        private static void Open()
        {
            GetWindow<MapGenerationWindow>().Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            var guids = AssetDatabase.FindAssets("t:MapGenerationConfig");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>(path);
                if (asset != null)
                    tree.Add(System.IO.Path.GetFileNameWithoutExtension(path), asset);
            }

            guids = AssetDatabase.FindAssets("t:TerrainSettings");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TerrainSettings>(path);
                if (asset != null)
                    tree.Add(System.IO.Path.GetFileNameWithoutExtension(path), asset);
            }
            return tree;
        }
    }
}
#endif
