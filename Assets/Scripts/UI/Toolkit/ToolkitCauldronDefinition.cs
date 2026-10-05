using System;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    [CreateAssetMenu(menuName = "Timeless Echoes/UI Toolkit/Cauldron")]
    public sealed class ToolkitCauldronDefinition : ScriptableObject
    {
        public CauldronConfig config;
        public SpriteAnimation portrait, pot;
        public string rewardHelp;

        [Serializable]
        public sealed class SpriteAnimation
        {
            public Sprite[] frames = Array.Empty<Sprite>();
            public float[] times = Array.Empty<float>();
            public float duration = 1;
            public Sprite At(float elapsed)
            {
                if (frames.Length == 0) return null;
                var t = Mathf.Repeat(elapsed, Mathf.Max(.001f, duration));
                for (var i = times.Length - 1; i >= 0; i--) if (t >= times[i]) return frames[i];
                return frames[0];
            }
        }
    }
}
