using System.Collections.Generic;
using System.Linq;
using References.UI;
using TMPro;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Utilities;
using static Blindsided.Oracle;
using EventHandler = Blindsided.EventHandler;
using UnityEngine;
using static Blindsided.SaveData.StaticReferences;
using UnityEngine.EventSystems;

namespace TimelessEchoes.UI
{
	/// <summary>
	/// Builds and updates the Collections UI. Highlights items on card gains.
	/// </summary>
	public class CollectionsWindowUI : MonoBehaviour
	{
		[SerializeField] private CauldronManager cauldron;
		[SerializeField] private CollectionSectionUIReferences sectionPrefab;
		[SerializeField] private CollectionItemUIReferences itemPrefab;
		[SerializeField] private Transform parent;

		private readonly Dictionary<string, CollectionItemUIReferences> itemById = new();
		private readonly Dictionary<string, Resource> resourceById = new();
		private readonly Dictionary<string, TimelessEchoes.Buffs.BuffRecipe> buffById = new();
		private CauldronWindowUI cachedCauldronWindow;
		private CauldronManager cachedCauldronManager;
		// Track current AE sections and their member ids to compute section tier (min of items)
		private readonly Dictionary<CauldronManager.AEResourceGroup, CollectionSectionUIReferences> currentAESections = new();
		private readonly Dictionary<CauldronManager.AEResourceGroup, List<string>> sectionItemIdsByGroup = new();
		private CollectionSectionUIReferences currentBuffsSection;
		private static float lastAppliedDiscipleBonus; // retained for immediate session-only tracking; replaced by StaticReferences bonus field
		// Track last-applied tiers so we only update borders when they change
		private readonly Dictionary<CauldronManager.AEResourceGroup, int> lastSectionTier = new();
		private int lastBuffsGroupTier;
		// Coalesced targeted updates during tasting
		private readonly HashSet<CauldronManager.AEResourceGroup> dirtyGroups = new();
		private bool buffsSectionDirty;
		[SerializeField] [Min(0.05f)] private float sectionUpdateCoalesceSeconds = 0.2f;
		private Coroutine sectionUpdateRoutine;

		// Per-card highlight timers
		private readonly Dictionary<string, Coroutine> highlightTimeouts = new();
		[SerializeField] [Min(0.1f)] private float selectionHighlightDurationSeconds = 1f;

		private ResourceManager rm;
		[SerializeField] [Min(0.25f)] private float unlockCheckIntervalSeconds = 0.75f;
		private Coroutine unlockMonitorRoutine;
		private bool unlocksDirty;
		private int lastUnlocksHash;

		[Header("Card Tooltip")]
		[SerializeField] private GameObject cardTooltipObject;
		[SerializeField] private TMP_Text cardTooltipText;
		[SerializeField] private UnityEngine.UI.Image cardTooltipBorderImage;
        [SerializeField] [Min(0.05f)] private float tooltipUpdateIntervalSeconds = 0.2f; // 5 Hz
        private Coroutine tooltipUpdateRoutine;
        private string currentTooltipId;

		private void Awake()
		{
			cauldron ??= CauldronManager.Instance;
			rm = ResourceManager.Instance;
			cachedCauldronManager = cauldron ?? CauldronManager.Instance;
			cachedCauldronWindow = FindFirstObjectByType<CauldronWindowUI>();
			if (cardTooltipObject != null)
				cardTooltipObject.SetActive(false);
		}

		private void OnEnable()
		{
			Rebuild();
			if (cauldron != null)
			{
				cauldron.OnCardGained += OnCardGained;
				cauldron.OnTasteSessionStarted += OnTasteSessionStarted;
				cauldron.OnTasteSessionStopped += OnTasteSessionStoppedForCollections;
			}
			EventHandler.OnLoadData += OnSaveOrLoad;
			// Switch to throttled unlock monitoring; avoid full rebuild every inventory change
			if (rm != null) rm.OnInventoryChanged += OnInventoryChangedMark;
			EventHandler.OnQuestHandin += OnQuestHandin;
			if (unlockMonitorRoutine == null)
				unlockMonitorRoutine = StartCoroutine(UnlockMonitorLoop());
		}

