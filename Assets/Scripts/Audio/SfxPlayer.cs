using Blindsided.SaveData;
using UnityEngine;
using UnityEngine.Audio;

namespace TimelessEchoes.Audio
{
    public static class SfxPlayer
    {
        private static AudioSource _source;
        private static AudioMixerGroup _mixerGroup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var go = new GameObject("SFX Source");
            Object.DontDestroyOnLoad(go);

            _source = go.AddComponent<AudioSource>();
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
            _source.loop = false;
            if (_mixerGroup != null)
                _source.outputAudioMixerGroup = _mixerGroup;
        }

        public static void SetMixerGroup(AudioMixerGroup group)
        {
            _mixerGroup = group;
            if (_source != null)
                _source.outputAudioMixerGroup = _mixerGroup;
        }

        public static void PlaySfx(AudioClip clip)
        {
            if (clip == null || _source == null) return;
            _source.PlayOneShot(clip, StaticReferences.SfxVolume);
        }
    }
}
