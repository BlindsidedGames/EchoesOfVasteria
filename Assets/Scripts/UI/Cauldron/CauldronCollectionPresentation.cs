using Blindsided.Utilities;
using TimelessEchoes.UI.Toolkit;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.Oracle;
namespace TimelessEchoes.UI.Cauldron
{
 public static class CauldronCollectionPresentation
 {
		public static string BuildTooltipText(string id, CauldronManager cachedCauldronManager, Dictionary<string, Resource> resourceById, Dictionary<string, TimelessEchoes.Buffs.BuffRecipe> buffById, System.Func<CauldronManager.AEResourceGroup, int> GetSectionTier, out int cardTier, out bool isInfinity)
		{
			cachedCauldronManager ??= CauldronManager.Instance;

			string T(string key, string english, params object[] args) => ToolkitLocalization.Text(key, english, args);
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
				var config = CauldronResourceYield.Config;
                cardTier = cachedCauldronManager != null ? cachedCauldronManager.GetResourceTier(cardName) : 0;
                var sectPct = CauldronResourceYield.CategoryBonusPercent(sectionTier, config);
                sectionEffect = T("cauldron.category-yield", "Category Yield: +{0} {1}", LocaleNumbers.PercentPoints(sectPct), sectionName);
                cardName = resourceById.TryGetValue(id, out var namedResource) ? ToolkitLocalization.Name(namedResource) : ToolkitLocalization.Text("resource." + cardName, cardName);
                var pct = CauldronResourceYield.CardBonusPercent(cardTier, config);
                cardEffect = T("cauldron.card-yield", "+{0} {1} Yield (gathering and harvests)", LocaleNumbers.PercentPoints(pct, "N0"), cardName);
			}
			else if (id.StartsWith("BUFF:"))
			{
				var assetName = id.Substring(5);
				cardName = assetName;
				if (buffById.TryGetValue(id, out var buff) && buff != null)
					cardName = buff.GetDisplayName();
				cardTier = Mathf.Max(1, cachedCauldronManager != null ? cachedCauldronManager.GetBuffTier(assetName) : 1);
				sectionName = T("hub.buffs", "Buffs");
				sectionTier = cachedCauldronManager != null ? Mathf.Max(0, cachedCauldronManager.GetBuffsGroupTier()) : 0;
				var sectPct = sectionTier * 2.5f;
				sectionEffect = T("cauldron.global-buff-power", "Global Buff Power: +{0}", LocaleNumbers.PercentPoints(sectPct));
				var cdr = cachedCauldronManager != null ? Mathf.Max(0f, cachedCauldronManager.GetBuffCooldownReductionPercent(assetName)) : 0f;
				var groupBonus = sectionTier * 2.5f;
				var totalPower = cachedCauldronManager != null ? Mathf.Max(0f, cachedCauldronManager.GetBuffPowerPercent(assetName)) : 0f;
				var perBuffOnly = Mathf.Max(0f, totalPower - groupBonus);
				cardEffect = T("cauldron.card-buff", "Cooldown: -{0} | Power: +{1}", LocaleNumbers.PercentPoints(cdr, "N0"), LocaleNumbers.PercentPoints(perBuffOnly, "N0"));
			}
			else if (id.StartsWith("INF:"))
			{
				isInfinity = true;
				var mappingStr = id.Substring(4);
				sectionName = T("cauldron.boon-formula", "Eternal Boon Formula");
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
					var valStr = val.ToString(fmt, LocaleNumbers.Culture);
					cardEffect = isPct ? $"+{valStr}{LocaleNumbers.PercentSuffix} {display}" : $"+{valStr} {display}";
					// Show formula details for Infinity on the section line
					int n = 0;
					var key = $"INF:{mapping}";
					if (oracle != null && oracle.saveData != null && oracle.saveData.CauldronCardCounts != null)
						oracle.saveData.CauldronCardCounts.TryGetValue(key, out n);
					sectionEffect = LocaleNumbers.Format($"{n:N0} ^ {(so != null ? so.Exponent : 1f)} = {val.ToString(fmt, LocaleNumbers.Culture)}");
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
			if (!isInfinity) { sb.Append(" | "); sb.Append(T("cauldron.tier", "Tier {0}", cardTier)); }
			sb.Append('\n');
			sb.Append(T("cauldron.effect", "Effect: {0}", "")); sb.Append(cardEffect);
			// Half-height spacer between parts
			sb.Append("<line-height=50%>\n</line-height>\n");
			// Section block below card
			sb.Append("<b>"); sb.Append(sectionName); sb.Append("</b>");
			if (!isInfinity) { sb.Append(" | "); sb.Append(T("cauldron.tier", "Tier {0}", sectionTier)); }
			sb.Append('\n');
			if (isInfinity)
			{
				// Show only the formula without the "Effect:" prefix
				sb.Append(sectionEffect);
			}
			else
			{
				sb.Append(T("cauldron.effect", "Effect: {0}", "")); sb.Append(sectionEffect);
			}

			return sb.ToString();
		}

		public static string FormatGroupName(CauldronManager.AEResourceGroup grp)
		{
			return ToolkitLocalization.Text("skill." + (grp == CauldronManager.AEResourceGroup.Woodcutting ? "Woodcutting" : grp.ToString()), grp switch
			{
				CauldronManager.AEResourceGroup.Farming => "Farming",
				CauldronManager.AEResourceGroup.Fishing => "Fishing",
				CauldronManager.AEResourceGroup.Mining => "Mining",
				CauldronManager.AEResourceGroup.Woodcutting => "Logging",
				CauldronManager.AEResourceGroup.Looting => "Looting",
				_ => "Combat"
			});
		}

		private static string FormatPercentNoTrailingZero(float value)
		{
			// Round to one decimal first, then hide .0 if present
			var rounded1 = Mathf.Round(value * 10f) / 10f;
			var whole = Mathf.Round(rounded1);
			bool hasFraction = Mathf.Abs(rounded1 - whole) > 0.0001f;
			return hasFraction ? LocaleNumbers.Format($"{rounded1:N1}") : LocaleNumbers.Format($"{whole:N0}");
		}
 }
}