		private void OnDisable()
		{
			if (rm != null) rm.OnInventoryChanged -= OnInventoryChangedMark;
			EventHandler.OnQuestHandin -= OnQuestHandin;
			if (cauldron != null)
			{
				cauldron.OnCardGained -= OnCardGained;
				cauldron.OnTasteSessionStarted -= OnTasteSessionStarted;
				cauldron.OnTasteSessionStopped -= OnTasteSessionStoppedForCollections;
			}
			EventHandler.OnLoadData -= OnSaveOrLoad;
			if (unlockMonitorRoutine != null)
			{
				StopCoroutine(unlockMonitorRoutine);
				unlockMonitorRoutine = null;
			}
			if (sectionUpdateRoutine != null)
			{
				StopCoroutine(sectionUpdateRoutine);
				sectionUpdateRoutine = null;
			}
			CancelAllHighlightTimers();
			HideCardTooltip();
		}

		private void Rebuild()
		{
			if (parent == null || sectionPrefab == null || itemPrefab == null) return;
			// Guard in case save data not ready
			if (oracle == null || oracle.saveData == null) return;
			CancelAllHighlightTimers();
			UIUtils.ClearChildren(parent);
			itemById.Clear();
			resourceById.Clear();
			buffById.Clear();
			currentAESections.Clear();
			sectionItemIdsByGroup.Clear();
			currentBuffsSection = null;
			lastSectionTier.Clear();
			lastBuffsGroupTier = 0;

			var qm = TimelessEchoes.Quests.QuestManager.Instance;

			// Buffs section (only if any eligible)
			var eligibleBuffs = Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Buffs.BuffRecipe>("")
				.Where(b => b != null && (b.requiredQuest == null || (qm != null && qm.IsQuestCompleted(b.requiredQuest))))
				.ToList();
			if (eligibleBuffs.Count > 0)
			{
				var secB = Instantiate(sectionPrefab, parent);
				if (secB.titleText != null) secB.titleText.text = "Buffs";
				currentBuffsSection = secB;
				foreach (var buff in eligibleBuffs)
				{
					var ui = Instantiate(itemPrefab, secB.contentTransform);
					ui.nameText.text = buff != null ? buff.GetDisplayName() : "";
					ui.iconImage.sprite = buff.buffIcon;
					var id = $"BUFF:{buff.name}";
					itemById[id] = ui;
					if (buff != null) buffById[id] = buff;
					UpdateItemCount(id);
				
				}
			}

			// Alter-Echoes split into subcategories (create sections lazily only if they have items)
			var cm = CauldronManager.Instance;
			var sections = new Dictionary<CauldronManager.AEResourceGroup, CollectionSectionUIReferences>();

			var allRes = Blindsided.Utilities.AssetCache.GetAll<Resource>("")
				.Where(r => r != null && !r.DisableAlterEcho && rm != null && rm.IsUnlocked(r))
				.OrderBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
				.ThenBy(r => r.name)
				.ToList();
			foreach (var res in allRes)
			{
				var grp = cm != null ? cm.GetResourceGroup(res) : CauldronManager.AEResourceGroup.Combat;
				if (!sections.TryGetValue(grp, out var sec) || sec == null)
				{
					sec = Instantiate(sectionPrefab, parent);
					if (sec.titleText != null)
						sec.titleText.text = grp switch
						{
							CauldronManager.AEResourceGroup.Farming => "Alter-Echoes — Farming",
							CauldronManager.AEResourceGroup.Fishing => "Alter-Echoes — Fishing",
							CauldronManager.AEResourceGroup.Mining => "Alter-Echoes — Mining",
							CauldronManager.AEResourceGroup.Woodcutting => "Alter-Echoes — Logging",
							CauldronManager.AEResourceGroup.Looting => "Alter-Echoes — Looting",
							_ => "Alter-Echoes — Combat"
						};
					sections[grp] = sec;
				}

				var ui = Instantiate(itemPrefab, sec.contentTransform);
				// Show plain resource name only
				ui.nameText.text = res.name;
				ui.iconImage.sprite = res.icon;
				var key = $"RES:{res.name}";
				itemById[key] = ui;
				resourceById[key] = res;
				if (!sectionItemIdsByGroup.TryGetValue(grp, out var list) || list == null)
				{
					list = new List<string>();
					sectionItemIdsByGroup[grp] = list;
				}
				list.Add(key);
				UpdateItemCount(key);
			}

			// (Buffs were added first above)
			// Persist current AE sections for later tier updates
			currentAESections.Clear();
			foreach (var kv in sections)
				if (kv.Value != null)
					currentAESections[kv.Key] = kv.Value;

			// Infinity (Eternal Boons) section appears only when active
			if (cm != null && cm.IsInfinityActive())
			{
				var secInf = Instantiate(sectionPrefab, parent);
				if (secInf.titleText != null) secInf.titleText.text = "Eternal Boons";
				foreach (var so in Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Upgrades.InfinityCauldronStatSO>("Infinity"))
				{
					if (so == null) continue;
					var ui = Instantiate(itemPrefab, secInf.contentTransform);
					ui.nameText.text = so.DisplayName;
					ui.iconImage.sprite = so.Icon;
					var key = $"INF:{so.Stat}";
					itemById[key] = ui;
					UpdateItemCount(key);
					// Tier visuals for Infinity: use Tier 7 colors by default
					var bgSprite7 = GetTierSpriteFromCauldron(7);
					var borderSprite7 = GetBorderTierSpriteFromCauldron(7);
					if (ui.tierImage != null) { ui.tierImage.sprite = bgSprite7; ui.tierImage.enabled = bgSprite7 != null; }
					if (ui.borderTierImage != null) { ui.borderTierImage.sprite = borderSprite7; ui.borderTierImage.enabled = borderSprite7 != null; }
					if (ui.tierFillImage != null) ui.tierFillImage.fillAmount = 0f;
				}
				// Section header visuals: use Tier 7 as well
				var secBg7 = GetTierSpriteFromCauldron(7);
				var secBorder7 = GetBorderTierSpriteFromCauldron(7);
				if (secInf.backgroundTierImage != null) { secInf.backgroundTierImage.sprite = secBg7; secInf.backgroundTierImage.enabled = secBg7 != null; }
				if (secInf.borderTierImage != null) { secInf.borderTierImage.sprite = secBorder7; secInf.borderTierImage.enabled = secBorder7 != null; }
				// Move Infinity section to top
				secInf.transform.SetSiblingIndex(0);
			}

			// Ensure fresh builds immediately reflect correct tiers (only-if-changed will avoid excess work)
			UpdateSectionTierVisuals();
			AttachHoverHandlers();
		}

