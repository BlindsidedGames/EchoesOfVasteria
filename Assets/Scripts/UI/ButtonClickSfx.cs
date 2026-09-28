using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TimelessEchoes.Audio;

namespace TimelessEchoes.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickSfx : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        [SerializeField] private Button button;

        private void Awake()
        {
            if (!enabled) return; // Retired presentation must not subscribe or build hidden UI.
            if (button == null)
                button = GetComponent<Button>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.interactable)
                return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;
            var audio = AudioManager.Instance;
            audio?.PlayUIButtonClick();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (button == null || !button.interactable)
                return;
            var audio = AudioManager.Instance;
            audio?.PlayUIButtonClick();
        }
    }
}


