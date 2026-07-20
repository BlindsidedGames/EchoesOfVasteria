#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.GUIWindows;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;

namespace VinTools.BetterRuleTiles.Editor.EditorWindows
{
    public class BetterRuleTileEditorWindowBase : EditorWindow
    {
        public BetterRuleTileContainer _file;

        public bool _needsRepaint = false;

        /*protected virtual void CreateGUI()
        {
            //set up styles
            SetUpStyles();
        }*/

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
        public virtual GUIWindow[] Windows { get; set; } = new GUIWindow[1];
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
            return Windows[id];
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

            for (int i = 0; i < Windows.Length; i++)
            {
                if (Windows[i] != null && Windows[i].rectangle.Contains(Event.current.mousePosition) && Windows[i].visible) return i;
            }
            return -1;
        }
        /// <summary>
        /// When calling Event.current.mousePosition the coordinates returned are relative to the window, so with this method you can check if the cursor is inside the current window
        /// </summary>
        /// <param name="windowSize"></param>
        /// <returns></returns>
        public bool IsMouseInMethodWindow(Vector2 windowSize)
        {
            Rect pos = new Rect(Vector2.zero, windowSize);
            return pos.Contains(Event.current.mousePosition);
        }

        protected virtual void SetUpWindows()
        {
            _initializedWindows = true;
        }

        protected virtual void DisplayWindows() => DisplayWindows(Array.Empty<GUIWindow>());
        protected void DisplayWindows(params GUIWindow[] extraWindows)
        {
            // check if the windows are set up or if they aren't missing
            if (!_initializedWindows || Windows.Any(t => t == null)) SetUpWindows();

            // draw the windows
            BeginWindows();
            for (int i = 0; i < Windows.Length; i++) if (Windows[i] != null) Windows[i].Show(i, position);
            for (int i = 0; i < extraWindows.Length; i++) if (extraWindows[i] != null) extraWindows[i].Show(Windows.Length + i, position);
            EndWindows();
        }
        #endregion
    }
}
#endif