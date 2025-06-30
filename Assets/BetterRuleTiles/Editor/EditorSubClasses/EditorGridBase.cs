#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles;
using VinTools.Utilities;

namespace VinToolsEditor.BetterRuleTiles
{
    public class EditorGridBase
    {
        #region Constructors
        public EditorGridBase(BetterRuleTileEditor window, bool _new = true)
        {
            this.window = window;
            SetUpClass(window, _new);
        }

        public void SetUpClass(BetterRuleTileEditor window, bool _new)
        {
            SetUpTextures();

            _miniGridButtonStyle = new GUIStyle(EditorStyles.miniButtonMid);
            _miniGridButtonStyle.normal.background = t_MiniButtonBG;
            _miniGridButtonStyle.hover.background = t_MiniButtonBG;
            _miniGridButtonStyle.active.background = t_MiniButtonBG;
            _miniGridButtonStyle.onNormal.background = t_MiniButtonBG;
            _miniGridButtonStyle.alignment = TextAnchor.MiddleCenter;
            _miniGridButtonStyle.imagePosition = ImagePosition.ImageOnly;
            _miniGridButtonStyle.padding = new RectOffset(1, 1, 2, 0);

            _whiteTextStyle = new GUIStyle("boldlabel");
            _whiteTextStyle.normal.textColor = Color.white;

            if (_new)
            {
                window._gridCellSize = new Vector2Int(32, 32);
                window._tileRenderOffset = Vector2.zero;
            }

            //cache all sprites
            foreach (var item in window._file._grid)
            {
                if (item.Sprite != null && !_spriteTextureCache.ContainsKey(item.Sprite)) CacheSprite(item.Sprite);
            }
        }
        void SetUpTextures()
        {
            t_backgroundTexture = TextureUtils.CreateColoredTexture(40f / 256f);
            t_lineTexture = TextureUtils.CreateColoredTexture(81f / 256f);
            t_highlightTexture = TextureUtils.CreateColoredTexture(168f / 255f);
            t_selectedTexture = TextureUtils.CreateColoredTexture(new Color(40f / 255f, 168f / 255f, 168f / 255f));
            t_clipboardTexture = TextureUtils.CreateColoredTexture(new Color(40f / 255f, 168f / 255f, 40f / 255f));
            t_inspectingTexture = TextureUtils.CreateColoredTexture(new Color(1f, 1f, 40f / 255f));
            t_modifiedTexture = TextureUtils.CreateColoredTexture(new Color(1f, 210f / 255f, 1f));
            t_modifiedDarkTexture = TextureUtils.CreateColoredTexture(new Color(.25f, 40f / 255f, .25f));

            t_MirrorX = TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG1JREFUOE+lj9ENwCAIRB2IFdyRfRiuDSaXAF4MrR9P5eRhHGb2Gxp2oaEjIovTXSrAnPNx6hlgyCZ7o6omOdYOldGIZhAziEmOTSfigLV0RYAB9y9f/7kO8L3WUaQyhCgz0dmCL9CwCw172HgBeyG6oloC8fAAAAAASUVORK5CYII=");
            t_MirrorY = TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsIBFShKgAAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG9JREFUOE+djckNACEMAykoLdAjHbPyw1IOJ0L7mAejjFlm9hspyd77Kk+kBAjPOXcakJIh6QaKyOE0EB5dSPJAiUmOiL8PMVGxugsP/0OOib8vsY8yYwy6gRyC8CB5QIWgCMKBLgRSkikEUr5h6wOPWfMoCYILdgAAAABJRU5ErkJggg==");
            t_MirrorXY = TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsIBFShKgAAAABl0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC4yMfEgaZUAAAHkSURBVDhPrVJLSwJRFJ4cdXwjPlrVJly1kB62cpEguElXKgYKIpaC+EIEEfGxLqI/UES1KaJlEdGmRY9ltCsIWrUJatGm0eZO3xkHIsJdH3zce+ec75z5zr3cf2MMmLdYLA/BYFA2mUyPOPvwnR+GR4PXaDQLLpfrKpVKSb1eT6bV6XTeocAS4sIw7S804BzEZ4IgsGq1ykhcr9dlj8czwPdbxJdBMyX/As/zLiz74Ar2J9lsVulcKpUYut5DnEbsHFwEx8AhtFqtGViD6BOc1ul0B5lMRhGXy2Wm1+ufkBOE/2fsL1FsQpXCiCAcQiAlk0kJRZjf7+9TRxI3Gg0WCoW+IpGISHHERBS5UKUch8n2K5WK3O125VqtpqydTkdZie12W261WjIVo73b7RZVKccZDIZ1q9XaT6fTLB6PD9BFKhQKjITFYpGFw+FBNBpVOgcCARH516pUGZYZXk5R4B3efLBxDM9f1CkWi/WR3ICtGVh6Rd4NPE+p0iEgmkSRLRoMEjYhHpA4kUiIOO8iZRU8AmnadK2/QOOfhnjPZrO95fN5Zdq5XE5yOBwvuKoNxGfBkQ8FzXkPprnj9Xrfm82mDI8fsLON3x5H/Od+RwHdLfDds9vtn0aj8QoF6QH9JzjuG3acpxmu1RgPAAAAAElFTkSuQmCC");
            t_Rotated = TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAHdJREFUOE+djssNwCAMQxmIFdgx+2S4Vj4YxWlQgcOT8nuG5u5C732Sd3lfLlmPMR4QhXgrTQaimUlA3EtD+CJlBuQ7aUAUMjEAv9gWCQNEPhHJUkYfZ1kEpcxDzioRzGIlr0Qwi0r+Q5rTgM+AAVcygHgt7+HtBZs/2QVWP8ahAAAAAElFTkSuQmCC");
            t_Fixed = TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAZdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuMjHxIGmVAAAA50lEQVQ4T51Ruw6CQBCkwBYKWkIgQAs9gfgCvgb4BML/qWBM9Bdo9QPIuVOQ3JIzosVkc7Mzty9NCPE3lORaKMm1YA/LsnTXdbdhGJ6iKHoVRTEi+r4/OI6zN01Tl/XM7HneLsuyW13XU9u2ous6gYh3kiR327YPsp6ZgyDom6aZYFqiqqqJ8mdZz8xoca64BHjkZT0zY0aVcQbysp6Z4zj+Vvkp65mZttxjOSozdkEzD7KemekcxzRNHxDOHSDiQ/DIy3pmpjtuSJBThStGKMtyRKSOLnSm3DCMz3f+FUpyLZTkOgjtDSWORSDbpbmNAAAAAElFTkSuQmCC");

            t_MiniButtonBG = TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAABQAAAAUCAYAAACNiR0NAAAAZUlEQVQ4Ee1UQQ7AIAibyx7Jbywvd63ZkQvByxKagEhooz0wzOwiJgMqCgC5/jCdENM7oCTBXfAcjAoWybgrChG3BSNXcr32MOdXNN0eRq7kev/wEN+ntM8qIZm9D12FbkWAfH8BcuYZpQ7rMLkAAAAASUVORK5CYII=");
            t_MiniButtonCheckmark = TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAnUlEQVQ4EaWSjQ6DIBCD1fgie1LPJ5/3LZTUIQvOJoT7actBmKf3a3qC5YkY7T8Gmx961wBx5KomdwwkZoBqMmrgYhnsBCMGXfGVQb0bzcRPMQSfQGSZKIcHItdnbBJhLYGTo9S0kxI3Yhqz/UQ3oSdEBpdiCH4FSEHREBl3xfDcgNxNouTUu9AbOEEnavdeE/sbNM2RwvcVRjQnzgHLpxuAC3HkEAAAAABJRU5ErkJggg==");

            t_fieldBoxTexture = TextureUtils.CreateColoredTexture(50f / 255f);
            t_windowBorderTexture = TextureUtils.CreateColoredTexture(60f / 255f);
            t_presetBlockTexture = TextureUtils.CreateColoredTexture(new Color(255 / 255f, 147 / 255f, 15 / 255f));

            t_lockedTexture = TextureUtils.MaskTexture(TextureUtils.CreateFilledTexture(Color.red, 64, 64), TextureUtils.Base64ToTexture("iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAABcElEQVR4Ae3Yyw7CMAxEUSrx34gvL7DsKCIvT2xPyi5Z0NzTRawe53m+Hn6/t8OjL73PxQdwD8ZeNkC4YDZA+GBrgHTBswDpg3sB5IJrAPLBCHB85wDcY68v9zD7YZX/f7Ovwd/zQwUjCAMgdDADIFWwBUDq4BEAqeAWAOngEsBWwSUA3Fu5dh+8GNfgP0D3YDwcGyBcMBsgfLA1QLrgWYD0wb0AcsE1APlgBLi/B6AIYR160mTMAaGD8QVbAKQKtgBIHTwCIBXcAiAdXALYKrgEgHsr1+6Dl8Ut0APmHoyHZQOEC2YDhA+2BkgXPAuQPrgXQC64BiAfjAD39wAUIaxDT5qMOSB0ML5gC4BUwRYAqYNHAKSCWwCkg0sAWwWXAHBv5dp98LK4BXrA3IPxsGyAcMFsgPDB1gDpgmcB0gf3AsgF1wDkgxHg/h6AIoR16EmTMQeEDsYXbAGQKtgCIHXwCIBUcAuAdHAJYKtgBPgAlvxQYsbXAVwAAAAASUVORK5CYII="));
        }
        #endregion

