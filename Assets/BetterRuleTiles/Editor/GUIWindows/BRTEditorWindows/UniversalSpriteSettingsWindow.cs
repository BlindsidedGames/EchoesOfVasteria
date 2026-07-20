#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Cache = VinTools.BetterRuleTiles.Editor.Data.SpriteCache;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class UniversalSpriteSettingsWindow : GUIWindow
    {
        public UniversalSpriteSettingsWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base( 
            GUIWindow.WindowLayout.SpanLeft, new RectOffset(4, 4, 30, 125), new Vector2(300, 400), new GUIContent(""))
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }

        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;
        
        Vector2 _universalSpriteSettingsScrollPosition;
        float _universalSpriteSettingAddedHeight = 0;
        float _universalSpriteSettingDialogHeight = 0;
        bool _showUniversalSpritesArray = false;

        private Rect _dragDropRect1;
        private Rect _dragDropRect2;
        
        private void UpdateUI(int windowID)
        {
            //title bar
            GUI.Label(new Rect(0, 0, width - 12, 25), "Sprite Override Settings", Style.CenteredBoldLabel);
            EditorGUILayout.Space();

            //separator
            EditorGUI.DrawRect(new Rect(0, 25, height, 1), Tex.Get(Tex.C_ID.c_backgroundColor));

            //const values
            const float margin = 5;
            const float spacing = 5;
            const int columns = 4;

            if (_editor.CurrentlySepectedOverrideSpriteToModify < 0)
            {
                _universalSpriteSettingAddedHeight = 0;
                _editor._spriteSettingsNoDropZone = new Rect();
            }

            //if there are no overrides
            if (_file._overrideSprites.Count <= 0)
            {
                EditorGUILayout.HelpBox("Drag in sprites to this window to modify their output settings. Every rule using this sprite will also use these sprite output and collider settings.", MessageType.Info);
                return;
            }

            //calculate scroll position
            Rect windowPos = new Rect(position, size);
            Rect scrollRect = new Rect(0, 26, windowPos.width - 2, windowPos.height - 28);

            float scrollViewWidth = windowPos.width - 15;

            float currentY = margin;
            float columnWidth = (scrollViewWidth - (2 * margin) - ((columns - 1) * spacing)) / columns;

            float scrollViewHeight =
                (Mathf.Ceil(_file._overrideSprites.Count / (float)columns) * (columnWidth + spacing) - spacing + (2 * margin)) +
                (_editor.CurrentlySepectedOverrideSpriteToModify >= 0 ? 
                _universalSpriteSettingAddedHeight + spacing : spacing);

            Rect scrollView = new Rect(0, 0, scrollViewWidth, scrollViewHeight);
            _universalSpriteSettingsScrollPosition = GUI.BeginScrollView(scrollRect, _universalSpriteSettingsScrollPosition, scrollView, false, true);

            //scroll to selected sprite
            if (_editor.CurrentlySepectedOverrideSpriteToModify >= 0 && _editor.AutoScrollToSelectedOverrideSprite)
            {
                _universalSpriteSettingsScrollPosition = new Vector2(0, (_editor.CurrentlySepectedOverrideSpriteToModify / columns) * (columnWidth + spacing));
                _editor.AutoScrollToSelectedOverrideSprite = false;
            }

            //draw sprites
            for (int i = 0; i < _file._overrideSprites.Count; i++)
            {
                //draw square
                Rect buttonPos = new Rect(margin + ((i % columns) * (columnWidth + spacing)), currentY, columnWidth, columnWidth);
                Rect imgPos = new Rect(buttonPos.x + 8, buttonPos.y + 8, buttonPos.width - 16, buttonPos.height - 16);

                var pressedButton = GUI.Button(buttonPos, new GUIContent());
                if (_editor.CurrentlySepectedOverrideSpriteToModify == i) GUI.DrawTexture(buttonPos, Tex.Get(Tex.T_ID.t_windowBorderTexture), ScaleMode.StretchToFill, true, 0, Color.white, 0, 3);
                GUI.DrawTexture(imgPos, Cache.TryGet(_file._overrideSprites[i].BaseSprite));

                if (pressedButton)
                {
                    //select
                    if (_editor.CurrentlySepectedOverrideSpriteToModify != i)
                    {
                        _editor.CurrentlySepectedOverrideSpriteToModify = i;
                        SelectSprite(_file._overrideSprites[i].BaseSprite, true);
                    }
                    //deselect
                    else _editor.CurrentlySepectedOverrideSpriteToModify = -1;
                }

                //draw UI
                if (_editor.CurrentlySepectedOverrideSpriteToModify == i)
                {
                    // draw UI
                    var optionsRect = new Rect(margin, currentY + columnWidth + spacing, scrollViewWidth - 2 * margin, 0);
                    _universalSpriteSettingAddedHeight = SpriteOptionWindowFunctions.DrawSpriteOptionsWithBackground(_editor.guiBuilder, ref _editor.CurrentlySepectedOverrideSpriteToModify, optionsRect, _file, ref _showUniversalSpritesArray, ref _universalSpriteSettingDialogHeight, ref _editor.ConfigWindowWidth, out var dragDropArea);

                    // calculate sprites drag & drop zone
                    if (dragDropArea.x > 0 && dragDropArea.y > 0)
                    {
                        _dragDropRect1 = new Rect(
                            dragDropArea.x + position.x,
                            dragDropArea.y + 25 - _universalSpriteSettingsScrollPosition.y + position.y,
                            dragDropArea.width,
                            40
                        );
                        
                        var usd = _file._overrideSprites[_editor.CurrentlySepectedOverrideSpriteToModify];
                        _dragDropRect2 = new Rect(
                            dragDropArea.x + position.x,
                            dragDropArea.y + 65 - _universalSpriteSettingsScrollPosition.y + position.y,
                            dragDropArea.width * 0.58f,
                            _showUniversalSpritesArray && usd != null ? (usd.Sprites.Length + 1) * 20 : 0
                        );
                    }
                    
                    // calculate a no-drop zone for drag and dropping
                    _editor._spriteSettingsNoDropZone = new Rect(
                        position + scrollRect.position + optionsRect.position - _universalSpriteSettingsScrollPosition,
                        new Vector2(optionsRect.width, _universalSpriteSettingAddedHeight)
                        );
                }

                //after the last column
                if (i % columns == columns - 1) 
                {
                    //skip to next row
                    currentY += columnWidth + spacing;
                    //if the selected one is in this row, also skip that amount
                    if (_editor.CurrentlySepectedOverrideSpriteToModify >= 0 && _editor.CurrentlySepectedOverrideSpriteToModify / columns == i / columns) currentY += _universalSpriteSettingAddedHeight + 5;
                }
            }

            GUI.EndScrollView();

            lockPosition = _file.settings._lockWindows;
            padding.top = (int)_editor._toolbarHeight + 4;
        }
        
        void SelectSprite(Sprite sprite, bool selectBrush = true)
        {
            _editor._selectedDrawerTile = 0;
            if (selectBrush) _editor._selectedTool = 0;
            _editor._selectedSprite = sprite;
        }
        
        public bool CheckDragDropSprites()
        {
            // Debug.Log($"Mouse {Event.current.mousePosition} - Drag Drop Rect: {dragDropRect}");
            // EditorGUI.DrawRect(_dragDropRect1, Color.red);
            // EditorGUI.DrawRect(_dragDropRect2, Color.red);
            // EditorGUI.DrawRect(new Rect(Event.current.mousePosition - Vector2.one * 5, Vector2.one * 10), Color.green);

            return _dragDropRect1.Contains(Event.current.mousePosition) || _dragDropRect2.Contains(Event.current.mousePosition);
        }
    }
}
#endif