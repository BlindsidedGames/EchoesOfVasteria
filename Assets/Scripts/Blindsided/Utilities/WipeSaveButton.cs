using UnityEngine;
using UnityEngine.UI;
using static Blindsided.Oracle;

namespace Blindsided.Utilities
{
    /// <summary>
    /// Invokes <see cref="Oracle.WipeCloudData"/> when the attached button is clicked.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WipeSaveButton : MonoBehaviour
    {
        [SerializeField] private Button button;

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
            if (oracle != null)
                oracle.WipeCloudData();
        }
    }
}
