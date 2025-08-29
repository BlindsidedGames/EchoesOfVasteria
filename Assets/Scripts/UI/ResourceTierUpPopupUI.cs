using System.Collections;
using System.Collections.Generic;
using Blindsided.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.UI
{
	public class ResourceTierUpPopupUI : MonoBehaviour
	{
		[Header("Popup Elements")]
		public GameObject popupObject;
		public Image resourceIconImage;
		public TMP_Text tierText;
		public SlicedFilledImage countdownBar;
		public Image tierBackgroundImage;

		[Header("Config")] 
		public float displaySeconds = 5f;
		public List<Sprite> tierBackgroundSprites;

		private ResourceManager resourceManager;
		private Coroutine countdownRoutine;

		private void Awake()
		{
			resourceManager = ResourceManager.Instance;
			if (popupObject != null)
				popupObject.SetActive(false);
		}

		private void OnEnable()
		{
			if (resourceManager == null)
				resourceManager = ResourceManager.Instance;
			if (resourceManager != null)
				resourceManager.OnResourceTierUpgraded += OnResourceTierUpgraded;
		}

		private void OnDisable()
		{
			if (resourceManager != null)
				resourceManager.OnResourceTierUpgraded -= OnResourceTierUpgraded;
			if (countdownRoutine != null)
			{
				StopCoroutine(countdownRoutine);
				countdownRoutine = null;
			}
		}

		private void OnResourceTierUpgraded(Resource resource, int newTier)
		{
			if (resource == null || newTier <= 1)
				return;

			var oldTier = Mathf.Max(1, newTier - 1);
			SetupPopup(resource, oldTier, newTier);
			// Flash when a resource tiers up
			FindFirstObjectByType<TaskbarFlasher>()?.FlashNow();
		}

		private void SetupPopup(Resource resource, int oldTier, int newTier)
		{
			if (resourceIconImage != null)
			{
				resourceIconImage.sprite = resource.icon;
				resourceIconImage.enabled = resource != null && resource.icon != null;
			}

			if (tierText != null)
				tierText.text = $"Tier {oldTier}<sprite=194>{newTier}";

			if (tierBackgroundImage != null)
			{
				Sprite bg = null;
				if (tierBackgroundSprites != null && tierBackgroundSprites.Count > 0)
				{
					var idx = Mathf.Clamp(newTier - 1, 0, tierBackgroundSprites.Count - 1);
					bg = tierBackgroundSprites[idx];
				}
				tierBackgroundImage.sprite = bg;
				tierBackgroundImage.enabled = bg != null;
			}

			if (popupObject != null)
				popupObject.SetActive(true);

			if (countdownRoutine != null)
				StopCoroutine(countdownRoutine);
			countdownRoutine = StartCoroutine(RunCountdown());
		}

		private IEnumerator RunCountdown()
		{
			var seconds = Mathf.Max(0.01f, displaySeconds);
			var timeLeft = seconds;
			if (countdownBar != null)
				countdownBar.fillAmount = 1f;
			while (timeLeft > 0f)
			{
				timeLeft -= Time.deltaTime;
				if (countdownBar != null)
					countdownBar.fillAmount = Mathf.Clamp01(timeLeft / seconds);
				yield return null;
			}
			if (popupObject != null)
				popupObject.SetActive(false);
			countdownRoutine = null;
		}
	}
}


