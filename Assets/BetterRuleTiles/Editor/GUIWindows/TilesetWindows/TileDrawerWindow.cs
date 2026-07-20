#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows
{
    public class TileDrawerWindow : GUIWindow
    {
        public TileDrawerWindow(TileSetEditor editor, int sidebarWidth, int tilebarWidth) : base(GUIWindow.WindowLayout.SpanLeft, new RectOffset(sidebarWidth + 3, 0, 29, 0), new Vector2(tilebarWidth, 0))
        {
            this._editor = editor;
            
            this.windowFunction = UpdateUI;
            this.visible = true;
        }

        private TileSetEditor _editor;
        private Vector2 _tileDrawerScrollPos;
        
        
        void UpdateUI(int windowID)
        {
            if (_editor.CheckInvalidSelectedSpriteSheet) return;

            //draw background
            EditorGUI.DrawRect(new Rect(0, 0, width, height), Tex.Get(Tex.C_ID.c_toolbarBackgroundColor));

            // Label
            GUI.Label(new Rect(5, 5, width - 10, 16), "Tiles", EditorStyles.boldLabel);

            //draw line
            GUI.DrawTexture(new Rect(3, 27, width - 6, 1), Tex.Get(Tex.T_ID.t_backgroundTexture));

            //display prompt to add tiles
            if (_editor._file._tiles.Count <= 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("No tiles yet...", Style.CenteredBoldLabel);
            }
            
            GUILayout.Space(6);
            _tileDrawerScrollPos = EditorGUILayout.BeginScrollView(_tileDrawerScrollPos);
            GUILayout.Space(4);

            for (int i = 0; i < _editor._file._tiles.Count; i++)
            {
                var tile = _editor._file._tiles[i];

                EditorGUILayout.BeginHorizontal();
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(24));

                //draw background if selected (this needs to be drawn before everything else)
                if (tile.UniqueID == _editor._selectedTileID) GUI.DrawTexture(new Rect(3, rect.y - 3, width - 6, rect.height + 6), Tex.Get(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, false, 0, Color.white, 0, 5);
                //draw divider line
                if (i > 0) GUI.DrawTexture(new Rect(3, rect.y - 7, width - 6, 1), Tex.Get(Tex.T_ID.t_backgroundTexture));

                //color field
                EditorGUI.DrawRect(new Rect(9, rect.y, rect.height, rect.height), Tex.Get(Tex.C_ID.c_backgroundColor));
                tile.Color = EditorGUI.ColorField(new Rect(10, rect.y + 1, rect.height - 2, rect.height - 2), new GUIContent(), tile.Color, false, false, false);

                //text field
                tile.Name = EditorGUILayout.TextField(tile.Name, Style.MinimalInputField);

                //select button (diable if selected)
                EditorGUI.BeginDisabledGroup(tile.UniqueID == _editor._selectedTileID);
                if (GUILayout.Button(new GUIContent(Tex.Get(Tex.T_ID.t_brushIcon), "Select tile for drawing"), Style.TileSelectButtonStyle)) _editor.SelectTile(tile.UniqueID);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(12);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add new tile")) _editor._file.CreateTile();
            if (GUILayout.Button(new GUIContent(Tex.Get(Tex.T_ID.t_smallDeleteIcon), "Delete currently selected tile"), Style.TileSelectButtonStyle)) _editor.DeleteSelectedTile();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }
    }
}
#endif