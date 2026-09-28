using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Quit")]
    public sealed class ToolkitQuitDefinition : ScriptableObject
    {
        public Sprite quit, confirm, cancel;
        public ToolkitBookDefinition.Text confirmText, cancelText;
        public float iconSize = 16, buttonWidth = 47, spacing = 1, fontSize = 8;
    }
}
