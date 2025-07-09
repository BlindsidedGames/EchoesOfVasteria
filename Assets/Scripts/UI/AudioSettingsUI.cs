using Blindsided.SaveData;
using TimelessEchoes.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    public class AudioSettingsUI : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private AudioManager audioManager => AudioManager.Instance;

        public void SetSliders()
        {
            if (masterSlider != null)
            {
                masterSlider.value = StaticReferences.MasterVolume;
                masterSlider.onValueChanged.AddListener(OnMasterChanged);
            }

            if (musicSlider != null)
            {
                musicSlider.value = StaticReferences.MusicVolume;
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = StaticReferences.SfxVolume;
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }
        }

        private void OnDisable()
        {
            if (masterSlider != null)
                masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }

        private void OnMasterChanged(float value)
        {
            StaticReferences.MasterVolume = value;
            audioManager?.SetMasterVolume(value);
        }

        private void OnMusicChanged(float value)
        {
            StaticReferences.MusicVolume = value;
            audioManager?.SetMusicVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            StaticReferences.SfxVolume = value;
            audioManager?.SetSfxVolume(value);
        }
    }
}