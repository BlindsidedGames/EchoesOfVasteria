using System;
using System.Collections;
using MPUIKIT;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace References.UI
{
    public class BuffSlotUIReferences : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Button activateButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private Image autoCastImage;
        [SerializeField] private MPImage radialFillImage;
        [SerializeField] private MPImage cooldownRadialFillImage;
        [SerializeField] [Min(0f)] private float longPressDuration = 1f;

        private Coroutine longPressRoutine;
        private bool suppressNextActivate;

        public event Action<BuffSlotUIReferences> PointerEnter;
        public event Action<BuffSlotUIReferences> PointerExit;
        public event Action<BuffSlotUIReferences> AutoCastToggleRequested;

        public Button ActivateButton => activateButton;
        public Image IconImage => iconImage;
        public TMP_Text DurationText => durationText;
        public Image AutoCastImage => autoCastImage;
        public MPImage RadialFillImage => radialFillImage;
        public MPImage CooldownRadialFillImage => cooldownRadialFillImage;

        private void Awake()
        {
            if (radialFillImage != null)
                radialFillImage.StrokeWidth = 1f;
        }

        private void OnDisable()
        {
            CancelLongPress();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelLongPress();
            PointerExit?.Invoke(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (longPressDuration <= 0f)
                return;

            CancelLongPress();
            longPressRoutine = StartCoroutine(WaitForLongPress());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            CancelLongPress();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                AutoCastToggleRequested?.Invoke(this);
        }

        public bool ConsumePendingSuppression()
        {
            if (!suppressNextActivate)
                return false;

            suppressNextActivate = false;
            return true;
        }

        private IEnumerator WaitForLongPress()
        {
            var elapsed = 0f;
            while (elapsed < longPressDuration)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            longPressRoutine = null;
            suppressNextActivate = true;
            AutoCastToggleRequested?.Invoke(this);
        }

        private void CancelLongPress()
        {
            if (longPressRoutine == null)
                return;

            StopCoroutine(longPressRoutine);
            longPressRoutine = null;
        }
    }
}
