using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Meeting")]
    public sealed class ToolkitMeetingDefinition : ScriptableObject
    {
        public Sprite portraitFrame, shadow;
        public ToolkitBookDefinition.Text meet = Text("meeting.meet", "Meet");
        public ToolkitBookDefinition.Text next = Text("meeting.next", "Next");
        public ToolkitBookDefinition.Text close = Text("meeting.close", "Close");

        private static ToolkitBookDefinition.Text Text(string key, string fallback) =>
            new() { key = key, fallback = fallback, bold = true, smallCaps = true };
    }
}
