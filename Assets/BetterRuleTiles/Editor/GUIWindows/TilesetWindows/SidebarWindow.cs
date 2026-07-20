#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.TilesetWindows
{
    public class SidebarWindow : GUIWindow
    {
        public SidebarWindow(TileSetEditor editor, int sidebarWidth, float height) : base(GUIWindow.WindowLayout.SpanLeft, new RectOffset(0, 0, 0, 29), new Vector2(sidebarWidth, height))
        {
            this._editor = editor;
            this.windowFunction = UpdateUI;
            this.visible = true;
        }

        private TileSetEditor _editor;
        private Vector2 _sidebarScrollPos;

        
        void UpdateUI(int windowID)
        {
            //draw background
            EditorGUI.DrawRect(new Rect(0, 0, width, height),
                Tex.Get(Tex.C_ID.c_toolbarBackgroundColor));

            // Label
            GUI.Label(new Rect(5, 5, width - 10, 16), "Spritesheets", EditorStyles.boldLabel);

            //start the scroll UI
            GUILayout.Space(5);
            _sidebarScrollPos = GUILayout.BeginScrollView(_sidebarScrollPos);
            GUILayout.Space(3);

            //if there are no sprites add a prompt
            if (_editor._file._spriteSheets.Count <= 0)
            {
                Rect rect = EditorGUILayout.GetControlRect();
                GUI.Label(new Rect(rect.x + 20, rect.y + 15, rect.width - 40, 30),
                    "Drag & drop sprite sheets here to add them.", EditorStyles.boldLabel);
            }

            //draw button for each sprite sheet
            for (int i = 0; i < _editor._file._spriteSheets.Count; i++)
            {
                //space
                if (i > 0) EditorGUILayout.Space(3);

                var sheet = _editor._file._spriteSheets[i];
                float height = 80;
                float margin = 8;
                Rect defaultRect = EditorGUILayout.GetControlRect();
                Rect newRect = new Rect(defaultRect.x, defaultRect.y, defaultRect.width, height);

                //divider line
                if (i > 0)
                {
                    EditorGUI.DrawRect(new Rect(defaultRect.x, defaultRect.y - 3, defaultRect.width, 1),
                        Tex.Get(Tex.C_ID.c_backgroundColor));
                }

                //draw the button, set selected sheet if pressed
                if (GUI.Button(newRect, "", Style.SpriteSheetButtonStyle)) _editor.SelectSpriteSheet(i);

                //hide button with the background color
                EditorGUI.DrawRect(newRect, Tex.Get(Tex.C_ID.c_toolbarBackgroundColor));
                //change color if it's the selected sheet
                if (_editor._selectedSpriteSheet == i)
                    GUI.DrawTexture(new Rect(newRect.x + 1, newRect.y + 1, newRect.width - 2, newRect.height - 2),
                        Tex.Get(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, false, 0, Color.white, 0, 3);

                //draw the image
                Rect imgRect = new Rect(newRect.x + margin, newRect.y + margin, newRect.height - 2 * margin,
                    newRect.height - 2 * margin);
                GUI.DrawTexture(imgRect, sheet.Texture, ScaleMode.ScaleToFit, true);

                Rect textRect = new Rect(newRect.x + newRect.height, newRect.y + margin,
                    newRect.width - newRect.height - margin, newRect.height - 2 * margin);
                GUI.Label(textRect, $"{sheet.Texture.name}", Style.SpriteSheetButtonTextStyle);

                EditorGUILayout.Space(height - 20);
            }

            GUILayout.Space(3);
            GUILayout.EndScrollView();

            //draw line on top
            GUI.DrawTexture(new Rect(3, 27, width - 6, 1), Tex.Get(Tex.T_ID.t_backgroundTexture));
        }
    }
}
#endif