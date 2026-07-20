#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.CustomGUI;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;
using Clipboard = VinTools.BetterRuleTiles.Editor.Data.TileEditorClipboard;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class EditorToolbarWindow : GUIWindow
    {

        public EditorToolbarWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base(
            GUIWindow.WindowLayout.SpanTop, new RectOffset(0, 0, 0, 0), new Vector2(editor.position.width, 26),
            new GUIContent(""), true, false, false, GUIStyle.none)
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }

        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;

        private int _toolbarNumLines = 1;

        private GUIContent[] _toolbarTextures => new GUIContent[]
        {
            new GUIContent(Tex.Get(Tex.T_ID.t_toolTex_Brush), "Paint selected tile or sprite to the grid (B)"),
            new GUIContent(Tex.Get(Tex.T_ID.t_toolTex_Pick), "Pick tiles or sprites from the grid (I)"),
            new GUIContent(Tex.Get(Tex.T_ID.t_toolTex_Erase), "Delete sprites (D)"),
            new GUIContent(Tex.Get(Tex.T_ID.t_toolTex_Move), "Move selected sprites on the grid (M)"),
            new GUIContent(Tex.Get(Tex.T_ID.t_toolTex_Select), "Select one or multiple sprites on the grid (S)"),
            new GUIContent(Tex.Get(Tex.T_ID.t_toolTex_Inspect), "Inspect a tile on the grid and modify it (E)")
        };

        private float _editorWidth => _editor.position.width;
        private float _editorHeight => _editor.position.height;

        private void UpdateUI(int windowID)
        {
            //Debug.Log(_editorWidth);

            //calculate toolbar height
            int toolbarStage = 1;
            if (_editorWidth < 980 + 36) toolbarStage = 2;
            if (_editorWidth < 668 + 36) toolbarStage = 3;
            //if (_editorWidth < 508) toolbarStage = 4;
            if (_editorWidth < 474 + 36) toolbarStage = 5;
            if (_editorWidth < 396) toolbarStage = 6;

            _toolbarNumLines = 1;
            if (toolbarStage > 1) _toolbarNumLines = 2;
            if (toolbarStage > 3) _toolbarNumLines = 3;
            if (toolbarStage > 5) _toolbarNumLines = 4;

            //set height
            height = _toolbarNumLines * 24 + 2;
            _editor._toolbarHeight = height;





            //draw background
            EditorGUI.DrawRect(_editor._window_Toolbar.rectangle, Tex.Get(Tex.C_ID.c_toolbarBackgroundColor));

            //create toolbar
            var toolbar = new GUIToolbar(_editor.position, Style.ToolbarButtonStyle);
            var toolbar2 = new GUIToolbar(new Rect(position.x, position.y + 25, _editorWidth, _editorHeight),
                Style.ToolbarButtonStyle, new GUIToolbar.Margin(-1, 3 + 24));
            var toolbar3 = new GUIToolbar(new Rect(position.x, position.y + 25, _editorWidth, _editorHeight),
                Style.ToolbarButtonStyle, new GUIToolbar.Margin(-1, 3 + 2 * 24));
            var toolbar4 = new GUIToolbar(new Rect(position.x, position.y + 25, _editorWidth, _editorHeight),
                Style.ToolbarButtonStyle, new GUIToolbar.Margin(-1, 3 + 3 * 24));

            GUIToolbar currentToolbar = toolbar;

            //left side (from left to right)
            currentToolbar.DrawDividerLeft(Tex.Get(Tex.C_ID.c_toolbarDividerColor));
            _editor._selectedTool = (BetterRuleTileEditor.ToolbarTool)currentToolbar.DrawToolbarLeft((int)_editor._selectedTool, _toolbarTextures, 198);
            
            if (toolbarStage >= 3) currentToolbar = toolbar2;

            currentToolbar.DrawDividerLeft(Tex.Get(Tex.C_ID.c_toolbarDividerColor));

            if (currentToolbar.DrawButtonLeft(new GUIContent(Tex.Get(Tex.T_ID.t_actionTex_Undo), "Undo (Ctrl + Z)")))
                Undo.PerformUndo();
            currentToolbar.DrawSpaceLeft();
            if (currentToolbar.DrawButtonLeft(new GUIContent(Tex.Get(Tex.T_ID.t_actionTex_Redo), "Redo (Ctrl + Y)")))
                Undo.PerformRedo();
            currentToolbar.DrawSpaceLeft();
            if (currentToolbar.DrawButtonLeft(new GUIContent(Tex.Get(Tex.T_ID.t_actionTex_DeleteSelection),
                    "Delete selected area (Delete)"))) _editor.DeleteSelection();
            currentToolbar.DrawSpaceLeft();
            if (currentToolbar.DrawButtonLeft(new GUIContent(Tex.Get(Tex.T_ID.t_actionTex_Copy), "Copy (Ctrl + C)")))
                Clipboard.CopySelection(_editor._file, _editor._selectionRect, out _editor._hasSelection);
            currentToolbar.DrawSpaceLeft();
            if (currentToolbar.DrawButtonLeft(
                    new GUIContent(Tex.Get(Tex.T_ID.t_actionTex_Paste), "Paste (Ctrl + V)"))) Clipboard.StartPaste(_editor);

            if (toolbarStage >= 6)
            {
                toolbar3.DrawDividerLeft(Tex.Get(Tex.C_ID.c_toolbarDividerColor));
                currentToolbar = toolbar4;
            }
            else if (toolbarStage >= 4) currentToolbar = toolbar3;
            else if (toolbarStage >= 2) currentToolbar = toolbar2;

            currentToolbar.DrawDividerLeft(Tex.Get(Tex.C_ID.c_toolbarDividerColor));

            if (currentToolbar.DrawButtonLeft(new GUIContent(Tex.Get(Tex.T_ID.t_actionTex_Lock),
                    "Lock selection to prevent it from being edited (L)"))) _editor.LockSelection();
            currentToolbar.DrawSpaceLeft();
            if (currentToolbar.DrawButtonLeft(new GUIContent(Tex.Get(Tex.T_ID.t_actionTex_Unlock),
                    "Unlock selection and allow editing it (U)"))) _editor.UnlockSelection();
            currentToolbar.DrawSpaceLeft();
            if (currentToolbar.DrawButtonLeft(new GUIContent(Tex.Get(Tex.T_ID.t_actionTex_PresetBlock),
                    "Create a preset block of the selected area (P)"))) _editor.CreatePresetBlock();

            currentToolbar.DrawDividerLeft(Tex.Get(Tex.C_ID.c_toolbarDividerColor));

            _editor._window_RecolorWindow.visible = currentToolbar.DrawDropdownLeft(
                _editor._window_RecolorWindow.visible,
                new GUIContent(Tex.Get(Tex.T_ID.t_toolTex_Recolor), "Replace selection"));
            float recolorWindowX = currentToolbar.currentMargin.Left - _editor._window_RecolorWindow.width;
            _editor._window_RecolorWindow.position = new Vector2(recolorWindowX < 0 ? 0 : recolorWindowX,
                currentToolbar.currentMargin.Up + 20);





            //right side (front right to left)
            currentToolbar = toolbar;

            currentToolbar.DrawDividerRight(Tex.Get(Tex.C_ID.c_toolbarDividerColor));

            if (_file.UseTileSet)
            {
                if (currentToolbar.DrawButtonRight(new GUIContent(Tex.Get(Tex.T_ID.t_swapWindowsIcon),
                        "Swap to the tileset editor window"))) TileSetEditor.ShowWindow(_file, _editor);
                currentToolbar.DrawSpaceRight();
            }

            _editor._window_ExportWindow.visible = currentToolbar.DrawDropdownRight(
                _editor._window_ExportWindow.visible,
                new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_ExportDropdown), "Export options"));
            float exportWindowX = _editorWidth - currentToolbar.currentMargin.Right -
                _editor._window_ExportWindow.width + 38;
            _editor._window_ExportWindow.position = new Vector2(exportWindowX < 0 ? 0 : exportWindowX,
                currentToolbar.currentMargin.Up + 20);

            if (toolbarStage >= 6) currentToolbar = toolbar3;
            else if (toolbarStage >= 5) currentToolbar = toolbar2;

            currentToolbar.DrawDividerRight(Tex.Get(Tex.C_ID.c_toolbarDividerColor));

            _editor._window_SettingsWindow.visible = currentToolbar.DrawDropdownRight(
                _editor._window_SettingsWindow.visible,
                new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_Settings), "Other Settings"));
            float settingsWindowX = _editorWidth - currentToolbar.currentMargin.Right -
                _editor._window_SettingsWindow.width + 38;
            _editor._window_SettingsWindow.position = new Vector2(settingsWindowX < 0 ? 0 : settingsWindowX,
                currentToolbar.currentMargin.Up + 20);

            currentToolbar.DrawSpaceRight();
            _editor._hideSprites = currentToolbar.DrawToggleRight(_editor._hideSprites,
                new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_HideSprites), "Hide sprites to see tiles below them"));
            currentToolbar.DrawSpaceRight();
            _editor._lockWindows = currentToolbar.DrawToggleRight(_editor._lockWindows,
                new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_LockWindows),
                    "Snap windows to the corners and lock them"));
            currentToolbar.DrawSpaceRight();
            _editor._showRuler = currentToolbar.DrawToggleRight(_editor._showRuler,
                new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_Ruler),
                    "Show the grid coordinates on the edge of the screen"));
            currentToolbar.DrawSpaceRight();
            _editor._showModified = currentToolbar.DrawToggleRight(_editor._showModified,
                new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_Modified),
                    "Highlights the cells which have been modified with a flashing outline"));

            if (toolbarStage >= 6)
            {
                toolbar2.DrawDividerRight(Tex.Get(Tex.C_ID.c_toolbarDividerColor));
                currentToolbar = toolbar4;
            }
            else if (toolbarStage >= 5) currentToolbar = toolbar3;
            else if (toolbarStage >= 2) currentToolbar = toolbar2;

            currentToolbar.DrawDividerRight(Tex.Get(Tex.C_ID.c_toolbarDividerColor));

            _file.settings.SetDisplaySpriteDrawer(currentToolbar.DrawToggleRight(_file.settings._displaySpriteDrawer,
                new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_showSprites), "Show the sprite drawer")));
            if (_file.settings._useUniversalSpriteSettings)
            {
                currentToolbar.DrawSpaceRight(1);
                _file.settings.SetDisplayUniversalSpriteSettings(currentToolbar.DrawToggleRight(
                    _file.settings._displayUniversalSpriteSettings,
                    new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_universalSpriteSettings),
                        "Show the sprite override settings window")));
            }

            currentToolbar.DrawDividerRight(Tex.Get(Tex.C_ID.c_toolbarDividerColor));

            if (currentToolbar.DrawButtonRight(new GUIContent(Tex.Get(Tex.T_ID.t_optionTex_helpIcon),
                    "Open documentation (opens browser)")))
                Application.OpenURL("https://docs.vinark.dev/#/./better-rule-tiles/index");
        }
    }
}
#endif