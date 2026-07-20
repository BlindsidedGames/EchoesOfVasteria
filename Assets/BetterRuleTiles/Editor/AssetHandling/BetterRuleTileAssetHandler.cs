#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;

namespace VinTools.BetterRuleTiles.Editor.AssetHandling
{
    public class BetterRuleTileAssetHandler
    {
#if UNITY_6000_3_OR_NEWER
        // Unity 6 (6000.3+)
        [OnOpenAsset]
        public static bool OpenCustomEditorWindow(EntityId entityId, int line)
        {
            Object target = EditorUtility.EntityIdToObject(entityId);
            return OpenCustomEditorWindowInternal(target, line);
        }
#else
        [OnOpenAsset]
        public static bool OpenCustomEditorWindow(int instanceID, int line)
        {
            Object target = EditorUtility.InstanceIDToObject(instanceID);
            return OpenCustomEditorWindowInternal(target, line);
        }
#endif
        
        public static bool OpenCustomEditorWindowInternal(Object target, int line)
        {
            // if asset is a BetterRuleTileContainer
            if (target is BetterRuleTileContainer)
            {
                var container  = target as BetterRuleTileContainer;
                
                // Tileset
                if (container.UseTileSet) TileSetEditor.ShowWindow(container);
                // Regular container
                else BetterRuleTileEditor.ShowWindow(container);
                
                // return
                return true;
            }
            return false;
        }
    }
}
#endif