        #region variables
        public BetterRuleTileEditor window;
        public static Texture2D t_backgroundTexture;
        public static Texture2D t_lineTexture;
        public static Texture2D t_highlightTexture;
        public static Texture2D t_selectedTexture;
        public static Texture2D t_clipboardTexture;
        public static Texture2D t_inspectingTexture;
        public static Texture2D t_modifiedTexture;
        public static Texture2D t_modifiedDarkTexture;
        public static Texture2D t_lockedTexture;
        public static Texture2D t_presetBlockTexture;

        public static Texture2D t_windowBorderTexture;
        public static Texture2D t_fieldBoxTexture;

        public static Texture2D t_MirrorX;
        public static Texture2D t_MirrorY;
        public static Texture2D t_MirrorXY;
        public static Texture2D t_Rotated;
        public static Texture2D t_Fixed;

        public static Texture2D t_MiniButtonBG;
        public static Texture2D t_MiniButtonCheckmark;


        public Vector2 d_currentGridCellSize;
        public Vector2 d_currentGridStart;
        public Vector2 d_currentGridCellAmount;
        public Vector2 d_currentGridOffset;
        public Vector2 d_currentGridOrigin;

        public Vector2 _dragStartPos = Vector2.zero;
        public Vector2 _zoomScroll = new Vector2(0, 250);
        public Dictionary<Sprite, Texture2D> _spriteTextureCache = new Dictionary<Sprite, Texture2D>();
        public KeyValuePair<Sprite, Texture2D>[] _sortedSpriteCache = new KeyValuePair<Sprite, Texture2D>[0];

