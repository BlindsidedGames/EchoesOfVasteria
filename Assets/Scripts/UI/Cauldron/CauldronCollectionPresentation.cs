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
                sectionEffect = $"Category Yield: +{FormatPercentNoTrailingZero(sectPct)}% {sectionName}";
                var pct = CauldronResourceYield.CardBonusPercent(cardTier, config);
                cardEffect = $"+{pct:N0}% {cardName} Yield (gathering and harvests)";
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

		public static string FormatGroupName(CauldronManager.AEResourceGroup grp)
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

		private static string FormatPercentNoTrailingZero(float value)
		{
			// Round to one decimal first, then hide .0 if present
			var rounded1 = Mathf.Round(value * 10f) / 10f;
			var whole = Mathf.Round(rounded1);
			bool hasFraction = Mathf.Abs(rounded1 - whole) > 0.0001f;
			return hasFraction ? $"{rounded1:N1}" : $"{whole:N0}";
		}
 }
}
