using Blindsided.SaveData;
using UnityEngine;
using UnityEngine.Audio;

namespace TimelessEchoes.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixers")] [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambianceGroup;

        [Header("Music")] [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip musicClip;

        [Header("Task Clips")] [SerializeField]
        private AudioClip[] woodcuttingClips;

        [SerializeField] private AudioClip[] farmingClips;
        [SerializeField] private AudioClip[] fishingClips;
        [SerializeField] private AudioClip[] miningClips;

        [Header("Combat Clips")] [SerializeField]
        private AudioClip[] slimeClips;

        public enum TaskType
        {
            Woodcutting,
            Farming,
            Fishing,
            Mining
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (musicClip != null && musicSource != null)
            {
                musicSource.clip = musicClip;
                musicSource.loop = true;
                musicSource.outputAudioMixerGroup = musicGroup;
                musicSource.Play();
            }

            ApplyVolumes();
        }

        public void ApplyVolumes()
        {
            if (mainMixer == null) return;
            mainMixer.SetFloat("MasterVolume", LinearToDecibel(StaticReferences.MasterVolume));
            mainMixer.SetFloat("MusicVolume", LinearToDecibel(StaticReferences.MusicVolume));
            mainMixer.SetFloat("SfxVolume", LinearToDecibel(StaticReferences.SfxVolume));
        }

        public void SetMasterVolume(float value)
        {
            StaticReferences.MasterVolume = value;
            if (mainMixer != null)
                mainMixer.SetFloat("MasterVolume", LinearToDecibel(value));
        }

        public void SetMusicVolume(float value)
        {
            StaticReferences.MusicVolume = value;
            if (mainMixer != null)
                mainMixer.SetFloat("MusicVolume", LinearToDecibel(value));
        }

        public void SetSfxVolume(float value)
        {
            StaticReferences.SfxVolume = value;
            if (mainMixer != null)
                mainMixer.SetFloat("SfxVolume", LinearToDecibel(value));
        }

        public void PlayTaskClip(TaskType type)
        {
            AudioClip clip = null;
            switch (type)
            {
                case TaskType.Woodcutting:
                    clip = GetRandom(woodcuttingClips);
                    break;
                case TaskType.Farming:
                    clip = GetRandom(farmingClips);
                    break;
                case TaskType.Fishing:
                    clip = GetRandom(fishingClips);
                    break;
                case TaskType.Mining:
                    clip = GetRandom(miningClips);
                    break;
            }

            PlaySfx(clip);
        }

        public void PlayCombatClip(AudioClip[] clips)
        {
            PlaySfx(GetRandom(clips));
        }

        public void PlaySlimeClip()
        {
            PlayCombatClip(slimeClips);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, StaticReferences.SfxVolume);
        }

        private static AudioClip GetRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        private static float LinearToDecibel(float value)
        {
            var v = Mathf.Clamp(value, 0.0001f, 1f);
            return Mathf.Log10(v) * 20f;
        }
    }
}