#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Editor.CustomGUI;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using VinTools.BetterRuleTiles.Editor.Generation;
using VinTools.BetterRuleTiles.Editor.GUIWindows;
using VinTools.BetterRuleTiles.Editor.GUIWindows.ImportWindows;
using VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows;
using VinTools.BetterRuleTiles.Internal;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows
{
    public class SidebarToolbarWindow : GUIWindow
    {
        public SidebarToolbarWindow(TileSetEditor editor, int sidebarWidth) : base(GUIWindow.WindowLayout.BottomLeftCorner, new RectOffset(), new Vector2(sidebarWidth, 26))
        {
            this._editor = editor;
            this.windowFunction = UpdateUI;
            this.visible = true;
        }
        
        private TileSetEditor _editor;

        void UpdateUI(int windowID)
        {
            //draw background
            EditorGUI.DrawRect(new Rect(0, 0, width, height), Tex.Get(Tex.C_ID.c_toolbarBackgroundColor));

            //create toolbar
            var toolbar = new GUIToolbar(rectangle, Style.ToolbarButtonStyle);
            toolbar.DrawSpaceRight();
            if (toolbar.DrawButtonRight(new GUIContent(Tex.Get(Tex.T_ID.t_deleteIcon), "Remove selected spritesheet (Delete)"))) _editor.RemoveCurrentSpriteSheet();
            toolbar.DrawSpaceRight();
            //import button will reveal object picker
            if (toolbar.DrawButtonRight(new GUIContent(Tex.Get(Tex.T_ID.t_importIcon), "Import sprite sheet"))) EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", _editor.ImportWindow.ImportTexturePickerID);

            //set 
            if (EditorGUIUtility.GetObjectPickerControlID() == _editor.ImportWindow.ImportTexturePickerID && EditorGUIUtility.GetObjectPickerObject() is Texture2D)
            {
                _editor.ImportWindow.OpenWindow(EditorGUIUtility.GetObjectPickerObject() as Texture2D);
            }
        }

    }
}
#endif