		private void OnCardGained(string id, int amt)
		{
			if (!itemById.ContainsKey(id))
			{
				Rebuild();
				return;
			}
			UpdateItemCount(id);
			if (itemById.TryGetValue(id, out var ui) && ui != null && ui.selectionImage != null)
			{
				ui.selectionImage.enabled = true;
				RestartHighlightTimer(id, ui);
			}

			// Targeted, throttled section updates
			if (id.StartsWith("RES:"))
			{
				if (resourceById.TryGetValue(id, out var res) && res != null)
				{
					cachedCauldronManager ??= CauldronManager.Instance;
					if (cachedCauldronManager != null)
					{
						var grp = cachedCauldronManager.GetResourceGroup(res);
						MarkGroupDirty(grp);
					}
				}
			}
			else if (id.StartsWith("BUFF:"))
			{
				buffsSectionDirty = true;
			}
			StartOrRestartSectionUpdateRoutine();
		}

		private void OnTasteSessionStarted()
		{
			// Clear all highlights at the start of a new tasting session
			foreach (var kv in itemById)
			{
				var ui = kv.Value;
				if (ui != null && ui.selectionImage != null)
					ui.selectionImage.enabled = false;
			}
			CancelAllHighlightTimers();
		}

		private void OnInventoryChangedMark()
		{
			// Mark dirty and let the monitor loop coalesce updates
			unlocksDirty = true;
		}

		private System.Collections.IEnumerator UnlockMonitorLoop()
		{
			var wait = new WaitForSecondsRealtime(Mathf.Max(0.25f, unlockCheckIntervalSeconds));
			lastUnlocksHash = ComputeUnlocksHash();
			while (enabled && gameObject.activeInHierarchy)
			{
				if (unlocksDirty)
				{
					var h = ComputeUnlocksHash();
					if (h != lastUnlocksHash)
					{
						Rebuild();
						lastUnlocksHash = h;
					}
					unlocksDirty = false;
				}
				yield return wait;
			}
		}

		private int ComputeUnlocksHash()
		{
			if (rm == null) return 0;
			int hash = 17;
			// Hash all unlocked resources that appear in the collection (non-DisableAlterEcho)
			foreach (var r in Blindsided.Utilities.AssetCache.GetAll<Resource>(""))
			{
				if (r == null || r.DisableAlterEcho) continue;
				if (rm.IsUnlocked(r))
				{
					hash = hash * 31 + r.name.GetHashCode();
				}
			}
			return hash;
		}

