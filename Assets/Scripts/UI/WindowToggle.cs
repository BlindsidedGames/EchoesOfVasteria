using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Toggles a window's active state when the assigned button is pressed.
    /// </summary>
    public class WindowToggle : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject window;

        private void Awake()
        {
            if (toggleButton == null)
                toggleButton = GetComponent<Button>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleWindow);
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(ToggleWindow);
        }

        private void ToggleWindow()
        {
            if (window != null)
                window.SetActive(!window.activeSelf);
        }
    }
}
