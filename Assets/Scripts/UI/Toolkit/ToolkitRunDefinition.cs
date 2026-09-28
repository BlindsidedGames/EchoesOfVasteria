using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Run HUD")]
    public sealed class ToolkitRunDefinition : ScriptableObject
    {
        public Sprite panel, portrait, portraitFrame, portraitInset, detailFrame, tooltipFrame, healthTrack, healthFill;
        public Sprite dadJokeBubble;
        public Font dadJokeFont;
        public ToolkitBuffsDefinition buffs;
    }
}
