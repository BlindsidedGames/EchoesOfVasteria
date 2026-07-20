#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VinTools.BetterRuleTiles.Editor.Utilities
{
    public static class DataUtils
    {
        /// <summary>
        /// Get every sprite from the list of objects
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static List<Sprite> GetSprites(Object[] objects)
        {
            List<Sprite> sprites = new List<Sprite>();
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                
                // add if sprite
                if (obj is Sprite sprite1 && !sprites.Contains(sprite1)) sprites.Add(sprite1);
                // add all sub sprites if texture
                if (obj is Texture2D tex)
                {
                    var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj)).ToArray();
                    foreach (var item in subAssets)
                    {
                        if (item is Sprite sprite2 && !sprites.Contains(sprite2)) sprites.Add(sprite2);
                    }
                }
            }

            return sprites;
        }

        /// <summary>
        /// Adds all objects to the sprite array that contain sprite assets
        /// </summary>
        /// <param name="array"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static Sprite[] AddObjectsToSpritesArray(this Sprite[] array, Object[] objects)
        {
            // get sprites from cell
            List<Sprite> sprites = array.ToList();
            // add new sprites
            sprites.AddRange(DataUtils.GetSprites(objects));
            // remove null sprites
            sprites.RemoveAll(s => s == null);
            // save
            array = sprites.ToArray();
            return array;
        }
    }
}
#endif