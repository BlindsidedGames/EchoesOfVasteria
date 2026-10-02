using System.Collections.Generic;
using System.Text;
using System.Linq;
using Blindsided.Utilities;
using References.UI;
using TMPro;
using TimelessEchoes.UI.Cauldron;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Upgrades.Cauldron;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;
using static Blindsided.Oracle;
using EventHandler = Blindsided.EventHandler;
using static TimelessEchoes.TELogger;
using UnityEngine.EventSystems;

namespace TimelessEchoes.UI
{
	/// <summary>
	/// Thin presenter that binds CauldronManager to prefabs.
	/// Assumes prefabs wired in the scene.
	/// </summary>
	public class CauldronWindowUI : MonoBehaviour
	{
		[SerializeField] private CauldronManager cauldron;
		[SerializeField] private CauldronConfig config;

		[Header("Mixing")]
		[SerializeField] private System.Collections.Generic.List<CauldronMixItemUIReferences> mixSlots = new(); // Authored starting slots; the adapter adds food slots as content grows.
		[SerializeField] private CauldronMixItemUIReferences slot1; // Retained serialized legacy display; hidden by direct conversion.
		[SerializeField] private CauldronMixItemUIReferences slot2; // Retained serialized legacy display; hidden by direct conversion.
		[SerializeField] private Button mixButton;
        [SerializeField] private TMP_Text predictedStewText;
        [SerializeField] private Image mixArrowImage;
        [SerializeField] private Sprite mixArrowGreenSprite;
        [SerializeField] private Sprite mixArrowRedSprite;

		[Header("Drinking")]
		[SerializeField] private CauldronDrinkingUIReferences drinking;
		// Layered pie slices: index 0 is background
		[SerializeField] private List<MPImageBasic> oddsPieSlices = new();

		[Header("Eva Progress")]
		[SerializeField] private SlicedFilledImage evaXpBar;
		[SerializeField] private TMP_Text evaLevelText;
		[SerializeField] private TMP_Text evaXpText;

		[Header("Session Stats")]
		[SerializeField] private TMP_Text statsText;
		private readonly StringBuilder _statsSb = new StringBuilder(512);
		private float[] _fractionsBuffer;

		[Header("Attention Indicator")]
		[Tooltip("Optional: object to enable when tasting finishes while the cauldron window is closed.")]
		[SerializeField] private GameObject cauldronAttentionObject;

		[Header("Weights Preview")]
		[SerializeField] private TMP_Text firstPercentText;
		[SerializeField] private TMP_Text spriteColText;
		[SerializeField] private TMP_Text nextPercentText;
		[SerializeField] private TMP_Text nameColText;

		[Header("Weights Tooltip")]
		[SerializeField] private Image weightsHoverImage;
		[SerializeField] private GameObject weightsHoverObject;

		[Header("Tier Sprites")]
		[SerializeField] private List<Sprite> tierSprites = new(); // 8 entries: index 0 used for unknown and tier 1
		[SerializeField] private List<Sprite> borderTierSprites = new(); // 8 entries matching tiers

		[Header("Presenters (Optional)")]
		[SerializeField] private CauldronPieChartPresenter pieChartPresenter;
		[SerializeField] private CauldronWeightsPresenter weightsPresenter;
		[SerializeField] private CauldronMixPresenter mixPresenter;

		public Sprite GetTierSprite(int tier)
		{
			var idx = Mathf.Clamp(tier <= 1 ? 0 : tier - 1, 0, tierSprites.Count - 1);
			return tierSprites.Count > 0 ? tierSprites[idx] : null;
		}

		public Sprite GetBorderTierSprite(int tier)
		{
			var idx = Mathf.Clamp(tier <= 1 ? 0 : tier - 1, 0, borderTierSprites.Count - 1);
			return borderTierSprites.Count > 0 ? borderTierSprites[idx] : null;
		}




		private LegacyCauldronConversion directConversion;
		private bool _tastingOccurredThisSession;
		[SerializeField] private ResourceManager rm;

