using UnityEngine;
using UnityEditor;

namespace VoxelBusters.CoreLibrary.Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute), true)]
    public class ReadOnlyAttributeDrawer : PropertyDrawer 
    {
        private const float m_padding = 10f;
        
        #region Base class methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var offset          = GetMessageHeight();
            var fieldHeight      = EditorGUI.GetPropertyHeight(property, label, true);
            var fieldRect        = new Rect(position.x, position.y + m_padding, position.width, fieldHeight);
            var messageRect     = new Rect(position.x, position.y + fieldHeight + m_padding + 2f, position.width, offset);

            GUI.enabled = false;
                EditorGUI.PropertyField(fieldRect, property, label, true);
                if (HasMessage())
                {
                    EditorGUI.LabelField(messageRect, GetMessage(), GetMessageStyle());
                }
            GUI.enabled = true;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true) + GetMessageHeight() + m_padding * 2f;    
        }

        #endregion

        #region Utility methods

        private GUIStyle GetMessageStyle()
        {
            return CustomEditorStyles.MiniLabel(wordWrap: false); //TODO: Using false as calcSize is not working properly with wordwrap on.
        }

        private bool HasMessage()
        {
            return !string.IsNullOrEmpty(GetMessage());
        }

        private string GetMessage()
        {
            var message   = ((ReadOnlyAttribute)attribute).Message;
            return message;
        }

        private float GetMessageHeight()
        {
            var message = GetMessage();

            if (string.IsNullOrEmpty(message))
                return 0f;

            return GetMessageStyle().CalcSize(new GUIContent(message)).y;
        }

        #endregion
    }
}