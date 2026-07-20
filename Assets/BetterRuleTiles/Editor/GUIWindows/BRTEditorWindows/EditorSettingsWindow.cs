#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Data;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class EditorSettingsWindow : GUIWindow
    {
        
        public EditorSettingsWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base(
            new Rect(0, 0, 300, 400), new GUIContent(""), false, true, true)
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }

        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;

        Vector2 _scrollPos;
        
        private void UpdateUI(int windowID)
        {
            // Title bar
            GUI.Label(new Rect(0, 0, width, 25), "More settings", Style.CenteredBoldLabel);
            EditorGUILayout.Space();

            // Scroll
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Tile drawer
            DrawTileDrawerOptions();
            EditorGUILayout.Space();

            // Sprite drawer
            DrawSpriteDrawerOptions();
            EditorGUILayout.Space();

            // Grid options
            DrawGridOptions();
            EditorGUILayout.Space();

            // Locked cells
            DrawLockedCellOptions();
            
            // reset values to not crash the app
            if (_file.settings._zoomAmount < .1f) _file.settings._zoomAmount = .1f;

            EditorGUILayout.EndScrollView();
        }

        void DrawTileDrawerOptions()
        {
            _editor.guiBuilder.DrawInBackground(new GUIContent("Tile Drawer", "Setting to change the appearance of the tile drawer"), 1, (r) =>
            {
                _file.settings._drawerSize = EditorGUILayout.IntField(new GUIContent("Tile drawer size", "Number of tiles visible in the drawer at once"), _file.settings._drawerSize);
            });
            
            // limit value
            _file.settings._drawerSize = Math.Max(_file.settings._drawerSize, 1);
        }

        void DrawSpriteDrawerOptions()
        {
            _editor.guiBuilder.DrawInBackground(new GUIContent("Sprite Drawer", "Settings to change the appearance of the sprite drawer"), 7, (r) =>
            {
                _file.settings._drawerHeight = EditorGUILayout.IntField(new GUIContent("Display size", "Changes the size of the sprites in the sprite drawer."), _file.settings._drawerHeight);
                _file.settings._drawerCollumns = EditorGUILayout.IntField(new GUIContent("Collapsed columns", "Sets the amount of columns visible in the sprite drawer when collapsed."), _file.settings._drawerCollumns);
                _file.settings._expandedDrawerCollumns = EditorGUILayout.IntField(new GUIContent("Expanded columns", "Specifies the amount of columns when the sprite drawer is expanded."), _file.settings._expandedDrawerCollumns);
                EditorGUILayout.Space();
                
                if (GUILayout.Button(new GUIContent("Clear sprite cache", "Remove all sprites from the sprite cache. Sprites currently in use will be immediately regenerate."))) 
                    SpriteCache.ClearCache();
                if (GUILayout.Button(new GUIContent("Reimport sprites", "Reimport all sprites in the sprite cache."))) 
                    SpriteCache.ReimportAll();
                if (GUILayout.Button(new GUIContent("Add all sprites", "Finds all sprites in the asset database and adds it to the sprite drawer.")))
                    _editor.AddAllTextureAssets();
            }, adjustHeight: -5);
            
            //limit drawer values
            _file.settings._drawerHeight = Math.Max(_file.settings._drawerHeight, 32);
            _file.settings._drawerCollumns = Math.Max(_file.settings._drawerCollumns, 1);
            _file.settings._expandedDrawerCollumns = Math.Max(_file.settings._expandedDrawerCollumns, 4);
        }

        void DrawGridOptions()
        {
            _editor.guiBuilder.DrawInBackground(new GUIContent("Grid", "Change the appearance of the editor's grid"), 12, (r) =>
            {
                _file.settings._cellSize = EditorGUILayout.Vector2Field(new GUIContent("Cell size", "Specifies how large should one cell be compared to the unit size. Only affects sprites."), _file.settings._cellSize);
                _file.settings._tileSize = EditorGUILayout.Vector2Field(new GUIContent("Tile size", "Specifies the size of the preview tiles inside the editor, compared to the unit size."), _file.settings._tileSize);
                _file.settings._spriteAnchor = EditorGUILayout.Vector2Field(new GUIContent("Sprite anchor", "The location of the pivot point in the grid cell."), _file.settings._spriteAnchor);
                _file.settings._tileRenderOffset = EditorGUILayout.Vector2Field("Grid cell offset", _file.settings._tileRenderOffset);

                if (_file.settings._cellSize.x < 0.03f) _file.settings._cellSize = new Vector2(0.03f, _file.settings._cellSize.y);
                if (_file.settings._cellSize.y < 0.03f) _file.settings._cellSize = new Vector2(_file.settings._cellSize.x, 0.03f);

                EditorGUILayout.Space(16);
                _file.settings._zoomAmount = EditorGUILayout.FloatField("Current zoom", _file.settings._zoomAmount);
                _file.settings._renderSmallGrid = EditorGUILayout.Toggle(new GUIContent("Render small grid", "Render the grid when it's zoomed out?"), _file.settings._renderSmallGrid);
                _file.settings._zoomTreshold = EditorGUILayout.FloatField(new GUIContent("Zoom threshold", "At what zoom value should the grid stop rendering?"), _file.settings._zoomTreshold);
            }, adjustHeight: 6);
        }

        void DrawLockedCellOptions()
        {
            _editor.guiBuilder.DrawInBackground(new GUIContent("Locked cells", "Change how locked cells are displayed"), 3, (r) =>
            {
                _file.settings._renderLockedOverlay = EditorGUILayout.Toggle(new GUIContent("Show locked overlay", "Shows a striped overlay above the tiles to indicate that the cells are locked"), _file.settings._renderLockedOverlay);
                _file.settings._renderLockedOutline = EditorGUILayout.Toggle(new GUIContent("Show locked outline", "Shows an outline around tiles to indicate that the cells are locked"), _file.settings._renderLockedOutline);
                _file.settings._lockedOutlineColor = EditorGUILayout.ColorField(new GUIContent("Outline color", "The color of the outline around locked cells"), _file.settings._lockedOutlineColor);
            });
        }
    }
}
#endif