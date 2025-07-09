using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes.Audio
{
    public static class SfxPlayer
    {
        private static AudioSource _source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var go = new GameObject("SFX Source");
            Object.DontDestroyOnLoad(go);

            _source = go.AddComponent<AudioSource>();
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
            _source.loop = false;
        }

        public static void PlaySfx(AudioClip clip)
        {
            if (clip == null || _source == null) return;
            _source.PlayOneShot(clip, StaticReferences.SfxVolume);
        }
    }
}
