#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace VinToolsEditor.Utilities
{
    public class EditorGUIField<T> where T : UnityEngine.Object
    {

        /// <summary>
        /// Displays an array field similar to default unity property fields
        /// </summary>
        /// <param name="label"></param>
        /// <param name="foldOut"></param>
        /// <param name="array"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T[] ArrayField(string label, ref bool foldOut, T[] array, Type type) => ArrayField(new GUIContent(label), ref foldOut, array, type);
        public static T[] ArrayField(GUIContent label, ref bool foldOut, T[] array, Type type)
        {
            //display a foldout
            foldOut = EditorGUILayout.Foldout(foldOut, label);

            //if the foldout is closed just ignore the method
            if (!foldOut) return array;

            //display and change size
            int newSize = EditorGUILayout.DelayedIntField(new GUIContent("    Size", "Size of the array. Press enter to apply changes."), array.Length);
            if (newSize != array.Length) Array.Resize(ref array, newSize);

            //set elements
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (T)EditorGUILayout.ObjectField(new GUIContent($"    Element {i}"), array[i], type, false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            return array;
        }
        public static T[] ArrayField(Rect rect, string label, ref bool foldOut, T[] array, Type type, out float height) => ArrayField(rect, new GUIContent(label), ref foldOut, array, type, out height);
        public static T[] ArrayField(Rect rect, GUIContent label, ref bool foldOut, T[] array, Type type, out float height)
        {
            float currentOffset = 0;


            //display a foldout
            foldOut = EditorGUI.Foldout(GetRect(), foldOut, label);

            //if the foldout is closed just ignore the method
            if (!foldOut)
            {
                height = currentOffset;
                return array;
            }

            //display and change size
            int newSize = EditorGUI.DelayedIntField(GetRect(), new GUIContent("    Size", "Size of the array. Press enter to apply changes."), array.Length);
            if (newSize != array.Length) Array.Resize(ref array, newSize);

            //set elements
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (T)EditorGUI.ObjectField(GetRect(), new GUIContent($"    Element {i}"), array[i], type, false);
            }

            height = currentOffset;
            return array;



            Rect GetRect(float h = 20)
            {
                var r = new Rect(rect.x, rect.y + currentOffset, rect.width, h - 2);
                currentOffset += h;
                return r;
            }
        }
    }
}
#endif