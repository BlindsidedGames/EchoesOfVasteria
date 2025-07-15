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
        [SerializeField] private bool startClosed = true;

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
            bool newState = true;
            if (toggleObject != null)
            {
                newState = !toggleObject.activeSelf;
                toggleObject.SetActive(newState);
            }

            UpdateImage(newState);
        }

        private void UpdateImage(bool active)
        {
            if (stateImage != null)
                stateImage.sprite = active ? closeSprite : openSprite;
        }
    }
}
