#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinToolsEditor.Utilities
{
    public class GUIToolbar
    {
        #region Properties
        private Margin defaultMargin;
        private Rect container;
        private GUIStyle buttonStyle;

        private Margin space;

        public Margin currentMargin => defaultMargin + space;
        #endregion

        #region Constructors
        public GUIToolbar(Rect containerRect, GUIStyle _buttonStyle)
        {
            container = containerRect;
            buttonStyle = _buttonStyle;
            defaultMargin = new Margin(-1, 3);
            space = new Margin();
        }
        public GUIToolbar(Rect containerRect, GUIStyle _buttonStyle, Margin _margin)
        {
            container = containerRect;
            buttonStyle = _buttonStyle;
            defaultMargin = _margin;
            space = new Margin();
        }
        #endregion

        #region Methods

        //Leght of buttons - 32
        //Leght of dropdown buttons - 38
        //Space between buttons - 4
        //Space between buttons and dividers - 7

        public bool DrawButtonLeft(GUIContent content)
        {
            bool result = GUI.Button(new Rect(currentMargin.Left, currentMargin.Up, 32, 20), content, buttonStyle);

            space.Left += 32;

            return result;
        }
        public bool DrawToggleLeft(bool value, GUIContent content)
        {
            bool result = GUI.Toggle(new Rect(currentMargin.Left, currentMargin.Up, 32, 20), value, content, buttonStyle);

            space.Left += 32;

            return result;
        }
        public bool DrawDropdownLeft(bool value, GUIContent content)
        {
            bool result = GUI.Toggle(new Rect(currentMargin.Left, currentMargin.Up, 38, 20), value, content, buttonStyle);

            space.Left += 38;

            return result;
        }
        public int DrawToolbarLeft(int selected, GUIContent[] contents, int width)
        {
            int result = GUI.Toolbar(new Rect(currentMargin.Left, currentMargin.Up, width, 20), selected, contents, buttonStyle);

            space.Left += width - 1;

            return result;
        }
        public void DrawSpaceLeft(int pixels = 4) => space.Left += pixels;
        public void DrawDividerLeft(Texture2D tex)
        {
            GUI.DrawTexture(new Rect(currentMargin.Left + 7, currentMargin.Up + 2, 1, 16), tex);
            GUI.DrawTexture(new Rect(currentMargin.Left + 10, currentMargin.Up + 2, 1, 16), tex);

            space.Left += 18;
        }

        public bool DrawButtonRight(GUIContent content)
        {
            bool result = GUI.Button(new Rect(container.width - 32 - currentMargin.Right, currentMargin.Up, 32, 20), content, buttonStyle);

            space.Right += 32;

            return result;
        }
        public bool DrawToggleRight(bool value, GUIContent content)
        {
            bool result = GUI.Toggle(new Rect(container.width - 32 - currentMargin.Right, currentMargin.Up, 32, 20), value, content, buttonStyle);

            space.Right += 32;

            return result;
        }
        public bool DrawDropdownRight(bool value, GUIContent content)
        {
            bool result = GUI.Toggle(new Rect(container.width - 38 - currentMargin.Right, currentMargin.Up, 38, 20), value, content, buttonStyle);

            space.Right += 38;

            return result;
        }
        public int DrawToolbarRight(int selected, GUIContent[] contents, int width)
        {
            int result = GUI.Toolbar(new Rect(container.width - width - currentMargin.Right, currentMargin.Up, width, 20), selected, contents, buttonStyle);

            space.Right += width - 1;

            return result;
        }
        public void DrawSpaceRight(int pixels = 4) => space.Right += pixels;
        public void DrawDividerRight(Texture2D tex)
        {
            GUI.DrawTexture(new Rect(container.width - currentMargin.Right - 8, currentMargin.Up + 2, 1, 16), tex);
            GUI.DrawTexture(new Rect(container.width - currentMargin.Right - 11, currentMargin.Up + 2, 1, 16), tex);

            space.Right += 18;
        }

        #endregion

        #region Subclasses
        public class Margin
        {
            public int Left;
            public int Up;
            public int Right;
            public int Down;

            public Margin(int left, int up, int right, int down)
            {
                Left = left;
                Up = up;
                Right = right;
                Down = down;
            }
            public Margin(int horizontal, int vertical)
            {
                Left = horizontal;
                Up = vertical;
                Right = horizontal;
                Down = vertical;
            }
            public Margin()
            {
                Left = 0;
                Up = 0;
                Right = 0;
                Down = 0;
            }

            public static Margin operator +(Margin a, Margin b) => new Margin(a.Left + b.Left, a.Up + b.Up, a.Right + b.Right, a.Down + b.Down);
            public static Margin operator +(Margin a, int b) => new Margin(a.Left + b, a.Up + b, a.Right + b, a.Down + b);
            public static Margin operator -(Margin a, Margin b) => new Margin(a.Left - b.Left, a.Up - b.Up, a.Right - b.Right, a.Down - b.Down);
            public static Margin operator -(Margin a, int b) => new Margin(a.Left - b, a.Up - b, a.Right - b, a.Down - b);
            public static Margin operator *(Margin a, int b) => new Margin(a.Left * b, a.Up * b, a.Right * b, a.Down * b);
        }
        #endregion
    }
}
#endif