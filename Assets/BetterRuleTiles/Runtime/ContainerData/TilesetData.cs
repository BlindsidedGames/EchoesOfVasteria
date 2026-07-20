using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using VinTools.BetterRuleTiles.Editor.Utilities;
#endif

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileContainer
    {
        #region Tileset editor

        [Header("Tileset editor")]
        public bool UseTileSet = false;
        public List<SpriteSheet> _spriteSheets = new List<SpriteSheet>();
        public Vector2Int _spriteSheetCurrentOffset = Vector2Int.zero;

#if UNITY_EDITOR
        /// <summary>
        /// Creates a sprite sheet object from a Texture2D and adds it to the container
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        public void AddSingleSpriteSheet(Texture2D tex)
        {
            if (_spriteSheets.Exists(s => s.Texture == tex)) return;

            var sheet = new SpriteSheet(tex, _spriteSheetCurrentOffset.x, _spriteSheetCurrentOffset.y);
            _spriteSheets.Add(sheet);
            AddSpritesToGrid(sheet);

            _spriteSheetCurrentOffset += new Vector2Int(0, sheet.SpriteCount.y);
        }

        /// <summary>
        /// Populate the grid with sprites from a sprite sheet
        /// </summary>
        /// <param name="sheet"></param>
        public void AddSpritesToGrid(SpriteSheet sheet)
        {
            //add sprites to the grid
            for (int i = 0; i < sheet.Sprites.Count; i++) SetSprite(sheet.GridCoords[i], sheet.Sprites[i]);
        }
        
        /// <summary>
        /// Populate the grid with sprites from a sprite sheet and assign values from the overrides
        /// </summary>
        /// <param name="sheet"></param>
        public void AddSpritesToGrid(SpriteSheet sheet, List<UniversalSpriteData> overrides)
        {
            //add sprites to the grid
            for (int i = 0; i < sheet.Sprites.Count; i++)
            {
                var cell = SetSprite(sheet.GridCoords[i], sheet.Sprites[i]);
                
                // find if there is a corresponding override
                var settings = overrides.Find(o => o.BaseSprite == sheet.Sprites[i]);
                if (settings != null) settings.ApplyToCell(cell);
            }
        }
#endif

        [System.Serializable]
        public class SpriteSheet
        {
            //this number indicates how large is the neighboring area, in this case it's 3x3
            public int TilesetNeighboringSize; 

            public Texture2D Texture;
            public List<Sprite> Sprites = new List<Sprite>();

            [Tooltip("Coordinates of the sprites on the better rule tiles grid, with space in-between")]
            public List<Vector2Int> GridCoords = new List<Vector2Int>();

            [Tooltip("Coordinates of the sprite on the texture")]
            public List<Vector2Int> TexturePositions = new List<Vector2Int>();

            public Vector2Int SpritePixelSize; //the size of sprites in pixels
            public Vector2Int SpritePixelPadding;
            public Vector2Int SpriteCount;

            [Tooltip("The offset in grid coordinates where the tiles start")]
            public Vector2Int Offset;

            public SpriteSheet(Texture2D tex, int xOffset = 0, int yOffset = 0, int tilesetNeighboringSize = 3)
            {
                Texture = tex;
                Offset = new Vector2Int(xOffset, yOffset);
                TilesetNeighboringSize = tilesetNeighboringSize;
#if UNITY_EDITOR
                LoadSpritesFromTexture(tex);
                AssignCoordinates();
#endif
            }

#if UNITY_EDITOR
            /// <summary>
            /// Load in all sprites from the specified texture and assign the values automatically
            /// </summary>
            /// <param name="tex"></param>
            public void LoadSpritesFromTexture(Texture2D tex)
            {
                // check if readable
                if (!tex.isReadable) TextureImporterFunctions.MakeTextureReadable(tex);

                // load sprites  
                List<Sprite> unsortedSprites = TextureImporterFunctions.LoadAllSpritesFromTexture(tex);

                // sort sprites from left to right, top to bottom
                Sprites = unsortedSprites.OrderBy(t => t.rect.y).ThenBy(t => t.rect.x).ToList();

                // get texture grid size
                SpritePixelSize = new Vector2Int((int)unsortedSprites.Average(t => t.rect.width),
                    (int)unsortedSprites.Average(t => t.rect.height));
                
                SpritePixelPadding = FindPadding(unsortedSprites);
                SpriteCount = FindSpriteCount(unsortedSprites);
            }

            public static Vector2Int FindPadding(List<Sprite> sprites)
            {
                var sortedByRow = sprites.OrderBy(t => t.rect.y).ThenBy(t => t.rect.x).ToList();
                var sortedByColumn = sprites.OrderBy(t => t.rect.x).ThenBy(t => t.rect.y).ToList();

                List<int> paddingX = new List<int>();
                // get X padding
                for (int i = 0; i < sortedByRow.Count - 1; i++)
                {
                    var s1 = sortedByRow[i];
                    var s2 = sortedByRow[i + 1];
                    
                    // if they are in the same row
                    if (Math.Abs(s1.rect.y - s2.rect.y) < 1) paddingX.Add(Mathf.RoundToInt(s2.rect.x - s1.rect.x - s1.rect.width));
                }
                
                List<int> paddingY = new List<int>();
                // get Y padding
                for (int i = 0; i < sortedByColumn.Count - 1; i++)
                {
                    var s1 = sortedByColumn[i];
                    var s2 = sortedByColumn[i + 1];
                    
                    // if they are in the same row
                    if (Math.Abs(s1.rect.x - s2.rect.x) < 1) paddingY.Add(Mathf.RoundToInt(s2.rect.y - s1.rect.y - s1.rect.height));
                }

                int x = 0;
                if (paddingX.Count > 0) x = paddingX.GroupBy(n => n).OrderByDescending(g => g.Count()).First().Key;
                int y = 0;
                if (paddingY.Count > 0) y = paddingY.GroupBy(n => n).OrderByDescending(g => g.Count()).First().Key;
                
                return new Vector2Int(x, y);
            }

            public static Vector2Int FindSpriteCount(List<Sprite> sprites)
            {
                var columns = sprites.GroupBy(s => s.rect.x);
                var rows = sprites.GroupBy(s => s.rect.y);
                
                return new Vector2Int(columns.Count(), rows.Count());
                
                //return new Vector2Int(tex.width / size.x, tex.height / size.y);
            }

            /// <summary>
            /// Assign the coordinates of the sprites based on the spritecount and the offset
            /// </summary>
            public void AssignCoordinates()
            {
                TexturePositions = new List<Vector2Int>();
                GridCoords = new List<Vector2Int>();

                // figure out the coordinates
                List<int> xCoords = new List<int>();
                List<int> yCoords = new List<int>();
                foreach (var sprite in Sprites)
                {
                    Vector2Int pos = Vector2Int.RoundToInt(sprite.rect.position);
                    if (!xCoords.Contains(pos.x)) xCoords.Add(pos.x);
                    if (!yCoords.Contains(pos.y)) yCoords.Add(pos.y);
                }
                xCoords.Sort();
                yCoords.Sort();
                
                //add sprites to the grid
                for (int i = 0; i < Sprites.Count; i++)
                {
                    int x = xCoords.IndexOf((int)Sprites[i].rect.x);
                    int y = yCoords.Count - 1 - yCoords.IndexOf((int)Sprites[i].rect.y);
                    TexturePositions.Add(new Vector2Int(x, y));

                    int xg = Mathf.FloorToInt(TilesetNeighboringSize / 2) + (x + Offset.x) * TilesetNeighboringSize;
                    int yg = Mathf.FloorToInt(TilesetNeighboringSize / 2) + (y + Offset.y) * TilesetNeighboringSize;
                    GridCoords.Add(new Vector2Int(xg, yg));
                }
            }

            /// <summary>
            /// Return a temporary texture2D array that matches with the sprite array
            /// </summary>
            /// <returns></returns>
            public Texture2D[] CacheSprites()
            {
                var cache = new Texture2D[Sprites.Count];

                for (int i = 0; i < cache.Length; i++)
                {
                    Rect rect = Sprites[i].rect;
                    var colors = Sprites[i].texture
                        .GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

                    Texture2D tex = new Texture2D((int)rect.width, (int)rect.height);
                    tex.SetPixels(colors);
                    tex.filterMode = FilterMode.Point;
                    tex.Apply();

                    cache[i] = tex;
                }

                return cache;
            }
#endif
        }

        #endregion
    }
}