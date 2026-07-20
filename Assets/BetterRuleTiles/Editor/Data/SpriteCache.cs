#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Utilities;
using VinTools.BetterRuleTiles.Runtime.Utilities;

namespace VinTools.BetterRuleTiles.Editor.Data
{
    public static class SpriteCache
    {
        public static Dictionary<Sprite, Texture2D> SpriteTextureCache = new Dictionary<Sprite, Texture2D>();
        public static Sprite[] SortedSprites = new Sprite[0];
        
        public static int SpriteCount => SpriteTextureCache.Count;

        /// <summary>
        /// Add a sprite to the cache if it's not added already
        /// </summary>
        /// <param name="sprite">The sprite to cache</param>
        /// <param name="cachedSprite">Returns true if cached a new sprite</param>
        /// <param name="forceCache">When set to true, the sprite will get cached even if it is already present</param>
        public static void CacheSprite(Sprite sprite, out bool cachedSprite, bool forceCache = false)
        {
            // set cached sprite to false by default
            cachedSprite = false;
            // return if sprite is null
            if (!sprite) return;

            // check if the sprite is already cached
            bool alreadyHasEntry = false;
            if (SpriteTextureCache.ContainsKey(sprite)) {
                alreadyHasEntry = true;
                
                // if sprite is already cached and has a texture associated with it, return
                if (!forceCache && SpriteTextureCache[sprite]) return;
            }

            // make sure texture is readable
            if (!sprite.texture.isReadable) TextureImporterFunctions.MakeSpriteReadable(sprite);

            // if texture was made readable successfully
            if (sprite.texture.isReadable)
            {
                // get color data from sprite
                Rect rect = sprite.textureRect;
                var colors = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

                // create new texture with the sprite color data
                Texture2D tex = new Texture2D((int)rect.width, (int)rect.height);
                tex.SetPixels(colors);
                tex.filterMode = FilterMode.Point;
                tex.Apply();

                // add it to the dictionary
                if (!alreadyHasEntry) SpriteTextureCache.Add(sprite, tex);
                else SpriteTextureCache[sprite] = tex;
            }
            // texture is still not readable
            else
            {
                // set it as a missing texture
                if (!alreadyHasEntry) SpriteTextureCache.Add(sprite, _missingTexture);
                else SpriteTextureCache[sprite] = _missingTexture;
                
                // print warning
                Debug.LogWarning($"Texture data of sprite \"{sprite}\" couldn't be read as the image is not readable. Enable the \"Read/Write\" option in the texture import settings, than reload the editor window.", sprite.texture);
            }

            // successfully cached a new sprite
            cachedSprite = true;
            SortSprites();
        }

        /// <summary>
        /// Add a sprite to the cache if it's not added already
        /// </summary>
        /// <param name="sprite">The sprite to cache</param>
        /// <param name="forceCache">When set to true, the sprite will get cached even if it is already present</param>
        public static void CacheSprite(Sprite sprite, bool forceCache = false) => CacheSprite(sprite, out bool cachedSprite, forceCache);
        /// <summary>
        /// Shorthand for CacheSprite(sprite)
        /// </summary>
        /// <param name="sprite"></param>
        public static void Add(Sprite sprite) => CacheSprite(sprite, out bool cachedSprite);


        /// <summary>
        /// Reimport all sprites in the cache
        /// </summary>
        public static void ReimportAll()
        {
            // reimport all sprites
            var keys = SpriteTextureCache.Keys.ToList();
            foreach (Sprite sprite in keys) CacheSprite(sprite, out bool cachedSprite, true);
        }
        /// <summary>
        /// Remove all cached textures
        /// </summary>
        public static void ClearCache()
        {
            while (SpriteTextureCache.Count > 0) SpriteTextureCache.Remove(SpriteTextureCache.Keys.First());
        }
        
        
        /// <summary>
        /// Sort sprites based on key names and save it to the sorted array
        /// </summary>
        public static void SortSprites() => SortedSprites = new List<Sprite>(SpriteTextureCache.Keys).OrderBy(t => t.name).ToArray();
        
        /// <summary>
        /// Shorthand for SpriteTextureChache[sprite]
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public static Texture2D Get(Sprite sprite) => SpriteTextureCache[sprite];
        /// <summary>
        /// Get texture based on index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Texture2D GetSorted(int index) => SpriteTextureCache[SortedSprites[index]];
        
        /// <summary>
        /// Missing texture
        /// </summary>
        private static Texture2D _missingTexture = TextureUtils.CreateMissingTexture();
        /// <summary>
        /// Check if the sprite has a key, if it doesn't it tries to resolve it, or return a missing texture
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public static Texture2D TryGet(Sprite sprite)
        {
            // if the sprite is null, return missing texture
            if (!sprite) return _missingTexture;
            
            // if it has key and a texture associated with it
            if (SpriteTextureCache.ContainsKey(sprite) && SpriteTextureCache[sprite] != null) return SpriteTextureCache[sprite];
            // else, try fixing it
            CacheSprite(sprite, out bool cachedSprite);
            
            // check if cached successfully
            if (cachedSprite) return SpriteTextureCache[sprite];
            else return _missingTexture;
        }
        /// <summary>
        /// Check if the sprite has a key, if it doesn't it tries to resolve it. If failed to resolve it returns false.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="tex"></param>
        /// <returns>Whether the texture is not null</returns>
        public static bool TryGet(Sprite sprite, out Texture2D tex)
        {
            tex = TryGet(sprite);
            return tex != null;
        }
        
        /// <summary>
        /// Index based TryGet function 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Texture2D TryGetSorted(int index) => TryGet(SortedSprites[index]);
    }
}
#endif