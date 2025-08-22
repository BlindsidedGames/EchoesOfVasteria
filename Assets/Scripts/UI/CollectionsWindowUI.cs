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

		private void Awake()
		{
			cauldron ??= CauldronManager.Instance ?? FindFirstObjectByType<CauldronManager>();
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
		}

		private void OnDisable()
		{
			if (cauldron != null)
			{
				cauldron.OnCardGained -= OnCardGained;
				cauldron.OnTasteSessionStarted -= OnTasteSessionStarted;
			}
			EventHandler.OnLoadData -= OnSaveOrLoad;
		}

		private void Rebuild()
		{
			if (parent == null || sectionPrefab == null || itemPrefab == null) return;
			// Guard in case save data not ready
			if (oracle == null || oracle.saveData == null) return;
			UIUtils.ClearChildren(parent);
			itemById.Clear();

			// Buffs section first
			var secB = Instantiate(sectionPrefab, parent);
			if (secB.titleText != null) secB.titleText.text = "Buffs";
			foreach (var buff in Blindsided.Utilities.AssetCache.GetAll<TimelessEchoes.Buffs.BuffRecipe>(""))
			{
				if (buff == null) continue;
				var ui = Instantiate(itemPrefab, secB.contentTransform);
				ui.nameText.text = buff.name;
				ui.iconImage.sprite = buff.buffIcon;
				var id = $"BUFF:{buff.name}";
				itemById[id] = ui;
				UpdateItemCount(id);
			}

			// Alter-Echoes split into subcategories
			var cm = CauldronManager.Instance ?? FindFirstObjectByType<CauldronManager>();
			var sections = new Dictionary<CauldronManager.AEResourceGroup, CollectionSectionUIReferences>();
			// Create sections in desired order
			var orderedGroups = new[]
			{
				CauldronManager.AEResourceGroup.Farming,
				CauldronManager.AEResourceGroup.Fishing,
				CauldronManager.AEResourceGroup.Mining,
				CauldronManager.AEResourceGroup.Woodcutting,
				CauldronManager.AEResourceGroup.Looting,
				CauldronManager.AEResourceGroup.Combat
			};
			foreach (var g in orderedGroups)
			{
				var sec = Instantiate(sectionPrefab, parent);
				if (sec.titleText != null)
					sec.titleText.text = g switch
					{
						CauldronManager.AEResourceGroup.Farming => "Alter-Echoes — Farming",
						CauldronManager.AEResourceGroup.Fishing => "Alter-Echoes — Fishing",
						CauldronManager.AEResourceGroup.Mining => "Alter-Echoes — Mining",
						CauldronManager.AEResourceGroup.Woodcutting => "Alter-Echoes — Logging",
						CauldronManager.AEResourceGroup.Looting => "Alter-Echoes — Looting",
						_ => "Alter-Echoes — Combat"
					};
				sections[g] = sec;
			}

			var allRes = Blindsided.Utilities.AssetCache.GetAll<Resource>("")
				.Where(r => r != null && !r.DisableAlterEcho)
				.OrderBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
				.ThenBy(r => r.name)
				.ToList();
			foreach (var res in allRes)
			{
				var grp = cm != null ? cm.GetResourceGroup(res) : CauldronManager.AEResourceGroup.Combat;
				if (!sections.TryGetValue(grp, out var sec) || sec == null) continue;
				var ui = Instantiate(itemPrefab, sec.contentTransform);
				// Show plain resource name only
				ui.nameText.text = res.name;
				ui.iconImage.sprite = res.UnknownIcon; // default to unknown
				var key = $"RES:{res.name}";
				itemById[key] = ui;
				UpdateItemCount(key);
			}

			// (Buffs were added first above)
		}

		private void OnCardGained(string id, int amt)
		{
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

		private void UpdateItemCount(string id)
		{
			if (!itemById.TryGetValue(id, out var ui) || ui == null) return;
			if (oracle == null || oracle.saveData == null) return;
			var dict = Blindsided.Oracle.oracle.saveData.CauldronCardCounts;
			var count = dict.TryGetValue(id, out var c) ? c : 0;
			if (ui.countText != null)
				ui.countText.text = count.ToString();
			// Icon shows Unknown sprite for resources with 0 copies
			if (id.StartsWith("RES:"))
			{
				var resName = id.Substring(4);
				var res = Blindsided.Utilities.AssetCache.GetAll<Resource>("").FirstOrDefault(r => r != null && r.name == resName);
				if (res != null)
				{
					if (ui.iconImage != null)
						ui.iconImage.sprite = count > 0 ? res.icon : res.UnknownIcon;
					// Keep card name as plain resource name (no AE percent here)
					if (ui.nameText != null)
						ui.nameText.text = res.name;
				}
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
			var cfg = FindFirstObjectByType<CauldronWindowUI>() != null ? FindFirstObjectByType<CauldronWindowUI>().GetComponent<CauldronWindowUI>() : null;
			var cm = CauldronManager.Instance;
			var config = cm != null ? cm.GetComponent<CauldronManager>() : null;
			// Use thresholds from CauldronConfig directly
			var cauldron = CauldronManager.Instance;
			var cc = cauldron != null ? cauldron.GetComponent<CauldronManager>() : null;
			var configSO = CauldronManager.Instance != null ? CauldronManager.Instance.GetType() : null;
			var cfgSO = CauldronManager.Instance != null ? CauldronManager.Instance : null;
			var configAsset = CauldronManager.Instance != null ? (CauldronManager.Instance as CauldronManager).GetComponent<CauldronManager>() : null;
			// Simpler: access via CauldronManager.Instance.config through a public helper
			var thresholds = GetThresholdsForId(id);
			if (thresholds == null || thresholds.Length == 0) return 1;
			// Determine highest tier whose threshold <= count
			var tier = 1;
			for (var i = 0; i < thresholds.Length; i++)
				if (count >= thresholds[i]) tier = i + 1;
			return Mathf.Clamp(tier, 1, 8);
		}

		private int[] GetThresholdsForId(string id)
		{
			var cm = CauldronManager.Instance;
			var cfg = cm != null ? cm.GetComponent<CauldronManager>() : null;
			// Expose thresholds through CauldronWindowUI or CauldronManager config
			var cw = FindFirstObjectByType<CauldronWindowUI>();
			var manager = CauldronManager.Instance;
			var config = manager != null ? manager.GetComponent<CauldronManager>() : null;
			var cfgSO = manager != null ? (manager as CauldronManager) : null;
			// Fallback: find CauldronConfig from any CauldronManager in scene
			var m = CauldronManager.Instance;
			var configSO2 = m != null ? m.GetType().GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(m) as CauldronConfig : null;
			if (configSO2 == null) return null;
			var isResource = id.StartsWith("RES:");
			return isResource ? configSO2.resourceTierThresholds : configSO2.buffTierThresholds;
		}

		private Sprite GetTierSpriteFromCauldron(int tier)
		{
			var cw = FindFirstObjectByType<CauldronWindowUI>();
			if (cw == null) return null;
			return cw.GetTierSprite(tier);
		}

		private Sprite GetBorderTierSpriteFromCauldron(int tier)
		{
			var cw = FindFirstObjectByType<CauldronWindowUI>();
			if (cw == null) return null;
			return cw.GetBorderTierSprite(tier);
		}

		private void OnSaveOrLoad()
		{
			Rebuild();
		}
	}
}


