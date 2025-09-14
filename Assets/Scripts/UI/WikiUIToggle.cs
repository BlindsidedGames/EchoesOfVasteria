using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Toggles a target GameObject when the assigned button is pressed and
    ///     updates the state image with either the open or close sprite.
    /// </summary>
    public class WikiUIToggle : MonoBehaviour
    {
        [SerializeField] private GameObject toggleObject;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closeSprite;
        [SerializeField] private Image stateImage;
        [SerializeField] private Button toggleButton;
        public bool startClosed = true;
        public Transform questsParent;
        public TMP_Text categoryName;

        private void Awake()
        {
            if (toggleButton == null)
                toggleButton = GetComponent<Button>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(OnToggle);

            if (startClosed && toggleObject != null)
                toggleObject.SetActive(false);

            UpdateImage(toggleObject != null && toggleObject.activeSelf);
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(OnToggle);
        }

        private void OnToggle()
        {
            var newState = true;
            if (toggleObject != null)
            {
                newState = !toggleObject.activeSelf;
                toggleObject.SetActive(newState);
            }

            UpdateImage(newState);
        }

        // Explicitly set expanded/collapsed without relying on button click.
        public void SetExpanded(bool expanded)
        {
            if (toggleObject != null)
                toggleObject.SetActive(expanded);

            // Keep questsParent visibility in sync if provided separately.
            var content = questsParent != null ? questsParent.gameObject : null;
            if (content != null && content.activeSelf != expanded)
                content.SetActive(expanded);

            UpdateImage(expanded);
        }

        public bool IsExpanded
        {
            get
            {
                if (toggleObject != null) return toggleObject.activeSelf;
                if (questsParent != null) return questsParent.gameObject.activeSelf;
                return true;
            }
        }

        private void UpdateImage(bool active)
        {
            if (stateImage != null)
                stateImage.sprite = active ? closeSprite : openSprite;
        }
    }
}
