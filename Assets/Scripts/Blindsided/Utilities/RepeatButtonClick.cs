using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blindsided.Utilities
{
    /// <summary>
    /// Invokes the attached button's onClick event repeatedly while held.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RepeatButtonClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] public Button button;
        [SerializeField] public float holdDelay = 0.5f;
        [SerializeField] public float repeatInterval = 0.1f;

        private bool held;
        private float nextTime;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            held = false;
        }

        private void Update()
        {
            if (!held || button == null || !button.interactable)
                return;

            if (Time.unscaledTime >= nextTime)
            {
                button.onClick.Invoke();
                nextTime = Time.unscaledTime + repeatInterval;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.interactable)
                return;
            held = true;
            nextTime = Time.unscaledTime + holdDelay;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            held = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            held = false;
        }
    }
}
