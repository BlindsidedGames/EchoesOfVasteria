// Needed for Guid
// Required for MPImageBasic

using System;
using MPUIKIT;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.SaveData.StaticReferences;

// If using Odin Inspector for the Button

namespace Blindsided.Utilities
{
    public class GenericFoldout : MonoBehaviour
    {
        [Header("Unique Identifier")] [Tooltip("Unique key for saving state. Generate one if empty.")] [SerializeField]
        private string guid; // Unique key for PlayerPrefs

        [Header("UI References")] [Tooltip("The button that triggers the foldout action.")] [SerializeField]
        private Button foldoutButton;

        [Tooltip("The panel (GameObject) to show/hide.")] [SerializeField]
        private GameObject foldoutPanel;

        [Tooltip("(Optional) The MPImageBasic to indicate the state (e.g., an arrow). Uses FlipVertical.")]
        [SerializeField]
        private MPImageBasic indicatorMPImageBasic; // Specific MPUIKIT Image

        [Header("Configuration")]
        [Tooltip("Set the default state if no saved preference exists. Checked = Expanded, Unchecked = Collapsed.")]
        [SerializeField]
        private bool startsExpanded;


        private bool _isExpanded;

        private void Start()
        {
            Foldouts.TryAdd(guid, _isExpanded);

            if (foldoutButton == null || foldoutPanel == null)
            {
                Debug.LogError($"FoldoutButton or FoldoutPanel is not assigned on {gameObject.name}", this);
                return;
            }

            if (string.IsNullOrEmpty(guid))
                Debug.LogError($"GUID is not set on {gameObject.name}. Please generate one in the inspector.", this);

            // Load saved state, using 'startsExpanded' as the default
            var defaultState = startsExpanded ? 1 : 0;
            _isExpanded = Foldouts[guid];

            // Apply initial state without saving again
            ApplyState(_isExpanded);

            // Add listener
            foldoutButton.onClick.AddListener(ToggleFoldout);
        }

        private void ToggleFoldout()
        {
            _isExpanded = !_isExpanded;
            ApplyState(_isExpanded);

            Foldouts[guid] = _isExpanded; // Save the state
        }

        private void ApplyState(bool expand)
        {
            foldoutPanel.SetActive(expand);
            UpdateIndicator(expand);
        }

        private void UpdateIndicator(bool isExpanded)
        {
            if (indicatorMPImageBasic != null)
                // FlipVertical is true when NOT expanded (i.e., collapsed)
                indicatorMPImageBasic.FlipVertical = !isExpanded;
        }

#if ODIN_INSPECTOR || UNITY_EDITOR
        [Button("Generate GUID")]
#endif
#if UNITY_EDITOR
        [ContextMenu("Generate GUID")]
#endif
        private void GenerateGuid()
        {
            guid = Guid.NewGuid().ToString();
            Debug.Log($"Generated new GUID for {gameObject.name}: {guid}", this);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            // Automatically generate GUID if empty when component is added or reset
            if (string.IsNullOrEmpty(guid)) GenerateGuid();
        }
#endif
    }
}