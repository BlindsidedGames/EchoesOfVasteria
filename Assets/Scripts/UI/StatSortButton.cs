using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Button component used for selecting a stat sorting mode.
    /// Displays different text objects depending on whether the button
    /// is interactable or not.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StatSortButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text enabledText;
        [SerializeField] private TMP_Text disabledText;

        private Button button;
        public Button Button => button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void SetLabel(string label)
        {
            if (enabledText != null) enabledText.text = label;
            if (disabledText != null) disabledText.text = label;
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
            if (enabledText != null) enabledText.gameObject.SetActive(interactable);
            if (disabledText != null) disabledText.gameObject.SetActive(!interactable);
        }
    }
}
