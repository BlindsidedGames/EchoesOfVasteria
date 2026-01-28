using Blindsided.Utilities;
using TMPro;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Manages the Ivan XP display in the forge UI.
    /// </summary>
    public class ForgeIvanXpDisplay : MonoBehaviour
    {
        [SerializeField] private SlicedFilledImage xpBar;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private TMP_Text levelText;

        private CraftingService _crafting;

        private void Awake()
        {
            _crafting = CraftingService.Instance;
        }

        private void OnEnable()
        {
            if (_crafting == null)
                _crafting = CraftingService.Instance;

            if (_crafting != null)
            {
                _crafting.OnIvanXpChanged += UpdateDisplay;
                _crafting.OnIvanLevelUp += OnLevelUp;
            }

            // Initial update
            RefreshFromCraftingService();
        }

        private void OnDisable()
        {
            if (_crafting != null)
            {
                _crafting.OnIvanXpChanged -= UpdateDisplay;
                _crafting.OnIvanLevelUp -= OnLevelUp;
            }
        }

        /// <summary>
        /// Updates the display with the current Ivan XP state.
        /// </summary>
        public void UpdateDisplay(int level, float currentXp, float neededXp)
        {
            if (levelText != null)
                levelText.text = $"Ivan Lv. {level}";

            if (xpText != null)
                xpText.text = $"{Mathf.FloorToInt(currentXp)}/{Mathf.FloorToInt(neededXp)}";

            if (xpBar != null)
            {
                var fill = neededXp > 0 ? Mathf.Clamp01(currentXp / neededXp) : 0f;
                xpBar.fillAmount = fill;
            }
        }

        /// <summary>
        /// Called when Ivan levels up.
        /// </summary>
        public void OnLevelUp(int newLevel)
        {
            // Could add visual feedback here (particles, sound, etc.)
            Debug.Log($"Ivan leveled up to {newLevel}!");
        }

        /// <summary>
        /// Refreshes the display from the crafting service.
        /// </summary>
        public void RefreshFromCraftingService()
        {
            if (_crafting == null)
                _crafting = CraftingService.Instance;

            if (_crafting == null) return;

            var (level, currentXp, neededXp) = _crafting.GetIvanXpState();
            UpdateDisplay(level, currentXp, neededXp);
        }
    }
}
