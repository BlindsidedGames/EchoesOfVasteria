#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Data;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class TileDrawerWindow : GUIWindow
    {
        public TileDrawerWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base( 
            GUIWindow.WindowLayout.BottomLeftCorner, new RectOffset(4, 4, 30, 4), Vector2.zero, new GUIContent(""))
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }

        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;
        
        private Vector2 _tileDrawerScroll;
        bool _drawerCollapsed = false;
        // bool _reorderingTiles = false;
        
        
        private void UpdateUI(int windowID)
        {
            // get values
            string[] drawerTileNames = _file.GetTileNames();

            // calculate values
            int scrollStart = 81;
            int numTiles = _file.Tiles.Count;
            int numDefaultTiles = _defaultTiles.Length;
            int scrollRectWidth = numTiles * 73 + numDefaultTiles * 73 + 94 + (numTiles == 0 ? 3 : 0);


            // set window properties
            width = _drawerCollapsed ? 80 : _file.settings._drawerSize * 70 + 80 + 19;
            height = 115;
            lockPosition = _file.settings._lockWindows;

            // show window title
            GUI.Label(new Rect(5, 0, 100, 25), "Selected", "boldlabel");
            GUI.Label(new Rect(scrollStart + 5, 0, 100, 25), "Tile Drawer", "boldlabel");
            
            // Before scroll rect
            GUI.Label(new Rect(5, 25, 70, 70), "", "button");
            var selectedTileTex = GetSelectedTileTex();
            if (selectedTileTex != null) GUI.DrawTexture(new Rect(5 + 7, 25 + 7, 56, 56), selectedTileTex, ScaleMode.ScaleToFit);

            // collapse
            if (GUI.Button(new Rect(width - 17, 4, 16, 16), _drawerCollapsed ? Tex.Get(Tex.T_ID.t_tileInfo_rightArrow) : Tex.Get(Tex.T_ID.t_tileInfo_leftArrow), Style.IconButton))
            {
                _drawerCollapsed = !_drawerCollapsed;
            }
            if (_drawerCollapsed) return;

            // draw divider
            EditorGUI.DrawRect(new Rect(scrollStart - 1, 0, 1, height), Tex.Get(Tex.C_ID.c_backgroundColor));

            //start scroll
            _tileDrawerScroll = GUI.BeginScrollView(new Rect(scrollStart, 25, width - scrollStart, 88), _tileDrawerScroll, new Rect(0, 0, scrollRectWidth, 70), true, false);

            // draw default tool buttons
            _editor._selectedDrawerTile = -1 - GUI.SelectionGrid(new Rect(5, 0, numDefaultTiles * 73 - 3, 70), -1 - _editor._selectedDrawerTile, _defaultTiles, numDefaultTiles);
            for (int i = 0; i < _defaultTileNames.Length; i++)
            {
                Rect labelPos = new Rect(i * 73 + 5, 0, 70, 65);
                GUI.Label(labelPos, _defaultTileNames[i], Style.LowerCenteredLabel);
            }
            
            // divider line
            EditorGUI.DrawRect(new Rect(numDefaultTiles * 73 + 7, 0, 1, 70), Tex.Get(Tex.C_ID.c_backgroundColor));
            
            // tiles buttons
            Rect userTileRect = new Rect(numDefaultTiles * 73 + 13, 0, numTiles * 73 - 3, 70);
            _editor._selectedDrawerTile = GUI.SelectionGrid(userTileRect, _editor._selectedDrawerTile - 1, new string[_file.GetTileTextures().Length], numTiles) + 1;
            for (int i = 0; i < drawerTileNames.Length; i++) DrawTileButton(userTileRect.x + i * 73, i);
            
            // add new tile button
            if (GUI.Button(new Rect(scrollRectWidth - 75, 0, 70, 70), new GUIContent("Add Tile", "Create a new tile")))
            {
                _file.CreateTile();
                _editor._needsRepaint = true;
            }

            //end scroll
            GUI.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawTileButton(float xPos, int i)
        {
            string tileName = _file.GetTileName(i);
            Rect labelPos = new Rect(xPos, 40, 70, 32);
            if (tileName.Length < 10) GUI.Label(labelPos, tileName, Style.CenteredLabel);
            else GUI.Label(labelPos, tileName, Style.SmallCenteredLabel);
            
            Rect imagePos = new Rect(xPos + 18, 10, 34, 34);
            GUI.DrawTexture(imagePos, _file.GetTileTexture(i));
        }
        
        // Keeping it in case I need the old description
        /*private GUIContent[] _defaultTiles => new GUIContent[] {
            new GUIContent(Tex.Get(Tex.T_ID.t_tileTex_Ignore), "This tile will be ignored in the rule tile, this is the default option, use this to clear tiles on the grid"),
            new GUIContent(Tex.Get(Tex.T_ID.t_tileTex_Empty), "No tile should be placed in this place for the rule to be right"),
            new GUIContent(Tex.Get(Tex.T_ID.t_tileTex_NotSame), "A tile of the same type shouldn't be placed here for the rule to be right"),
            new GUIContent(Tex.Get(Tex.T_ID.t_tileTex_Any), "Any tile could be placed here for the rule to be right")
        };*/
        
        private GUIContent[] _defaultTiles 
        {
            get
            {
                GUIContent[] _return = new GUIContent[_editor._drawerData.Ids.Length];
                for (int i = 0; i < _return.Length; i++)
                {
                    _return[i] = new GUIContent(
                        _editor._drawerData.GetButtonTexture(_editor._drawerData.Ids[i]),
                        _editor._drawerData.GetTileDescription(_editor._drawerData.Ids[i]));
                }

                return _return;
            }
        }

        private string[] _defaultTileNames
        {
            get
            {
                string[] _return = new string[_editor._drawerData.Ids.Length];
                for (int i = 0; i < _return.Length; i++)
                {
                    _return[i] = _editor._drawerData.GetTileName(_editor._drawerData.Ids[i]);
                }

                return _return;
            }
        }
        
        Texture2D GetSelectedTileTex()
        {
            // Get the tiles from the rule tile
            foreach (var idTexturePair in _editor._drawerData.TileTextureDictionary)
                if (_editor._selectedDrawerTile == idTexturePair.Key) return idTexturePair.Value;
            
            // If drawer tile is 0, display selected sprite
            if (_editor._selectedDrawerTile == 0) return SpriteCache.TryGet(_editor._selectedSprite);

            // if selected tile > 0, get the user defined tiles 
            if (_editor._selectedDrawerTile > 0)
                return _file.GetTileTexture(_editor._selectedDrawerTile - 1);

            // return missing texture if the tile selected is invalid
            return Tex.Get(Tex.T_ID.NULL);
        }
    }
}
#endif