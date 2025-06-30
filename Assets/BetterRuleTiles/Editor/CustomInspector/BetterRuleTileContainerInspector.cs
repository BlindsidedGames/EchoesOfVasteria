#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VinTools.BetterRuleTiles;

namespace VinToolsEditor.BetterRuleTiles
{
    [CustomEditor(typeof(BetterRuleTileContainer))]
    public class BetterRuleTileContainerInspector : Editor
    {
        bool showInspector = false;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Open in editor window")) BetterRuleTileEditor.ShowWindow(target as BetterRuleTileContainer);
            if (GUILayout.Button("Close editor window")) BetterRuleTileEditor.CloseWindow(target as BetterRuleTileContainer);
            //if (GUILayout.Button("Debug editor window")) BetterRuleTileEditor.DebugWindow(target as BetterRuleTileContainer);
            EditorGUILayout.Space();

            if (!showInspector)
            {
                EditorGUILayout.HelpBox("This asset was not intended to be edited by users. Open the editor tool instead with the button above. Only view the content of this object for debugging purposes.", MessageType.Warning);
                if (GUILayout.Button("Display inspector")) showInspector = true;
            }
            if (showInspector) base.OnInspectorGUI();
        }
    }
}

#endif