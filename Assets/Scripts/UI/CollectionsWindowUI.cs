using System.Collections.Generic;
using System.Linq;
using References.UI;
using TMPro;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Utilities;
using static Blindsided.Oracle;
using EventHandler = Blindsided.EventHandler;
using UnityEngine;

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
		private CauldronWindowUI cachedCauldronWindow;
		private CauldronManager cachedCauldronManager;

		private ResourceManager rm;
		[SerializeField] [Min(0.25f)] private float unlockCheckIntervalSeconds = 0.75f;
		private Coroutine unlockMonitorRoutine;
		private bool unlocksDirty;
		private int lastUnlocksHash;

		private void Awake()
		{
			cauldron ??= CauldronManager.Instance ?? FindFirstObjectByType<CauldronManager>();
			rm = ResourceManager.Instance ?? FindFirstObjectByType<ResourceManager>();
			cachedCauldronManager = cauldron ?? CauldronManager.Instance ?? FindFirstObjectByType<CauldronManager>();
			cachedCauldronWindow = FindFirstObjectByType<CauldronWindowUI>();
		}

		private void OnEnable()
		{
			Rebuild();
			if (cauldron != null)
			{
				cauldron.OnCardGained += OnCardGained;
				cauldron.OnTasteSessionStarted += OnTasteSessionStarted;
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
			}
			EventHandler.OnLoadData -= OnSaveOrLoad;
			if (unlockMonitorRoutine != null)
			{
				StopCoroutine(unlockMonitorRoutine);
				unlockMonitorRoutine = null;
			}
		}

		private void Rebuild()
		{
			if (parent == null || sectionPrefab == null || itemPrefab == null) return;
			// Guard in case save data not ready
			if (oracle == null || oracle.saveData == null) return;
			UIUtils.ClearChildren(parent);
			itemById.Clear();
			resourceById.Clear();

			var qm = TimelessEchoes.Quests.QuestManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Quests.QuestManager>();

			// Buffs section (only if any eligible)
			var eligibleBuffs = Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Buffs.BuffRecipe>("")
				.Where(b => b != null && (b.requiredQuest == null || (qm != null && qm.IsQuestCompleted(b.requiredQuest))))
				.ToList();
			if (eligibleBuffs.Count > 0)
			{
				var secB = Instantiate(sectionPrefab, parent);
				if (secB.titleText != null) secB.titleText.text = "Buffs";
				foreach (var buff in eligibleBuffs)
				{
					var ui = Instantiate(itemPrefab, secB.contentTransform);
					ui.nameText.text = buff.name;
					ui.iconImage.sprite = buff.buffIcon;
					var id = $"BUFF:{buff.name}";
					itemById[id] = ui;
					UpdateItemCount(id);
				}
			}

			// Alter-Echoes split into subcategories (create sections lazily only if they have items)
			var cm = CauldronManager.Instance ?? FindFirstObjectByType<CauldronManager>();
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
				UpdateItemCount(key);
			}

			// (Buffs were added first above)
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
				ui.selectionImage.enabled = false;
				ui.selectionImage.enabled = true; // simple flash
			}
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
			// Keep card name as plain resource name
			if (id.StartsWith("RES:"))
			{
				if (resourceById.TryGetValue(id, out var res) && res != null && ui.nameText != null)
					ui.nameText.text = res.name;
			}
			// Tier images (placeholder: compute tier index later when thresholds are available)
			if (ui.tierImage != null || ui.borderTierImage != null)
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
			}
			// Tier text: you’ll wire thresholds in a later pass; for now show raw count
		}

		private int ComputeTierForCount(string id, int count)
		{
			// Use CauldronManager's public helpers to compute tiers; avoids reflection and extra lookups
			cachedCauldronManager ??= CauldronManager.Instance ?? FindFirstObjectByType<CauldronManager>();
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
		}

		private void OnQuestHandin(string questId)
		{
			Rebuild();
			unlocksDirty = false;
			lastUnlocksHash = ComputeUnlocksHash();
		}
	}
}


