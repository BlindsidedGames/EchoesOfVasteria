using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Updates a slider or image to reflect the progress of an NPC generator.
    /// </summary>
    public class NpcGeneratorProgressUI : MonoBehaviour
    {
        [SerializeField, HideInInspector] private NPCResourceGenerator generator;
        [SerializeField, HideInInspector] private Resource resource;
        [SerializeField] private double amountPerCycle;
        [SerializeField] private SlicedFilledImage image;
        [SerializeField] private TMP_Text resourceNameText;
        [SerializeField] private TMP_Text totalCollectedText;
        [SerializeField] private TMP_Text awaitingCollectionText;
        [SerializeField] private TMP_Text collectionRateText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button collectButton;

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
                {
                    inventoryUI = ResourceInventoryUI.Instance;
                    if (inventoryUI == null)
                        TELogger.Log("ResourceInventoryUI missing", TELogCategory.Resource, this);
                }
                var r = res;
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => inventoryUI?.HighlightResource(r));
            }

            if (collectButton != null)
            {
                collectButton.onClick.RemoveAllListeners();
                collectButton.onClick.AddListener(() => generator?.CollectResources());
            }
        }

        private void Awake()
        {
            if (inventoryUI == null)
            {
                inventoryUI = ResourceInventoryUI.Instance;
                if (inventoryUI == null)
                    TELogger.Log("ResourceInventoryUI missing", TELogCategory.Resource, this);
            }
            if (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveAllListeners();
            if (collectButton != null)
                collectButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (generator == null || resource == null) return;
            var pct = generator.Interval > 0f ? Mathf.Clamp01(generator.Progress / generator.Interval) : 0f;
            if (image != null)
                image.fillAmount = pct;

            if (resourceNameText != null)
                resourceNameText.text = resource.name;
            if (resourceManager != null && totalCollectedText != null)
                totalCollectedText.text = CalcUtils.FormatNumber(generator.GetTotalCollected(resource), true);
            if (awaitingCollectionText != null)
                awaitingCollectionText.text = CalcUtils.FormatNumber(generator.GetStoredAmount(resource), true);
            if (collectionRateText != null)
            {
                if (generator.Interval > 0)
                {
                    var time = generator.Interval.ToString("0.##");
                    collectionRateText.text = CalcUtils.FormatNumber(amountPerCycle, true) + " / " + time + "s";
                }
                else
                {
                    collectionRateText.text = CalcUtils.FormatNumber(amountPerCycle, true);
                }
            }
        }
    }
}