		// Dirty flags for throttled updates (coalesce multiple events per frame)
		private bool _mixSlotsNeedRefresh;
		private bool _pieChartNeedsRefresh;
		private bool _weightsNeedRefresh;

		// Cached eligible foods list to avoid repeated LINQ allocations


		private bool initialized;
        private void Awake()
        {
            if (enabled) Initialize();
        }
        private void Initialize()
		{
            if (initialized) return;
            initialized = true;
			cauldron ??= CauldronManager.Instance;
			rm ??= ResourceManager.Instance;
			if (mixSlots == null || mixSlots.Count == 0)
				Log("Cauldron mix slot list is empty; assign up to 30 CauldronMixItemUIReferences in the Inspector.", TELogCategory.General, this);

			directConversion = new LegacyCauldronConversion(mixSlots, slot1, slot2, mixButton, drinking ? drinking.mixAll : null, predictedStewText, mixArrowImage);
			if (drinking != null)
			{
				if (drinking.tasteButton != null) drinking.tasteButton.onClick.AddListener(() => cauldron?.StartTasting());
				if (drinking.stopButton != null) drinking.stopButton.onClick.AddListener(() => cauldron?.StopTasting());

			}
			// Weights tooltip wiring (mirror Forge behavior)
			if (weightsHoverObject != null)
				weightsHoverObject.SetActive(false);
			if (weightsHoverImage != null)
			{
				var trigger = weightsHoverImage.GetComponent<EventTrigger>() ?? weightsHoverImage.gameObject.AddComponent<EventTrigger>();
				trigger.triggers.Clear();
				var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
				enter.callback.AddListener(_ => ShowWeightsTooltip());
				trigger.triggers.Add(enter);
				var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
				exit.callback.AddListener(_ => HideWeightsTooltip());
				trigger.triggers.Add(exit);
			}

		}

		private void OnEnable()
		{
            Initialize();
            if (rm != null) { rm.OnInventoryChanged -= OnInventoryChangedUi; rm.OnInventoryChanged += OnInventoryChangedUi; }
			// Invalidate cache on window open in case unlocks changed while closed

			RefreshMixSlots();
			RefreshDrinkingTexts();
			RefreshPieChart();
			RefreshWeightsText();
			_tastingOccurredThisSession = cauldron != null && cauldron.IsTasting;
			// Clear any prior attention indicator when the window is reopened
			if (cauldronAttentionObject != null)
				cauldronAttentionObject.SetActive(false);
			if (cauldron != null)
			{
				cauldron.OnStewChanged += RefreshDrinkingTexts;
				cauldron.OnWeightsChanged += RefreshPieChart;
				cauldron.OnWeightsChanged += RefreshWeightsText;
				cauldron.OnCardGained += OnCardGained;
				cauldron.OnTasteSessionStarted += OnTasteSessionStarted;
				cauldron.OnTasteSessionStopped += OnTasteSessionStopped;
				cauldron.OnSessionCardsChanged += OnSessionCardsChanged;
			}
			// Handle switching save files gracefully (only on load)
			EventHandler.OnLoadData += OnSaveOrLoad;
			// Subscribe to stats updates
			if (cauldron != null)
				cauldron.OnStatsChanged += OnStatsChanged;
		}

