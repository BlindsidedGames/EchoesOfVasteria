using UnityEngine;
using UnityEngine.UI;

namespace Blindsided.Utilities
{
    /// <summary>
    /// Opens a URL when the attached button is clicked.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OpenUrlButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private string url;

        private UnityEngine.Events.UnityAction clickAction;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            clickAction = OnButtonClicked;
            button.onClick.AddListener(clickAction);
        }

        private void OnDestroy()
        {
            if (button != null && clickAction != null)
                button.onClick.RemoveListener(clickAction);
        }

        private void OnButtonClicked()
        {
            if (!string.IsNullOrEmpty(url))
                Application.OpenURL(url);
        }
    }
}