        public GUIStyle _miniGridButtonStyle;
        public GUIStyle _whiteTextStyle;

        public float _zoomAmount { get => window._zoomAmount; set => window._zoomAmount = value; }
        public Vector2Int _gridCellSize { get => window._gridCellSize; set => window._gridCellSize = value; }
        public Vector2 _tileRenderOffset { get => window._tileRenderOffset; set => window._tileRenderOffset = value; }
        public Vector2 _gridOffset { get => window._gridOffset; set => window._gridOffset = value; }
        public BetterRuleTileContainer.EditorSettings settings => window._file.settings;
        #endregion

        public void DrawAll()
        {
            //draw base grid, tiles and sprites
            DrawgGridBG();
            DrawTiles(window._file.Grid);
            ManageGrid();
            if (!window._hideSprites) DrawSprites(window._file.Grid);

            //draw ruler
            if (window._showRuler && _zoomAmount > .8f) DrawRuler();

            //draw paste tiles, sprites and selection
            if (window._hasPasteSelection)
            {
                var clipboardMovedBy = new Vector2Int((int)window._clipboardRect.position.x, (int)window._clipboardRect.position.y) - window._clipboardOriginalPos;
                DrawPreviewTiles(window._clipboard, clipboardMovedBy);
                DrawPreviewSprites(window._clipboard, clipboardMovedBy);
                DrawGridTileOutline(Vector2Int.RoundToInt(window._clipboardRect.position), Vector2Int.RoundToInt(window._clipboardRect.size), t_clipboardTexture);
            }


            //highlight modified cells
            if (window._showModified)
                foreach (var tile in window._file.Grid.Where(t => t.AreNeighborPositionsModified || t.AreOutputSpritesModified))
                {
                    bool cycle = Mathf.FloorToInt((float)EditorApplication.timeSinceStartup * 5f) % 2 == 0;


                    if (cycle)
                    {
                        if (tile.AreOutputSpritesModified) DrawGridTileOutline(tile.Position, Vector2Int.one, new Color(1f, 210f / 255f, 1f));
                    }
                    else
                    {
                        if (tile.AreNeighborPositionsModified) DrawGridTileOutline(tile.Position, Vector2Int.one, new Color(1f, 1f, 210f / 255f));
                    }
                }

            //draw preset blocks
            DrawPresetBlocksOverlay(window._file._presetBlocks);
            //draw locked tile overlay
            DrawLockedOverlay(window._file.Grid);

            //draw selection
            if (window._inspectingCell != null) DrawGridTileOutline(window._inspectingCell.Position, t_inspectingTexture);
            if (window._hasSelection) DrawGridTileOutline(Vector2Int.RoundToInt(window._selectionRect.position), Vector2Int.RoundToInt(window._selectionRect.size), t_highlightTexture);
            if (window._movingSelection) DrawGridTileOutline(Vector2Int.RoundToInt(window._selectionRect.position) + window._moveToPos - window._moveFromPos, Vector2Int.RoundToInt(window._selectionRect.size), t_selectedTexture);
            if (window._movingPasteSelection) DrawGridTileOutline(Vector2Int.RoundToInt(window._clipboardRect.position) + window._moveToPos - window._moveFromPos, Vector2Int.RoundToInt(window._clipboardRect.size), t_selectedTexture);

            //update
            window.Repaint();
        }

