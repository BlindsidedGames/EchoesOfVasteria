using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    /// Updates a slider or image to reflect the progress of an NPC generator.
    /// </summary>
    public class NpcGeneratorProgressUI : MonoBehaviour
    {
        [SerializeField] private NPCResourceGenerator generator;
        [SerializeField] private Slider slider;
        [SerializeField] private Image image;

        private void Update()
        {
            if (generator == null) return;
            float pct = generator.Interval > 0f ? Mathf.Clamp01(generator.Progress / generator.Interval) : 0f;
            if (slider != null)
                slider.value = pct;
            if (image != null)
                image.fillAmount = pct;
        }
    }
}
