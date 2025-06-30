#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using VinTools.Utilities;

namespace VinToolsEditor.Utilities
{
    public class GUIBuilder
    {
        private bool _initialized;

        private Texture2D t_windowBorderTexture;
        private Texture2D t_fieldBoxTexture;
        private Texture2D t_backgroundTexture;

        public GUIBuilder()
        {

        }

        void Initialize()
        {
            if (_initialized &&
                t_backgroundTexture != null &&
                t_fieldBoxTexture != null &&
                t_windowBorderTexture != null) return;

            bool darkTheme = EditorGUIUtility.isProSkin;

            if (darkTheme)
            {
                t_backgroundTexture = TextureUtils.CreateColoredTexture(40f / 255f);
                t_fieldBoxTexture = TextureUtils.CreateColoredTexture(50f / 255f);
                t_windowBorderTexture = TextureUtils.CreateColoredTexture(60f / 255f);
            }
            else
            {
                t_backgroundTexture = TextureUtils.CreateColoredTexture(160f / 255f);
                t_fieldBoxTexture = TextureUtils.CreateColoredTexture(180f / 255f);
                t_windowBorderTexture = TextureUtils.CreateColoredTexture(200f / 255f);
            }

            _initialized = true;
        }

        public Rect DrawInBackground(GUIContent label, int numberOfLines, Action<Rect> executeIn, float lineHeight = 19, float lineStart = 5, float adjustHeight = 0)
        {
            Initialize();

            Rect controlRect = EditorGUILayout.GetControlRect();

            Rect rect = new Rect(
                controlRect.x,
                controlRect.y,
                controlRect.width,
                controlRect.height + 17 + numberOfLines * lineHeight + adjustHeight
                );

            Color color = new Color(1, 1, 1, 0.9f);

            //draw background
            GUI.DrawTexture(rect, t_windowBorderTexture, ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(rect, t_backgroundTexture, ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), t_fieldBoxTexture, ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), t_backgroundTexture, ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), label, "boldlabel");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(lineStart);
            executeIn?.Invoke(rect);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            return rect;
        }

        public Rect DrawBackground(float height) => DrawBackground(EditorGUILayout.GetControlRect(), height);
        public Rect DrawBackground(Rect controlRect, float height)
        {
            Initialize();
            Rect rect = new Rect(
                controlRect.x,
                controlRect.y,
                controlRect.width,
                height
                );

            Color color = new Color(1, 1, 1, 0.9f);

            //draw background
            GUI.DrawTexture(rect, t_windowBorderTexture, ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(rect, t_backgroundTexture, ScaleMode.StretchToFill, true, 0, color, 1, 5);

            return rect;
        }
        public Rect DrawTitleBar(GUIContent label)
        {
            Initialize();

            Rect controlRect = EditorGUILayout.GetControlRect();

            Rect rect = new Rect(
                controlRect.x,
                controlRect.y,
                controlRect.width,
                20
                );

            Color color = new Color(1, 1, 1, 0.9f);

            //draw background
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), t_fieldBoxTexture, ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), t_backgroundTexture, ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), label, "boldlabel");

            return rect;
        }
        public Rect DrawBackground(GUIContent label, float height) => DrawBackground(EditorGUILayout.GetControlRect(), label, height);
        public Rect DrawBackground(Rect controlRect, GUIContent label, float height)
        {
            Initialize();

            Rect rect = new Rect(
                controlRect.x,
                controlRect.y,
                controlRect.width,
                height + 20
                );

            Color color = new Color(1, 1, 1, 0.9f);

            //draw background
            GUI.DrawTexture(rect, t_windowBorderTexture, ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(rect, t_backgroundTexture, ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), t_fieldBoxTexture, ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), t_backgroundTexture, ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), label, "boldlabel");

            return rect;
        }
    }
}
#endif