		private void UpdateItemCount(string id)
		{
			if (!itemById.TryGetValue(id, out var ui) || ui == null) return;
			if (oracle == null || oracle.saveData == null) return;
			var dict = Blindsided.Oracle.oracle.saveData.CauldronCardCounts;
			var count = dict.TryGetValue(id, out var c) ? c : 0;
			if (ui.countText != null)
				ui.countText.text = count.ToString();

			// Infinity items: force Tier 7 visuals and skip normal tier computation
			if (id.StartsWith("INF:"))
			{
				var sprite7 = GetTierSpriteFromCauldron(7);
				var border7 = GetBorderTierSpriteFromCauldron(7);
				if (ui.tierImage != null) { ui.tierImage.sprite = sprite7; ui.tierImage.enabled = sprite7 != null; }
				if (ui.borderTierImage != null) { ui.borderTierImage.sprite = border7; ui.borderTierImage.enabled = border7 != null; }
				if (ui.tierFillImage != null) ui.tierFillImage.fillAmount = 0f;
				// no further processing for INF cards
				return;
			}
			// Keep card name as plain resource name
			if (id.StartsWith("RES:"))
			{
				if (resourceById.TryGetValue(id, out var res) && res != null && ui.nameText != null)
					ui.nameText.text = res.name;
			}
			// Tier visuals
			if (ui.tierImage != null || ui.borderTierImage != null || ui.tierFillImage != null)
			{
				var tier = ComputeTierForCount(id, count); // 1-8 (0/unknown maps to 1)
				var sprite = GetTierSpriteFromCauldron(tier);
				var borderSprite = GetBorderTierSpriteFromCauldron(tier);
				if (ui.tierImage != null)
				{
					ui.tierImage.sprite = sprite;
					ui.tierImage.enabled = sprite != null;
				}
				if (ui.borderTierImage != null)
				{
					ui.borderTierImage.sprite = borderSprite;
					ui.borderTierImage.enabled = borderSprite != null;
				}
				if (ui.tierFillImage != null)
				{
					cachedCauldronManager ??= CauldronManager.Instance;
					var fill = cachedCauldronManager != null ? cachedCauldronManager.GetTierFill01(id) : 0f;
					ui.tierFillImage.fillAmount = Mathf.Clamp01(fill);
					// If card is maxed (RES/BUFF), override count text to display "Max"
					if (ui.countText != null && (id.StartsWith("RES:") || id.StartsWith("BUFF:")))
					{
						if (fill >= 0.9999f)
							ui.countText.text = "Max";
					}
				}
			}
			// Tier text: you’ll wire thresholds in a later pass; for now show raw count
			// Progress bar label: show "Max" only when at max (RES/BUFF). Otherwise leave as-is.
			if (ui.tierText != null && (id.StartsWith("RES:") || id.StartsWith("BUFF:")))
			{
				cachedCauldronManager ??= CauldronManager.Instance;
				var fill = cachedCauldronManager != null ? cachedCauldronManager.GetTierFill01(id) : 0f;
				ui.tierText.text = fill >= 0.9999f ? "Max" : string.Empty;
			}
		}

		private int ComputeTierForCount(string id, int count)
		{
			// Use CauldronManager's public helpers to compute tiers; avoids reflection and extra lookups
			cachedCauldronManager ??= CauldronManager.Instance;
			if (cachedCauldronManager == null) return 1;
			if (id.StartsWith("RES:"))
			{
				var name = id.Substring(4);
				return Mathf.Max(1, cachedCauldronManager.GetResourceTier(name));
			}
			else if (id.StartsWith("BUFF:"))
			{
				var name = id.Substring(5);
				return Mathf.Max(1, cachedCauldronManager.GetBuffTier(name));
			}
			return 1;
		}

		private Sprite GetTierSpriteFromCauldron(int tier)
		{
			cachedCauldronWindow ??= FindFirstObjectByType<CauldronWindowUI>();
			return cachedCauldronWindow != null ? cachedCauldronWindow.GetTierSprite(tier) : null;
		}

		private Sprite GetBorderTierSpriteFromCauldron(int tier)
		{
			cachedCauldronWindow ??= FindFirstObjectByType<CauldronWindowUI>();
			return cachedCauldronWindow != null ? cachedCauldronWindow.GetBorderTierSprite(tier) : null;
		}

		private void OnSaveOrLoad()
		{
			Rebuild();
			unlocksDirty = false;
			lastUnlocksHash = ComputeUnlocksHash();
			// Update section tier visuals and disciple percent only on load
			UpdateSectionTiersAndDiscipleBonus();
		}

		private void OnQuestHandin(string questId)
		{
			Rebuild();
			unlocksDirty = false;
			lastUnlocksHash = ComputeUnlocksHash();
		}

