using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;
using References.UI;

namespace TimelessEchoes.NpcGeneration
{
    /// <summary>
    ///     Updates a slider or image to reflect the progress of an NPC generator.
    /// </summary>
    public class DiscipleGeneratorProgressUI : MonoBehaviour
    {
        [SerializeField, HideInInspector] private DiscipleGenerator generator;
        [SerializeField] private Transform generatedParent;
        [SerializeField] private DiscipleGeneratedResourceUIReferences generatedPrefab;
        [SerializeField] private SlicedFilledImage image;
        [SerializeField] private TMP_Text resourceNameText;
        [SerializeField] private TMP_Text totalCollectedText;
        [SerializeField] private TMP_Text collectionRateText;
        [SerializeField] private Button collectButton;

        private DiscipleGeneratedResourceUIReferences resourceUI;
        private ResourceInventoryUI inventoryUI;
        private ResourceManager resourceManager;

        public void SetData(DiscipleGenerator gen)
        {
            generator = gen;

            if (generatedParent == null || generatedPrefab == null || generator == null)
                return;

            foreach (Transform child in generatedParent)
                Destroy(child.gameObject);
            resourceUI = null;

            var res = generator.Resource;
            if (res != null)
            {
                resourceUI = Instantiate(generatedPrefab, generatedParent);
                if (resourceUI.iconImage != null)
                    resourceUI.iconImage.sprite = res.icon;
                if (resourceUI.selectButton != null)
                {
                    EnsureInventoryUI();
                    resourceUI.selectButton.onClick.RemoveAllListeners();
                    resourceUI.selectButton.onClick.AddListener(() => inventoryUI?.HighlightResource(res));
                }
            }

            if (resourceNameText != null)
            {
                resourceNameText.text = res != null ? res.name : string.Empty;
            }

            if (collectButton != null)
            {
                collectButton.onClick.RemoveAllListeners();
                collectButton.onClick.AddListener(() => generator?.CollectResources());
            }
        }

        private void Awake()
        {
            EnsureInventoryUI();
            if (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            }
        }

        private void EnsureInventoryUI()
        {
            if (inventoryUI != null) return;
            inventoryUI = ResourceInventoryUI.Instance;
            if (inventoryUI == null)
                TELogger.Log("ResourceInventoryUI missing", TELogCategory.Resource, this);
        }

        private void OnDestroy()
        {
            if (resourceUI != null && resourceUI.selectButton != null)
                resourceUI.selectButton.onClick.RemoveAllListeners();
            if (collectButton != null)
                collectButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (generator == null) return;

            var pct = generator.Interval > 0f ? Mathf.Clamp01(generator.Progress / generator.Interval) : 0f;
            if (image != null)
                image.fillAmount = pct;

            var res = generator.Resource;
            if (resourceManager != null && totalCollectedText != null && res != null)
            {
                var val = generator.GetTotalCollected(res);
                totalCollectedText.text = CalcUtils.FormatNumber(val, true);
            }

            if (resourceUI != null && resourceUI.awaitingCollectionText != null && res != null)
                resourceUI.awaitingCollectionText.text = CalcUtils.FormatNumber(generator.GetStoredAmount(res), true);

            if (collectionRateText != null && res != null)
            {
                if (generator.Interval > 0)
                {
                    var time = CalcUtils.FormatTime(generator.Interval, showDecimal: true, shortForm: true);
                    collectionRateText.text =
                        $"{CalcUtils.FormatNumber(generator.CycleAmount, true)} / {time}";
                }
                else
                {
                    collectionRateText.text = CalcUtils.FormatNumber(0, true);
                }
            }

            if (collectButton != null && res != null)
            {
                collectButton.interactable = generator.GetStoredAmount(res) > 0;
            }
        }
    }
}