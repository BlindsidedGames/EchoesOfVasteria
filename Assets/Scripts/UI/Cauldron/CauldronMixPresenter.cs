using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using References.UI;
using TMPro;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI.Cauldron
{
    /// <summary>
    /// Handles mixing slot selection and display.
    /// </summary>
    public class CauldronMixPresenter : MonoBehaviour
    {
        [SerializeField] private List<CauldronMixItemUIReferences> mixSlots;
        [SerializeField] private CauldronMixItemUIReferences slot1;
        [SerializeField] private CauldronMixItemUIReferences slot2;
        [SerializeField] private Button mixButton;
        [SerializeField] private Button mixAllButton;
        [SerializeField] private TMP_Text predictedStewText;
        [SerializeField] private Image mixArrowImage;
        [SerializeField] private Sprite mixArrowGreenSprite;
        [SerializeField] private Sprite mixArrowRedSprite;

        private Resource _selectedA;
        private Resource _selectedB;
        private bool _nextGreen = true;

        /// <summary>
        /// Currently selected first resource for mixing.
        /// </summary>
        public Resource SelectedA => _selectedA;

        /// <summary>
        /// Currently selected second resource for mixing.
        /// </summary>
        public Resource SelectedB => _selectedB;

        /// <summary>
        /// Event fired when the mix button is clicked with two valid selections.
        /// </summary>
        public event Action<Resource, Resource> OnMixRequested;

        /// <summary>
        /// Event fired when the mix all button is clicked.
        /// </summary>
        public event Action OnMixAllRequested;

        /// <summary>
        /// Event fired when the selected display slots need refreshing.
        /// </summary>
        public event Action OnSelectionChanged;

        /// <summary>
        /// Initializes button listeners.
        /// </summary>
        public void Initialize()
        {
            if (mixButton != null)
                mixButton.onClick.AddListener(() => OnMixRequested?.Invoke(_selectedA, _selectedB));
            if (mixAllButton != null)
                mixAllButton.onClick.AddListener(() => OnMixAllRequested?.Invoke());
        }

        /// <summary>
        /// Refreshes all mix slots with current eligible foods.
        /// </summary>
        /// <param name="eligibleFoods">List of resources eligible for mixing.</param>
        /// <param name="rm">ResourceManager for amounts.</param>
        public void RefreshSlots(List<Resource> eligibleFoods, ResourceManager rm)
        {
            if (rm == null || mixSlots == null || mixSlots.Count == 0) return;

            // Clear any previous selections that are not foods or have zero amount
            if (_selectedA != null && !eligibleFoods.Contains(_selectedA)) _selectedA = null;
            if (_selectedB != null && !eligibleFoods.Contains(_selectedB)) _selectedB = null;
            if (_selectedA != null && rm.GetAmount(_selectedA) <= 0) _selectedA = null;
            if (_selectedB != null && rm.GetAmount(_selectedB) <= 0) _selectedB = null;

            // Clamp to first 30 and to available UI slots
            var maxByDesign = 30;
            var capacity = mixSlots.Count;
            var clamped = eligibleFoods.Take(Mathf.Min(maxByDesign, eligibleFoods.Count)).ToList();
            if (clamped.Count > capacity)
                clamped = clamped.Take(capacity).ToList();

            var all = clamped;
            for (int i = 0; i < mixSlots.Count; i++)
            {
                var ui = mixSlots[i];
                Resource r = i < all.Count ? all[i] : null;
                if (ui == null) continue;

                if (r == null)
                {
                    if (ui.iconImage != null)
                    {
                        ui.iconImage.enabled = false;
                        ui.iconImage.sprite = null;
                    }
                    if (ui.countText != null) ui.countText.text = "";
                    if (ui.selectButton != null) ui.selectButton.interactable = false;
                    if (ui.selectionImageGreen != null) ui.selectionImageGreen.enabled = false;
                    if (ui.selectionImageWhite != null) ui.selectionImageWhite.enabled = false;
                    continue;
                }

                if (ui.iconImage != null)
                {
                    ui.iconImage.enabled = true;
                    ui.iconImage.sprite = r.icon;
                }

                var amount = rm.GetAmount(r);
                if (ui.countText != null) ui.countText.text = CalcUtils.FormatNumber(amount, true);

                if (ui.selectButton != null)
                {
                    ui.selectButton.onClick.RemoveAllListeners();
                    var isSelA = _selectedA == r;
                    var isSelB = _selectedB == r;
                    var hasAny = amount > 0;
                    var canSelect = hasAny && !(isSelA || isSelB);
                    ui.selectButton.interactable = canSelect;

                    if (canSelect)
                    {
                        // Capture resource for closure
                        var captured = r;
                        ui.selectButton.onClick.AddListener(() => ToggleSelection(captured));
                    }
                }

                {
                    var isSelA = _selectedA == r;
                    var isSelB = _selectedB == r;
                    if (ui.selectionImageGreen != null)
                        ui.selectionImageGreen.enabled = (isSelA && !_nextGreen) || (isSelB && _nextGreen);
                    if (ui.selectionImageWhite != null)
                        ui.selectionImageWhite.enabled = (isSelA && _nextGreen) || (isSelB && !_nextGreen);
                }
            }
        }

        /// <summary>
        /// Refreshes the selected display slots (slot1 and slot2).
        /// </summary>
        public void RefreshSelectedDisplaySlots()
        {
            // Update slot1 (selectedA display)
            if (slot1 != null)
            {
                if (slot1.iconImage != null)
                {
                    if (_selectedA != null)
                    {
                        slot1.iconImage.enabled = true;
                        slot1.iconImage.sprite = _selectedA.icon;
                    }
                    else
                    {
                        slot1.iconImage.enabled = false;
                        slot1.iconImage.sprite = null;
                    }
                }
            }

            // Update slot2 (selectedB display)
            if (slot2 != null)
            {
                if (slot2.iconImage != null)
                {
                    if (_selectedB != null)
                    {
                        slot2.iconImage.enabled = true;
                        slot2.iconImage.sprite = _selectedB.icon;
                    }
                    else
                    {
                        slot2.iconImage.enabled = false;
                        slot2.iconImage.sprite = null;
                    }
                }
            }
        }

        /// <summary>
        /// Refreshes the mix button state and predicted stew text.
        /// </summary>
        /// <param name="rm">ResourceManager for amounts.</param>
        public void RefreshMixButton(ResourceManager rm)
        {
            double amountA = 0, amountB = 0;
            if (rm != null && _selectedA != null) amountA = rm.GetAmount(_selectedA);
            if (rm != null && _selectedB != null) amountB = rm.GetAmount(_selectedB);
            var canMix = _selectedA != null && _selectedB != null && _selectedA != _selectedB && amountA > 0 && amountB > 0;
            if (mixButton != null) mixButton.interactable = canMix;

            // Update predicted stew text
            double predicted = 0;
            if (_selectedA != null && _selectedB != null && _selectedA != _selectedB)
            {
                double points = amountA * _selectedA.baseValue * _selectedA.valueMultiplier
                                + amountB * _selectedB.baseValue * _selectedB.valueMultiplier;
                predicted = points / 100.0;
            }
            if (predictedStewText != null)
                predictedStewText.text = CalcUtils.FormatNumber(predicted);

            // Arrow color
            if (mixArrowImage != null)
            {
                mixArrowImage.sprite = canMix ? mixArrowGreenSprite : mixArrowRedSprite;
                mixArrowImage.enabled = mixArrowImage.sprite != null;
            }
        }

        /// <summary>
        /// Updates the mix all button state based on stocked foods.
        /// </summary>
        /// <param name="eligibleFoods">List of eligible foods.</param>
        /// <param name="rm">ResourceManager for amounts.</param>
        public void UpdateMixAllButtonState(List<Resource> eligibleFoods, ResourceManager rm)
        {
            if (mixAllButton == null || rm == null) return;
            var stocked = 0;
            foreach (var res in eligibleFoods)
            {
                if (res == null) continue;
                if (rm.GetAmount(res) > 0)
                {
                    stocked++;
                    if (stocked >= 2) break;
                }
            }
            mixAllButton.interactable = stocked >= 2;
        }

        /// <summary>
        /// Clears the current selection state.
        /// </summary>
        public void ClearSelection()
        {
            _selectedA = _selectedB = null;
            _nextGreen = true;
        }

        private void ToggleSelection(Resource r)
        {
            // Always distinct; selecting another replaces the oldest (alternating colors)
            if (_selectedA == null && _selectedB == null)
            {
                _selectedA = r;
                _nextGreen = false;
            }
            else if (_selectedA != null && _selectedB == null)
            {
                _selectedB = r;
                _nextGreen = true;
            }
            else
            {
                if (_nextGreen)
                {
                    _selectedA = r;
                    _nextGreen = false;
                }
                else
                {
                    _selectedB = r;
                    _nextGreen = true;
                }
            }

            // Notify that selection changed
            OnSelectionChanged?.Invoke();
        }

        /// <summary>
        /// Builds a list of resources eligible for mixing from task drop tables and category overrides.
        /// </summary>
        /// <param name="rm">ResourceManager for unlock state.</param>
        /// <returns>Sorted list of eligible, unlocked foods.</returns>
        public static List<Resource> BuildEligibleFoodsList(ResourceManager rm)
        {
            // Only allow mixing with foods: resources that appear in Farming or Fishing task drop tables
            var eligibleFromTasks = new HashSet<Resource>();
            foreach (var t in AssetCache.GetAll<TimelessEchoes.Tasks.TaskData>("Tasks"))
            {
                if (t == null) continue;
                var skillName = t.associatedSkill != null ? t.associatedSkill.name : null;
                if (string.IsNullOrEmpty(skillName)) continue;
                var isFarming = skillName.IndexOf("farm", StringComparison.OrdinalIgnoreCase) >= 0;
                var isFishing = skillName.IndexOf("fish", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isFarming && !isFishing) continue;
                foreach (var drop in t.resourceDrops)
                {
                    if (drop == null || drop.resource == null) continue;
                    eligibleFromTasks.Add(drop.resource);
                }
            }

            // Include manual overrides: any Resource categorized as Farming or Fishing
            var eligible = new HashSet<Resource>(eligibleFromTasks);
            foreach (var r in AssetCache.GetAll<Resource>(""))
            {
                if (r == null) continue;
                if (r.cauldronCategory == Resource.CauldronCategory.Farming || r.cauldronCategory == Resource.CauldronCategory.Fishing)
                    eligible.Add(r);
            }

            return AssetCache.GetAll<Resource>("")
                .Where(r => r != null && eligible.Contains(r) && (rm != null && rm.IsUnlocked(r)))
                .OrderBy(r => r.resourceID)
                .ThenBy(r => r.name)
                .ToList();
        }
    }
}
