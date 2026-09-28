using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Alter Echoes")]
    public sealed class ToolkitAlterEchoesDefinition : ScriptableObject
    {
        public Sprite inset, row, slot, button, track, fill;
        public Color fillColor, countColor, disabledColor;
        public bool preserveIconAspect;
    }
}
