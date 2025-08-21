using System.Collections.Generic;
using System.Linq;
using References.UI;
using TMPro;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Utilities;
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
			if (cauldron != null) cauldron.OnCardGained += OnCardGained;
		}

		private void OnDisable()
		{
			if (cauldron != null) cauldron.OnCardGained -= OnCardGained;
		}

		private void Rebuild()
		{
			if (parent == null || sectionPrefab == null || itemPrefab == null) return;
			UIUtils.ClearChildren(parent);
			itemById.Clear();

			// Alter-Echoes section
			var secAE = Instantiate(sectionPrefab, parent);
			if (secAE.titleText != null) secAE.titleText.text = "Alter-Echoes";
			foreach (var res in Blindsided.Utilities.AssetCache.GetAll<Resource>(""))
			{
				if (res == null || res.DisableAlterEcho) continue;
				var ui = Instantiate(itemPrefab, secAE.contentTransform);
				ui.nameText.text = res.name;
				ui.iconImage.sprite = res.icon;
				var id = $"RES:{res.name}";
				itemById[id] = ui;
				UpdateItemCount(id);
			}

			// Buffs section
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

		private void UpdateItemCount(string id)
		{
			if (!itemById.TryGetValue(id, out var ui) || ui == null) return;
			var dict = Blindsided.Oracle.oracle.saveData.CauldronCardCounts;
			var count = dict.TryGetValue(id, out var c) ? c : 0;
			if (ui.countText != null)
				ui.countText.text = count.ToString();
			// Tier text: you’ll wire thresholds in a later pass; for now show raw count
		}
	}
}


