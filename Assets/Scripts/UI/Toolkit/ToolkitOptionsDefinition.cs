using System;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Options")]
    public sealed class ToolkitOptionsDefinition : ScriptableObject
    {
        public Sprite frame, inset, button, toggleOn, toggleOff, sliderTrack, sliderFill, sliderHandle;
        public Sprite dialogFrame, close, widthPreview;
        public ToolkitBookDefinition.Text[] texts = Array.Empty<ToolkitBookDefinition.Text>();
        public ToolkitBookDefinition.Text Text(string id)
        {
            foreach (var text in texts) if (text.key == id) return text;
            return new ToolkitBookDefinition.Text { key = id, fallback = id };
        }
    }
}
