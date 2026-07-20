#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Editor.CustomGUI;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using VinTools.BetterRuleTiles.Internal;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;


namespace VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows
{
    public class SpriteSheetEditorWindow : GUIWindow
    {
        public SpriteSheetEditorWindow(TileSetEditor editor, int sidebarWidth, int tilebarWidth) : base(GUIWindow.WindowLayout.Fill, new RectOffset(sidebarWidth + tilebarWidth + 6, 0, 29, 0), Vector2.zero)
        {
            this._editor = editor;
            this.windowFunction = UpdateUI;
            this.visible = true;
        }

        #region Editor references
        
        private TileSetEditor _editor;
        private bool _needsRepaint;
        
        
        private BetterRuleTileContainer _file { get => _editor._file; set => _editor._file = value; }
        private int _selectedTileID { get => _editor._selectedTileID; set => _editor._selectedTileID = value; }
        private BetterRuleTileContainer.TileData _selectedTile { get => _editor._selectedTile; set => _editor._selectedTile = value; }
        private int _selectedSpriteSheet { get => _editor._selectedSpriteSheet; set => _editor._selectedSpriteSheet = value; }
        private bool _isDrawPageSelected => _editor.IsDrawPageSelected;
        private bool _isGameObjectPageSelected => _editor.IsGameObjectPageSelected;
        private bool _isCollisionPageSelected => _editor.IsCollisionPageSelected;
        private bool _isOutputPageSelected => _editor.IsOutputPageSelected;
        private GUIGrid _grid { get => _editor._grid; set => _editor._grid = value; }
        private bool CheckInvalidSelectedSpriteSheet => _editor.CheckInvalidSelectedSpriteSheet;
        private Texture2D[] _spriteSheetCache => _editor.spriteSheetCache;
        
        
        public bool CurrentlyEditingSpriteOptions => _isGameObjectPageSelected || _isCollisionPageSelected || _isOutputPageSelected;
        public const int GameObjectPickerID = 945128523;
        public int spriteDataThatObjectWasPickedFor = -1;
        
        #endregion
        
        
        void UpdateUI(int windowID)
        {
            if (_editor._selectedSpriteSheet < 0 && _editor._selectedSpriteSheet >= _file._spriteSheets.Count) return;
            if (_editor.ImportWindow.visible) return;

            //draw background
            EditorGUI.DrawRect(new Rect(0, 0, size.x, size.y), Tex.Get(Tex.C_ID.c_toolbarBackgroundColor));

            //create grid
            if (_grid == null) _grid = new GUIGrid();

            //draw grid
            if (_isDrawPageSelected) _grid.DrawGrid(size, DrawGridBackground, DrawSpriteSheetOnGrid, DrawTilesOnGrid, DrawBrushOnGrid, DrawSpriteOutlineOnGrid);
            if (_isGameObjectPageSelected) _grid.DrawGrid(size, new GUIGrid.DrawMethod[] { DrawGridBackground, DrawSpriteSelectionOnGrid }, DrawSpriteSheetOnGrid, DrawGameObjectOverlayOnGrid, DrawSpriteOutlineOnGrid);
            if (_isCollisionPageSelected) _grid.DrawGrid(size, new GUIGrid.DrawMethod[] { DrawGridBackground, DrawSpriteSelectionOnGrid }, DrawSpriteSheetOnGrid, DrawColliderTypeOverlayOnGrid, DrawSpriteOutlineOnGrid);
            if (_isOutputPageSelected) _grid.DrawGrid(size, new GUIGrid.DrawMethod[] { DrawGridBackground, DrawSpriteSelectionOnGrid }, DrawAnimatedSpriteSheetOnGrid, DrawSpriteOutlineOnGrid);

            //refresh grid
            if (_grid.NeedsRepaint || _needsRepaint)
            {
                _needsRepaint = false;
                _editor.Repaint();
            }
        }
        
        #region Draw grid functions
        
