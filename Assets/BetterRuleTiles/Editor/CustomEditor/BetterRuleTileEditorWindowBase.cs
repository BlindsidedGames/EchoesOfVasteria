#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VinTools.BetterRuleTiles;
using VinTools.BetterRuleTiles.Internal;
using VinToolsEditor.Utilities;
using VinTools.Utilities;

namespace VinToolsEditor.BetterRuleTiles
{
    public class BetterRuleTileEditorWindowBase : EditorWindow
    {
        #region Textures
        protected virtual void SetUpTextures()
        {
            SetUpTextures_Solid();
        }

        #region Solid color textures
        protected static Color c_windowBorderColor = new Color(60f / 255f, 60f / 255f, 60f / 255f);
        protected static Color c_fieldBoxColor = new Color(50f / 255f, 50f / 255f, 50f / 255f);
        protected static Color c_backgroundColor = new Color(40f / 255f, 40f / 255f, 40f / 255f);
        protected static Color c_toolbarBackgroundColor = new Color(60f / 255f, 60f / 255f, 60f / 255f);
        protected static Color c_highlightColor = new Color(168f / 255f, 168f / 255f, 168f / 255f);

        protected static Texture2D t_windowBorderTexture;
        protected static Texture2D t_blankTexture;
        protected static Texture2D t_fieldBoxTexture;

        protected static Texture2D t_backgroundTexture;
        protected static Texture2D t_toolbarBackgroundTexture;
        protected static Texture2D t_highlightTexture;

        protected static Texture2D t_emptyTexture;
        protected static Texture2D t_defaultButtonTexture;
        protected static Texture2D t_hoverButtonTexture;
        protected static Texture2D t_activeButtonTexture;

        protected void SetUpTextures_Solid()
        {
            bool darkTheme = EditorGUIUtility.isProSkin;

            if (darkTheme)
            {
                t_backgroundTexture = TextureUtils.CreateColoredTexture(c_backgroundColor);
                t_fieldBoxTexture = TextureUtils.CreateColoredTexture(c_fieldBoxColor);
                t_windowBorderTexture = TextureUtils.CreateColoredTexture(c_windowBorderColor);
            }
            else
            {
                c_backgroundColor = new Color(160f / 255f, 160f / 255f, 160f / 255f);
                c_fieldBoxColor = new Color(180f / 255f, 180f / 255f, 180f / 255f);
                c_windowBorderColor = new Color(200f / 255f, 200f / 255f, 200f / 255f);

                t_backgroundTexture = TextureUtils.CreateColoredTexture(c_backgroundColor);
                t_fieldBoxTexture = TextureUtils.CreateColoredTexture(c_fieldBoxColor);
                t_windowBorderTexture = TextureUtils.CreateColoredTexture(c_windowBorderColor);
            }

            t_toolbarBackgroundTexture = TextureUtils.CreateColoredTexture(c_toolbarBackgroundColor);
            t_highlightTexture = TextureUtils.CreateColoredTexture(c_highlightColor);

            t_emptyTexture = TextureUtils.CreateColoredTexture(Color.clear);
            t_defaultButtonTexture = TextureUtils.CreateColoredTexture(88f / 255f);
            t_hoverButtonTexture = TextureUtils.CreateColoredTexture(103f / 255f);
            t_activeButtonTexture = TextureUtils.CreateColoredTexture(new Color(70f / 255f, 96f / 255f, 124f / 255f));

            t_blankTexture = TextureUtils.CreateColoredTexture(Color.white);
        }
        #endregion
        #endregion

        #region Styles
        protected GUIStyle _toolbarButtonStyle;

        protected virtual void SetUpStyles()
        {
            //set up toolbar button style
            _toolbarButtonStyle = new GUIStyle();
            _toolbarButtonStyle.normal.background = t_defaultButtonTexture;
            _toolbarButtonStyle.hover.background = t_hoverButtonTexture;
            _toolbarButtonStyle.active.background = t_activeButtonTexture;
            _toolbarButtonStyle.onNormal.background = t_activeButtonTexture;
            _toolbarButtonStyle.margin = new RectOffset(1, 1, 0, 0);
            _toolbarButtonStyle.alignment = TextAnchor.MiddleCenter;
            _toolbarButtonStyle.imagePosition = ImagePosition.ImageOnly;
            _toolbarButtonStyle.padding = new RectOffset(2, 2, 2, 2);
        }
        #endregion

        protected bool _needsRepaint = false;

        protected virtual void CreateGUI()
        {
            //set up textures
            SetUpTextures();

            //set up styles
            SetUpStyles();
        }

        protected virtual void OnGUI()
        {
            //repaint when needed
            if (_needsRepaint)
            {
                Repaint();
                _needsRepaint = false;
            }
        }

        #region Windows
        public virtual GUIWindow[] _windows { get; set; } = new GUIWindow[1];
        public bool _initializedWindows { get; protected set; } = false;
        private bool _clickedOnWindow = false;


        /// <summary>
        /// returns true if the mouse cursor is above the GUIWindow
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        protected bool IsMouseOverWindow(GUIWindow window) => window != null && window.visible && window.rectangle.Contains(Event.current.mousePosition);
        /// <summary>
        /// Returns true if the cursor is over any window
        /// </summary>
        /// <returns></returns>
        public bool IsMouseOverWindow() => GetWindowIdUnderMouse() != -1;
        /// <summary>
        /// Returns the window which is under the mouse cursor
        /// </summary>
        /// <returns></returns>
        protected GUIWindow GetWindowUnderMouse()
        {
            int id = GetWindowIdUnderMouse();

            if (id < 0) return null;
            return _windows[id];
        }
        /// <summary>
        /// returns true if a mouse button is held down over a window
        /// </summary>
        /// <returns></returns>
        protected bool DidMouseClickWindow()
        {
            if (Event.current.type == EventType.MouseDown) _clickedOnWindow = IsMouseOverWindow();
            if (Event.current.type == EventType.MouseDrag) return _clickedOnWindow;

            return IsMouseOverWindow();
        }
        /// <summary>
        /// Returns the window id of the window where the mouse cursor is, returns -1 if the mouse is not over a window
        /// </summary>
        /// <returns></returns>
        protected int GetWindowIdUnderMouse()
        {
            if (!_initializedWindows) return -1;

            for (int i = 0; i < _windows.Length; i++)
            {
                if (_windows[i] != null && _windows[i].rectangle.Contains(Event.current.mousePosition) && _windows[i].visible) return i;
            }
            return -1;
        }
        /// <summary>
        /// When calling Event.current.mousePosition the coordinates returned are relative to the window, so with this method you can check if the cursor is inside the current window
        /// </summary>
        /// <param name="windowSize"></param>
        /// <returns></returns>
        protected bool IsMouseInMethodWindow(Vector2 windowSize)
        {
            Rect pos = new Rect(Vector2.zero, windowSize);
            return pos.Contains(Event.current.mousePosition);
        }


        protected virtual void SetUpWindows()
        {
            _initializedWindows = true;
        }

        protected virtual void DisplayWindows()
        {
            //check if the windows are set up or if they aren't missing
            if (!_initializedWindows || _windows.Any(t => t == null)) SetUpWindows();

            //draw the windows
            BeginWindows();
            for (int i = 0; i < _windows.Length; i++) if (_windows[i] != null) _windows[i].Show(i, position);
            EndWindows();
        }
        #endregion
    }
}
#endif