        #region Grid value functions
        /// <summary>
        /// Returns the tile coordinate of the mouse cursor
        /// </summary>
        /// <returns></returns>
        public Vector2Int GetMouseTilePos() => GetTilePos(Event.current.mousePosition);

        /// <summary>
        /// Returns the screen position of the tile that's under the mouse cursor
        /// </summary>
        /// <returns></returns>
        public Rect GetMouseGridPos() => GetGridPos(GetMouseTilePos());

        /// <summary>
        /// Returns the coordinate of a tile at a given screen position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector2Int GetTilePos(Vector2 position) => Vector2Int.FloorToInt((position - d_currentGridOrigin) / d_currentGridCellSize);

        /// <summary>
        /// Returns the screen coordinate of a given tile
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public virtual Rect GetGridPos(Vector2Int tile) => new Rect(
                d_currentGridOrigin.x + tile.x * d_currentGridCellSize.x,
                d_currentGridOrigin.y + tile.y * d_currentGridCellSize.y,
                d_currentGridCellSize.x,
                d_currentGridCellSize.y
                );

        /// <summary>
        /// Check whether a tile is visible in the window or not
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public virtual bool IsTileInWindow(Vector2Int tile)
        {
            Rect rect = GetGridPos(tile);

            if (
                rect.x > -d_currentGridCellSize.x &&
                rect.x < window.position.width + d_currentGridCellSize.x &&
                rect.y > -d_currentGridCellSize.y &&
                rect.y < window.position.height + d_currentGridCellSize.y
                )
                return true;
            else return false;
        }
        #endregion

        #region Draw grid functions
        //universal methods
        public void DrawgGridBG()
        {
            if (!window.IsMouseOverWindow())
            {
                //get scrollwheel
                _zoomScroll = GUI.BeginScrollView(new Rect(Vector2.zero, window.position.size), _zoomScroll, new Rect(Vector2.zero, window.position.size + new Vector2(0, 500)));
                GUI.EndScrollView();
            }

            //draw background
            if (t_backgroundTexture == null) SetUpTextures();
            GUI.DrawTexture(new Rect(Vector2.zero, new Vector2(window.position.width, window.position.height)), t_backgroundTexture);
        }
        public void ManageGrid()
        {
            //calculate values and draw gridlines
            DrawGrid();

            //quit if mouse is over a ui
            if (window.IsMouseOverWindow()) return;

            //drag move grid
            DragMoveGrid();

            //zoom
            Zoom();
        }

