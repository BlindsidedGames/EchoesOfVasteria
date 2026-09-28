using UnityEngine;
using TimelessEchoes.Skills;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Skills")]
    public sealed class ToolkitSkillsDefinition : ScriptableObject
    {
        public Skill[] skills;
        public Sprite[] icons;
        public Sprite frame, inset, rightFrame, slot, selection, highlight, xpTrack, xpFill, toggleOn, toggleOff, taskFrame, taskInset;
        public Color lockedColor = new(1, 1, 1, .35f);
        public ToolkitBookDefinition.Text totalsTitle;
    }
}
