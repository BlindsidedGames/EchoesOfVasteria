using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
	/// <summary>
	/// 	Builds and displays Forge stats into a single TMP_Text field.
	/// 	Refreshes only when visible (OnEnable) and on a throttled interval.
	/// 	Use with a WikiUIToggle controlling this GameObject; set it to start open in the inspector.
	/// </summary>
	public class ForgeStatsUIController : MonoBehaviour
	{
		[SerializeField] private TMP_Text statsText;
		[SerializeField] [Min(0.1f)] private float refreshIntervalSeconds = 0.75f;
		// Removed maxListEntries; always show all entries in lists

		private bool dirty = true;
		private Coroutine refreshRoutine;
		private readonly ForgeStatisticsPresentation presentation = new();

		private void Awake()
		{
            if (!enabled) return; // Retired presentation must not subscribe or build hidden UI.
			if (statsText == null)
				statsText = GetComponentInChildren<TMP_Text>(true);
			// Ensure TMP can render <sprite> tags for stat icons
			if (statsText != null)
				statsText.spriteAsset = StatIconLookup.GetSpriteAsset();
		}

		private void OnEnable()
		{
			dirty = true;
			Subscribe();
			refreshRoutine = StartCoroutine(RefreshLoop());
		}

		private void OnDisable()
		{
			Unsubscribe();
			if (refreshRoutine != null)
			{
				StopCoroutine(refreshRoutine);
				refreshRoutine = null;
			}
		}

		public void MarkDirty()
		{
			dirty = true;
		}

		private void Subscribe()
		{
			var svc = CraftingService.Instance;
			if (svc != null)
				svc.OnIvanXpChanged += OnIvanXpChanged;
		}

		private void Unsubscribe()
		{
			var svc = CraftingService.Instance;
			if (svc != null)
				svc.OnIvanXpChanged -= OnIvanXpChanged;
		}

		private void OnIvanXpChanged(int level, float current, float needed)
		{
			dirty = true;
		}

		private System.Collections.IEnumerator RefreshLoop()
		{
			var wait = new WaitForSecondsRealtime(Mathf.Max(0.1f, refreshIntervalSeconds));
			while (enabled && gameObject.activeInHierarchy)
			{
				if (dirty)
				{
					RefreshNow();
					dirty = false;
				}
				yield return wait;
			}
		}

		private void RefreshNow()
		{
			if (statsText == null)
				return;
			var o = Blindsided.Oracle.oracle;
			var forge = o != null ? o.saveData?.Forge : null;
			statsText.text = presentation.BuildStatsText(forge);
		}

	}
}
