using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Buffs")]
    public sealed class ToolkitBuffsDefinition : ScriptableObject
    {
        public Sprite window, row, inset, slot, button, autoCast;
        public Sprite pickerFrame, pickerRow, pickerIconFrame, pickerButton;
        public Color autoCastTint, pickerDimmer, selectedRowTint;
        public ToolkitBookDefinition.Text instructions;
    }
}
