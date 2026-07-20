#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace VinTools.BetterRuleTiles.Editor.Utilities
{
    public static class TypeUtils
    {
        public static Type GetTypeFromGuid(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
    
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            return script?.GetClass();
        }
    
        public static string GetTypeGuid(Type type)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        
                if (script != null && script.GetClass() == type) return guid;
            }
            
            // fallback
            return null;
        }
    }
}
#endif