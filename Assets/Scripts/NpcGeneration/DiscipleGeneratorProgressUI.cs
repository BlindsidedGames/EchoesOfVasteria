using Blindsided.Utilities;
using System.Collections.Generic;
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

        private readonly Dictionary<Resource, DiscipleGeneratedResourceUIReferences> resourceUIs = new();
        private readonly Dictionary<Resource, double> amountsPerCycle = new();
        private ResourceInventoryUI inventoryUI;
        private ResourceManager resourceManager;

        public void SetData(DiscipleGenerator gen)
        {
            generator = gen;

            if (generatedParent == null || generatedPrefab == null || generator == null)
                return;

            foreach (Transform child in generatedParent)
                Destroy(child.gameObject);
            resourceUIs.Clear();
            amountsPerCycle.Clear();

            foreach (var entry in generator.ResourceEntries)
            {
                if (entry.resource == null) continue;
                var ui = Instantiate(generatedPrefab, generatedParent);
                if (ui.iconImage != null)
                    ui.iconImage.sprite = entry.resource.icon;
                if (ui.selectButton != null)
                {
                    EnsureInventoryUI();
                    var r = entry.resource;
                    ui.selectButton.onClick.RemoveAllListeners();
                    ui.selectButton.onClick.AddListener(() => inventoryUI?.HighlightResource(r));
                }
                resourceUIs[entry.resource] = ui;
                amountsPerCycle[entry.resource] = entry.amount;
            }

            if (resourceNameText != null)
            {
                var first = generator.ResourceEntries != null && generator.ResourceEntries.Count > 0 ?
                    generator.ResourceEntries[0].resource : null;
                resourceNameText.text = first ? first.name : string.Empty;
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
            foreach (var ui in resourceUIs.Values)
                if (ui != null && ui.selectButton != null)
                    ui.selectButton.onClick.RemoveAllListeners();
            if (collectButton != null)
                collectButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (generator == null) return;

            var pct = generator.Interval > 0f ? Mathf.Clamp01(generator.Progress / generator.Interval) : 0f;
            if (image != null)
                image.fillAmount = pct;

            if (resourceManager != null && totalCollectedText != null)
            {
                double total = 0;
                foreach (var entry in generator.ResourceEntries)
                    if (entry.resource != null)
                        total += generator.GetTotalCollected(entry.resource);
                totalCollectedText.text = CalcUtils.FormatNumber(total, true);
            }

            foreach (var pair in resourceUIs)
            {
                var res = pair.Key;
                var ui = pair.Value;
                if (ui != null && ui.awaitingCollectionText != null)
                    ui.awaitingCollectionText.text = CalcUtils.FormatNumber(generator.GetStoredAmount(res), true);
            }

            if (collectionRateText != null)
            {
                double totalPerCycle = 0;
                foreach (var amt in amountsPerCycle.Values)
                    totalPerCycle += amt;
                if (generator.Interval > 0)
                {
                    var time = generator.Interval.ToString("0.##");
                    collectionRateText.text = CalcUtils.FormatNumber(totalPerCycle, true) + " / " + time + "s";
                }
                else
                {
                    collectionRateText.text = CalcUtils.FormatNumber(totalPerCycle, true);
                }
            }

            if (collectButton != null)
            {
                bool interact = false;
                foreach (var entry in generator.ResourceEntries)
                {
                    if (entry.resource != null && generator.GetStoredAmount(entry.resource) > 0)
                    {
                        interact = true;
                        break;
                    }
                }
                collectButton.interactable = interact;
            }
        }
    }
}