        public virtual void DragMoveGrid()
        {
            if (Event.current.button > 0 && Event.current.type == EventType.MouseDown) _dragStartPos = Vector2Int.FloorToInt(Event.current.mousePosition);
            if (Event.current.button > 0 && Event.current.type == EventType.MouseDrag)
            {
                _gridOffset += Vector2Int.CeilToInt(Event.current.mousePosition) - _dragStartPos;
                _dragStartPos = Vector2Int.CeilToInt(Event.current.mousePosition);
            }
        }
        public virtual void Zoom()
        {
            if (_zoomScroll.y != 250)
            {
                float zoom = (_zoomScroll.y - 250) / 1000;
                float prewZoomAmount = _zoomAmount;

                //apply zoom
                _zoomAmount -= zoom * _zoomAmount;
                _zoomAmount = Mathf.Clamp(_zoomAmount, .1f, 6f);

                //reset scroll
                _zoomScroll.y = 250;

                //change zoom origin
                Vector2Int newGridSize = new Vector2Int(
                (int)(_gridCellSize.x * _zoomAmount),
                (int)(_gridCellSize.y * _zoomAmount)
                );

                Vector2 dist = Event.current.mousePosition - d_currentGridOrigin;
                Vector2 newDist = new Vector2(dist.x / d_currentGridCellSize.x * newGridSize.x, dist.y / d_currentGridCellSize.y * newGridSize.y);
                _gridOffset += dist - newDist;
            }
        }
        public virtual void CalculateGridVariables()
        {
            if (_gridCellSize.x < 1) _gridCellSize = new Vector2Int(1, _gridCellSize.y);
            if (_gridCellSize.y < 1) _gridCellSize = new Vector2Int(_gridCellSize.x, 1);

            //calculate variables
            d_currentGridCellSize = Vector2Int.FloorToInt((Vector2)_gridCellSize * _zoomAmount);

            if (d_currentGridCellSize.x < 1) d_currentGridCellSize.x = 1;
            if (d_currentGridCellSize.y < 1) d_currentGridCellSize.y = 1;

            d_currentGridCellAmount = new Vector2Int(
                (int)window.position.width / (int)d_currentGridCellSize.x + 40,
                (int)window.position.height / (int)d_currentGridCellSize.y + 40
                );
            d_currentGridStart = new Vector2Int(
                (int)_gridOffset.x % (int)d_currentGridCellSize.x - 3 * _gridCellSize.x,
                (int)_gridOffset.y % (int)d_currentGridCellSize.y - 3 * _gridCellSize.y
                );
            d_currentGridOffset = new Vector2Int(
                -(int)_gridOffset.x / (int)d_currentGridCellSize.x,
                -(int)_gridOffset.y / (int)d_currentGridCellSize.y
                );
            d_currentGridOrigin = d_currentGridStart - d_currentGridOffset * d_currentGridCellSize;
        }
        public virtual void DrawGrid()
        {
            if (t_lineTexture == null) SetUpTextures();

            CalculateGridVariables();

            if (!window._file.settings._renderSmallGrid && _zoomAmount < window._file.settings._zoomTreshold) return;

            //draw grid lines
            for (int x = 0; x < d_currentGridCellAmount.x; x++)
            {
                GUI.DrawTexture(new Rect(
                    d_currentGridStart.x + x * d_currentGridCellSize.x,
                    d_currentGridStart.y,
                    1,
                    d_currentGridCellAmount.y * d_currentGridCellSize.y
                    ),
                    t_lineTexture);
            }
            for (int y = 0; y < d_currentGridCellAmount.y; y++)
            {
                GUI.DrawTexture(new Rect(
                    d_currentGridStart.x,
                    d_currentGridStart.y + y * d_currentGridCellSize.y,
                    d_currentGridCellAmount.x * d_currentGridCellSize.x,
                    1
                    ),
                    t_lineTexture);
            }
        }
        public virtual void DrawRuler()
        {
            //get style
            GUIStyle centeredTextStyle = new GUIStyle("label");
            centeredTextStyle.alignment = TextAnchor.LowerCenter;

            //draw bottom row
            for (int x = 0; x < d_currentGridCellAmount.x; x++)
            {
                GUI.Label(new Rect(d_currentGridStart.x + x * d_currentGridCellSize.x, window.position.height - 50, d_currentGridCellSize.x, 50), $"{d_currentGridOffset.x + x}", centeredTextStyle);
            }

            //change style
            centeredTextStyle.alignment = TextAnchor.MiddleLeft;

            //draw left row
            for (int y = 0; y < d_currentGridCellAmount.y; y++)
            {
                GUI.Label(new Rect(0, d_currentGridStart.y + y * d_currentGridCellSize.y, 50, d_currentGridCellSize.y), $"{d_currentGridOffset.y + y}", centeredTextStyle);
            }
        }

