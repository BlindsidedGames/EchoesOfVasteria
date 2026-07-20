#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace VinTools.BetterRuleTiles.Editor.Utilities
{
    public class TexToBaseConverter : EditorWindow
    {
        [MenuItem("Tools/Better Rule Tiles/Open TexToBase64 Converter", false, 160)]
        public static TexToBaseConverter ShowWindow() => GetWindow<TexToBaseConverter>("TexToBase64 Converter");

        private Texture2D tex;
        private string base64;

        private void OnGUI()
        {
            tex = (Texture2D)EditorGUILayout.ObjectField("Texture to convert", tex, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (tex == null) GUILayout.Box("No texture selected!");
            else if (!tex.isReadable) GUILayout.Box("Texture is not readable!");
            else
            {
                if (GUILayout.Button("Convert texture")) base64 = "\"" + TextureToBase64(tex) + "\"";
            }
            
            if (GUILayout.Button("Clear Output")) base64 = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.TextArea(base64);
        }

        public static string TextureToBase64(Texture2D texture)
        {
            byte[] bytes = texture.EncodeToPNG();
            string str = Convert.ToBase64String(bytes);
            Debug.Log($"{texture.name}:\n\"{str}\"");
            return str;
        }
    }
}
#endif