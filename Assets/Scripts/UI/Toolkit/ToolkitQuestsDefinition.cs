using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Quests")]
    public sealed class ToolkitQuestsDefinition : ScriptableObject
    {
        public Sprite inset, category, row, button, progressTrack, progressFill;
        public ToolkitBookDefinition.Text instructions;
    }
}
