#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Object = UnityEngine.Object;

namespace VinTools.BetterRuleTiles.Editor.Inspector
{
    [CustomEditor(typeof(BetterRuleTileContainer))]
    public class BetterRuleTileContainerInspector : UnityEditor.Editor
    {
        public Sprite BRTContainerIcon;
        public Sprite TilesetIcon;

        bool showInspector = false;

        public override void OnInspectorGUI()
        {
            // Make sure serialized object is updated
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Tile References", EditorStyles.boldLabel);
            
            SerializedProperty tileObjectsProperty = serializedObject.FindProperty("_tileObjects");
            EditorGUILayout.PropertyField(tileObjectsProperty, true);
            
            if (GUILayout.Button("Add missing tiles")) AddMissingTileReferences(target as BetterRuleTileContainer, tileObjectsProperty);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Window Management", EditorStyles.boldLabel);
            if (GUILayout.Button("Open in editor window")) BetterRuleTileEditor.ShowWindow(target as BetterRuleTileContainer);
            if (GUILayout.Button("Close editor window")) BetterRuleTileEditor.CloseWindow(target as BetterRuleTileContainer);
            //if (GUILayout.Button("Debug editor window")) BetterRuleTileEditor.DebugWindow(target as BetterRuleTileContainer);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            if (!showInspector)
            {
                EditorGUILayout.HelpBox("This asset was not intended to be edited by users. Open the editor tool instead with the button above. Only view the content of this object for debugging purposes.", MessageType.Warning);
                if (GUILayout.Button("Display inspector")) showInspector = true;
            }
            if (showInspector) base.OnInspectorGUI();
            
            // Apply properties
            serializedObject.ApplyModifiedProperties();
        }

        private void AddMissingTileReferences(BetterRuleTileContainer container, SerializedProperty tileObjectsProperty)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(container)))
            {
                // skip asset if not tile
                if (!(asset is TileBase tile)) continue;
                
                // add tiles to list
                if (!container._tileObjects.Contains(tile))
                {
                    // Add new element to the serialized property
                    tileObjectsProperty.arraySize++;
                    SerializedProperty newElement = tileObjectsProperty.GetArrayElementAtIndex(tileObjectsProperty.arraySize - 1);
                    newElement.objectReferenceValue = tile;
                }
            }
        }



        #region preview texture
        private BetterRuleTileContainer item { get { return (target as BetterRuleTileContainer); } }
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            Type t = GetType("UnityEditor.SpriteUtility");
            if (t != null)
            {
                MethodInfo method = t.GetMethod("RenderStaticPreview", new[] { typeof(Sprite), typeof(Color), typeof(int), typeof(int) });
                if (method != null)
                {
                    if (item.UseTileSet)
                    {
                        object ret = method.Invoke("RenderStaticPreview", new object[] { TilesetIcon, Color.white, width, height });
                        if (ret is Texture2D)
                            return ret as Texture2D;
                    }
                    else
                    {
                        object ret = method.Invoke("RenderStaticPreview", new object[] { BRTContainerIcon, Color.white, width, height });
                        if (ret is Texture2D)
                            return ret as Texture2D;
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
        #endregion
    }
}
#endif