		private void OnTasteSessionStoppedForCollections()
		{
			// Recompute section tier visuals and disciple percent only when tasting stops
			UpdateSectionTierVisuals();
			ApplyCollectionsDiscipleBonus();
		}

		private void UpdateSectionTiersAndDiscipleBonus()
		{
			// Backward-compatible wrapper: recompute visuals and then apply bonus
			UpdateSectionTierVisuals();
			ApplyCollectionsDiscipleBonus();
		}

		private void UpdateSectionTierVisuals()
		{
			cachedCauldronManager ??= CauldronManager.Instance;
			cachedCauldronWindow ??= FindFirstObjectByType<CauldronWindowUI>();

			foreach (var kv in currentAESections)
			{
				var group = kv.Key;
				var section = kv.Value;
				if (section == null) continue;
				if (!sectionItemIdsByGroup.TryGetValue(group, out var ids) || ids == null || ids.Count == 0) continue;

				int minTier = int.MaxValue;
				foreach (var id in ids)
				{
					var tier = ComputeTierForCount(id, 0);
					if (tier < minTier) minTier = tier;
				}
				if (minTier == int.MaxValue) minTier = 1;

				var prev = lastSectionTier.TryGetValue(group, out var p) ? p : 0;
				if (prev == minTier) continue; // unchanged; skip
				lastSectionTier[group] = minTier;

				var bg = section.backgroundTierImage;
				var border = section.borderTierImage;
				var bgSprite = GetTierSpriteFromCauldron(minTier);
				var borderSprite = GetBorderTierSpriteFromCauldron(minTier);
				if (bg != null)
				{
					bg.sprite = bgSprite;
					bg.enabled = bgSprite != null;
				}
				if (border != null)
				{
					border.sprite = borderSprite;
					border.enabled = borderSprite != null;
				}
			}

			// Buffs group visuals
			if (currentBuffsSection != null)
			{
				var grpTier = cachedCauldronManager != null ? cachedCauldronManager.GetBuffsGroupTier() : 0;
				if (grpTier != lastBuffsGroupTier)
				{
					lastBuffsGroupTier = grpTier;
					var bg = currentBuffsSection.backgroundTierImage;
					var border = currentBuffsSection.borderTierImage;
					if (grpTier > 0)
					{
						var bgSprite = GetTierSpriteFromCauldron(grpTier);
						var borderSprite = GetBorderTierSpriteFromCauldron(grpTier);
						if (bg != null)
						{
							bg.sprite = bgSprite;
							bg.enabled = bgSprite != null;
						}
						if (border != null)
						{
							border.sprite = borderSprite;
							border.enabled = borderSprite != null;
						}
					}
					else
					{
						if (bg != null) bg.enabled = false;
						if (border != null) border.enabled = false;
					}
				}
			}
		}

		private void ApplyCollectionsDiscipleBonus()
		{
			cachedCauldronManager ??= CauldronManager.Instance;
			int totalCompletedTiers = 0;
			foreach (var kv in currentAESections)
			{
				var group = kv.Key;
				if (!sectionItemIdsByGroup.TryGetValue(group, out var ids) || ids == null || ids.Count == 0) continue;
				int minTier = int.MaxValue;
				foreach (var id in ids)
				{
					var tier = ComputeTierForCount(id, 0);
					if (tier < minTier) minTier = tier;
				}
				if (minTier == int.MaxValue) minTier = 1;
				totalCompletedTiers += Mathf.Max(1, minTier);
			}

			var newBonus = 0.001f * Mathf.Max(0, totalCompletedTiers);
			DisciplePercentCollectionsBonus = newBonus;
			lastAppliedDiscipleBonus = newBonus;
			TimelessEchoes.NpcGeneration.DiscipleGenerationManager.Instance?.RefreshRates();
		}

		private void MarkGroupDirty(CauldronManager.AEResourceGroup grp)
		{
			if (!currentAESections.ContainsKey(grp)) return;
			dirtyGroups.Add(grp);
		}

		private void StartOrRestartSectionUpdateRoutine()
		{
			if (sectionUpdateRoutine != null) return;
			sectionUpdateRoutine = StartCoroutine(SectionUpdateCoalesce());
		}

		private System.Collections.IEnumerator SectionUpdateCoalesce()
		{
			var wait = new WaitForSecondsRealtime(Mathf.Max(0.05f, sectionUpdateCoalesceSeconds));
			yield return wait;
			FlushDirtySections();
			sectionUpdateRoutine = null;
		}

