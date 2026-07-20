#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Editor.CustomGUI;
using VinTools.BetterRuleTiles.Editor.Generation;
using VinTools.BetterRuleTiles.Editor.GUIWindows;
using VinTools.BetterRuleTiles.Editor.GUIWindows.ImportWindows;
using VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows;
using VinTools.BetterRuleTiles.Editor.Utilities;
using VinTools.BetterRuleTiles.Internal;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;
using TileDrawerWindow = VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows.TileDrawerWindow;

namespace VinTools.BetterRuleTiles.Editor.EditorWindows
{

    public class TileSetEditor : BetterRuleTileEditorWindowBase
    {
        #region Exposed variables to reference in class

        public Texture2D t_editorIcon;

        #endregion

        public static TileSetEditor ShowWindow(BetterRuleTileContainer container,
            BetterRuleTileEditorWindowBase swap = null)
        {
            TileSetEditor window = Resources.FindObjectsOfTypeAll<TileSetEditor>().ToList()
                .Find(t => t._file == container || t._file == null);

            bool newWindow = false;
            if (window == null || window._file == null)
            {
                //create new window
                window = CreateInstance<TileSetEditor>();
                window._file = container;
                window.titleContent = new GUIContent(
                    container.name,
                    window.t_editorIcon,
                    $"Tileset editor ({container.name})"
                );

                newWindow = true;
            }

            //focus
            window.Show();
            window.Focus();

            //copy window properties if swapping
            if (newWindow && swap != null)
            {
                window.position = swap.position;
                window.maximized = swap.maximized;
            }

            //return window
            return window;
        }


        public GUIGrid _grid = new GUIGrid(new GUIGridOptions() { DrawGrid = false, DragGridMouseButtons = new List<int>() { 2 }, });

        public int _selectedSpriteSheet = -1;
        public int _selectedTileID = -1;
        public BetterRuleTileContainer.TileData _selectedTile = null;

        private bool _isSpriteSheetSelected => _selectedSpriteSheet != -1;
        private bool _isTileSelected => _selectedTileID != -1;

        protected void CreateGUI()
        {
            //windows
            SetUpWindows();

            //cache sprites
            CacheSelectedSpriteSheet();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            //draw a static background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), Tex.Get(Tex.C_ID.c_backgroundColor));

            //drag & drop
            CheckDragAndDrop();

            //keyboard shortcuts
            CheckKeyboardShortcuts();

            //draw windows
            DisplayWindows();
        }