		private void OnDisable()
		{
			if (rm != null) rm.OnInventoryChanged -= OnInventoryChangedUi;
			if (cauldron != null)
			{
				cauldron.OnStewChanged -= RefreshDrinkingTexts;
				cauldron.OnWeightsChanged -= RefreshPieChart;
				cauldron.OnWeightsChanged -= RefreshWeightsText;
				cauldron.OnCardGained -= OnCardGained;
				cauldron.OnTasteSessionStarted -= OnTasteSessionStarted;
				cauldron.OnTasteSessionStopped -= OnTasteSessionStopped;
				cauldron.OnSessionCardsChanged -= OnSessionCardsChanged;
				cauldron.OnStatsChanged -= OnStatsChanged;

				// If window is closing while tasting is active, arm a one-shot listener
				// to enable the attention indicator when tasting naturally stops.
				if (cauldron.IsTasting)
				{
					void OneShotStopped()
					{
						cauldron.OnTasteSessionStopped -= OneShotStopped;
						if (!TimelessEchoes.UI.TownWindowManager.IsCauldronOpen)
						{
							if (cauldronAttentionObject != null)
								cauldronAttentionObject.SetActive(true);
							TimelessEchoes.UI.TownWindowManager.ShowCauldronAttention();
						}
						FindAnyObjectByType<TaskbarFlasher>()?.FlashNow();
					}
					cauldron.OnTasteSessionStopped += OneShotStopped;
				}
			}
			EventHandler.OnLoadData -= OnSaveOrLoad;
			if (weightsHoverObject != null)
				weightsHoverObject.SetActive(false);
			_tastingOccurredThisSession = false;
		}

        private void RefreshMixSlots() => directConversion?.Refresh();
        private void RefreshSelectedDisplaySlots() => directConversion?.Refresh();


		private void RefreshDrinkingTexts()
		{
			if (drinking == null || cauldron == null) return;
			// Guard against early calls before save is available
			if (oracle == null || oracle.saveData == null) return;
			if (drinking.stewRemainingText != null)
				drinking.stewRemainingText.text = $"Stew Remaining | {CalcUtils.FormatNumber(cauldron.Stew)}";
			// Eva XP bar & texts
			if (evaLevelText != null)
				evaLevelText.text = $"Eva | Level {cauldron.EvaLevel}";
			if (evaXpBar != null || evaXpText != null)
			{
				var lvl = cauldron.EvaLevel;
				var current = cauldron.EvaXp;
				// match CauldronManager's formula 50 + 10*(level-1)
				var needed = 50 + 10 * Mathf.Max(0, lvl - 1);
				if (evaXpBar != null)
					evaXpBar.fillAmount = needed > 0f ? Mathf.Clamp01((float)(current / needed)) : 0f;
				if (evaXpText != null)
					evaXpText.text = $"xp: {current:N0} / {needed:N0}";
			}
			UpdateTasteStopButtons();
		}

