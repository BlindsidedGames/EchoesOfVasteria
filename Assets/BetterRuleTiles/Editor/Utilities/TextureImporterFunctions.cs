#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace VinTools.BetterRuleTiles.Editor.Utilities
{
    public static class TextureImporterFunctions
    {
        public static void MakeSpriteReadable(Sprite sprite)
        {
            if (IsSpritePartOfAtlas(sprite, out var atlas)) MakeAtlasReadable(sprite);
            else MakeTextureReadable(sprite.texture);
        }

        public static bool IsSpritePartOfAtlas(Sprite sprite, out SpriteAtlas atlas)
        {
            // set default value for atlas
            atlas = null;
            
            // check if sprite is null
            if (!sprite) return false;
            // get path
            string spritePath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(spritePath)) return false;
            
            // find atlases that contain this sprite guid
            string[] atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { spritePath });

            // found atlas
            if (atlasGuids.Length != 0)
            {
                string atlasPath = AssetDatabase.GUIDToAssetPath(atlasGuids[0]);
                atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                return true;
            }
            
            // fallback
            atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
            foreach (string guid in atlasGuids)
            {
                string atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                string[] dependencies = AssetDatabase.GetDependencies(atlasPath, false);
            
                foreach (string dep in dependencies)
                {
                    if (dep == spritePath)
                    {
                        atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Make the texture readable in the editor
        /// </summary>
        /// <param name="tex"></param>
        public static void MakeTextureReadable(Texture2D tex)
        {
            try
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
                
                importer.isReadable = true;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            catch (InvalidCastException)
            {
                Debug.LogError($"Couldn't enable the read & write option on texture \"{tex.name}\".", tex);
            }
        }
        
        public static void MakeAtlasReadable(Sprite sprite)
        {
            // Find and loop through all sprite atlas assets
            string[] atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
            foreach (string guid in atlasGuids)
            {
                // load the atlas
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid));
                if (!atlas) continue;

                //Debug.Log($"Checking sprite \"{sprite.name}\" on atlas \"{atlas.name}\".");
                // Check if our texture is packed in this atlas
                if (!atlas.CanBindTo(sprite)) return;
                
                //Debug.Log($"Found atlas {atlas.name}.");
                
                // get settings and check if readable
                SpriteAtlasTextureSettings atlasTextureSettings = atlas.GetTextureSettings();
                if (atlasTextureSettings.readable) return;
                
                MakeAtlasReadable(atlas);
                return;
            }
        }

        public static void MakeAtlasReadable(SpriteAtlas atlas)
        {
            Debug.LogWarning($"Better Rule Tiles is not capable of enabling the \"Read/Write Enabled\" setting on " +
                             $"the \"{atlas.name}\" sprite atlas, which is required for the tool to properly display the sprites. " +
                             $"Navigate to the asset and please manually enable this option. Once the option is enabled, you can " +
                             $"force a refresh by selecting the settings dropdown in the BRT editor and clicking the \"Clear Sprite Cache\" button.", atlas);
            
        }
        
        /// <summary>
        /// Return a list of all sprites that is contained within a Texture2D asset
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        public static List<Sprite> LoadAllSpritesFromTexture(Texture2D tex)
        {
            List<Sprite> sprites = new List<Sprite>();
            foreach (var item in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(tex))
                         .Where(t => t is Sprite).ToArray()) sprites.Add(item as Sprite);
            
            return sprites;
        }
    }
}
#endif