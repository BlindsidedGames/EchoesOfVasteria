using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Introduction")]
    public sealed class ToolkitIntroDefinition : ScriptableObject
    {
        public ToolkitBookDefinition.Text title = new(), body = new(), close = new();
        public Sprite frame, progressTrack, progressFill;
        public float width = 166.7333f;
        [Min(0)] public float countdownSeconds = 10;
    }
}
