using System.Collections.Generic;
using References.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using static TimelessEchoes.TELogger;
using static Blindsided.Utilities.CalcUtils;

namespace TimelessEchoes.Upgrades
{
    /// <summary>
    ///     Tracks resources gained during a run and displays them when returning to town.
    /// </summary>
    public class RunResourceTrackerUI : MonoBehaviour
    {
        [SerializeField] private Transform slotParent;
        [SerializeField] private ResourceUIReferences slotPrefab;
        [SerializeField] private GameObject window;

        private readonly Dictionary<Resource, double> amounts = new();
        private readonly Dictionary<Resource, double> bonusAmounts = new();
        private ResourceManager resourceManager;

        private void Awake()
        {
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);
            if (slotParent == null)
                slotParent = transform;
            if (window != null)
                window.SetActive(false);
            ClearSlots();
        }

        private void OnEnable()
        {
            if (resourceManager != null)
                resourceManager.OnResourceAdded += OnResourceAdded;
        }

        private void OnDisable()
        {
            if (resourceManager != null)
                resourceManager.OnResourceAdded -= OnResourceAdded;
        }

        /// <summary>
        ///     Clears all recorded resource amounts. Call this when a run begins.
        /// </summary>
        public void BeginRun()
        {
            amounts.Clear();
            bonusAmounts.Clear();
            ClearSlots();
            if (window != null)
                window.SetActive(false);
        }

        private void ClearSlots()
        {
            if (slotParent == null)
                return;
            foreach (Transform child in slotParent)
                Destroy(child.gameObject);
        }

        private void OnResourceAdded(Resource resource, double amount, bool bonus)
        {
            if (resource == null || amount <= 0)
                return;
            if (amounts.ContainsKey(resource))
                amounts[resource] += amount;
            else
                amounts[resource] = amount;
            if (bonus)
            {
                if (bonusAmounts.ContainsKey(resource))
                    bonusAmounts[resource] += amount;
                else
                    bonusAmounts[resource] = amount;
            }
        }

        /// <summary>
        ///     Displays the recorded resources and amounts.
        /// </summary>
        public void ShowWindow()
        {
            if (slotParent == null || slotPrefab == null)
                return;
            if (amounts.Count == 0)
            {
                if (window != null)
                    window.SetActive(false);
                return;
            }

            ClearSlots();
            foreach (var pair in amounts)
            {
                var slot = Instantiate(slotPrefab, slotParent);
                bonusAmounts.TryGetValue(pair.Key, out var bonus);
                SetupSlot(slot, pair.Key, pair.Value - bonus, bonus);
            }

            if (window != null)
                window.SetActive(true);
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                if (window != null && window.activeSelf)
                    window.SetActive(false);
        }

        private void SetupSlot(ResourceUIReferences slot, Resource res, double amount, double bonus)
        {
            if (slot == null)
                return;
            if (slot.iconImage)
            {
                slot.iconImage.sprite = res ? res.icon : null;
                slot.iconImage.enabled = res != null && res.icon != null;
            }

            if (slot.countText)
            {
                slot.countText.text = FormatNumber(amount, true);
                if (bonus >= 1)
                    slot.countText.text += $" (+{FormatNumber(bonus, true)})";
                slot.countText.gameObject.SetActive(true);
            }
        }
    }
}