        public void DrawGridTileOutline(Vector2Int tile, Texture2D tex, bool drawAll = true) => DrawGridTileOutline(tile, Vector2Int.one, tex, drawAll);
        public virtual void DrawGridTileOutline(Vector2Int tile, Vector2Int size, Texture2D tex, bool drawAll = true)
        {
            Rect rect = new Rect(GetGridPos(tile).position, size * d_currentGridCellSize);

            GUI.DrawTexture(new Rect(rect.x + 0, rect.y + 0, 1, rect.height), tex); // Left
            GUI.DrawTexture(new Rect(rect.x + 0, rect.y + rect.height, rect.width, 1), tex); // Down

            if (drawAll)
            {
                GUI.DrawTexture(new Rect(rect.x + 0, rect.y, rect.width, 1), tex); // Up
                GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y + 0, 1, rect.height), tex); // Right
            }
        }
        public void DrawGridTileOutline(Vector2 tile, Vector2 size, Color col, bool drawAll = true) => DrawGridTileOutline(new Vector2Int(Mathf.RoundToInt(tile.x), Mathf.RoundToInt(tile.y)), new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y)), col, drawAll);
        public virtual void DrawGridTileOutline(Vector2Int tile, Vector2Int size, Color col, bool drawAll = true)
        {
            Rect rect = new Rect(GetGridPos(tile).position, size * d_currentGridCellSize);

            Handles.BeginGUI();
            Handles.color = col;

            Handles.DrawLine(rect.position + Vector2.one, rect.position + Vector2.up * rect.height + Vector2.one); // Left
            Handles.DrawLine(rect.position + Vector2.up * rect.height + Vector2.one, rect.position + rect.size + Vector2.one); // Down

            if (drawAll)
            {
                Handles.DrawLine(rect.position + Vector2.one, rect.position + Vector2.right * rect.width + Vector2.one);
                Handles.DrawLine(rect.position + Vector2.right * rect.width + Vector2.one, rect.position + rect.size + Vector2.one);
            }

            Handles.EndGUI();
        }
        public void DrawEdge(BetterRuleTileContainer.PresetBlock.TileEdge edge, Color col) => DrawEdge(edge.Pos, edge.Pos, edge.Side, col);
        public void DrawEdge(BetterRuleTileContainer.PresetBlock.BlockEdgeSpan edge, Color col) => DrawEdge(edge.Pos1, edge.Pos2, edge.Side, col);
        public virtual void DrawEdge(Vector2Int pos1, Vector2Int pos2, BetterRuleTileContainer.Edge side, Color col)
        {
            Vector2 startGridPos = GetGridPos(Min(pos1, pos2)).position + Vector2.one;

            Handles.BeginGUI();
            Handles.color = col;

            switch (side)
            {
                case BetterRuleTileContainer.Edge.Up:
                    Handles.DrawLine(startGridPos + Vector2.up * d_currentGridCellSize, startGridPos + Vector2.up * d_currentGridCellSize + (Abs(pos2 - pos1) + Vector2.right) * d_currentGridCellSize);
                    break;
                case BetterRuleTileContainer.Edge.Down:
                    Handles.DrawLine(startGridPos, startGridPos + (Abs(pos2 - pos1) + Vector2.right) * d_currentGridCellSize);
                    break;
                case BetterRuleTileContainer.Edge.Left:
                    Handles.DrawLine(startGridPos, startGridPos + (Abs(pos2 - pos1) + Vector2.up) * d_currentGridCellSize);
                    break;
                case BetterRuleTileContainer.Edge.Right:
                    Handles.DrawLine(startGridPos + Vector2.right * d_currentGridCellSize, startGridPos + Vector2.right * d_currentGridCellSize + (Abs(pos2 - pos1) + Vector2.up) * d_currentGridCellSize);
                    break;
                default:
                    break;
            }

            Handles.EndGUI();

            Vector2 Abs(Vector2 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
            Vector2Int Min(Vector2Int v1, Vector2Int v2) => new Vector2Int(Mathf.Min(v1.x, v2.x), Mathf.Min(v1.y, v2.y));
        }
        public virtual void DrawLockedOverlay(List<BetterRuleTileContainer.GridCell> item)
        {
            if (t_lockedTexture == null) SetUpTextures();

            foreach (var cell in item.FindAll(t => t.Locked))
            {
                if (settings._renderLockedOverlay) GUI.DrawTexture(GetGridPos(cell.Position), t_lockedTexture);
                if (settings._renderLockedOutline) DrawGridTileOutline(cell.Position, Vector2Int.one, settings._lockedOutlineColor, true);
            }
        }
        public virtual void DrawPresetBlocksOverlay(List<BetterRuleTileContainer.PresetBlock> blocks)
        {
            foreach (var block in blocks)
            {
                foreach (var item in block.EdgeSpans) DrawEdge(item, settings._presetBlockColor);

                /*foreach (var bounds in block.Bounds)
                {
                    DrawGridTileOutline(bounds.position, bounds.size, settings._presetBlockColor, true);
                }*/
            }
        }

        public virtual void DrawSprites(List<BetterRuleTileContainer.GridCell> item)
        {
            for (int i = 0; i < item.Count; i++)
            {
                //if item not on screen, ignore
                if (!IsTileInWindow(item[i].Position)) continue;
                if (item[i].Sprite == null) continue;

                //add sprite to cache if not in it yet
                if (!_spriteTextureCache.ContainsKey(item[i].Sprite) || _spriteTextureCache[item[i].Sprite] == null) CacheSprite(item[i].Sprite);

                GUI.DrawTexture(GetOffsetGridPos(item[i].Position), _spriteTextureCache[item[i].Sprite]);
            }
        }
        public virtual void DrawPreviewSprites(List<BetterRuleTileContainer.GridCell> item, Vector2Int moveBy) => DrawSprites(item);

        public virtual void CacheSprite(Sprite sprite)
        {
            if (sprite == null) return;

            bool createNew = true;
            if (_spriteTextureCache.ContainsKey(sprite))
            {
                if (_spriteTextureCache[sprite] == null) createNew = false;
                else return;
            }

            if (!sprite.texture.isReadable) FixMissingTexture(sprite.texture);

            if (sprite.texture.isReadable)
            {
                Rect rect = sprite.textureRect;
                var colors = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

                Texture2D tex = new Texture2D((int)rect.width, (int)rect.height);
                tex.SetPixels(colors);
                tex.filterMode = FilterMode.Point;
                tex.Apply();

                if (createNew) _spriteTextureCache.Add(sprite, tex);
                else _spriteTextureCache[sprite] = tex;
                SortSpriteCache();
            }
            else
            {
                var tex = TextureUtils.CreateMissingTexture();
                if (createNew) _spriteTextureCache.Add(sprite, tex);
                else _spriteTextureCache[sprite] = tex;
                SortSpriteCache();
                DisplayMissingTextureError(sprite);
            }
        }
        public void SortSpriteCache() => _sortedSpriteCache = _spriteTextureCache.ToArray().OrderBy(t => t.Key.name).ToArray();
        public virtual void DisplayMissingTextureError(Sprite sprite) => Debug.LogWarning($"Texture data of sprite \"{sprite}\" couldn't be read as the image is not readable. Enable the \"Read/Write\" option in the texture import settings, than reload the editor window.", sprite.texture);

        public void FixMissingTexture(Texture2D tex)
        {
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));

            importer.isReadable = true;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        public virtual void DrawTiles(List<BetterRuleTileContainer.GridCell> item)
        {
            for (int i = 0; i < item.Count; i++)
            {
                //if item not on screen, ignore
                if (!IsTileInWindow(item[i].Position)) continue;
                if (!window._file.DoesTileExist(item[i].TileID)) continue;

                if (item[i].TileID > 0) GUI.DrawTexture(GetOffsetGridPos(item[i].Position), window._file.GetTileTex(item[i].TileID));
                else
                {
                    DrawDefaultTiles(item, i, GetGridPos(item[i].Position));
                }
            }
        }
        public virtual void DrawPreviewTiles(List<BetterRuleTileContainer.GridCell> item, Vector2Int movedBy) => DrawTiles(item);
        public virtual void DrawDefaultTiles(List<BetterRuleTileContainer.GridCell> item, int i, Rect rect)
        {
            if (!window._initializedWindows) return;

            switch (item[i].TileID)
            {
                case -2: GUI.DrawTexture(rect, window.t_gridTex_Empty); break;
                case -3: GUI.DrawTexture(rect, window.t_gridTex_NotSame); break;
                case -4: GUI.DrawTexture(rect, window.t_gridTex_Any); break;
            }
        }

        public Rect GetOffsetGridPos(Vector2Int tile)
        {
            Rect original = GetGridPos(tile);
            return new Rect(
                original.x + d_currentGridCellSize.x * _tileRenderOffset.x,
                original.y + d_currentGridCellSize.y * _tileRenderOffset.y,
                original.width,
                original.height
                );
        }

        #endregion

        #region windows
        public virtual Rect DrawInteractiveMiniGrid(Rect rect, BetterRuleTileContainer.GridCell cell)
        {
            //reset if deleted all neighbors, this is to prevent errors
            if (cell.NeighborPositions.Count <= 0) ResetCellNeighbors(cell);

            //calculate values
            Vector2Int size = new Vector2Int(18, 18);
            Vector2Int min = new Vector2Int(cell.NeighborPositions.Min(t => t.x) - 1, cell.NeighborPositions.Min(t => t.y) - 1);
            Vector2Int max = new Vector2Int(cell.NeighborPositions.Max(t => t.x) + 1, cell.NeighborPositions.Max(t => t.y) + 1);
            Vector2 start = new Vector2(rect.x + (rect.width - (max.x - min.x + 1) * size.x) / 2f, rect.y + 26);
            Rect allRect = new Rect(rect.x, rect.y, rect.width, size.y * (max.y - min.y) + 75);

            //draw background, title and buttons
            DrawMiniGridBackground(allRect, cell);

            //display grid
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector2 index = new Vector2(x - min.x, y - min.y);
                    Rect cellRect = new Rect(start + index * size - Vector2.one, size + new Vector2Int(2, 2));

                    //center
                    if (x == 0 && y == 0)
                    {
                        GUIContent buttonIcon = new GUIContent("");
                        switch (cell.Transform)
                        {
                            case RuleTile.TilingRuleOutput.Transform.Fixed: buttonIcon = new GUIContent(t_Fixed, "Fixed"); break;
                            case RuleTile.TilingRuleOutput.Transform.Rotated: buttonIcon = new GUIContent(t_Rotated, "Rotated"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorX: buttonIcon = new GUIContent(t_MirrorX, "MirrorX"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorY: buttonIcon = new GUIContent(t_MirrorY, "MirrorY"); break;
                            case RuleTile.TilingRuleOutput.Transform.MirrorXY: buttonIcon = new GUIContent(t_MirrorXY, "MirrorXY"); break;
                        }

                        if (GUI.Button(cellRect, buttonIcon, _miniGridButtonStyle))
                        {
                            switch (cell.Transform)
                            {
                                case RuleTile.TilingRuleOutput.Transform.Fixed: cell.Transform = RuleTile.TilingRuleOutput.Transform.Rotated; continue;
                                case RuleTile.TilingRuleOutput.Transform.Rotated: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorX; continue;
                                case RuleTile.TilingRuleOutput.Transform.MirrorX: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorY; continue;
                                case RuleTile.TilingRuleOutput.Transform.MirrorY: cell.Transform = RuleTile.TilingRuleOutput.Transform.MirrorXY; continue;
                                case RuleTile.TilingRuleOutput.Transform.MirrorXY: cell.Transform = RuleTile.TilingRuleOutput.Transform.Fixed; continue;
                            }
                        }
                        continue;
                    }

                    //has rule
                    if (cell.NeighborPositions.Contains(new Vector3Int(x, y, 0)))
                    {
                        if (GUI.Button(cellRect, t_MiniButtonCheckmark, _miniGridButtonStyle)) cell.NeighborPositions.Remove(new Vector3Int(x, y, 0));
                        continue;
                    }
                    //has no rule
                    else
                    {
                        if (GUI.Button(cellRect, "", _miniGridButtonStyle)) cell.NeighborPositions.Add(new Vector3Int(x, y, 0));
                        continue;
                    }
                }
            }

            //return size of the window
            return new Rect(allRect.x, allRect.y, allRect.width, allRect.height - size.y);
        }
        public void DrawMiniGridBackground(Rect rect, BetterRuleTileContainer.GridCell cell)
        {
            //draw background
            GUI.DrawTexture(rect, t_windowBorderTexture, ScaleMode.StretchToFill, true, 0, Color.white, 0, 5);
            GUI.DrawTexture(rect, t_backgroundTexture, ScaleMode.StretchToFill, true, 0, Color.white, 1, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), t_fieldBoxTexture, ScaleMode.StretchToFill, true, 0, Color.white, 0, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), t_backgroundTexture, ScaleMode.StretchToFill, true, 0, Color.white, 1, 5);
            
            if (EditorGUIUtility.isProSkin) GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), "Neighbor positions", "boldlabel");
            else GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), "Neighbor positions", _whiteTextStyle);

            //bottom buttons
            if (!window._hasSelection) GUI.Box(new Rect(rect.position + new Vector2(5, rect.height - 25), new Vector2(rect.width / 2 - 7, 20)), "Select an area to add it");
            if (window._hasSelection && GUI.Button(new Rect(rect.position + new Vector2(5, rect.height - 25), new Vector2(rect.width / 2 - 7, 20)), "Add selection")) AddSelectionToRules(cell);
            if (GUI.Button(new Rect(rect.position + new Vector2(rect.width / 2 + 2, rect.height - 25), new Vector2(rect.width / 2 - 7, 20)), "Reset to default")) ResetCellNeighbors(cell);

        }
        public virtual void AddSelectionToRules(BetterRuleTileContainer.GridCell cell)
        {
            List<Vector3Int> pos = new List<Vector3Int>();
            for (int x = Mathf.Min(window._selectionStart.x, window._selectionEnd.x); x <= Mathf.Max(window._selectionStart.x, window._selectionEnd.x); x++)
            {
                for (int y = Mathf.Min(window._selectionStart.y, window._selectionEnd.y); y <= Mathf.Max(window._selectionStart.y, window._selectionEnd.y); y++)
                {
                    var p = new Vector3Int(x - cell.Position.x, y - cell.Position.y, 0);
                    if (p != Vector3Int.zero && !cell.NeighborPositions.Contains(p)) pos.Add(p);
                }
            }
            cell.NeighborPositions.AddRange(pos);
        }
        public void ResetCellNeighbors(BetterRuleTileContainer.GridCell cell)
        {
            cell.NeighborPositions = window._file.DefaultNeighborPositions(cell.Position);

            cell.Transform = RuleTile.TilingRuleOutput.Transform.Fixed;
        }
        #endregion
    }
}
#endif