        /// <summary>
        /// Draws the transparent texture under the grid
        /// </summary>
        void DrawGridBackground()
        {
            //draw transparent texture
            if (CheckInvalidSelectedSpriteSheet) return;
            //Debug.Log($"File: {_file}, Sprite sheet count: {_file._spriteSheets.Count}, Selected sprite sheet: {_selectedSpriteSheet}");
            var spriteCount = _file._spriteSheets[_selectedSpriteSheet].SpriteCount;

            for (int y = 0; y < spriteCount.y; y++)
            {
                for (int x = 0; x < spriteCount.x; x++)
                {
                    _grid.DrawCells(x * 3, y * 3, 3, 3, Tex.Get(Tex.T_ID.t_checkerBoard));
                }
            }
        }
        /// <summary>
        /// Draw the sprite sheet on the grid
        /// </summary>
        void DrawSpriteSheetOnGrid()
        {
            //get the selected sprite sheet
            if (CheckInvalidSelectedSpriteSheet) return;
            var sheet = _file._spriteSheets[_selectedSpriteSheet];

            //draw the sprite sheet
            for (int i = 0; i < sheet.Sprites.Count; i++)
                _grid.DrawCells(sheet.TexturePositions[i] * sheet.TilesetNeighboringSize, Vector2Int.one * sheet.TilesetNeighboringSize, _spriteSheetCache[i]);
        }
        /// <summary>
        /// Draw the sprite sheet on the grid
        /// </summary>
        void DrawAnimatedSpriteSheetOnGrid()
        {
            //get the selected sprite sheet
            if (CheckInvalidSelectedSpriteSheet) return;
            var sheet = _file._spriteSheets[_selectedSpriteSheet];

            //draw the sprite sheet
            for (int i = 0; i < sheet.Sprites.Count; i++)
            {
                //get reference to sprite override settings
                var spriteSettings = _file._overrideSprites.Find(t => t.BaseSprite == sheet.Sprites[i]);

                //draw the default if there are no sprite settings
                if (spriteSettings == null || spriteSettings.Sprites.Length == 0)
                {
                    DrawCell(_spriteSheetCache[i]);
                    continue;
                }

                //draw stuff based on the output
                switch (spriteSettings.OutputSprite)
                {
                    case ExtendedOutputSprite.Single:
                        DrawCell(_spriteSheetCache[i]);
                        break;
                    case ExtendedOutputSprite.Random:
                        var rand = new System.Random((int)EditorApplication.timeSinceStartup);
                        int randomIndex = rand.Next(0, spriteSettings.Sprites.Length);
                        DrawCellSprite(spriteSettings.Sprites[randomIndex]);
                        break;
                    case ExtendedOutputSprite.Animation:
                        int animIndex = (int)(EditorApplication.timeSinceStartup * ((spriteSettings.MaxAnimationSpeed + spriteSettings.MinAnimationSpeed) / 2f)) % spriteSettings.Sprites.Length;
                        DrawCellSprite(spriteSettings.Sprites[animIndex]);
                        break;
                    case ExtendedOutputSprite.Pattern:
                        int x = sheet.TexturePositions[i].x;
                        int y = sheet.TexturePositions[i].y;
                        int patternIndex = spriteSettings.PatternSize.x * (y % spriteSettings.PatternSize.y) + (x % spriteSettings.PatternSize.x);
                        DrawCellSprite(spriteSettings.Sprites[patternIndex]);
                        break;
                    default:
                        DrawCell(_spriteSheetCache[i]);
                        break;
                }

                void DrawCell(Texture2D tex) => _grid.DrawCells(sheet.TexturePositions[i] * sheet.TilesetNeighboringSize, Vector2Int.one * sheet.TilesetNeighboringSize, tex);
                void DrawCellSprite(Sprite sprite) { if (sprite != null) _grid.DrawCells(sheet.TexturePositions[i] * sheet.TilesetNeighboringSize, Vector2Int.one * sheet.TilesetNeighboringSize, sprite); }
            }
        }
        /// <summary>
        /// Draw the tiles on the grid
        /// </summary>
        void DrawTilesOnGrid()
        {
            //get the selected sprite sheet
            if (CheckInvalidSelectedSpriteSheet) return;
            var sheet = _file._spriteSheets[_selectedSpriteSheet];

            //draw the tiles on top
            for (int x = 0; x < sheet.SpriteCount.x * sheet.TilesetNeighboringSize; x++)
            {
                for (int y = 0; y < sheet.SpriteCount.y * sheet.TilesetNeighboringSize; y++)
                {
                    //find the cell
                    var cell = _file._grid.Find(t =>
                    t.Position.x == x + sheet.Offset.x * sheet.TilesetNeighboringSize &&
                    t.Position.y == y + sheet.Offset.y * sheet.TilesetNeighboringSize);
                    if (cell == null) continue;

                    //get the corresponding tile
                    var tile = _file._tiles.Find(t => t.UniqueID == cell.TileID);
                    if (tile == null) continue;

                    //draw overlay
                    Color color = new Color(tile.Color.r, tile.Color.g, tile.Color.b, .6f);
                    _grid.DrawCell(x, y, color);
                }
            }
        }
        /// <summary>
        /// Draws the preview brush over the grid and places the tile when using the brush
        /// </summary>
        void DrawBrushOnGrid()
        {
            //draw brush preview
            if (_selectedTile == null || !_editor.IsMouseInMethodWindow(_editor._window_SpriteSheetEditor.size)) return;

            //get color
            Color color = new Color(_selectedTile.Color.r, _selectedTile.Color.g, _selectedTile.Color.b, .3f);
            var cell = _grid.GetMouseTilePos();

            //if mouse is not on a drawable area, return
            _needsRepaint = true;
            if (!new RectInt(Vector2Int.zero, _file._spriteSheets[_selectedSpriteSheet].SpriteCount * 3).Contains(cell)) return;

            //draw at mouse position
            _grid.DrawCell(cell, color);

            //modify cell
            if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown))
            {
                if (Event.current.button == 0) _file.SetTile(cell + _file._spriteSheets[_selectedSpriteSheet].Offset * _file._spriteSheets[_selectedSpriteSheet].TilesetNeighboringSize, _selectedTileID);
                else if (Event.current.button == 1) _file.SetTile(cell + _file._spriteSheets[_selectedSpriteSheet].Offset * _file._spriteSheets[_selectedSpriteSheet].TilesetNeighboringSize, -1);
            }
        }

        /// <summary>
        /// Draws an outline around the sprites on the grid
        /// </summary>
        void DrawSpriteOutlineOnGrid()
        {
            //get the selected sprite sheet
            if (CheckInvalidSelectedSpriteSheet) return;
            var sheet = _file._spriteSheets[_selectedSpriteSheet];

            //draw outline
            Color lineCol = new Color(233f / 255f, 170f / 255f, 133f / 255f);
            for (int y = 0; y < sheet.SpriteCount.y; y++)
            {
                for (int x = 0; x < sheet.SpriteCount.x; x++)
                {
                    _grid.DrawCellOutline(x * 3, y * 3, 3, 3, lineCol);
                }
            }

            //diplay selected sprite
            if (_editor.selectedSpriteIndex >= 0 && CurrentlyEditingSpriteOptions)
            {
                lineCol = new Color(1f, 1f, 0f);
                _grid.DrawCellOutline(_editor.selectedSpriteLocation.x * 3, _editor.selectedSpriteLocation.y * 3, 3, 3, lineCol, true, 2);
            }

            _needsRepaint = true;
        }

        /// <summary>
        /// Handles hovering a clicking events on sprites
        /// </summary>
        void DrawSpriteSelectionOnGrid()
        {
            //get the selected sprite sheet
            if (CheckInvalidSelectedSpriteSheet) return;
            var sheet = _file._spriteSheets[_selectedSpriteSheet];

            for (int i = 0; i < sheet.Sprites.Count; i++)
            {
                //this used the sprite location for reference, which is outdated
                //int x = Mathf.RoundToInt(sprite.rect.x / sheet.SpritePixelSize.x);
                //int y = sheet.SpriteCount.y - Mathf.RoundToInt(sprite.rect.y / sheet.SpritePixelSize.y) - 1;
                int x = sheet.TexturePositions[i].x;
                int y = sheet.TexturePositions[i].y;

                //get reference to sprite override settings
                var spriteSettings = _file._overrideSprites.Find(t => t.BaseSprite == sheet.Sprites[i]);

                //get rect 
                Rect rect = _grid.GetRect(x * 3, y * 3, 3, 3);

                //if the mouse is not over the rect we can just go onto the next one
                if (!rect.Contains(Event.current.mousePosition)) continue;

                //set which sprite are we hovering over
                //if (_hoveringOverSpriteLocation != new Vector2Int(x, y)) Debug.Log("Hovering over sprite: " + x + ", " + y);
                _editor.isCursorHoveringOverSprite = true;
                _editor.hoveringOverSpriteLocation = new Vector2Int(x, y);

                //Select with clicking
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (spriteSettings == null)
                    {
                        spriteSettings = new BetterRuleTileContainer.UniversalSpriteData(sheet.Sprites[i]);
                        _file._overrideSprites.Add(spriteSettings);
                    }

                    _editor.selectedSpriteIndex = _file._overrideSprites.FindIndex(t => t.BaseSprite == sheet.Sprites[i]);
                    _editor.selectedSpriteLocation = new Vector2Int(x, y);
                    HandleSpriteSheetClickedEvent(spriteSettings, _editor.selectedSpriteIndex, x, y);
                }

                //return if the mouse is over a sprite
                return;
            }

            //mouse isn't hovering over a sprite
            _editor.isCursorHoveringOverSprite = false;
        }

        /// <summary>
        /// Draws an overlay indicating the type of collider that has been set for that sprite
        /// </summary>
        void DrawColliderTypeOverlayOnGrid()
        {
            //get the selected sprite sheet
            if (CheckInvalidSelectedSpriteSheet) return;
            var sheet = _file._spriteSheets[_selectedSpriteSheet];

            for (int i = 0; i < sheet.Sprites.Count; i++)
            {
                int x = sheet.TexturePositions[i].x;
                int y = sheet.TexturePositions[i].y;

                //get reference to sprite override settings
                var spriteSettings = _file._overrideSprites.Find(t => t.BaseSprite == sheet.Sprites[i]);

                //get rect 
                Rect rect = _grid.GetRect(x * 3, y * 3, 3, 3);

                //cycle with right click
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
                {
                    //create settings if theres not
                    if (spriteSettings == null) spriteSettings = _editor.GetSpriteOverrideSetting(sheet.Sprites[i]);

                    //cycle through enums
                    var enumValues = Enum.GetNames(typeof(Tile.ColliderType)).ToList();
                    int currentIndex = enumValues.IndexOf(Enum.GetName(typeof(Tile.ColliderType), spriteSettings.ColliderType)) + 1;
                    if (currentIndex >= enumValues.Count) currentIndex = 0;
                    spriteSettings.ColliderType = (Tile.ColliderType)Enum.Parse(typeof(Tile.ColliderType), enumValues[currentIndex]);

                    //Debug.Log("Cycling through enum");
                }

                //draw asset preview
                if (spriteSettings == null)
                {
                    GUI.DrawTexture(rect, Tex.Get(Tex.T_ID.t_spriteColliderType));
                    GUI.Label(rect, new GUIContent("", "Sprite"));
                }
                else
                {
                    switch (spriteSettings.ColliderType)
                    {
                        case Tile.ColliderType.None:
                            GUI.DrawTexture(rect, Tex.Get(Tex.T_ID.t_noneColliderType));
                            break;
                        case Tile.ColliderType.Sprite:
                            GUI.DrawTexture(rect, Tex.Get(Tex.T_ID.t_spriteColliderType));
                            break;
                        case Tile.ColliderType.Grid:
                            GUI.DrawTexture(rect, Tex.Get(Tex.T_ID.t_gridColliderType));
                            break;
                        default:
                            break;
                    }

                    GUI.Label(rect, new GUIContent("", Enum.GetName(typeof(Tile.ColliderType), spriteSettings.ColliderType) + " collider type"));
                }
            }
        }
        
        /// <summary>
        /// Draws an overlay indicating which sprites have a gameobject reference, it also handles assigning the picked object from the object picker
        /// </summary>
        void DrawGameObjectOverlayOnGrid()
        {
            //get the selected sprite sheet
            if (CheckInvalidSelectedSpriteSheet) return;
            var sheet = _file._spriteSheets[_selectedSpriteSheet];

            for (int i = 0; i < sheet.Sprites.Count; i++)
            {
                int x = sheet.TexturePositions[i].x;
                int y = sheet.TexturePositions[i].y;

                //get reference to sprite override settings
                var spriteSettings = _file._overrideSprites.Find(t => t.BaseSprite == sheet.Sprites[i]);

                //get rect 
                Rect rect = _grid.GetRect(x * 3, y * 3, 3, 3);

                //delet ewith right click
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition) && spriteSettings != null)
                {
                    spriteSettings.GameObject = null;
                    _needsRepaint = true;
                }

                //draw asset preview
                if (spriteSettings != null && spriteSettings.GameObject != null)
                {
                    GUI.DrawTexture(rect, Tex.Get(Tex.T_ID.t_gameObjectPreview));
                    GUI.Label(rect, new GUIContent("", spriteSettings.GameObject.name));
                }
            }

            //set value picked for object picker
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == GameObjectPickerID && spriteDataThatObjectWasPickedFor >= 0)
                {
                    var assigned = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                    _file._overrideSprites[spriteDataThatObjectWasPickedFor].GameObject = assigned;
                    spriteDataThatObjectWasPickedFor = -1;
                }
            }
        }
        #endregion

        private double _lastMouseClickTime = 0;
        private const double _doubleClickTime = .3f;
        private Vector2 _lastMousePos = Vector2.zero;
        
        public void HandleSpriteSheetClickedEvent(BetterRuleTileContainer.UniversalSpriteData spriteSettings, int spriteIndex, int x, int y)
        {
            //Debug.Log($"Selected sprite {spriteIndex}");

            //detect double click
            bool doubleClick = false;
            if ((EditorApplication.timeSinceStartup - _lastMouseClickTime) < _doubleClickTime && (_lastMousePos - Event.current.mousePosition).magnitude < 10) doubleClick = true;
            _lastMouseClickTime = EditorApplication.timeSinceStartup;
            _lastMousePos = Event.current.mousePosition;


            //gameobject picker
            if (doubleClick && _isGameObjectPageSelected)
            {
                EditorGUIUtility.ShowObjectPicker<GameObject>(spriteSettings.GameObject, false, "", GameObjectPickerID);
                spriteDataThatObjectWasPickedFor = spriteIndex;
            }
        }
    }
}
#endif