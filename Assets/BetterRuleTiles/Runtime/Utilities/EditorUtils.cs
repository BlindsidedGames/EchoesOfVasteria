#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VinTools.BetterRuleTiles.Runtime.Utilities
{
    public class EditorUtils
    {
        public static T CreateScriptableObjectAsset<T>(string assetName) where T: ScriptableObject
        {
            // Get selected folder from the project window
            string folder = Selection.activeObject ? AssetDatabase.GetAssetPath(Selection.activeObject) : "Assets";
            //string folder = Selection.assetGUIDs.Length == 0 ? "Assets" : AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            
            // Check if folder is valid
            if (!AssetDatabase.IsValidFolder(folder))
            {
                int lastSeparator = folder.LastIndexOfAny(new []{'\\', '/'});
                folder = folder.Substring(0,  lastSeparator);
                
                // if still not valid folder, default to assets
                if (!AssetDatabase.IsValidFolder(folder)) folder = "Assets";
            }

            // create full path
            string name = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{assetName}.asset");

            // Create asset object
            T asset = ScriptableObject.CreateInstance<T>();

            // save with renaming
            ProjectWindowUtil.CreateAsset(asset, name);

            /* Manual method (doesn't rename)
            // Create asset
            AssetDatabase.CreateAsset(asset, name);
            AssetDatabase.SaveAssets();
            */

            // focus
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            
            // return
            return asset;
        }
    }
}

#endif