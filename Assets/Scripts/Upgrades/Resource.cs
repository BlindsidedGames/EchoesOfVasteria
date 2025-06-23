using UnityEngine;
using Blindsided.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TimelessEchoes.Upgrades
{
    [ManageableData]
    [CreateAssetMenu(fileName = "Resource", menuName = "SO/Upgrade Resource")]
    public class Resource : ScriptableObject
    {
        public Sprite icon;
#if UNITY_EDITOR
        private void OnEnable()
        {
            SetIcon();
        }

        private void OnValidate()
        {
            SetIcon();
        }

        private void SetIcon()
        {
            if (icon != null)
            {
                EditorGUIUtility.SetIconForObject(this, icon.texture);
            }
        }
#endif
    }
}