		private void OnSaveOrLoad()
		{
            directConversion?.Clear();
			// Invalidate cache when save data changes (unlocks may differ)

			RefreshMixSlots();
			RefreshDrinkingTexts();
			RefreshPieChart();
			RefreshWeightsText();
			// Reset session counter on data reload
			if (drinking != null && drinking.cardsGainedThisSessionText != null)
				drinking.cardsGainedThisSessionText.text = "Cards Gained | 0";
			// Refresh stats from persisted totals on load
			if (cauldron != null)
				OnStatsChanged(cauldron != null ? cauldron.GetType()
					.GetMethod("GetStatsSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
					?.Invoke(cauldron, null) as CauldronManager.TastingStats? ?? default : default);
		}

		private void OnInventoryChangedUi()
		{
			if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
			// Skip if cauldron window isn't actually visible to user
			if (!TownWindowManager.IsCauldronOpen) return;
			// Set dirty flags for LateUpdate to process (coalesces multiple events per frame)
			_mixSlotsNeedRefresh = true;
			_pieChartNeedsRefresh = true;
			_weightsNeedRefresh = true;
		}

		private void LateUpdate()
		{
			// Process dirty flags - coalesces multiple OnInventoryChanged events into single refresh per frame
			if (_mixSlotsNeedRefresh)
			{
				RefreshMixSlots();
				_mixSlotsNeedRefresh = false;
			}
			if (_pieChartNeedsRefresh)
			{
				RefreshPieChart();
				_pieChartNeedsRefresh = false;
			}
			if (_weightsNeedRefresh)
			{
				RefreshWeightsText();
				_weightsNeedRefresh = false;
			}
		}

		private void RefreshPieChart()
		{
			if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;

			// Delegate to presenter if available
			if (pieChartPresenter != null && cauldron != null && config != null)
			{
				var presenterWeights = cauldron.GetEffectiveWeightsAtLevel(cauldron.EvaLevel);
				pieChartPresenter.Refresh(presenterWeights, config);
				return;
			}

			// Keep existing implementation as fallback
			if (config == null || oddsPieSlices == null || oddsPieSlices.Count == 0) return;
			var lvl = cauldron != null ? cauldron.EvaLevel : 1;
			var eff = cauldron != null ? cauldron.GetEffectiveWeightsAtLevel(lvl) : default;
			// Prefer subcategory slices if configured; otherwise fall back to legacy single Alter-Echo slice
			var hasSub = (eff.wAEFarming
			       + eff.wAEFishing
			       + eff.wAEMining
			       + eff.wAEWoodcutting
			       + eff.wAELooting
			       + eff.wAECombat) > 0f;
			(UnityEngine.Color c, float w)[] weights;
			if (hasSub)
			{
				weights = new (Color c, float w)[]
				{
					(config.sliceNothing, eff.wNothing),
					(config.sliceAEFarming, eff.wAEFarming),
					(config.sliceAEFishing, eff.wAEFishing),
					(config.sliceAEMining, eff.wAEMining),
					(config.sliceAEWoodcutting, eff.wAEWoodcutting),
					(config.sliceAELooting, eff.wAELooting),
					(config.sliceAECombat, eff.wAECombat),
					(config.sliceBuff, eff.wBuff),
					(config.sliceLowest, eff.wLow),
					(config.sliceEvas, eff.wX2),
					(config.sliceVast, eff.wX10),
					(config.sliceInfinity, eff.wInfinity),
				};
			}
			else
			{
				weights = new (Color c, float w)[]
				{
					(config.sliceNothing, eff.wNothing),
					(config.sliceBuff, eff.wBuff),
					(config.sliceLowest, eff.wLow),
					(config.sliceEvas, eff.wX2),
					(config.sliceVast, eff.wX10),
					(config.sliceInfinity, eff.wInfinity),
				};
			}

			var total = 0f;
			for (var i = 0; i < weights.Length; i++) total += Mathf.Max(0f, weights[i].w);
			if (total <= 0f)
			{
				for (var i = 0; i < oddsPieSlices.Count; i++)
					if (oddsPieSlices[i] != null) oddsPieSlices[i].enabled = false;
				return;
			}

			// background is at index 0; overlays are 1..N using the same layering logic as forge
			var overlayCapacity = Mathf.Max(0, oddsPieSlices.Count - 1);
			var sliceCount = Mathf.Min(overlayCapacity, weights.Length);

			for (var i = 0; i < sliceCount; i++)
			{
				var img = oddsPieSlices[i + 1];
				if (img != null) img.transform.SetSiblingIndex(i + 1);
			}

			if (_fractionsBuffer == null || _fractionsBuffer.Length < sliceCount)
				_fractionsBuffer = new float[Mathf.NextPowerOfTwo(sliceCount)];
			var fractions = _fractionsBuffer;
			for (var i = 0; i < sliceCount; i++)
				fractions[i] = Mathf.Max(0f, weights[i].w) / total;

			var used = 0f;
			for (var layer = 0; layer < sliceCount; layer++)
			{
				var weightIndex = sliceCount - 1 - layer; // reverse like forge
				var img = oddsPieSlices[layer + 1];
				if (img == null) { used += fractions[weightIndex]; continue; }

				var fill = layer == 0 ? 1f : Mathf.Clamp01(1f - used);
				used += fractions[weightIndex];

				img.enabled = fill > 0f;
				img.type = Image.Type.Filled;
				img.fillMethod = Image.FillMethod.Radial360;
				img.fillOrigin = 2;
				img.fillClockwise = true;
				img.fillAmount = fill;
				img.color = weights[weightIndex].c;
				var rt = img.rectTransform;
				if (rt != null) rt.localEulerAngles = Vector3.zero;
			}

			for (var i = sliceCount + 1; i < oddsPieSlices.Count; i++)
				if (oddsPieSlices[i] != null) oddsPieSlices[i].enabled = false;
		}

		private void OnCardGained(string id, int amt)
		{
			// Collection highlights handled in Collections window
			// Collection highlights are handled by the Collection UI (subscribing to OnCardGained)
		}

		private void OnTasteSessionStarted()
		{
			_tastingOccurredThisSession = true;
			// Reset cards gained counter in UI
			if (drinking != null && drinking.cardsGainedThisSessionText != null)
				drinking.cardsGainedThisSessionText.text = "Cards Gained | 0";
			UpdateTasteStopButtons();
			// Ensure any previous flashes are cleared when tasting starts
			FindAnyObjectByType<TaskbarFlasher>()?.StopFlashing();
		}

		private void OnTasteSessionStopped()
		{
			UpdateTasteStopButtons();
			if (!_tastingOccurredThisSession) return;
			_tastingOccurredThisSession = false;
			// Flash when cauldron tasting stops
			FindAnyObjectByType<TaskbarFlasher>()?.FlashNow();
		}

		private void OnSessionCardsChanged(int total)
		{
			if (drinking != null && drinking.cardsGainedThisSessionText != null)
				drinking.cardsGainedThisSessionText.text = $"Cards Gained | {total}";
		}

		private void OnStatsChanged(CauldronManager.TastingStats stats)
		{
			if (statsText == null) return;

			// Use the extracted formatter
			TastingStatsFormatter.Format(_statsSb, stats, showSubcategories: true);
			statsText.SetText(_statsSb);
		}

		private void UpdateTasteStopButtons()
		{
			if (drinking == null || cauldron == null) return;
			var isTasting = cauldron.IsTasting;
			// Require enough stew to start tasting (default: 1 stew per roll)
			var cost = Mathf.Max(0.0001f, config != null ? config.stewPerRoll : 1f);
			var hasStewForOneRoll = cauldron.Stew >= cost;
			if (drinking.tasteButton != null) drinking.tasteButton.interactable = !isTasting && hasStewForOneRoll;
			if (drinking.stopButton != null) drinking.stopButton.interactable = isTasting;
		}

		private void RefreshWeightsText()
		{
			// Delegate to presenter if available
			if (weightsPresenter != null && cauldron != null && config != null)
			{
				weightsPresenter.Refresh(cauldron.EvaLevel, cauldron, config);
				return;
			}

			// Keep existing implementation as fallback
			if ((firstPercentText == null && spriteColText == null && nextPercentText == null && nameColText == null) || config == null || cauldron == null) return;
			var lvl = Mathf.Max(1, cauldron.EvaLevel);
			var next = lvl + 1;

			var cur = cauldron.GetEffectiveWeightsAtLevel(lvl);
			float wNothing = cur.wNothing;
			float wAEF = cur.wAEFarming;
			float wAEFi = cur.wAEFishing;
			float wAEM = cur.wAEMining;
			float wAEW = cur.wAEWoodcutting;
			float wAEL = cur.wAELooting;
			float wAEC = cur.wAECombat;
			float wBuff = cur.wBuff;
			float wLow = cur.wLow;
			float wX2 = cur.wX2;
			float wX10 = cur.wX10;
			float wInf = cur.wInfinity;

			float tCurrent = wNothing + wAEF + wAEFi + wAEM + wAEW + wAEL + wAEC + wBuff + wLow + wX2 + wX10 + wInf;
			if (tCurrent <= 0f)
			{
				if (firstPercentText != null) firstPercentText.text = string.Empty;
				if (spriteColText != null) spriteColText.text = string.Empty;
				if (nextPercentText != null) nextPercentText.text = string.Empty;
				if (nameColText != null) nameColText.text = string.Empty;
				return;
			}

			var nxt = cauldron.GetEffectiveWeightsAtLevel(next);
			float nNothing = nxt.wNothing;
			float nAEF = nxt.wAEFarming;
			float nAEFi = nxt.wAEFishing;
			float nAEM = nxt.wAEMining;
			float nAEW = nxt.wAEWoodcutting;
			float nAEL = nxt.wAELooting;
			float nAEC = nxt.wAECombat;
			float nBuff = nxt.wBuff;
			float nLow = nxt.wLow;
			float nX2 = nxt.wX2;
			float nX10 = nxt.wX10;
			float nInf = nxt.wInfinity;

			float tNext = nNothing + nAEF + nAEFi + nAEM + nAEW + nAEL + nAEC + nBuff + nLow + nX2 + nX10 + nInf;
			if (tNext <= 0f) tNext = 1f;

			string HeaderFirst() => "<b>Current</b>\n";
			string HeaderSprite() => "<sprite=9>\n";
			string HeaderNext() => "<b>Next</b>\n";
			string HeaderName() => "\n";
			string ColorHex(Color c) => ColorUtility.ToHtmlStringRGB(c);
			string FormatPct(float val) => $"{val:F2}%"; // fixed 2 decimals for alignment

			// Determine if AE subcategories are present
			bool hasAE = (wAEF + wAEFi + wAEM + wAEW + wAEL + wAEC) > 0f;

			var rows = new List<(string label, float cur, float nxt, Color color)>();
			rows.Add(("Nothing", wNothing, nNothing, config.sliceNothing));
			if (hasAE)
			{
				rows.Add(("Farming", wAEF, nAEF, config.sliceAEFarming));
				rows.Add(("Fishing", wAEFi, nAEFi, config.sliceAEFishing));
				rows.Add(("Mining", wAEM, nAEM, config.sliceAEMining));
				rows.Add(("Logging", wAEW, nAEW, config.sliceAEWoodcutting));
				rows.Add(("Looting", wAEL, nAEL, config.sliceAELooting));
				rows.Add(("Combat", wAEC, nAEC, config.sliceAECombat));
			}
			rows.Add(("Buffs", wBuff, nBuff, config.sliceBuff));
			rows.Add(("Lowest", wLow, nLow, config.sliceLowest));
			rows.Add(("Blessing", wX2, nX2, config.sliceEvas));
			rows.Add(("Surge", wX10, nX10, config.sliceVast));
			rows.Add(("Eternal", wInf, nInf, config.sliceInfinity));

			var colFirst = new System.Text.StringBuilder();
			var colSprite = new System.Text.StringBuilder();
			var colNext = new System.Text.StringBuilder();
			var colName = new System.Text.StringBuilder();
			colFirst.Append(HeaderFirst());
			colSprite.Append(HeaderSprite());
			colNext.Append(HeaderNext());
			colName.Append(HeaderName());

			for (int i = 0; i < rows.Count; i++)
			{
				var hex = ColorHex(rows[i].color);
				var cp = Mathf.Clamp01(rows[i].cur / tCurrent) * 100f;
				var np = Mathf.Clamp01(rows[i].nxt / tNext) * 100f;
				colFirst.Append(i == 0 ? "" : "\n");
				colFirst.Append($"<b>{FormatPct(cp)}</b>");
				colSprite.Append(i == 0 ? "" : "\n");
				colSprite.Append($"<sprite=9 color=#{hex}>");
				colNext.Append(i == 0 ? "" : "\n");
				colNext.Append(FormatPct(np));
				colName.Append(i == 0 ? "" : "\n");
				colName.Append($"• {rows[i].label}");
			}

			if (firstPercentText != null) firstPercentText.SetText(colFirst);
			if (spriteColText != null) spriteColText.SetText(colSprite);
			if (nextPercentText != null) nextPercentText.SetText(colNext);
			if (nameColText != null) nameColText.SetText(colName);
		}

		private void ShowWeightsTooltip()
		{
			RefreshWeightsText();
			if (weightsPresenter != null)
			{
				weightsPresenter.ShowTooltip();
				return;
			}

			// Existing fallback
			if (weightsHoverObject != null)
				weightsHoverObject.SetActive(true);
		}

		private void HideWeightsTooltip()
		{
			if (weightsPresenter != null)
			{
				weightsPresenter.HideTooltip();
				return;
			}

			// Existing fallback
			if (weightsHoverObject != null)
				weightsHoverObject.SetActive(false);
		}
	}
}



