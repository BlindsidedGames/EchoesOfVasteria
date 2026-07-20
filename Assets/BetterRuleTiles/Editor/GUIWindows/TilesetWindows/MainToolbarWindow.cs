#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.CustomGUI;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using VinTools.BetterRuleTiles.Editor.Generation;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows
{
    public class MainToolbarWindow : GUIWindow
    {
        public MainToolbarWindow(TileSetEditor editor, int sidebarWidth) : base(GUIWindow.WindowLayout.SpanTop, new RectOffset(sidebarWidth + 3, 0, 0, 0), new Vector2(0, 26))
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
            // create a toolbar
            var toolbar = new GUIToolbar(rectangle, Style.ToolbarButtonStyle);

            // set up contents
            GUIContent[] tabOptions = new GUIContent[] 
            {
                new GUIContent(Tex.Get(Tex.T_ID.t_tileSetTab_Draw), "Draw"), //index 0
                new GUIContent(Tex.Get(Tex.T_ID.t_tileSetTab_GameObject), "GameObject"), //1
                new GUIContent(Tex.Get(Tex.T_ID.t_tileSetTab_Collision), "Collision"), //2
                new GUIContent(Tex.Get(Tex.T_ID.t_tileSetTab_Output), "Output"), //3
                //new GUIContent("Edit"),
                //new GUIContent("Extras"),
            };
            int tabsWidth = tabOptions.Length * 49 - 1;

            // draw tabs selection
            toolbar.DrawDividerLeft(Tex.Get(Tex.C_ID.c_toolbarDividerColor));
            _editor.selectedPage = toolbar.DrawToolbarLeft(_editor.selectedPage, tabOptions, tabsWidth);
            toolbar.DrawDividerLeft(Tex.Get(Tex.C_ID.c_toolbarDividerColor));
            
            // right side            
            toolbar.DrawDividerRight(Tex.Get(Tex.C_ID.c_toolbarDividerColor));
            if (toolbar.DrawButtonRight(new GUIContent(Tex.Get(Tex.T_ID.t_swapWindowsIcon), "Switch to the Better Rule Tiles editor"))) BetterRuleTileEditor.ShowWindow(_editor._file, _editor);
            toolbar.DrawSpaceRight();
            if (toolbar.DrawButtonRight(new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_ExportButton), "Export the tiles"))) BetterRuleTileGenerator.GenerateTiles(_editor._file);
            toolbar.DrawDividerRight(Tex.Get(Tex.C_ID.c_toolbarDividerColor));
        }
    }
}
#endif