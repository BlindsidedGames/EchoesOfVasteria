using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     References to the UI elements used for each save slot.
    ///     The "load" button can switch to a delete button when the
    ///     safety toggle is enabled.
    /// </summary>
    public class SaveSlotReferences : MonoBehaviour
    {
        // Button used for saving. Disabled on inactive slots unless
        // safety is enabled.
        public Button saveButton;

        // Button that loads the slot normally, or deletes it when the
        // safety toggle is active.
        public Button loadDeleteButton;

        // Text attached to the load/delete button so its label can be
        // updated dynamically.
        public TMP_Text loadDeleteText;

        public Button toggleSafetyButton;
        public TMP_Text fileNameText;
        public TMP_Text playtimeText;
        public TMP_Text lastPlayedText;

        // Visual indicator for the safety toggle state.
        [HideInInspector] public Image safetyToggleImage;

        // True when the user has enabled the safety switch.
        [HideInInspector] public bool safetyEnabled;

        public DateTime? lastPlayed;

        // Completion percentage for the slot.
        public float completionPercentage;
    }
}