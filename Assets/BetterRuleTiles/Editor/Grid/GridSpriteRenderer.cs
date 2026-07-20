#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Data;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using Cache = VinTools.BetterRuleTiles.Editor.Data.SpriteCache;

namespace VinTools.BetterRuleTiles.Editor.Grid
{
    public class GridSpriteRenderer
    {
        private static Vector2 CalculateAnchor(Rect cellPosition, Vector2 gridAnchor)
        {
            return new Vector2(
                cellPosition.x + cellPosition.width * gridAnchor.x, 
                cellPosition.y - cellPosition.height * (gridAnchor.y - 1)
                );
        }
        
        private static Rect OffsetRect(Rect originalRect, Vector2 offset) => OffsetRect(originalRect, offset.x, offset.y);
        private static Rect OffsetRect(Rect originalRect, float offsetX, float offsetY)
        {
            return new Rect(
                originalRect.x + offsetX,
                originalRect.y + offsetY,
                originalRect.width,
                originalRect.height
                );
        }

        private static Rect CalculateAnchoredPosition(Vector2 anchorPosition, Vector2 pivot, Vector2 imageSize, Vector2 unscaledGridCellSize)
        {
            return new Rect(
                anchorPosition.x - pivot.x * unscaledGridCellSize.x, 
                anchorPosition.y - (imageSize.y - pivot.y) * unscaledGridCellSize.y, 
                imageSize.x * unscaledGridCellSize.x,
                imageSize.y * unscaledGridCellSize.y
            );
        }
        
        /// <summary>
        /// Draw a sprite on the specified grid, while respecting the pivot and anchor points and the aspect ratio
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="cell"></param>
        public static void DrawSpriteOnGrid(EditorGridBase grid, BetterRuleTileContainer.GridCell cell) => DrawSpriteOnGrid(grid, cell.Sprite, cell.Position);
        public static void DrawSpriteOnGrid(EditorGridBase grid, Sprite sprite, Vector2Int position) => DrawSpriteOnGrid(grid, sprite, position, Color.white);
        public static void DrawSpriteOnGrid(EditorGridBase grid, Sprite sprite, Vector2Int position, Color color)
        {
            // get the location of the grid cell
            var cellPosition = grid.GetGridPos(position);
            
            // if sprite is null, just draw a missing texture at the cell position
            /*if (!sprite)
            {
                GUI.DrawTexture(cellPosition, TextureUtils.CreateMissingTexture());
                return;
            }*/
            // Commented as the TryGet function accounts for missing textures
            
            // calculate necessary values
            Vector2 anchorPosition = CalculateAnchor(cellPosition, grid.settings._spriteAnchor);
            Vector2 pivotPosition = sprite.pivot / sprite.pixelsPerUnit;
            Vector2 imageSize = sprite.rect.size / sprite.pixelsPerUnit;
            
            // calculate the position of the sprite
            Rect spritePos = CalculateAnchoredPosition(anchorPosition, pivotPosition, imageSize, grid.d_currentUnscaledGridCellSize);
            
            // offset the position by the offset amount
            var offsetPosition = OffsetRect(
                spritePos, 
                grid.d_currentGridCellSize.x * grid._tileRenderOffset.x,
                grid.d_currentGridCellSize.y * grid._tileRenderOffset.y
                );
            
            // draw the sprite
            GUI.DrawTexture(RectTools.SquareifyRect(offsetPosition), Cache.TryGet(sprite), ScaleMode.ScaleToFit, true, 0.0f, color, 0, 0);
        }

        public static void DrawTileOnGrid(EditorGridBase grid, BetterRuleTileContainer.GridCell cell, TileDrawerData drawerData) => DrawTileOnGrid(grid, cell.TileID, cell.Position, drawerData);
        public static void DrawTileOnGrid(EditorGridBase grid, int tileID, Vector2Int position, TileDrawerData drawerData) => DrawTileOnGrid(grid, tileID, position, drawerData, Color.white);
        public static void DrawTileOnGrid(EditorGridBase grid, int tileID, Vector2Int position, TileDrawerData drawerData, Color color)
        {
            // TODO check if tile ID is valid
            Texture2D tex = null;
            // get texture for user defined tiles
            if (tileID > 0) tex = grid.window._file.GetTileTex(tileID);
            // get texture of default tiles
            else if (grid.window._initializedWindows)
            {
                foreach (var id in drawerData.Ids) 
                    if (tileID == id) 
                        tex = drawerData.GetTileTexture(id);
            }
            
            // return if there was no texture found
            if (!tex) return;
            
            // get the size of the image texture
            Vector2 imageSize = (tileID > 0 ? Vector2.one : grid._defaultTileScale) * grid.settings._tileSize;
            // calculate the position of the texture
            Vector2 anchorPosition = CalculateAnchor(grid.GetGridPos(position), Vector2.one / 2);// grid.settings._spriteAnchor);
            Rect texturePos = CalculateAnchoredPosition(anchorPosition, imageSize / 2, imageSize, grid.d_currentUnscaledGridCellSize);

            // add the offset to the position
            var offsetPosition = OffsetRect(
                texturePos, 
                grid.d_currentGridCellSize.x * grid._tileRenderOffset.x,
                grid.d_currentGridCellSize.y * grid._tileRenderOffset.y
            );

            // draw
            GUI.DrawTexture(RectTools.SquareifyRect(offsetPosition), tex, ScaleMode.ScaleToFit, true, 0.0f, color, 0, 0);
        }
    }
}
#endif