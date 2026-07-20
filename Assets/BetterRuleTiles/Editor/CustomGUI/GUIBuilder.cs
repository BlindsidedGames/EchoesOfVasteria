#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;

namespace VinTools.BetterRuleTiles.Editor.CustomGUI
{
    public class GUIBuilder
    {
        public Rect DrawInBackground(GUIContent label, int numberOfLines, Action<Rect> executeIn, float lineHeight = 19, float lineStart = 5, float adjustHeight = 0)
        {
            Rect controlRect = EditorGUILayout.GetControlRect();

            Rect rect = new Rect(
                controlRect.x,
                controlRect.y,
                controlRect.width,
                controlRect.height + 17 + numberOfLines * lineHeight + adjustHeight
                );

            Color color = new Color(1, 1, 1, 0.9f);

            //draw background
            GUI.DrawTexture(rect, Tex.GetTexture(Tex.T_ID.t_windowBorderTexture), ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(rect, Tex.GetTexture(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), Tex.GetTexture(Tex.T_ID.t_fieldBoxTexture), ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), Tex.GetTexture(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), label, EditorStyles.boldLabel);

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
            Rect rect = new Rect(
                controlRect.x,
                controlRect.y,
                controlRect.width,
                height
                );

            Color color = new Color(1, 1, 1, 0.9f);

            //draw background
            GUI.DrawTexture(rect, Tex.GetTexture(Tex.T_ID.t_windowBorderTexture), ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(rect, Tex.GetTexture(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, true, 0, color, 1, 5);

            return rect;
        }
        public Rect DrawTitleBar(GUIContent label)
        {
            Rect controlRect = EditorGUILayout.GetControlRect();

            Rect rect = new Rect(
                controlRect.x,
                controlRect.y,
                controlRect.width,
                20
                );

            Color color = new Color(1, 1, 1, 0.9f);

            //draw background
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), Tex.GetTexture(Tex.T_ID.t_fieldBoxTexture), ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), Tex.GetTexture(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), label, EditorStyles.boldLabel);

            return rect;
        }
        public Rect DrawBackground(GUIContent label, float height) => DrawBackground(EditorGUILayout.GetControlRect(), label, height);
        public Rect DrawBackground(Rect controlRect, GUIContent label, float height)
        {
            Rect rect = new Rect(
                controlRect.x,
                controlRect.y,
                controlRect.width,
                height + 20
                );

            Color color = new Color(1, 1, 1, 0.9f);

            //draw background
            GUI.DrawTexture(rect, Tex.GetTexture(Tex.T_ID.t_windowBorderTexture), ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(rect, Tex.GetTexture(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), Tex.GetTexture(Tex.T_ID.t_fieldBoxTexture), ScaleMode.StretchToFill, true, 0, color, 0, 5);
            GUI.DrawTexture(new Rect(rect.position, new Vector2(rect.width, 20)), Tex.GetTexture(Tex.T_ID.t_backgroundTexture), ScaleMode.StretchToFill, true, 0, color, 1, 5);
            GUI.Label(new Rect(rect.position + new Vector2(5, 0), new Vector2(rect.width, 20)), label, EditorStyles.boldLabel);

            return rect;
        }
    }
}
#endif