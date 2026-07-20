#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows
{
    public class SpriteOptionsWindow : GUIWindow
    {
        public SpriteOptionsWindow(TileSetEditor editor, int sidebarWidth, int tilebarWidth) : base(GUIWindow.WindowLayout.SpanLeft, new RectOffset(sidebarWidth + 3, 0, 29, 0), new Vector2(tilebarWidth, 0))
        {
            this._editor = editor;
            this.windowFunction = UpdateUI;
            this.visible = false;
        }

        private TileSetEditor _editor;
        
        Vector2 scrollPosition;
        float scrollViewHeight;
        bool showUniversalSpritesArray = true;
        
        private Rect _dragDropRect1;
        private Rect _dragDropRect2;
        
        void UpdateUI(int windowID)
        {
            //draw background
            EditorGUI.DrawRect(new Rect(0, 0, width, height), Tex.Get(Tex.C_ID.c_toolbarBackgroundColor));

            // Label
            GUI.Label(new Rect(5, 5, width - 10, 16), "Sprite Options" + (_editor.selectedSpriteIndex < 0 ? "" : $" ({ _editor._file._overrideSprites[_editor.selectedSpriteIndex].BaseSprite.name})"), EditorStyles.boldLabel);

            // Draw line
            GUI.DrawTexture(new Rect(3, 27, width - 6, 1), Tex.Get(Tex.T_ID.t_backgroundTexture));

            if (_editor.selectedSpriteIndex < 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Select a sprite on the grid to edit its properties", EditorStyles.helpBox);

                return;
            }

            float startOffset = 30;
            float endMargin = 0;
            scrollPosition = GUI.BeginScrollView(
                new Rect(0, startOffset, width, height - startOffset - endMargin), 
                scrollPosition, 
                new Rect(0, 0, width - 13, scrollViewHeight + startOffset), 
                false, 
                false);
            //scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);

            //change label width to allow more space for content
            EditorGUIUtility.labelWidth = width / 2;

            //draw options
            float windowWidth = width;
            Rect rect = EditorGUILayout.GetControlRect();
            SpriteOptionWindowFunctions.DrawSpriteOptionsUI(
                new Rect(0, 4, rect.width + (height < scrollViewHeight + 2 * startOffset + endMargin ? -5 : 5), 0), 
                _editor._file, 
                _editor.selectedSpriteIndex, 
                ref showUniversalSpritesArray, 
                ref scrollViewHeight, 
                ref windowWidth, 
                out Rect dragDropArea,
                0);

            GUI.EndScrollView();
            
            // get drag and drop area
            if (dragDropArea.x > 0 && dragDropArea.y > 0)
            {
                _dragDropRect1 = new Rect(
                    dragDropArea.x + position.x,
                    dragDropArea.y + 25 - scrollPosition.y + position.y,
                    width - 2 * dragDropArea.x,
                    40
                );
                        
                var usd = _editor._file._overrideSprites[_editor.selectedSpriteIndex];
                _dragDropRect2 = new Rect(
                    dragDropArea.x + position.x,
                    dragDropArea.y + 65 - scrollPosition.y + position.y,
                    width * .5f,
                    showUniversalSpritesArray && usd != null ? (usd.Sprites.Length + 1) * 20 : 0
                );
            }

            //reset label width
            EditorGUIUtility.labelWidth = 0;
        }
        
        public bool CheckDragDropSprites()
        {
            // Debug.Log($"Mouse {Event.current.mousePosition} - Drag Drop Rect: {dragDropRect}");
            EditorGUI.DrawRect(new Rect(_dragDropRect1.position, _dragDropRect1.size), Color.red);
            EditorGUI.DrawRect(new Rect(_dragDropRect2.position, _dragDropRect2.size), Color.red);
            EditorGUI.DrawRect(new Rect(Event.current.mousePosition - Vector2.one * 5, Vector2.one * 10), Color.green);

            return _dragDropRect1.Contains(Event.current.mousePosition) || _dragDropRect2.Contains(Event.current.mousePosition);
        }
    }
}
#endif