		private void FlushDirtySections()
		{
			cachedCauldronManager ??= CauldronManager.Instance;
			cachedCauldronWindow ??= FindFirstObjectByType<CauldronWindowUI>();
			// Update AE groups
			foreach (var grp in dirtyGroups)
			{
				if (!sectionItemIdsByGroup.TryGetValue(grp, out var ids) || ids == null || ids.Count == 0) continue;
				int minTier = int.MaxValue;
				foreach (var id in ids)
				{
					var tier = ComputeTierForCount(id, 0);
					if (tier < minTier) minTier = tier;
				}
				if (minTier == int.MaxValue) minTier = 1;
				var prev = lastSectionTier.TryGetValue(grp, out var p) ? p : 0;
				if (prev != minTier)
				{
					lastSectionTier[grp] = minTier;
					if (currentAESections.TryGetValue(grp, out var sec) && sec != null)
					{
						var bg = sec.backgroundTierImage;
						var border = sec.borderTierImage;
						var bgSprite = GetTierSpriteFromCauldron(minTier);
						var borderSprite = GetBorderTierSpriteFromCauldron(minTier);
						if (bg != null) { bg.sprite = bgSprite; bg.enabled = bgSprite != null; }
						if (border != null) { border.sprite = borderSprite; border.enabled = borderSprite != null; }
					}
				}
			}
			dirtyGroups.Clear();

			// Update Buffs section if needed
			if (buffsSectionDirty && currentBuffsSection != null)
			{
				var grpTier = cachedCauldronManager != null ? cachedCauldronManager.GetBuffsGroupTier() : 0;
				if (grpTier != lastBuffsGroupTier)
				{
					lastBuffsGroupTier = grpTier;
					var bg = currentBuffsSection.backgroundTierImage;
					var border = currentBuffsSection.borderTierImage;
					if (grpTier > 0)
					{
						var bgSprite = GetTierSpriteFromCauldron(grpTier);
						var borderSprite = GetBorderTierSpriteFromCauldron(grpTier);
						if (bg != null) { bg.sprite = bgSprite; bg.enabled = bgSprite != null; }
						if (border != null) { border.sprite = borderSprite; border.enabled = borderSprite != null; }
					}
					else
					{
						if (bg != null) bg.enabled = false;
						if (border != null) border.enabled = false;
					}
				}
				buffsSectionDirty = false;
			}
		}

		private void RestartHighlightTimer(string id, CollectionItemUIReferences ui)
		{
			if (string.IsNullOrEmpty(id) || ui == null || ui.selectionImage == null) return;
			if (highlightTimeouts.TryGetValue(id, out var co) && co != null)
			{
				StopCoroutine(co);
			}
			var routine = StartCoroutine(SelectionTimeoutRoutine(id, ui));
			highlightTimeouts[id] = routine;
		}

		private System.Collections.IEnumerator SelectionTimeoutRoutine(string id, CollectionItemUIReferences ui)
		{
			var wait = new WaitForSecondsRealtime(Mathf.Max(0.1f, selectionHighlightDurationSeconds));
			yield return wait;
			// UI could have been rebuilt; verify still valid
			if (ui != null && ui.selectionImage != null)
				ui.selectionImage.enabled = false;
			highlightTimeouts.Remove(id);
		}

		private void CancelAllHighlightTimers()
		{
			if (highlightTimeouts.Count == 0) return;
			foreach (var kv in highlightTimeouts)
			{
				if (kv.Value != null)
					StopCoroutine(kv.Value);
			}
			highlightTimeouts.Clear();
		}

		private void AttachHoverHandlers()
		{
			if (itemById == null || itemById.Count == 0) return;
			foreach (var kv in itemById)
			{
				SetupHover(kv.Value, kv.Key);
			}
		}

		private void SetupHover(CollectionItemUIReferences ui, string id)
		{
			if (ui == null) return;
			var target = ui.tierImage != null ? ui.tierImage.gameObject : ui.gameObject;
			var trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
			if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
			trigger.triggers.Clear();

			var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
			enter.callback.AddListener(_ => ShowCardTooltip(id));
			trigger.triggers.Add(enter);

			var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
			exit.callback.AddListener(_ => HideCardTooltip());
			trigger.triggers.Add(exit);
		}

		private void ShowCardTooltip(string id)
		{
			if (string.IsNullOrEmpty(id) || cardTooltipObject == null || cardTooltipText == null)
				return;

			int cardTier;
			bool isInfinity;
			var text = BuildTooltipText(id, out cardTier, out isInfinity);

			// Apply immediately for instant feedback
			cardTooltipText.text = text;
			ApplyTooltipBorder(cardTier, isInfinity);
			cardTooltipObject.SetActive(true);

			// Start periodic tooltip updates while visible (5 Hz)
			currentTooltipId = id;
			if (tooltipUpdateRoutine != null)
			{
				StopCoroutine(tooltipUpdateRoutine);
				tooltipUpdateRoutine = null;
			}
			tooltipUpdateRoutine = StartCoroutine(TooltipUpdateLoop());
		}

