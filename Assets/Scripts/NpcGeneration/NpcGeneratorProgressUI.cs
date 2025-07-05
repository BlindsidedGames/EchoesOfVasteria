using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TMPro;
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
        [SerializeField] private Resource resource;
        [SerializeField] private double amountPerCycle;
        [SerializeField] private Slider slider;
        [SerializeField] private Image image;
        [SerializeField] private TMP_Text resourceNameText;
        [SerializeField] private TMP_Text totalCollectedText;
        [SerializeField] private TMP_Text awaitingCollectionText;
        [SerializeField] private TMP_Text collectionRateText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button selectButton;

        private ResourceInventoryUI inventoryUI;
        private ResourceManager resourceManager;

        public void SetData(NPCResourceGenerator gen, Resource res, double perCycle)
        {
            generator = gen;
            resource = res;
            amountPerCycle = perCycle;

            if (iconImage != null)
                iconImage.sprite = res ? res.icon : null;
            if (resourceNameText != null && res != null)
                resourceNameText.text = res.name;

            if (selectButton != null)
            {
                if (inventoryUI == null)
                    inventoryUI = FindFirstObjectByType<ResourceInventoryUI>();
                var r = res;
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => inventoryUI?.HighlightResource(r));
            }
        }

        private void Awake()
        {
            if (inventoryUI == null)
                inventoryUI = FindFirstObjectByType<ResourceInventoryUI>();
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (generator == null || resource == null) return;
            float pct = generator.Interval > 0f ? Mathf.Clamp01(generator.Progress / generator.Interval) : 0f;
            if (slider != null)
                slider.value = pct;
            if (image != null)
                image.fillAmount = pct;

            if (resourceNameText != null)
                resourceNameText.text = resource.name;
            if (resourceManager != null && totalCollectedText != null)
                totalCollectedText.text = CalcUtils.FormatNumber(resource.totalReceived, true);
            if (awaitingCollectionText != null)
                awaitingCollectionText.text = CalcUtils.FormatNumber(generator.GetStoredAmount(resource), true);
            if (collectionRateText != null)
            {
                var rate = generator.Interval > 0 ? amountPerCycle / generator.Interval : 0;
                collectionRateText.text = CalcUtils.FormatNumber(rate, true) + "/s";
            }
        }
    }
}
