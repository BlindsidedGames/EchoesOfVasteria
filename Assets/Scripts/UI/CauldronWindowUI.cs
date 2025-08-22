using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using References.UI;
using TMPro;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;
using static Blindsided.Oracle;
using EventHandler = Blindsided.EventHandler;

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
		[SerializeField] private Transform mixSlotsParent; // 6 children with CauldronMixItemUIReferences
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

		[Header("Weights Preview")]
		[SerializeField] private TMP_Text weightsText;

		[Header("Tier Sprites")] 
		[SerializeField] private List<Sprite> tierSprites = new(); // 8 entries: index 0 used for unknown and tier 1
		[SerializeField] private List<Sprite> borderTierSprites = new(); // 8 entries matching tiers

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

		private readonly List<CauldronMixItemUIReferences> mixSlots = new();


		private Resource selectedA;
		private Resource selectedB;
		private bool nextGreen = true;
		private ResourceManager rm;

		private void Awake()
		{
			cauldron ??= CauldronManager.Instance ?? FindFirstObjectByType<CauldronManager>();
			rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
			if (mixSlotsParent != null)
				foreach (Transform t in mixSlotsParent)
				{
					var slot = t.GetComponent<CauldronMixItemUIReferences>();
					if (slot != null) mixSlots.Add(slot);
				}
			if (rm != null) rm.OnInventoryChanged += RefreshMixSlots;
			if (mixButton != null) mixButton.onClick.AddListener(OnMixClicked);
			if (drinking != null)
			{
				if (drinking.tasteButton != null) drinking.tasteButton.onClick.AddListener(() => cauldron?.StartTasting());
				if (drinking.stopButton != null) drinking.stopButton.onClick.AddListener(() => cauldron?.StopTasting());
			}
		}

		private void OnEnable()
		{
			RefreshMixSlots();
			RefreshDrinkingTexts();
			RefreshPieChart();
			RefreshWeightsText();
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
			}
			EventHandler.OnLoadData -= OnSaveOrLoad;
		}

		private void RefreshMixSlots()
		{
			if (rm == null || mixSlots.Count == 0) return;
			var all = Blindsided.Utilities.AssetCache.GetAll<Resource>("")
				.Where(r => r != null && rm.GetAmount(r) > 0)
				.OrderByDescending(r => rm.GetAmount(r))
				.Take(6)
				.ToList();
			for (int i = 0; i < mixSlots.Count; i++)
			{
				var ui = mixSlots[i];
				Resource r = i < all.Count ? all[i] : null;
				if (ui == null) continue;
				if (r == null)
				{
					if (ui.iconImage != null) { ui.iconImage.enabled = false; ui.iconImage.sprite = null; }
					if (ui.countText != null) { ui.countText.text = ""; }
					if (ui.selectButton != null) ui.selectButton.interactable = false;
					if (ui.selectionImageGreen != null) ui.selectionImageGreen.enabled = false;
					if (ui.selectionImageWhite != null) ui.selectionImageWhite.enabled = false;
					continue;
				}
				if (ui.iconImage != null) { ui.iconImage.enabled = true; ui.iconImage.sprite = r.icon; }
				if (ui.countText != null) ui.countText.text = CalcUtils.FormatNumber(rm.GetAmount(r), true);
				if (ui.selectButton != null)
				{
					ui.selectButton.onClick.RemoveAllListeners();
					var isSelA = selectedA == r;
					var isSelB = selectedB == r;
					ui.selectButton.interactable = !(isSelA || isSelB);
					if (ui.selectButton.interactable)
						ui.selectButton.onClick.AddListener(() => ToggleSelection(r, ui));
				}
				{
					var isSelA = selectedA == r;
					var isSelB = selectedB == r;
					if (ui.selectionImageGreen != null) ui.selectionImageGreen.enabled = (isSelA && !nextGreen) || (isSelB && nextGreen);
					if (ui.selectionImageWhite != null) ui.selectionImageWhite.enabled = (isSelA && nextGreen) || (isSelB && !nextGreen);
				}
			}
			RefreshMixButton();
		}

		private void ToggleSelection(Resource r, CauldronMixItemUIReferences ui)
		{
			// Always distinct; selecting another replaces the oldest (alternating colors)
			if (selectedA == null && selectedB == null)
			{
				selectedA = r; nextGreen = false;
			}
			else if (selectedA != null && selectedB == null)
			{
				selectedB = r; nextGreen = true;
			}
			else
			{
				if (nextGreen)
				{
					selectedA = r; nextGreen = false;
				}
				else
				{
					selectedB = r; nextGreen = true;
				}
			}
			RefreshMixButton();
			// Force visuals
			foreach (var s in mixSlots)
			{
				var isA = s != null && rm != null && s.iconImage != null && selectedA != null && s.iconImage.sprite == selectedA.icon;
				var isB = s != null && rm != null && s.iconImage != null && selectedB != null && s.iconImage.sprite == selectedB.icon;
				if (s != null)
				{
					if (s.selectionImageGreen != null) s.selectionImageGreen.enabled = (isA && !nextGreen) || (isB && nextGreen);
					if (s.selectionImageWhite != null) s.selectionImageWhite.enabled = (isA && nextGreen) || (isB && !nextGreen);
					if (s.selectButton != null) s.selectButton.interactable = !(isA || isB);
				}
			}
		}

		private void RefreshMixButton()
		{
			var canMix = selectedA != null && selectedB != null && selectedA != selectedB && cauldron != null && cauldron.CanMix(selectedA, selectedB);
			if (mixButton != null) mixButton.interactable = canMix;

			// Update predicted stew text
			double predicted = 0;
			if (rm != null && selectedA != null && selectedB != null && selectedA != selectedB)
			{
				var amountA = rm.GetAmount(selectedA);
				var amountB = rm.GetAmount(selectedB);
				double points = amountA * selectedA.baseValue * selectedA.valueMultiplier + amountB * selectedB.baseValue * selectedB.valueMultiplier;
				predicted = points / 100.0;
			}
			if (predictedStewText != null)
				predictedStewText.text = CalcUtils.FormatNumber(predicted);

			// Arrow color
			if (mixArrowImage != null)
			{
				mixArrowImage.sprite = canMix ? mixArrowGreenSprite : mixArrowRedSprite;
				mixArrowImage.enabled = mixArrowImage.sprite != null;
			}
		}

		private void OnMixClicked()
		{
			if (cauldron == null || selectedA == null || selectedB == null) return;
			cauldron.MixMax(selectedA, selectedB);
			selectedA = selectedB = null;
			nextGreen = true;
			RefreshMixSlots();
			RefreshDrinkingTexts();
		}

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
					evaXpText.text = $"xp: {CalcUtils.FormatNumber(current)} / {CalcUtils.FormatNumber(needed)}";
			}
			UpdateTasteStopButtons();
		}

		private void OnSaveOrLoad()
		{
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

		private void RefreshPieChart()
		{
			if (config == null || oddsPieSlices == null || oddsPieSlices.Count == 0) return;
			var lvl = cauldron != null ? cauldron.EvaLevel : 1;
			// Prefer subcategory slices if configured; otherwise fall back to legacy single Alter-Echo slice
			var hasSub = (config.weightAEFarming.Evaluate(lvl)
				       + config.weightAEFishing.Evaluate(lvl)
				       + config.weightAEMining.Evaluate(lvl)
				       + config.weightAEWoodcutting.Evaluate(lvl)
				       + config.weightAELooting.Evaluate(lvl)
				       + config.weightAECombat.Evaluate(lvl)) > 0f;
			(UnityEngine.Color c, float w)[] weights;
			if (hasSub)
			{
				weights = new (Color c, float w)[]
				{
					(config.sliceNothing, config.weightNothing.Evaluate(lvl)),
					(config.sliceAEFarming, config.weightAEFarming.Evaluate(lvl)),
					(config.sliceAEFishing, config.weightAEFishing.Evaluate(lvl)),
					(config.sliceAEMining, config.weightAEMining.Evaluate(lvl)),
					(config.sliceAEWoodcutting, config.weightAEWoodcutting.Evaluate(lvl)),
					(config.sliceAELooting, config.weightAELooting.Evaluate(lvl)),
					(config.sliceAECombat, config.weightAECombat.Evaluate(lvl)),
					(config.sliceBuff, config.weightBuffCard.Evaluate(lvl)),
					(config.sliceLowest, config.weightLowestCountCard.Evaluate(lvl)),
					(config.sliceEvas, config.weightEvasBlessingX2.Evaluate(lvl)),
					(config.sliceVast, config.weightVastSurgeX10.Evaluate(lvl)),
				};
			}
			else
			{
				weights = new (Color c, float w)[]
				{
					(config.sliceNothing, config.weightNothing.Evaluate(lvl)),
					(config.sliceBuff, config.weightBuffCard.Evaluate(lvl)),
					(config.sliceLowest, config.weightLowestCountCard.Evaluate(lvl)),
					(config.sliceEvas, config.weightEvasBlessingX2.Evaluate(lvl)),
					(config.sliceVast, config.weightVastSurgeX10.Evaluate(lvl)),
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

			var fractions = new float[sliceCount];
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
			// Reset cards gained counter in UI
			if (drinking != null && drinking.cardsGainedThisSessionText != null)
				drinking.cardsGainedThisSessionText.text = "Cards Gained | 0";
			UpdateTasteStopButtons();
		}

		private void OnTasteSessionStopped()
		{
			UpdateTasteStopButtons();
		}

		private void OnSessionCardsChanged(int total)
		{
			if (drinking != null && drinking.cardsGainedThisSessionText != null)
				drinking.cardsGainedThisSessionText.text = $"Cards Gained | {total}";
		}

		private void OnStatsChanged(CauldronManager.TastingStats s)
		{
			if (statsText == null) return;
			// Use <b> headers and bullet character
			var hasSub = s.aeFarming + s.aeFishing + s.aeMining + s.aeWoodcutting + s.aeCombat > 0;
			if (!hasSub)
			{
				statsText.text =
					$"<b>Totals</b>\n" +
					$"• Tastings: {s.tastings}\n" +
					$"• Cards Gained: {s.cardsGained}\n" +
					$"<b>Roll Distribution</b>\n" +
					$"• Gained Nothing: {s.gainedNothing}\n" +
					$"• Alter-Echo: {s.alterEcho}\n" +
					$"• Buffs: {s.buffs}\n" +
					$"• Low Cards: {s.lowCards}\n" +
					$"• Eva's Blessing: {s.evasBlessing}\n" +
					$"• The Vast One's Surge: {s.vastSurge}";
			}
			else
			{
				statsText.text =
					$"<b>Totals</b>\n" +
					$"• Tastings: {s.tastings}\n" +
					$"• Cards Gained: {s.cardsGained}\n" +
					$"<b>Roll Distribution</b>\n" +
					$"• Gained Nothing: {s.gainedNothing}\n" +
					$"• AE - Farming: {s.aeFarming}\n" +
					$"• AE - Fishing: {s.aeFishing}\n" +
					$"• AE - Mining: {s.aeMining}\n" +
					$"• AE - Woodcutting: {s.aeWoodcutting}\n" +
					$"• AE - Looting: {s.aeLooting}\n" +
					$"• AE - Combat: {s.aeCombat}\n" +
					$"• Buffs: {s.buffs}\n" +
					$"• Low Cards: {s.lowCards}\n" +
					$"• Eva's Blessing: {s.evasBlessing}\n" +
					$"• The Vast One's Surge: {s.vastSurge}";
			}
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
			if (weightsText == null || config == null || cauldron == null) return;
			var lvl = Mathf.Max(1, cauldron.EvaLevel);
			var next = lvl + 1;

			float wNothing = config.weightNothing.Evaluate(lvl);
			float wAEF = config.weightAEFarming.Evaluate(lvl);
			float wAEFi = config.weightAEFishing.Evaluate(lvl);
			float wAEM = config.weightAEMining.Evaluate(lvl);
			float wAEW = config.weightAEWoodcutting.Evaluate(lvl);
			float wAEL = config.weightAELooting.Evaluate(lvl);
			float wAEC = config.weightAECombat.Evaluate(lvl);
			float wBuff = config.weightBuffCard.Evaluate(lvl);
			float wLow = config.weightLowestCountCard.Evaluate(lvl);
			float wX2 = config.weightEvasBlessingX2.Evaluate(lvl);
			float wX10 = config.weightVastSurgeX10.Evaluate(lvl);

			float tCurrent = wNothing + wAEF + wAEFi + wAEM + wAEW + wAEL + wAEC + wBuff + wLow + wX2 + wX10;
			if (tCurrent <= 0f) { weightsText.text = string.Empty; return; }

			float nNothing = config.weightNothing.Evaluate(next);
			float nAEF = config.weightAEFarming.Evaluate(next);
			float nAEFi = config.weightAEFishing.Evaluate(next);
			float nAEM = config.weightAEMining.Evaluate(next);
			float nAEW = config.weightAEWoodcutting.Evaluate(next);
			float nAEL = config.weightAELooting.Evaluate(next);
			float nAEC = config.weightAECombat.Evaluate(next);
			float nBuff = config.weightBuffCard.Evaluate(next);
			float nLow = config.weightLowestCountCard.Evaluate(next);
			float nX2 = config.weightEvasBlessingX2.Evaluate(next);
			float nX10 = config.weightVastSurgeX10.Evaluate(next);

			float tNext = nNothing + nAEF + nAEFi + nAEM + nAEW + nAEL + nAEC + nBuff + nLow + nX2 + nX10;
			if (tNext <= 0f) tNext = 1f;

			string Header() => "<b>Weights</b>\nCurrent<sprite=9 color=#4C4C4C>Next Level";
			string ColorHex(Color c) => ColorUtility.ToHtmlStringRGB(c);
			string Row(string label, float cur, float nxt, Color spriteColor)
			{
				var cp = Mathf.Clamp01(cur / tCurrent) * 100f;
				var np = Mathf.Clamp01(nxt / tNext) * 100f;
				var hex = ColorHex(spriteColor);
				return $"\n{cp:0.##}%<sprite=9 color=#{hex}>{np:0.##}% • {label}";
			}

			// Determine if AE subcategories are present
			bool hasAE = (wAEF + wAEFi + wAEM + wAEW + wAEL + wAEC) > 0f;

			var sb = new System.Text.StringBuilder();
			sb.Append(Header());
			sb.Append(Row("Nothing", wNothing, nNothing, config.sliceNothing));
			if (hasAE)
			{
				sb.Append(Row("AE - Farming", wAEF, nAEF, config.sliceAEFarming));
				sb.Append(Row("AE - Fishing", wAEFi, nAEFi, config.sliceAEFishing));
				sb.Append(Row("AE - Mining", wAEM, nAEM, config.sliceAEMining));
				sb.Append(Row("AE - Woodcutting", wAEW, nAEW, config.sliceAEWoodcutting));
				sb.Append(Row("AE - Looting", wAEL, nAEL, config.sliceAELooting));
				sb.Append(Row("AE - Combat", wAEC, nAEC, config.sliceAECombat));
			}
			sb.Append(Row("Buffs", wBuff, nBuff, config.sliceBuff));
			sb.Append(Row("Lowest", wLow, nLow, config.sliceLowest));
			sb.Append(Row("Eva's Blessing x2", wX2, nX2, config.sliceEvas));
			sb.Append(Row("Vast Surge x10", wX10, nX10, config.sliceVast));

			weightsText.text = sb.ToString();
		}
	}
}