		private System.Collections.IEnumerator TooltipUpdateLoop()
		{
			var wait = new WaitForSecondsRealtime(Mathf.Max(0.05f, tooltipUpdateIntervalSeconds));
			while (cardTooltipObject != null && cardTooltipObject.activeSelf && !string.IsNullOrEmpty(currentTooltipId))
			{
				// Rebuild the tooltip contents for the current card id
				// Reuse the same method to avoid duplication
				ReapplyCardTooltip(currentTooltipId);
				yield return wait;
			}
			tooltipUpdateRoutine = null;
		}

		private void ReapplyCardTooltip(string id)
		{
			if (string.IsNullOrEmpty(id) || cardTooltipObject == null || cardTooltipText == null)
				return;

			int cardTier;
			bool isInfinity;
			var text = BuildTooltipText(id, out cardTier, out isInfinity);
			cardTooltipText.text = text;
			ApplyTooltipBorder(cardTier, isInfinity);
		}

		// Builds the tooltip text and returns cardTier and whether it's an Infinity card
		private string BuildTooltipText(string id, out int cardTier, out bool isInfinity)
		{
			cachedCauldronManager ??= CauldronManager.Instance;
			cachedCauldronWindow ??= FindFirstObjectByType<CauldronWindowUI>();

			string sectionName;
			int sectionTier;
			string cardName;
			cardTier = 1;
			string sectionEffect;
			string cardEffect;
			isInfinity = false;

			if (id.StartsWith("RES:"))
			{
				cardName = id.Substring(4);
				cardTier = Mathf.Max(1, cachedCauldronManager != null ? cachedCauldronManager.GetResourceTier(cardName) : 1);
				var grp = CauldronManager.AEResourceGroup.Combat;
				if (resourceById.TryGetValue(id, out var res) && res != null && cachedCauldronManager != null)
					grp = cachedCauldronManager.GetResourceGroup(res);
				sectionName = FormatGroupName(grp);
				sectionTier = GetSectionTier(grp);
				var sectPct = sectionTier * 0.1f;
				sectionEffect = $"Collections Bonus: +{FormatPercentNoTrailingZero(sectPct)}% Disciple Rate";
				var mult = cachedCauldronManager != null ? cachedCauldronManager.GetResourceAlterEchoMultiplier(cardName) : 1f;
				var pct = Mathf.Max(0f, (mult - 1f) * 100f);
				cardEffect = $"+{pct:N0}% Alter-Echo Power";
			}
			else if (id.StartsWith("BUFF:"))
			{
				var assetName = id.Substring(5);
				cardName = assetName;
				if (buffById.TryGetValue(id, out var buff) && buff != null)
					cardName = buff.GetDisplayName();
				cardTier = Mathf.Max(1, cachedCauldronManager != null ? cachedCauldronManager.GetBuffTier(assetName) : 1);
				sectionName = "Buffs";
				sectionTier = cachedCauldronManager != null ? Mathf.Max(0, cachedCauldronManager.GetBuffsGroupTier()) : 0;
				var sectPct = sectionTier * 2.5f;
				sectionEffect = $"Global Buff Power: +{FormatPercentNoTrailingZero(sectPct)}%";
				var cdr = cachedCauldronManager != null ? Mathf.Max(0f, cachedCauldronManager.GetBuffCooldownReductionPercent(assetName)) : 0f;
				var groupBonus = sectionTier * 2.5f;
				var totalPower = cachedCauldronManager != null ? Mathf.Max(0f, cachedCauldronManager.GetBuffPowerPercent(assetName)) : 0f;
				var perBuffOnly = Mathf.Max(0f, totalPower - groupBonus);
				cardEffect = $"Cooldown: -{cdr:N0}% | Power: +{perBuffOnly:N0}%";
			}
			else if (id.StartsWith("INF:"))
			{
				isInfinity = true;
				var mappingStr = id.Substring(4);
				sectionName = "Eternal Boon Formula";
				sectionTier = 0;
				sectionEffect = string.Empty;
				cardName = mappingStr;
				if (System.Enum.TryParse<TimelessEchoes.Gear.HeroStatMapping>(mappingStr, out var mapping))
				{
					bool isPct = false;
					var val = cachedCauldronManager != null ? cachedCauldronManager.GetInfinityValueFor(mapping, out isPct) : 0f;
					// Resolve SO for display name and decimal places
					var so = Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Upgrades.InfinityCauldronStatSO>("Infinity")
						?.FirstOrDefault(s => s != null && s.Stat == mapping);
					var display = so != null ? so.DisplayName : mapping.ToString();
					cardName = display;
					var dp = so != null ? Mathf.Clamp(so.DecimalPlaces, 0, 6) : 0;
					string fmt = "N" + dp;
					var valStr = val.ToString(fmt);
					cardEffect = isPct ? $"+{valStr}% {display}" : $"+{valStr} {display}";
					// Show formula details for Infinity on the section line
					int n = 0;
					var key = $"INF:{mapping}";
					if (oracle != null && oracle.saveData != null && oracle.saveData.CauldronCardCounts != null)
						oracle.saveData.CauldronCardCounts.TryGetValue(key, out n);
					sectionEffect = $"{n:N0} ^ {(so != null ? so.Exponent : 1f)} = {val.ToString(fmt)}";
				}
				else cardEffect = string.Empty;
			}
			else
			{
				// Unknown id format
				isInfinity = false;
				return string.Empty;
			}

			var sb = new System.Text.StringBuilder(128);
			// Card block first
			sb.Append("<b>"); sb.Append(cardName); sb.Append("</b>");
			if (!isInfinity) { sb.Append(" | Tier "); sb.Append(cardTier); }
			sb.Append('\n');
			sb.Append("Effect: "); sb.Append(cardEffect);
			// Half-height spacer between parts
			sb.Append("<line-height=50%>\n</line-height>\n");
			// Section block below card
			sb.Append("<b>"); sb.Append(sectionName); sb.Append("</b>");
			if (!isInfinity) { sb.Append(" | Tier "); sb.Append(sectionTier); }
			sb.Append('\n');
			if (isInfinity)
			{
				// Show only the formula without the "Effect:" prefix
				sb.Append(sectionEffect);
			}
			else
			{
				sb.Append("Effect: "); sb.Append(sectionEffect);
			}

			return sb.ToString();
		}

