#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using VinTools.BetterRuleTiles;

namespace VinToolsEditor.BetterRuleTiles
{
    public class BetterRuleTileAssetHandler
    {
        [OnOpenAsset]
        public static bool OpenCustomEditorWindow(int instanceID, int line)
        {
            Object target = EditorUtility.InstanceIDToObject(instanceID);

            if (target is BetterRuleTileContainer)
            {
                BetterRuleTileEditor.ShowWindow(target as BetterRuleTileContainer);
                return true;
            }
            return false;
        }
        public static void OpenCustomEditorWindow(BetterRuleTileContainer target)
        {
            BetterRuleTileEditor.ShowWindow(target);
        }
    }
}
#endif