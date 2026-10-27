using UnityEngine;

namespace TimelessEchoes.Quests
{
    [CreateAssetMenu(menuName = "Timeless Echoes/Quest Speaker")]
    public sealed class QuestSpeaker : ScriptableObject
    {
        public string speakerId;
        public Sprite portrait;
        public Sprite[] frames;
        [Min(1)] public float framesPerSecond = 12;
        [Min(0)] public float loopPauseSeconds;
        [Min(1)] public float dialoguePixelScale = 1;

        public Sprite CurrentPortrait => PortraitAt(Time.unscaledTime);

        public Sprite PortraitAt(float elapsedSeconds)
        {
            if (frames == null || frames.Length == 0) return portrait;
            var rate = Mathf.Max(1, framesPerSecond);
            var duration = frames.Length / rate;
            var phase = Mathf.Repeat(Mathf.Max(0, elapsedSeconds), duration + Mathf.Max(0, loopPauseSeconds));
            return phase < duration ? frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(phase * rate))] : null;
        }
    }
}
