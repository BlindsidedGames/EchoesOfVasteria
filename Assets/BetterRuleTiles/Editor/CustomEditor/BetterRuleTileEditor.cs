#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Data;
using VinTools.BetterRuleTiles.Editor.Grid;
using VinTools.BetterRuleTiles.Editor.GUIWindows;
using VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows;
using VinTools.BetterRuleTiles.Editor.GUIWindows.ImportWindows;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.EditorWindows
{
    public partial class BetterRuleTileEditor : BetterRuleTileEditorWindowBase
    {
        public Texture2D t_editorIcon;

        #region Open window methods
        public static BetterRuleTileEditor ShowWindow(BetterRuleTileContainer container, BetterRuleTileEditorWindowBase swap = null)
        {
            BetterRuleTileEditor window = Resources.FindObjectsOfTypeAll<BetterRuleTileEditor>().ToList().Find(t => t._file == container || t._file == null);

            bool newWindow = false;
            if (!window || !window._file)
            {
                //create new window
                window = CreateInstance<BetterRuleTileEditor>();
                window._file = container;
                window.titleContent = new GUIContent(
                    container.name,
                    window.t_editorIcon,
                    $"Rule Tile Editor ({container.name})"
                    );

                newWindow = true;
            }

            //set up window
            window.SetupGrid(false);
            window.SetUpGUI();

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
        public static void CloseWindow(BetterRuleTileContainer container)
        {
            BetterRuleTileEditor window = Resources.FindObjectsOfTypeAll<BetterRuleTileEditor>().ToList().Find(t => t._file == container);

            if (window != null)
            {
                Debug.Log($"Closed window for asset \"{container.name}\"");
                try
                {
                    window.Close();
                }
                catch (Exception)
                {

                }
            }
            else
            {
                Debug.Log($"There is no window opened for asset \"{container.name}\"");
            }
        }
        public static void DebugWindow(BetterRuleTileContainer container)
        {
            BetterRuleTileEditor window = Resources.FindObjectsOfTypeAll<BetterRuleTileEditor>().ToList().Find(t => t._file == container);

            Debug.Log(window);
        }
        #endregion

        public void SetupGrid(bool _new = false)
        {
            switch (_file.settings._gridShape)
            {
                case BetterRuleTileContainer.GridShape.Isometric: _grid = new IsometricEditorGrid(this, _new); return;
                case BetterRuleTileContainer.GridShape.HexagonalPointedTop: _grid = new HexagonalEditorGrid(this, false, _new); return;
                case BetterRuleTileContainer.GridShape.HexagonalFlatTop: _grid = new HexagonalEditorGrid(this, true, _new); return;
                default: _grid = new EditorGridBase(this, _new); return;
            }
        }

        private void SetUpGUI()
        {
            //reset variables
            //_selectedDrawerTile = int.MinValue;
            _hideSprites = false;
            _hasSelection = false;
            _inspectingCell = null;
        }
        protected void CreateGUI()
        {
            // set up window positions
            SetUpWindows();

            // set window variables
            foreach (var item in Windows) item.ApplyLayout(position);
            _window_Toolbar.lockPosition = true;
        }
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (_file == null)
            {
                GUI.Label(new Rect(position.width / 2 - 150, position.height / 2 - 20, 300, 20), "Double click or open a \"Better Rule Tile Container\"", Style.CenteredBoldLabel);
                GUI.Label(new Rect(position.width / 2 - 150, position.height / 2 - 00, 300, 20), "asset to start editing it in this window.", Style.CenteredBoldLabel);

                this.Close();

                return;
            }

            wantsMouseMove = true;

            // draw grid stuff
            if (_grid == null) SetupGrid(false);
            _grid.DrawAll();

            // drag and drop
            CheckDragAndDrop();

            // tools
            HandleTools();
            HighlightCells();

            // wondows
            DisplayWindows();
            
            // Repaint
            Repaint();
        }

        protected override void SetUpWindows()
        {
            base.SetUpWindows();
            
            UpdateDrawerData();
            Windows = new GUIWindow[11];

            _window_Toolbar = new EditorToolbarWindow(_file, this);
            _window_TileDrawer = new TileDrawerWindow(_file, this);
            _window_TileInfo = new TileInfoWindow(_file, this);
            _window_SettingsWindow = new EditorSettingsWindow(_file, this);
            _window_ExportWindow = new ExportDropdownWindow(_file, this);
            _window_RecolorWindow = new SpriteReplaceWindow(_file, this);
            _window_GridCellInfo = new GridCellInfoWindow(_file, this);
            _window_SpriteDrawer = new SpriteDrawerWindow(_file, this);
            _window_UnivesalSpriteSettings = new UniversalSpriteSettingsWindow(_file, this);
            _window_ImportWindow = new TileSetImportWindow(_file, true);
            _window_BulkCellEditorWindow = new BulkCellEditorWindow(_file, this);
        }
        protected override void DisplayWindows()
        {
            base.DisplayWindows();

            if (_window_TileInfo != null) _window_TileInfo.visible = _selectedDrawerTile > 0 && _inspectingCell == null;
            if (_window_GridCellInfo != null) _window_GridCellInfo.visible = _inspectingCell != null;
            if (_window_BulkCellEditorWindow != null) _window_BulkCellEditorWindow.visible = _selectedTool == ToolbarTool.Inspect && _hasSelection;
            if (!_window_UnivesalSpriteSettings.visible) CurrentlySepectedOverrideSpriteToModify = -1;
            _window_SpriteDrawer.visible = _file.settings._displaySpriteDrawer;
            _window_UnivesalSpriteSettings.visible = _file.settings._displayUniversalSpriteSettings;
        }
        
        public void UpdateDrawerData()
        {
            if (_drawerData == null) _drawerData = new TileDrawerData(_file);
            else _drawerData.Initialize(_file);
        }
    }
}
#endif