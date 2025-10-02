using Blindsided.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    public class IntroScreenController : MonoBehaviour
    {
        private const string IntroCompleteKey = "TimelessEchoes.IntroScreenCompleted";

        [SerializeField] private GameObject introRoot;
        [SerializeField] private GameObject progressContainer;
        [SerializeField] private GameObject clickToCloseContainer;
        [SerializeField] private SlicedFilledImage progressFillImage;
        [SerializeField] private Button closeButton;
        [SerializeField] [Min(0f)] private float countdownDurationSeconds = 10f;

        private float countdownRemaining;
        private bool countdownActive;

        private void Awake()
        {
            if (introRoot == null)
                introRoot = gameObject;

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);

            if (HasCompletedIntro())
            {
                HideIntro();
                return;
            }

            BeginIntro();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        private void Update()
        {
            if (!countdownActive)
                return;

            countdownRemaining -= Time.unscaledDeltaTime;
            if (countdownRemaining <= 0f)
                CompleteCountdown();
            else
                UpdateProgressFill();
        }

        private void BeginIntro()
        {
            countdownRemaining = countdownDurationSeconds;
            countdownActive = countdownRemaining > 0f;

            if (introRoot != null)
                introRoot.SetActive(true);

            if (progressContainer != null)
                progressContainer.SetActive(true);

            if (clickToCloseContainer != null)
                clickToCloseContainer.SetActive(false);

            if (closeButton != null)
                closeButton.interactable = !countdownActive;

            UpdateProgressFill();

            if (!countdownActive)
                CompleteCountdown();
        }

        private void CompleteCountdown()
        {
            countdownActive = false;
            countdownRemaining = 0f;
            UpdateProgressFill();

            if (progressContainer != null)
                progressContainer.SetActive(false);

            if (clickToCloseContainer != null)
                clickToCloseContainer.SetActive(true);

            if (closeButton != null)
                closeButton.interactable = true;
        }

        private void UpdateProgressFill()
        {
            if (progressFillImage == null)
                return;

            if (countdownDurationSeconds <= 0f)
            {
                progressFillImage.fillAmount = 1f;
                return;
            }

            float normalized = 1f - Mathf.Clamp01(countdownRemaining / countdownDurationSeconds);
            progressFillImage.fillAmount = normalized;
        }

        private void OnCloseButtonClicked()
        {
            if (countdownActive)
                return;

            PlayerPrefs.SetInt(IntroCompleteKey, 1);
            PlayerPrefs.Save();
            HideIntro();
        }

        private void HideIntro()
        {
            countdownActive = false;

            if (introRoot != null)
                introRoot.SetActive(false);

            if (progressContainer != null)
                progressContainer.SetActive(false);

            if (clickToCloseContainer != null)
                clickToCloseContainer.SetActive(false);

            if (closeButton != null)
                closeButton.interactable = false;
        }

        private static bool HasCompletedIntro()
        {
            return PlayerPrefs.GetInt(IntroCompleteKey, 0) == 1;
        }
    }
}
