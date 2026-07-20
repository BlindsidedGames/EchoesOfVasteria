#if UNITY_EDITOR
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.CustomGUI;
using VinTools.BetterRuleTiles.Editor.Data;
using VinTools.BetterRuleTiles.Editor.Grid;
using VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows;
using VinTools.BetterRuleTiles.Editor.GUIWindows.ImportWindows;

namespace VinTools.BetterRuleTiles.Editor.EditorWindows
{
    public partial class BetterRuleTileEditor
    {
        // ------------------- Variables -------------------
        
        public GUIBuilder guiBuilder = new GUIBuilder();
        public EditorGridBase _grid;
        public bool _hasSelection = false;
        public Vector2Int _selectionStart;
        public Vector2Int _selectionEnd;
        public Vector2Int _moveFromPos;
        public Vector2Int _moveToPos;
        public bool _movingSelection = false;
        public bool _movingPasteSelection = false;
        public BetterRuleTileContainer.GridCell _inspectingCell;
        private Vector2Int _inspectTool_selectionStart;
        private Vector2Int _inspectTool_selectionEnd;
        
        // ------------------- Window variables -------------------
        
        public float ConfigWindowWidth = 0;
        public ToolbarTool _selectedTool;
        public Sprite _selectedSprite = null;
        public int CurrentlySepectedOverrideSpriteToModify = -1;
        public bool AutoScrollToSelectedOverrideSprite = false;
        public Rect _spriteSettingsNoDropZone = new Rect();
        public float _toolbarHeight;
        public TileDrawerData _drawerData;
        
        // ------------------- Properties -------------------
        
        public Rect _selectionRect
        {
            get
            {
                return new Rect(
                    new Vector2Int(Mathf.Min(_selectionEnd.x, _selectionStart.x),
                        Mathf.Min(_selectionEnd.y, _selectionStart.y)),
                    new Vector2Int(Mathf.Abs(_selectionEnd.x - _selectionStart.x) + 1,
                        Mathf.Abs(_selectionEnd.y - _selectionStart.y) + 1)
                );
            }
        }
        
        public bool HasInspectingSelection(out RectInt rect)
        {
            rect = new RectInt(
                new Vector2Int(Mathf.Min(_inspectTool_selectionEnd.x, _inspectTool_selectionStart.x),
                    Mathf.Min(_inspectTool_selectionEnd.y, _inspectTool_selectionStart.y)),
                new Vector2Int(Mathf.Abs(_inspectTool_selectionEnd.x - _inspectTool_selectionStart.x) + 1,
                    Mathf.Abs(_inspectTool_selectionEnd.y - _inspectTool_selectionStart.y) + 1)
            );
            return _selectedTool == ToolbarTool.Inspect && _inspectTool_selectionStart != _inspectTool_selectionEnd;
        }
        
        // ------------------- Window properties -------------------
        
        public int _selectedDrawerTile
        {
            get => _window_TileInfo._selectedDrawerTile;
            set
            {
                if (_window_TileInfo._selectedDrawerTile == value) return;
                if (value > 0) _inspectingCell = null;
                
                _window_TileInfo._selectedDrawerTile = value;

                if (value != 0 && _selectedTool != ToolbarTool.Brush) _selectedTool = ToolbarTool.Brush;
            }
        }
        /// <summary>
        /// _selectedDrawerTile == 0
        /// </summary>
        private bool HaveSelectedSprite => _selectedDrawerTile == 0;
        /// <summary>
        /// _selectedDrawerTile > 0
        /// </summary>
        private bool HaveSelectedUserTiles => _selectedDrawerTile > 0;
        /// <summary>
        /// _selectedDrawerTile less than 0
        /// </summary>
        private bool HaveSelectedDefaultTiles => _selectedDrawerTile < 0;

        // ------------------- Settings -------------------

        public float _zoomAmount
        {
            get => _file.settings._zoomAmount;
            set => _file.settings._zoomAmount = value;
        }

        public Vector2 _tileRenderOffset
        {
            get => _file.settings._tileRenderOffset;
            set => _file.settings._tileRenderOffset = value;
        }

        public Vector2 _gridOffset
        {
            get => _file.settings._gridOffset;
            set => _file.settings._gridOffset = value;
        }

        public bool _lockWindows
        {
            get => _file.settings._lockWindows;
            set => _file.settings._lockWindows = value;
        }

        public bool _hideSprites
        {
            get => _file.settings._hideSprites;
            set => _file.settings._hideSprites = value;
        }

        public bool _showRuler
        {
            get => _file.settings._showRuler;
            set => _file.settings._showRuler = value;
        }

        public bool _showModified
        {
            get => _file.settings._showModified;
            set => _file.settings._showModified = value;
        }

        // ------------------- Tools -------------------

        public enum ToolbarTool
        {
            Brush = 0,
            Picker = 1,
            Eraser = 2,
            Move = 3,
            Select = 4,
            Inspect = 5
        }

        private bool HoldingCtrl => 
            Event.current.modifiers.HasFlag(EventModifiers.Control) 
            || Event.current.modifiers.HasFlag(EventModifiers.Command);
        
        // ------------------- Windows -------------------
        
        public EditorToolbarWindow _window_Toolbar
        {
            get => (EditorToolbarWindow)Windows[0];
            set => Windows[0] = value;
        }

        private TileDrawerWindow _window_TileDrawer
        {
            get => (TileDrawerWindow)Windows[1];
            set => Windows[1] = value;
        }

        private TileInfoWindow _window_TileInfo
        {
            get => (TileInfoWindow)Windows[2];
            set => Windows[2] = value;
        }

        public EditorSettingsWindow _window_SettingsWindow
        {
            get => (EditorSettingsWindow)Windows[3];
            set => Windows[3] = value;
        }

        public ExportDropdownWindow _window_ExportWindow
        {
            get => (ExportDropdownWindow)Windows[4];
            set => Windows[4] = value;
        }

        public SpriteReplaceWindow _window_RecolorWindow
        {
            get => (SpriteReplaceWindow)Windows[5];
            set => Windows[5] = value;
        }

        public GridCellInfoWindow _window_GridCellInfo
        {
            get => (GridCellInfoWindow)Windows[6];
            set => Windows[6] = value;
        }

        private SpriteDrawerWindow _window_SpriteDrawer
        {
            get => (SpriteDrawerWindow)Windows[7];
            set => Windows[7] = value;
        }

        private UniversalSpriteSettingsWindow _window_UnivesalSpriteSettings
        {
            get => (UniversalSpriteSettingsWindow)Windows[8];
            set => Windows[8] = value;
        }

        private TileSetImportWindow _window_ImportWindow
        {
            get => (TileSetImportWindow)Windows[9];
            set => Windows[9] = value;
        }

        private BulkCellEditorWindow _window_BulkCellEditorWindow
        {
            get => (BulkCellEditorWindow)Windows[10];
            set => Windows[10] = value;
        }

    }
}
#endif