using TimelessEchoes.UI.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear.UI
{
	/// <summary>Forge statistics text shared by Canvas and UI Toolkit presenters.</summary>
	public sealed class ForgeStatisticsPresentation
	{
		private Dictionary<string, StatDefSO> idToStat;
		public string BuildStatsText(GameData.ForgeStats forge, bool preserveSectionIds = false)
		{
			var sb = new StringBuilder(1024);

			// Title
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.title", "<size=120%><b>Forge Stats</b></size>"));

			if (forge == null)
			{
				sb.AppendLine(ToolkitLocalization.Text("forge.stats.no-data", "No data yet."));
				return sb.ToString();
			}

			// Totals
			sb.AppendLine(SectionTitle("forge.stats.section-totals", "Totals", preserveSectionIds));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.total-crafts", "• Total Crafts: {0:N0}", forge.TotalCrafts));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.equipped-from-craft", "• Equipped From Craft: {0:N0}", forge.TotalEquippedFromCraft));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.total-salvaged", "• Total Salvaged: {0:N0}", forge.TotalSalvaged));

			// Ivan
			sb.AppendLine(SectionTitle("forge.stats.section-ivan", "Ivan", preserveSectionIds));
			var svc = CraftingService.Instance;
			int ivLevel; float ivCurrent; float ivNeeded;
			if (svc != null)
			{
				(var level, var current, var needed) = svc.GetIvanXpState();
				ivLevel = level; ivCurrent = current; ivNeeded = needed;
			}
			else
			{
				ivLevel = forge.IvanLevelAtCraft; ivCurrent = forge.IvanXpAtCraft; ivNeeded = Mathf.Max(ivCurrent, 1f);
			}
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.level", "• Level: {0:N0}", ivLevel));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.xp", "• XP: {0:N0} / {1:N0}", ivCurrent, ivNeeded));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.total-xp-gained-level-ups", "• Total XP Gained: {0:N0} (level-ups: {1:N0})", forge.IvanXpGainedTotal, forge.IvanLevelUpsFromCrafts));

			// Autocraft
			sb.AppendLine(SectionTitle("forge.stats.section-autocraft", "Autocraft", preserveSectionIds));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.sessions-crafts", "• Sessions: {0:N0}, Crafts: {1:N0}", forge.TotalAutocraftSessions, forge.AutocraftCrafts));
			AppendAll(sb, forge.AutocraftStopReasons, formatKey: k => FormatStopReason(k), prefix: ToolkitLocalization.Text("forge.stats.stop-reasons", "• Stop Reasons:"));

			// Quality (Equipped vs Best Rolled by slot)
			AppendQualitySection(sb, forge, preserveSectionIds);
			// Best By Core (Quality)
			if (forge.BestAbsolutePieceScoreByCore != null && forge.BestAbsolutePieceScoreByCore.Count > 0)
			{
				var (_, maxBestCore, _) = ComputeTheoreticalBestStatScoreRange();
				sb.AppendLine(ToolkitLocalization.Text("forge.stats.best-core", "• Best By Core:"));
				var ordered = OrderCoresByPreferred(forge.BestAbsolutePieceScoreByCore.Keys);
				foreach (var core in ordered)
				{
					forge.BestAbsolutePieceScoreByCore.TryGetValue(core, out var bestAbs);
					float pct = maxBestCore > 0f ? Mathf.Clamp01(bestAbs / maxBestCore) * 100f : 0f;
					sb.AppendLine(ToolkitLocalization.Text("forge.stats.core-quality", "  • core {0}: {1:0.#}%", core, pct));
				}
			}
			// Best By Rarity (Quality)
			if (forge.BestAbsolutePieceScoreByRarity != null && forge.BestAbsolutePieceScoreByRarity.Count > 0)
			{
				var (_, maxBestRarity, _) = ComputeTheoreticalBestStatScoreRange();
				sb.AppendLine(ToolkitLocalization.Text("forge.stats.best-rarity", "• Best By Rarity:"));
				foreach (var r in OrderRaritiesByTier(forge.BestAbsolutePieceScoreByRarity.Keys))
				{
					forge.BestAbsolutePieceScoreByRarity.TryGetValue(r, out var bestAbs);
					float pct = maxBestRarity > 0f ? Mathf.Clamp01(bestAbs / maxBestRarity) * 100f : 0f;
					sb.AppendLine($"  • {r}: {pct:0.#}%");
				}
			}

			// Salvage
			sb.AppendLine(SectionTitle("forge.stats.section-salvage", "Salvage", preserveSectionIds));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.items-entries-avg-item", "• Items: {0:N0}  • Entries: {1:N0}  • Avg/Item: {2:N2}", forge.SalvageItems, forge.SalvageEntries, SafeDiv(forge.SalvageEntries, forge.SalvageItems)));
			AppendAll(sb, forge.SalvagesByRarity, formatKey: k => ToolkitLocalization.Text("forge.stats.rarity", "rarity {0}", k));
			AppendAll(sb, forge.SalvagesByCore, formatKey: k => ToolkitLocalization.Text("forge.stats.core", "core {0}", k));
			AppendAll(sb, forge.SalvageYieldPerResource?.ToDictionary(p => p.Key, p => p.Value.sum), formatKey: k => ToolkitLocalization.Text("forge.stats.gained", "gained {0}", k));

			// Distributions
			sb.AppendLine(SectionTitle("forge.stats.section-distributions", "Distributions", preserveSectionIds));
			// Overall (rarity)
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.overall", "• Overall:"));
			if (forge.CraftsByRarity != null && forge.CraftsByRarity.Count > 0)
				AppendTopK(sb, forge.CraftsByRarity, forge.CraftsByRarity.Count, formatKey: k => k, total: forge.TotalCrafts);
			// Per-core rarity distributions
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.cores", "• Cores:"));
			AppendCoreRarityDistributions(sb, forge);

			// Upgrades
			sb.AppendLine(SectionTitle("forge.stats.section-upgrades", "Upgrades", preserveSectionIds));
			AppendAll(sb, forge.UpgradesBySlot, formatKey: k => k);
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.avg-crafts-upgrade-longest-gap", "• Avg Crafts / Upgrade: {0:N2}  • Longest Gap: {1:N0}", forge.AverageCraftsPerUpgrade, forge.MaxCraftsBetweenUpgrades));

			// Per-Slot Totals (sorted by slot name)
			sb.AppendLine(SectionTitle("forge.stats.section-per-slot-totals", "Per-Slot Totals", preserveSectionIds));
			AppendCountsByKey(sb, forge.CraftsBySlotTotals, "");

			// Stat Rolls (highlights)
			sb.AppendLine(SectionTitle("forge.stats.section-stat-rolls", "Stat Rolls", preserveSectionIds));
			AppendStatsInPreferredOrder(sb, forge.CumulativeStatTotalsByStat, formatKey: k => GetIconForStatId(k), prefix: ToolkitLocalization.Text("forge.stats.totals", "• Totals:"), valueFormat: v => Blindsided.Utilities.CalcUtils.FormatNumber(v, true));
			if (forge.HighestRollByStat != null && forge.HighestRollByStat.Count > 0)
			{
				sb.AppendLine(ToolkitLocalization.Text("forge.stats.highest", "• Highest:"));
				var entries = new List<(string icon, string cur, string max)>();
				int maxLen = 0;
				foreach (var id in OrderStatIdsByPreferred(forge.HighestRollByStat.Keys))
				{
					var icon = GetIconForStatId(id);
					float maxRoll = GetMaxRollForStat(id);
					float curVal = forge.HighestRollByStat.TryGetValue(id, out var v) ? v : 0f;
					string curStr = curVal.ToString("0.000");
					string maxStr = maxRoll.ToString("0.000");
					entries.Add((icon, curStr, maxStr));
					if (curStr.Length > maxLen) maxLen = curStr.Length;
				}
				foreach (var e in entries)
				{
					int pad = Mathf.Max(0, maxLen - e.cur.Length);
					sb.AppendLine($"  • {e.icon}: {e.cur}{MakeMSpaces(pad)} | ({e.max})");
				}
			}
			// Describe high rolls threshold dynamically from saved settings
			var topPct = Mathf.Clamp01(1f - forge.HighRollTopPercentThreshold) * 100f;
			AppendStatsInPreferredOrder(sb, forge.HighRollsByStat, formatKey: k => GetIconForStatId(k), prefix: ToolkitLocalization.Text("forge.stats.high-rolls-times-rolled-in-top-of-stat", "• High Rolls | Times rolled in top {0:0.#}% of stat:", topPct));

			// Conversions moved to bottom
			sb.AppendLine(SectionTitle("forge.stats.section-conversions", "Conversions", preserveSectionIds));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.ingot-conversions", "• Ingot Conversions: {0:N0}", forge.IngotConversions));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.core-conversions", "• Core Conversions: {0:N0}", forge.CoreConversions));
			// Totals at top
			// Guard against null maps in old saves
			if (forge.CrystalsCraftedByResource == null) forge.CrystalsCraftedByResource = new Dictionary<string, double>();
			if (forge.IngotsCraftedByResource == null) forge.IngotsCraftedByResource = new Dictionary<string, double>();
			if (forge.CoresCraftedByResource == null) forge.CoresCraftedByResource = new Dictionary<string, double>();
			double totalCrystals = forge.CrystalsCraftedByResource.Values.Sum();
			double totalIngots = forge.IngotsCraftedByResource.Values.Sum();
			double totalCores = forge.CoresCraftedByResource.Values.Sum();
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.total-crystals", "• Total Crystals: {0}", Blindsided.Utilities.CalcUtils.FormatNumber(totalCrystals, true)));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.total-ingots", "• Total Ingots: {0}", Blindsided.Utilities.CalcUtils.FormatNumber(totalIngots, true)));
			sb.AppendLine(ToolkitLocalization.Text("forge.stats.total-cores", "• Total Cores: {0}", Blindsided.Utilities.CalcUtils.FormatNumber(totalCores, true)));

			sb.AppendLine(ToolkitLocalization.Text("forge.stats.created", "Created:"));
			// Group created resources by core and add a 20% spacer between core groups, mirroring Consumed spacing
			RenderCreatedByCore(sb, forge.CrystalsCraftedByResource, forge.ChunksCraftedByResource, forge.CoresCraftedByResource);
			if (forge.ConversionSpentByResource != null && forge.ConversionSpentByResource.Count > 0)
			{
				sb.AppendLine(ToolkitLocalization.Text("forge.stats.consumed", "Consumed:"));
				RenderConversionSpends(sb, forge.ConversionSpentByResource);
			}

			return sb.ToString();
		}

        private static string SectionTitle(string key, string english, bool preserveSectionIds)
        { return "<size=105%><b>" + (preserveSectionIds ? english : ToolkitLocalization.Text(key, english)) + "</b></size>"; }
		private static double SafeDiv(double num, double den)
		{
			return den <= 0 ? 0 : num / den;
		}

		private void AppendTopK<TK>(StringBuilder sb,
			Dictionary<TK, int> dict,
			int k,
			Func<TK, string> formatKey,
			string prefix = null,
			int? total = null)
		{
			if (dict == null || dict.Count == 0) return;
			var ordered = dict.OrderByDescending(p => p.Value).Take(Mathf.Max(1, k)).ToList();
			if (!string.IsNullOrEmpty(prefix)) sb.AppendLine(prefix);
			foreach (var (key, value) in ordered)
			{
				if (total.HasValue && total.Value > 0)
				{
					double pct = 100.0 * value / total.Value;
					sb.AppendLine($"  • {formatKey(key)}: {value:N0} ({pct:N1}%)");
				}
				else
				{
					sb.AppendLine($"  • {formatKey(key)}: {value:N0}");
				}
			}
		}

		private void AppendTopK<TK>(StringBuilder sb,
			Dictionary<TK, double> dict,
			int k,
			Func<TK, string> formatKey,
			string prefix = null,
			Func<double, string> valueFormat = null)
		{
			if (dict == null || dict.Count == 0) return;
			var ordered = dict.OrderByDescending(p => p.Value).Take(Mathf.Max(1, k)).ToList();
			if (!string.IsNullOrEmpty(prefix)) sb.AppendLine(prefix);
			foreach (var (key, value) in ordered)
			{
				string vf = valueFormat != null ? valueFormat(value) : value.ToString("N0");
				sb.AppendLine($"  • {formatKey(key)}: {vf}");
			}
		}

		private void AppendAll(StringBuilder sb,
			Dictionary<string, int> dict,
			Func<string, string> formatKey,
			string prefix = null)
		{
			if (dict == null || dict.Count == 0) return;
			var ordered = dict.OrderByDescending(p => p.Value).ToList();
			if (!string.IsNullOrEmpty(prefix)) sb.AppendLine(prefix);
			foreach (var (key, value) in ordered)
				sb.AppendLine($"  • {formatKey(key)}: {value:N0}");
		}

		private void AppendStatsInPreferredOrder(StringBuilder sb,
			Dictionary<string, int> dict,
			Func<string, string> formatKey,
			string prefix = null)
		{
			if (dict == null || dict.Count == 0) return;
			if (!string.IsNullOrEmpty(prefix)) sb.AppendLine(prefix);
			foreach (var key in OrderStatIdsByPreferred(dict.Keys))
			{
				int value = dict.TryGetValue(key, out var v) ? v : 0;
				sb.AppendLine($"  • {formatKey(key)}: {value:N0}");
			}
		}

		private void AppendAll(StringBuilder sb,
			Dictionary<string, double> dict,
			Func<string, string> formatKey,
			string prefix = null,
			Func<double, string> valueFormat = null)
		{
			if (dict == null || dict.Count == 0) return;
			var ordered = dict.OrderByDescending(p => p.Value).ToList();
			if (!string.IsNullOrEmpty(prefix)) sb.AppendLine(prefix);
			foreach (var (key, value) in ordered)
			{
				string vf = valueFormat != null ? valueFormat(value) : value.ToString("N0");
				sb.AppendLine($"  • {formatKey(key)}: {vf}");
			}
		}

		private void AppendStatsInPreferredOrder(StringBuilder sb,
			Dictionary<string, double> dict,
			Func<string, string> formatKey,
			string prefix = null,
			Func<double, string> valueFormat = null)
		{
			if (dict == null || dict.Count == 0) return;
			if (!string.IsNullOrEmpty(prefix)) sb.AppendLine(prefix);
			foreach (var key in OrderStatIdsByPreferred(dict.Keys))
			{
				double value = dict.TryGetValue(key, out var v) ? v : 0;
				string vf = valueFormat != null ? valueFormat(value) : value.ToString("N0");
				sb.AppendLine($"  • {formatKey(key)}: {vf}");
			}
		}

		private string FormatStopReason(string reasonKey)
		{
			if (string.IsNullOrWhiteSpace(reasonKey)) return reasonKey;
			switch (reasonKey)
			{
				case "OutOfResources": return ToolkitLocalization.Text("forge.stats.out-of-resources", "Out of Resources");
				case "MaxIterations": return ToolkitLocalization.Text("forge.stats.max-iterations", "Max Iterations");
				case "Vastium": return ToolkitLocalization.Text("forge.stats.stop-vastium", "Vastium");
                case "Upgraded": return ToolkitLocalization.Text("forge.stats.stop-upgraded", "Upgraded");
                case "Cancelled": return ToolkitLocalization.Text("forge.stats.stop-cancelled", "Cancelled");
                default: return reasonKey;
			}
		}

		private void AppendCoreRarityDistributions(StringBuilder sb, GameData.ForgeStats forge)
		{
			if (forge == null || forge.RarityCountsByCore == null || forge.RarityCountsByCore.Count == 0)
				return;
			// Use preferred order
			var cores = OrderCoresByPreferred(forge.RarityCountsByCore.Keys);
			foreach (var core in cores)
			{
				int total = 0;
				if (forge.RarityCountsByCore.TryGetValue(core, out var rarMapTotal) && rarMapTotal != null)
					foreach (var c in rarMapTotal.Values) total += c;
				sb.AppendLine($"  • {core}:");
				if (forge.RarityCountsByCore.TryGetValue(core, out var rarMap) && rarMap != null && rarMap.Count > 0)
				{
					// order rarities by tier order, not by count
					foreach (var rar in OrderRaritiesByTier(rarMap.Keys))
					{
						int value = rarMap.TryGetValue(rar, out var v) ? v : 0;
						double pct = total > 0 ? 100.0 * value / total : 0.0;
						sb.AppendLine($"    • {rar}: {value:N0} ({pct:N1}%)");
					}
				}
			}
		}

		private IEnumerable<string> OrderCoresByPreferred(IEnumerable<string> input)
		{
			var preferred = new List<string> { "Eznorb", "Nori", "Dlog", "Erif", "Lirium", "Copium", "Idle", "Vastium" };
			var set = new HashSet<string>(input ?? Array.Empty<string>());
			foreach (var p in preferred)
				if (set.Contains(p)) yield return p;
			// Append any others not in the preferred list, stable order
			foreach (var other in set)
				if (!preferred.Contains(other)) yield return other;
		}

		private IEnumerable<string> OrderRaritiesByTier(IEnumerable<string> rarityNames)
		{
			// Build name -> tier index lookup once
			var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var r in AssetCache.GetAll<RaritySO>(string.Empty))
				if (r != null && !lookup.ContainsKey(r.name)) lookup[r.name] = r.tierIndex;
			var list = new List<string>(rarityNames ?? Array.Empty<string>());
			list.Sort((a, b) =>
			{
				lookup.TryGetValue(a ?? string.Empty, out var ta);
				lookup.TryGetValue(b ?? string.Empty, out var tb);
				var cmp = ta.CompareTo(tb);
				if (cmp != 0) return cmp;
				return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
			});
			return list;
		}

		private void RenderConversionSpends(StringBuilder sb, System.Collections.Generic.Dictionary<string, double> dict)
		{
			// Build remaining keys by guessed core name (prefix before first space)
			var remaining = dict.Keys.Where(k => !string.Equals(k, "Slime", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(k, "Stone", StringComparison.OrdinalIgnoreCase)).ToList();
			var byCore = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var key in remaining)
			{
				var parts = key.Split(' ');
				var core = parts.Length > 0 ? parts[0] : key;
				if (!byCore.ContainsKey(core)) byCore[core] = new System.Collections.Generic.List<string>();
				byCore[core].Add(key);
			}

			// Write Slime and Stone first, appending a 20% half-line break if we have any core groups following
			bool hasCoreGroups = byCore.Keys.Any();
			bool hasSlime = dict.TryGetValue("Slime", out var slime);
			bool hasStone = dict.TryGetValue("Stone", out var stone);
			if (hasSlime)
			{
				sb.Append(ToolkitLocalization.Text("forge.stats.slime", "• Slime: {0}", Blindsided.Utilities.CalcUtils.FormatNumber(slime, true)));
				if (hasCoreGroups && !hasStone)
					sb.Append("<line-height=20%>\n\u200B</line-height>\n");
				else sb.Append("\n");
			}
			if (hasStone)
			{
				sb.Append(ToolkitLocalization.Text("forge.stats.stone", "• Stone: {0}", Blindsided.Utilities.CalcUtils.FormatNumber(stone, true)));
				if (hasCoreGroups)
					sb.Append("<line-height=20%>\n\u200B</line-height>\n");
				else sb.Append("\n");
			}

			// Now each core group in preferred order. Between core groups, add a half-line spacer appended to the prior line
			var coresOrdered = OrderCoresByPreferred(byCore.Keys).ToList();
			for (int c = 0; c < coresOrdered.Count; c++)
			{
				var core = coresOrdered[c];
				var keys = byCore[core];
				if (keys == null || keys.Count == 0) continue;
				// Sort entries by name for stability
				keys.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));

				for (int i = 0; i < keys.Count; i++)
				{
					var k = keys[i];
					bool isLastInGroup = i == keys.Count - 1;
					bool needsSpacer = isLastInGroup && (c < coresOrdered.Count - 1); // spacer between groups only
					sb.Append($"• {k}: {Blindsided.Utilities.CalcUtils.FormatNumber(dict[k], true)}");
					if (needsSpacer)
						sb.Append("<line-height=20%>\n\u200B</line-height>\n");
					else sb.Append("\n");
				}
			}
		}

		private void RenderCreatedByCore(StringBuilder sb,
			System.Collections.Generic.Dictionary<string, double> crystals,
			System.Collections.Generic.Dictionary<string, double> chunks,
			System.Collections.Generic.Dictionary<string, double> ingots)
		{
			// Aggregate keys by guessed core (prefix before first space)
			var hasCrystals = crystals != null && crystals.Count > 0;
			var hasChunks = chunks != null && chunks.Count > 0;
			var hasIngots = ingots != null && ingots.Count > 0;
			if (!hasCrystals && !hasChunks && !hasIngots) return;

			var byCore = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(StringComparer.OrdinalIgnoreCase);
			void AddKeys(System.Collections.Generic.Dictionary<string, double> d)
			{
				if (d == null) return;
				foreach (var key in d.Keys)
				{
					var parts = key.Split(' ');
					var core = parts.Length > 0 ? parts[0] : key;
					if (!byCore.ContainsKey(core)) byCore[core] = new System.Collections.Generic.List<string>();
					if (!byCore[core].Contains(key)) byCore[core].Add(key);
				}
			}

			AddKeys(crystals);
			AddKeys(chunks);
			AddKeys(ingots);

			var coresOrdered = OrderCoresByPreferred(byCore.Keys).ToList();
			for (int c = 0; c < coresOrdered.Count; c++)
			{
				var core = coresOrdered[c];
				var keys = byCore[core];
				if (keys == null || keys.Count == 0) continue;
				// Sort entries by name for stability
				keys.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));

				for (int i = 0; i < keys.Count; i++)
				{
					var k = keys[i];
					bool isLastInGroup = i == keys.Count - 1;
					bool needsSpacer = isLastInGroup && (c < coresOrdered.Count - 1); // spacer between groups only
					double value = 0;
					if ((hasCrystals && crystals.TryGetValue(k, out var v1))) value = v1;
					else if ((hasChunks && chunks.TryGetValue(k, out var v2))) value = v2;
					else if ((hasIngots && ingots.TryGetValue(k, out var v3))) value = v3;
					sb.Append($"• {k}: {Blindsided.Utilities.CalcUtils.FormatNumber(value, true)}");
					if (needsSpacer)
						sb.Append("<line-height=20%>\n\u200B</line-height>\n");
					else sb.Append("\n");
				}
			}
		}

		private (float minScore, float maxScore, int affixCount) ComputeTheoreticalBestStatScoreRange()
		{
			// Max affixes available among rarities
			int maxAffixes = 1;
			foreach (var r in AssetCache.GetAll<RaritySO>(string.Empty))
				if (r != null && r.affixCount > maxAffixes) maxAffixes = r.affixCount;

			var stats = AssetCache.GetAll<StatDefSO>(string.Empty).Where(s => s != null).ToList();
			if (stats.Count == 0) return (0f, 0f, maxAffixes);

			// Contribution per stat = roll * ComparisonScale
			var maxContribs = new List<float>();
			var minContribs = new List<float>();
			foreach (var s in stats)
			{
				float scale = Mathf.Max(0f, s.ComparisonScale);
				maxContribs.Add(s.maxRoll * scale);
				minContribs.Add(Mathf.Max(0f, s.minRoll * scale));
			}

			maxContribs.Sort((a,b) => b.CompareTo(a));
			minContribs.Sort((a,b) => a.CompareTo(b));

			int n = Mathf.Clamp(maxAffixes, 1, maxContribs.Count);
			float maxSum = 0f, minSum = 0f;
			for (int i = 0; i < n; i++)
			{
				maxSum += maxContribs[i];
				minSum += minContribs[i];
			}

			return (minSum, maxSum, n);
		}

		private void AppendQualitySection(StringBuilder sb, GameData.ForgeStats forge, bool preserveSectionIds)
		{
			var equip = EquipmentController.Instance;
			sb.AppendLine(SectionTitle("forge.stats.section-quality", "Quality", preserveSectionIds));
			var slots = equip != null && equip.Slots != null && equip.Slots.Count > 0
				? equip.Slots
				: new System.Collections.Generic.List<string> { "Weapon", "Helmet", "Chest", "Boots" };

			sb.AppendLine(ToolkitLocalization.Text("forge.stats.best-rolled", "• Best Rolled:"));
			foreach (var slot in slots)
			{
				float best = 0f;
				if (forge != null && forge.BestAbsolutePieceScoreBySlot != null)
					forge.BestAbsolutePieceScoreBySlot.TryGetValue(slot, out best);
				var maxSlot = UpgradeEvaluator.ComputeTheoreticalMaxForSlot(slot);
				float pct = maxSlot > 0f ? Mathf.Clamp01(best / maxSlot) * 100f : 0f;
				sb.AppendLine($"  • {slot}: {pct:0.#}%");
			}
		}


		private void EnsureStatLookup()
		{
			if (idToStat != null) return;
			idToStat = new Dictionary<string, StatDefSO>(StringComparer.OrdinalIgnoreCase);
			foreach (var def in AssetCache.GetAll<StatDefSO>(string.Empty))
			{
				if (def == null) continue;
				if (!string.IsNullOrWhiteSpace(def.id) && !idToStat.ContainsKey(def.id)) idToStat[def.id] = def;
				if (!idToStat.ContainsKey(def.name)) idToStat[def.name] = def;
				if (!string.IsNullOrWhiteSpace(def.displayName) && !idToStat.ContainsKey(def.displayName)) idToStat[def.displayName] = def;
			}
		}

		private IEnumerable<string> OrderStatIdsByPreferred(IEnumerable<string> statIds)
		{
			EnsureStatLookup();
			var list = new List<string>(statIds ?? Array.Empty<string>());
			int MapIndex(string id)
			{
				if (string.IsNullOrWhiteSpace(id)) return int.MaxValue;
				if (idToStat != null && idToStat.TryGetValue(id, out var def) && def != null)
					return StatSortOrder.GetIndex(def.heroMapping);
				// Fallback: try name-based mapping via StatIconLookup so unknowns end up after known
				return int.MaxValue;
			}
			list.Sort((a, b) =>
			{
				int ia = MapIndex(a);
				int ib = MapIndex(b);
				int cmp = ia.CompareTo(ib);
				if (cmp != 0) return cmp;
				return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
			});
			return list;
		}

		private string GetIconForStatId(string statId)
		{
			if (string.IsNullOrWhiteSpace(statId)) return statId;
			EnsureStatLookup();
			if (idToStat != null && idToStat.TryGetValue(statId, out var def) && def != null)
				return StatIconLookup.GetIconTag(def.heroMapping);
			// Fallback: attempt by name mapping
			var tag = StatIconLookup.GetIconTag(statId);
			return string.IsNullOrEmpty(tag) ? statId : tag;
		}

		private float GetMaxRollForStat(string statId)
		{
			EnsureStatLookup();
			if (idToStat != null && idToStat.TryGetValue(statId, out var def) && def != null)
				return def.maxRoll;
			// Fallback: unknown stat id
			return 0f;
		}

		private string MakeMSpaces(int count)
		{
			if (count <= 0) return string.Empty;
			// Use <mspace> to reserve width; assumes monospace-like TMP spacing for digits
			return $"<mspace={0.6f}em>{new string(' ', count)}</mspace>";
		}

		private void AppendCountsByKey(StringBuilder sb, Dictionary<string, int> dict, string unused)
		{
			var o = Blindsided.Oracle.oracle;
			var forge = o != null ? o.saveData?.Forge : null;
			if (forge == null) return;
			var allSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (forge.CraftsBySlotTotals != null) foreach (var k in forge.CraftsBySlotTotals.Keys) allSlots.Add(k);
			if (forge.EquipsBySlot != null) foreach (var k in forge.EquipsBySlot.Keys) allSlots.Add(k);
			if (forge.SalvagesBySlot != null) foreach (var k in forge.SalvagesBySlot.Keys) allSlots.Add(k);
			foreach (var slot in allSlots.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
			{
				int crafts = 0, equips = 0, salvages = 0;
				if (forge.CraftsBySlotTotals != null) forge.CraftsBySlotTotals.TryGetValue(slot, out crafts);
				if (forge.EquipsBySlot != null) forge.EquipsBySlot.TryGetValue(slot, out equips);
				if (forge.SalvagesBySlot != null) forge.SalvagesBySlot.TryGetValue(slot, out salvages);
				sb.AppendLine(ToolkitLocalization.Text("forge.stats.crafts", "  • {0} crafts: {1:N0}", slot, crafts));
				sb.AppendLine(ToolkitLocalization.Text("forge.stats.equips", "  • {0} equips: {1:N0}", slot, equips));
				sb.Append(ToolkitLocalization.Text("forge.stats.salvages", "  • {0} salvages: {1:N0}", slot, salvages));
				sb.Append("<line-height=20%>\n\u200B</line-height>\n");
			}
		}

		private IEnumerable<string> OrderResourcesByCore(IEnumerable<string> resources)
		{
			var list = new List<string>(resources ?? Array.Empty<string>());
			var preferred = new List<string> { "Eznorb", "Nori", "Dlog", "Erif", "Lirium", "Copium", "Idle", "Vastium" };
			int CoreIndex(string res)
			{
				if (string.IsNullOrWhiteSpace(res)) return int.MaxValue;
				var parts = res.Split(' ');
				var core = parts.Length > 0 ? parts[0] : res;
				var idx = preferred.FindIndex(c => string.Equals(c, core, StringComparison.OrdinalIgnoreCase));
				return idx < 0 ? int.MaxValue : idx;
			}
			list.Sort((a, b) =>
			{
				int ia = CoreIndex(a);
				int ib = CoreIndex(b);
				int cmp = ia.CompareTo(ib);
				if (cmp != 0) return cmp;
				return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
			});
			return list;
		}
	}
}

