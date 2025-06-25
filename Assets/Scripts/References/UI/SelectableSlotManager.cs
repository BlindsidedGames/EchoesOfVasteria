using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace References.UI
{
    /// <summary>
    /// Handles selection logic and tooltip display for a group of selectable slots.
    /// </summary>
    public class SelectableSlotManager : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> slotBehaviours = new();
        [SerializeField] private TooltipUIReferences tooltip;
        [SerializeField] private bool showTooltipOnHover = false;
        [SerializeField] private Vector2 tooltipOffset = Vector2.zero;

        private readonly List<ISelectableSlot> slots = new();
        private int selectedIndex = -1;

        /// <summary>Invoked when a tooltip needs content for the given slot index.</summary>
        public event Action<int> TooltipRequested;
        /// <summary>Invoked when a slot is selected.</summary>
        public event Action<int> Selected;
        /// <summary>Invoked when selection is cleared.</summary>
        public event Action Deselected;

        /// <summary>Currently selected index or -1.</summary>
        public int SelectedIndex => selectedIndex;
        /// <summary>Access to the underlying slots.</summary>
        public IReadOnlyList<ISelectableSlot> Slots => slots;
        /// <summary>The tooltip reference used by this manager.</summary>
        public TooltipUIReferences Tooltip => tooltip;

        private void Awake()
        {
            if (slotBehaviours.Count == 0)
                slotBehaviours.AddRange(GetComponentsInChildren<MonoBehaviour>(true));

            foreach (var beh in slotBehaviours)
                if (beh is ISelectableSlot slot)
                    slots.Add(slot);

            for (int i = 0; i < slots.Count; i++)
                RegisterSlot(i);
        }

        private void RegisterSlot(int index)
        {
            var slot = slots[index];
            if (slot.SelectButton != null)
                slot.SelectButton.onClick.AddListener(() => Select(index));

            slot.PointerClick += (_, button) =>
            {
                if (button == PointerEventData.InputButton.Right)
                    Deselect();
            };

            slot.PointerEnter += _ => { if (showTooltipOnHover) ShowTooltip(index); };
            slot.PointerExit += _ => { if (showTooltipOnHover) HideTooltip(); };
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
                Deselect();
        }

        /// <summary>Select the slot at the given index.</summary>
        public void Select(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].SelectionImage != null)
                    slots[i].SelectionImage.enabled = i == selectedIndex;
            ShowTooltip(selectedIndex);
            Selected?.Invoke(index);
        }

        /// <summary>Clear selection and hide any tooltip.</summary>
        public void Deselect()
        {
            selectedIndex = -1;
            foreach (var slot in slots)
                if (slot.SelectionImage != null)
                    slot.SelectionImage.enabled = false;
            HideTooltip();
            Deselected?.Invoke();
        }

        private void ShowTooltip(int index)
        {
            if (tooltip == null)
                return;
            if (index < 0 || index >= slots.Count)
            {
                HideTooltip();
                return;
            }
            tooltip.transform.position = slots[index].Transform.position + (Vector3)tooltipOffset;
            TooltipRequested?.Invoke(index);
            tooltip.gameObject.SetActive(true);
        }

        /// <summary>Hide the tooltip if active.</summary>
        public void HideTooltip()
        {
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }

        /// <summary>Remove all tracked slots.</summary>
        public void ClearSlots()
        {
            slotBehaviours.Clear();
            slots.Clear();
            selectedIndex = -1;
            HideTooltip();
        }

        /// <summary>Add a slot at runtime.</summary>
        public void AddSlot(MonoBehaviour behaviour)
        {
            if (behaviour is ISelectableSlot slot)
            {
                slotBehaviours.Add(behaviour);
                slots.Add(slot);
                RegisterSlot(slots.Count - 1);
            }
        }
    }
}
