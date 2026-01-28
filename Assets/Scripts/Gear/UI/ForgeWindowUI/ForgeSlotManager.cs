using System;
using System.Collections.Generic;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Manages gear slot and core slot selection state for the forge UI.
    /// Decouples selection logic from the main ForgeWindowUI.
    /// </summary>
    public class ForgeSlotManager
    {
        private readonly Dictionary<GearSlotUIReferences, string> _gearSlotNameByRef = new();
        private readonly Dictionary<CoreSlotUIReferences, CoreSO> _coreSlotCoreByRef = new();

        public string SelectedSlot { get; private set; }
        public CoreSO SelectedCore { get; private set; }

        public event Action<string> OnSlotSelected;
        public event Action<CoreSO> OnCoreSelected;

        /// <summary>
        /// Initializes the slot manager with gear and core slot references.
        /// </summary>
        public void Initialize(
            List<GearSlotUIReferences> gearSlots,
            List<CoreSlotUIReferences> coreSlots,
            List<CoreSO> cores,
            List<string> slotNames)
        {
            _gearSlotNameByRef.Clear();
            _coreSlotCoreByRef.Clear();

            // Map gear slots
            for (var i = 0; i < gearSlots.Count; i++)
            {
                var slotRef = gearSlots[i];
                if (slotRef == null) continue;

                var resolvedName = !string.IsNullOrWhiteSpace(slotRef.SlotName) ? slotRef.SlotName :
                    i < slotNames.Count ? slotNames[i] : null;

                if (!string.IsNullOrWhiteSpace(resolvedName))
                    _gearSlotNameByRef[slotRef] = resolvedName;
            }

            // Map core slots
            for (var i = 0; i < coreSlots.Count; i++)
            {
                var slot = coreSlots[i];
                if (slot == null) continue;

                var mappedCore = slot.Core != null ? slot.Core : i < cores.Count ? cores[i] : null;
                _coreSlotCoreByRef[slot] = mappedCore;
            }
        }

        /// <summary>
        /// Gets the slot name for a gear slot reference.
        /// </summary>
        public string GetSlotName(GearSlotUIReferences slotRef)
        {
            return _gearSlotNameByRef.TryGetValue(slotRef, out var name) ? name : null;
        }

        /// <summary>
        /// Gets the core for a core slot reference.
        /// </summary>
        public CoreSO GetCore(CoreSlotUIReferences slotRef)
        {
            return _coreSlotCoreByRef.TryGetValue(slotRef, out var core) ? core : slotRef?.Core;
        }

        /// <summary>
        /// Selects a gear slot by name.
        /// </summary>
        public void SelectSlot(string slotName)
        {
            if (SelectedSlot == slotName) return;
            SelectedSlot = slotName;
            OnSlotSelected?.Invoke(slotName);
        }

        /// <summary>
        /// Selects a core.
        /// </summary>
        public void SelectCore(CoreSO core)
        {
            if (SelectedCore == core) return;
            SelectedCore = core;
            OnCoreSelected?.Invoke(core);
        }

        /// <summary>
        /// Updates visual selections for all gear slots.
        /// </summary>
        public void UpdateGearSlotVisuals(string selectedSlot)
        {
            foreach (var kvp in _gearSlotNameByRef)
            {
                var slotRef = kvp.Key;
                var slotName = kvp.Value;
                slotRef?.SetSelected(string.Equals(slotName, selectedSlot, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Updates visual selections for all core slots.
        /// </summary>
        public void UpdateCoreSlotVisuals(CoreSO selectedCore)
        {
            foreach (var kvp in _coreSlotCoreByRef)
            {
                var slotRef = kvp.Key;
                var core = kvp.Value;
                slotRef?.SetSelected(core == selectedCore);
            }
        }

        /// <summary>
        /// Clears all selections.
        /// </summary>
        public void ClearSelections()
        {
            SelectedSlot = null;
            SelectedCore = null;
            UpdateGearSlotVisuals(null);
            UpdateCoreSlotVisuals(null);
        }

        /// <summary>
        /// Gets all gear slot references.
        /// </summary>
        public IEnumerable<KeyValuePair<GearSlotUIReferences, string>> GetGearSlots() => _gearSlotNameByRef;

        /// <summary>
        /// Gets all core slot references.
        /// </summary>
        public IEnumerable<KeyValuePair<CoreSlotUIReferences, CoreSO>> GetCoreSlots() => _coreSlotCoreByRef;
    }
}