        private void CheckKeyboardShortcuts()
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.Delete:
                    RemoveCurrentSpriteSheet();
                    break;
            }
        }

        private void SelectSpriteSheet(BetterRuleTileContainer.SpriteSheet sheet)
        {
            int index = _file._spriteSheets.IndexOf(sheet);
            if (index != -1) SelectSpriteSheet(index);
        }
        public void SelectSpriteSheet(int id)
        {
            _selectedSpriteSheet = id;
            selectedSpriteIndex = -1;

            CacheSelectedSpriteSheet();
            SetSheetGridPosition();
        }

        public void SelectTile(int id)
        {
            _selectedTile = _file._tiles.Find(t => t.UniqueID == id);
            _selectedTileID = id;
        }

        private void CacheSelectedSpriteSheet()
        {
            if (_file._spriteSheets.Count <= _selectedSpriteSheet) _selectedSpriteSheet = -1;
            if (!_isSpriteSheetSelected) return;

            spriteSheetCache = _file._spriteSheets[_selectedSpriteSheet].CacheSprites();
        }

        public void RemoveCurrentSpriteSheet()
        {
            if (!_isSpriteSheetSelected) return;
            RemoveSpriteSheet(_selectedSpriteSheet);
            _selectedSpriteSheet = -1;
        }

        /// <summary>
        /// Deletes the sprite sheet containing the specified texture
        /// </summary>
        /// <param name="texture"></param>
        private void RemoveSpriteSheet(Texture2D texture)
        {
            int index = _file._spriteSheets.FindIndex(s => s.Texture == texture);
            if (index != -1) RemoveSpriteSheet(index);
            _selectedSpriteSheet = -1;
        }

        /// <summary>
        /// Deletes the sprite sheet at the specified index
        /// </summary>
        /// <param name="index"></param>
        private void RemoveSpriteSheet(int index)
        {
            if (EditorUtility.DisplayDialog("Remove sprite sheet?",
                    $"Are you sure you want to remove the texture \"{_file._spriteSheets[index].Texture.name}\"? Any changes made to this sprite sheet will be lost.",
                    "Yes", "Cancel"))
            {
                Rect area = new Rect(
                    _file._spriteSheets[index].Offset *
                    _file._spriteSheets[index].TilesetNeighboringSize,
                    _file._spriteSheets[index].SpriteCount *
                    _file._spriteSheets[index].TilesetNeighboringSize
                );
                _file.DeleteArea(area);

                _file._spriteSheets.RemoveAt(index);
            }
        }

        public void DeleteSelectedTile()
        {
            if (!_isTileSelected) return;

            var tile = _file._tiles.Find(t => t.UniqueID == _selectedTileID);

            if (EditorUtility.DisplayDialog("Delete tile?",
                    $"Are you sure you want to delete tile \"{tile.Name}\"? Any reference to this tile will be lost.",
                    "Yes", "Cancel"))
            {
                _file.DeleteTile(tile);
                SelectTile(-1);
            }
        }

        #region Drag & drop

        private void CheckDragAndDrop()
        {
            if (DragAndDrop.objectReferences.Length <= 0) return;

            //get window under mouse
            var window = GetWindowUnderMouse();

            //drag & drop into the sidebar
            if (Event.current.type == EventType.DragUpdated) DoDragUpdated(window);
            if (Event.current.type == EventType.DragPerform) DoDragPerform(window);
        }

        private void DoDragUpdated(GUIWindow window)
        {
            //show icon
            if (window == _window_Sidebar) DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            else if (window == _window_SpriteOptions && _window_SpriteOptions.CheckDragDropSprites())
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            else if (window == _window_SpriteSheetEditor && isCursorHoveringOverSprite)
            {
                if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is GameObject)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                //else if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is Sprite) DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                else if (DragAndDrop.objectReferences.Length > 1 &&
                         DragAndDrop.objectReferences.All(t => t is Sprite))
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                else DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            else DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }
        
        private void DoDragPerform(GUIWindow window)
        {
            //if over the sidebar
            if (window == _window_Sidebar)
            {
                //if there's only one object, use the import window
                if (DragAndDrop.objectReferences.Length == 1)
                {
                    if (DragAndDrop.objectReferences[0] is Texture2D)
                    {
                        ImportWindow.OpenWindow(DragAndDrop.objectReferences[0] as Texture2D);
                    }
                }
                //check every object
                else
                {
                    foreach (var item in DragAndDrop.objectReferences)
                    {
                        //add spritesheet if it's a texture
                        if (item is Texture2D) _file.AddSingleSpriteSheet(item as Texture2D);
                    }
                }
            }

            // drop into sprites array
            if (window == _window_SpriteOptions && _window_SpriteOptions.CheckDragDropSprites())
            {
                var uSpriteData = _file._overrideSprites[selectedSpriteIndex];
                if (uSpriteData != null) uSpriteData.Sprites = uSpriteData.Sprites.AddObjectsToSpritesArray(DragAndDrop.objectReferences);
            }

            // if assigning gameobjects
            // if assigning sprites
            if (window == _window_SpriteSheetEditor && isCursorHoveringOverSprite)
            {
                //get the override setting for working with it in the following checks
                var spriteOverride = GetSpriteOverrideSetting(GetSpriteAtLocation(hoveringOverSpriteLocation));

                //if dragging only one gameobject
                if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is GameObject)
                {
                    spriteOverride.GameObject = DragAndDrop.objectReferences[0] as GameObject;
                }

                /*if dragging only one sprite
                if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is Sprite)
                {
                    //spriteOverride.BaseSprite = DragAndDrop.objectReferences[0] as Sprite;
                    spriteOverride.Sprites[0] = DragAndDrop.objectReferences[0] as Sprite;
                }*/
                
                //if dragging multiple sprites
                if (DragAndDrop.objectReferences.Length > 1 && DragAndDrop.objectReferences.All(t => t is Sprite))
                {
                    spriteOverride.Sprites = new Sprite[DragAndDrop.objectReferences.Length];
                    for (int i = 0;
                         i < Math.Min(DragAndDrop.objectReferences.Length, spriteOverride.Sprites.Length);
                         i++) spriteOverride.Sprites[i] = DragAndDrop.objectReferences[i] as Sprite;

                    //set the output to animation if it was single
                    if (spriteOverride.OutputSprite == ExtendedOutputSprite.Single)
                        spriteOverride.OutputSprite = ExtendedOutputSprite.Animation;
                }
            }
        }
        #endregion

        #region Window variables

        private GUIWindow _window_Sidebar { get => Windows[0]; set => Windows[0] = value; }
        private GUIWindow _window_SidebarToolbar { get => Windows[1]; set => Windows[1] = value; }
        private GUIWindow _window_MainToolbar { get => Windows[2]; set => Windows[2] = value; }
        private GUIWindow _window_TileDrawer { get => Windows[3]; set => Windows[3] = value; }
        public GUIWindow _window_SpriteSheetEditor { get => Windows[4]; set => Windows[4] = value; }
        public SpriteOptionsWindow _window_SpriteOptions { get => (SpriteOptionsWindow)Windows[5]; set => Windows[5] = value; }

        // this window is separate since we need a way to call the class specific functions
        public TileSetImportWindow ImportWindow;

        protected override void SetUpWindows()
        {
            base.SetUpWindows();

            Windows = new GUIWindow[7];

            int sidebarWidth = 240; //240
            int tilebarWidth = 240; //180

            _window_Sidebar = new SidebarWindow(this, sidebarWidth, position.height);
            _window_SidebarToolbar = new SidebarToolbarWindow(this, sidebarWidth);
            _window_MainToolbar = new MainToolbarWindow(this, sidebarWidth);
            _window_TileDrawer = new TileDrawerWindow(this, sidebarWidth, tilebarWidth);
            _window_SpriteOptions = new SpriteOptionsWindow(this, sidebarWidth, tilebarWidth);
            _window_SpriteSheetEditor = new SpriteSheetEditorWindow(this, sidebarWidth, tilebarWidth);

            //the import window has its own class that handles everything
            ImportWindow = new TileSetImportWindow(_file);
            ImportWindow.OnImportedSpriteSheet += SelectSpriteSheet;
            ImportWindow.OnDeleteSpriteSheet += RemoveSpriteSheet;
            //add custom window to the list of windows
            Windows[6] = ImportWindow;
        }

        protected override void DisplayWindows()
        {
            base.DisplayWindows();

            _window_TileDrawer.visible = DisplayTileDrawer;
            _window_SpriteOptions.visible = DisplaySpriteOptions;
            _window_SpriteSheetEditor.visible = DisplaySpriteSheetEditor;
            //_window_Import.visible = _currentlySelectedTextureToImport != null;
        }

        public int selectedPage = 0;

        public bool IsDrawPageSelected => selectedPage == 0;
        public bool IsGameObjectPageSelected => selectedPage == 1;
        public bool IsCollisionPageSelected => selectedPage == 2;
        public bool IsOutputPageSelected => selectedPage == 3;
        public bool DisplaySpriteSheetEditor => _isSpriteSheetSelected && selectedPage >= 0 && selectedPage <= 3;
        public bool DisplayTileDrawer => IsDrawPageSelected && _isSpriteSheetSelected;
        public bool DisplaySpriteOptions => selectedPage > 0 && selectedPage <= 3;

        #endregion

        #region Sprite sheet editor
        public Texture2D[] spriteSheetCache;

        public int selectedSpriteIndex = -1;
        public Vector2Int selectedSpriteLocation = Vector2Int.zero;
        public Vector2Int hoveringOverSpriteLocation = Vector2Int.zero;
        public bool isCursorHoveringOverSprite = false;
        
        public bool CheckInvalidSelectedSpriteSheet => _selectedSpriteSheet < 0 || _selectedSpriteSheet >= _file._spriteSheets.Count;
        
        /// <summary>
        /// Gets a sprite from the currently selected sheet based on the texture position
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        Sprite GetSpriteAtLocation(Vector2Int location) => GetSpriteAtLocation(location.x, location.y);
        /// <summary>
        /// Gets a sprite from the currently selected sheet based on the texture position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        Sprite GetSpriteAtLocation(int x, int y)
        {
            if (CheckInvalidSelectedSpriteSheet) return null;

            var sheet = _file._spriteSheets[_selectedSpriteSheet];

            int spriteIndex = y * (sheet.SpriteCount.x) + x;
            return sheet.Sprites[spriteIndex];
        }

        /// <summary>
        /// Gets the override data for a sprite, if it doesn't exist it creates one for the sprite
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public BetterRuleTileContainer.UniversalSpriteData GetSpriteOverrideSetting(Sprite sprite)
        {
            var spriteSettings = _file._overrideSprites.Find(t => t.BaseSprite == sprite);
            if (spriteSettings != null) return spriteSettings;

            spriteSettings = new BetterRuleTileContainer.UniversalSpriteData(sprite);
            _file._overrideSprites.Add(spriteSettings);
            return spriteSettings;
        }

        /// <summary>
        /// Centers the currently selected sheet and sets it's scale and bounds
        /// </summary>
        void SetSheetGridPosition()
        {
            // apply layout since it's not setup when we first access the window
            _window_SpriteSheetEditor.ApplyLayout(position);
            
            Vector2 windowSize = _window_SpriteSheetEditor.size;
            Vector2Int spriteCount = _file._spriteSheets[_selectedSpriteSheet].SpriteCount;
            
            // calculate how large the tiles would be if we stretched them to either direction
            float wX = windowSize.x / (spriteCount.x + .6f);
            float wY = windowSize.y / (spriteCount.y + .6f);

            // select the smaller one
            int gridCellSize = wX < wY ? (int)(wX / 3) : (int)(wY / 3);
            int realCellSize = gridCellSize * 3;
            _grid.GridOptions.GridCellSize = new Vector2Int(gridCellSize, gridCellSize);
            _grid.GridOptions.SetZoom(1);
            _grid.GridOptions.MinZoom = .5f;

            // set bounds
            _grid.GridOptions.limitBounds = true;
            _grid.GridOptions.CellBounds = new RectInt(Vector2Int.zero, spriteCount * 3);

            // set offset
            Vector2 offset = gridCellSize * 3 * Vector2.one;
            Vector2 remainingSpace = new Vector2(windowSize.x - spriteCount.x * realCellSize, windowSize.y - spriteCount.y * realCellSize);
            _grid.GridOptions.GridOffset = offset + remainingSpace / 2;
        }
        #endregion
    }
}
#endif