		private void ApplyTooltipBorder(int cardTier, bool isInfinity)
		{
			if (cardTooltipBorderImage == null) return;
			cachedCauldronWindow ??= FindFirstObjectByType<CauldronWindowUI>();
			if (cachedCauldronWindow == null) return;
			var borderSprite = isInfinity ? GetBorderTierSpriteFromCauldron(7) : GetBorderTierSpriteFromCauldron(cardTier);
			cardTooltipBorderImage.sprite = borderSprite;
			cardTooltipBorderImage.enabled = borderSprite != null;
		}

		private void HideCardTooltip()
		{
			if (tooltipUpdateRoutine != null)
			{
				StopCoroutine(tooltipUpdateRoutine);
				tooltipUpdateRoutine = null;
			}
			currentTooltipId = null;
			if (cardTooltipObject != null)
				cardTooltipObject.SetActive(false);
		}

		private int GetSectionTier(CauldronManager.AEResourceGroup grp)
		{
			if (lastSectionTier.TryGetValue(grp, out var cached) && cached > 0)
				return cached;
			if (!sectionItemIdsByGroup.TryGetValue(grp, out var ids) || ids == null || ids.Count == 0)
				return 1;
			int minTier = int.MaxValue;
			foreach (var sid in ids)
			{
				var t = ComputeTierForCount(sid, 0);
				if (t < minTier) minTier = t;
			}
			return minTier == int.MaxValue ? 1 : Mathf.Max(1, minTier);
		}

		private string FormatGroupName(CauldronManager.AEResourceGroup grp)
		{
			return grp switch
			{
				CauldronManager.AEResourceGroup.Farming => "Farming",
				CauldronManager.AEResourceGroup.Fishing => "Fishing",
				CauldronManager.AEResourceGroup.Mining => "Mining",
				CauldronManager.AEResourceGroup.Woodcutting => "Logging",
				CauldronManager.AEResourceGroup.Looting => "Looting",
				_ => "Combat"
			};
		}

		private string FormatPercentNoTrailingZero(float value)
		{
			// Round to one decimal first, then hide .0 if present
			var rounded1 = Mathf.Round(value * 10f) / 10f;
			var whole = Mathf.Round(rounded1);
			bool hasFraction = Mathf.Abs(rounded1 - whole) > 0.0001f;
			return hasFraction ? $"{rounded1:N1}" : $"{whole:N0}";
		}
	}
}
