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
		[SerializeField] private int maxListEntries = 3;

		private bool dirty = true;
		private Coroutine refreshRoutine;
		private Dictionary<string, StatDefSO> idToStat;

		private void Awake()
		{
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
			var svc = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
			if (svc != null)
				svc.OnIvanXpChanged += OnIvanXpChanged;
		}

		private void Unsubscribe()
		{
			var svc = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
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
			statsText.text = BuildStatsText(forge);
		}

		private string BuildStatsText(GameData.ForgeStats forge)
		{
			var sb = new StringBuilder(1024);

			// Title
			sb.AppendLine("<size=120%><b>Forge Stats</b></size>");

			if (forge == null)
			{
				sb.AppendLine("No data yet.");
				return sb.ToString();
			}

			// Totals
			sb.AppendLine("<size=105%><b>Totals</b></size>");
			sb.AppendLine($"• Total Crafts: {forge.TotalCrafts:N0}");
			sb.AppendLine($"• Equipped From Craft: {forge.TotalEquippedFromCraft:N0}");
			sb.AppendLine($"• Total Salvaged: {forge.TotalSalvaged:N0}");

			// Ivan
			sb.AppendLine("<size=105%><b>Ivan</b></size>");
			var svc = CraftingService.Instance ?? FindFirstObjectByType<CraftingService>();
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
			sb.AppendLine($"• Level: {ivLevel:N0}");
			sb.AppendLine($"• XP: {ivCurrent:N0} / {ivNeeded:N0}");
			sb.AppendLine($"• Total XP Gained: {forge.IvanXpGainedTotal:N0} (level-ups: {forge.IvanLevelUpsFromCrafts:N0})");

			// Autocraft
			sb.AppendLine("<size=105%><b>Autocraft</b></size>");
			sb.AppendLine($"• Sessions: {forge.TotalAutocraftSessions:N0}, Crafts: {forge.AutocraftCrafts:N0}");
			AppendTopK(sb, forge.AutocraftStopReasons, maxListEntries, formatKey: k => k, prefix: "• Stop Reasons:");

			// Salvage
			sb.AppendLine("<size=105%><b>Salvage</b></size>");
			sb.AppendLine($"• Items: {forge.SalvageItems:N0}  • Entries: {forge.SalvageEntries:N0}  • Avg/Item: {SafeDiv(forge.SalvageEntries, forge.SalvageItems):N2}");
			AppendTopK(sb, forge.SalvagesByRarity, maxListEntries, formatKey: k => $"rarity {k}");
			AppendTopK(sb, forge.SalvagesByCore, maxListEntries, formatKey: k => $"core {k}");
			AppendTopK(sb, forge.SalvageYieldPerResource?.ToDictionary(p => p.Key, p => p.Value.sum), maxListEntries, formatKey: k => $"gained {k}");

			// Distributions
			sb.AppendLine("<size=105%><b>Distributions</b></size>");
			// Overall (rarity)
			sb.AppendLine("• Overall:");
			if (forge.CraftsByRarity != null && forge.CraftsByRarity.Count > 0)
				AppendTopK(sb, forge.CraftsByRarity, forge.CraftsByRarity.Count, formatKey: k => k, total: forge.TotalCrafts);
			// Per-core rarity distributions
			sb.AppendLine("• Cores:");
			AppendCoreRarityDistributions(sb, forge);

			// Upgrades
			sb.AppendLine("<size=105%><b>Upgrades</b></size>");
			AppendTopK(sb, forge.UpgradesBySlot, Math.Max(4, maxListEntries), formatKey: k => k);
			sb.AppendLine($"• Avg Crafts / Upgrade: {forge.AverageCraftsPerUpgrade:N2}  • Longest Gap: {forge.MaxCraftsBetweenUpgrades:N0}");

			// Best Stat Scores (theoretical min–max based on stat defs and max affixes)
			var (minBest, maxBest, affixes) = ComputeTheoreticalBestStatScoreRange();
			sb.AppendLine($"<size=105%><b>Best Stat Scores</b></size>");
			sb.AppendLine($"• {minBest:0.00} – {maxBest:0.00}  <size=90%>(affixes={affixes})</size>");

			// Best single-piece scores
			if (forge.BestPieceScoreBySlot != null && forge.BestPieceScoreBySlot.Count > 0)
			{
				sb.AppendLine("• Best Piece Scores:");
				foreach (var pair in forge.BestPieceScoreBySlot.OrderByDescending(p => p.Value))
					sb.AppendLine($"  • slot {pair.Key}: {pair.Value:N2}");
			}
			if (forge.BestPieceScoreByCore != null && forge.BestPieceScoreByCore.Count > 0)
			{
				sb.AppendLine("• Best By Core:");
				var ordered = OrderCoresByPreferred(forge.BestPieceScoreByCore.Keys);
				foreach (var core in ordered)
				{
					forge.BestPieceScoreByCore.TryGetValue(core, out var best);
					sb.AppendLine($"  • core {core}: {best:N2}");
				}
			}
			if (forge.BestPieceScoreByRarity != null && forge.BestPieceScoreByRarity.Count > 0)
			{
				sb.AppendLine("• Best By Rarity:");
				foreach (var r in OrderCoresByPreferred(forge.BestPieceScoreByRarity.Keys))
				{
					forge.BestPieceScoreByRarity.TryGetValue(r, out var best);
					sb.AppendLine($"  • {r}: {best:N2}");
				}
			}

			// Per-Slot Totals (sorted by slot name)
			sb.AppendLine("<size=105%><b>Per-Slot Totals</b></size>");
			AppendCountsByKey(sb, forge.CraftsBySlotTotals, "");

			// Stat Rolls (highlights)
			sb.AppendLine("<size=105%><b>Stat Rolls</b></size>");
			AppendAll(sb, forge.CumulativeStatTotalsByStat, formatKey: k => GetIconForStatId(k), prefix: "• Totals:", valueFormat: v => Blindsided.Utilities.CalcUtils.FormatNumber(v, true));
			if (forge.HighestRollByStat != null && forge.HighestRollByStat.Count > 0)
			{
				sb.AppendLine("• Highest:");
				var entries = new List<(string icon, string cur, string max)>();
				int maxLen = 0;
				foreach (var pair in forge.HighestRollByStat.OrderByDescending(p => p.Value))
				{
					var icon = GetIconForStatId(pair.Key);
					float maxRoll = GetMaxRollForStat(pair.Key);
					string curStr = pair.Value.ToString("0.000");
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
			AppendAll(sb, forge.HighRollsByStat, formatKey: k => GetIconForStatId(k), prefix: $"• High Rolls | Times rolled in top {topPct:0.#}% of stat:");

			// Conversions moved to bottom
			sb.AppendLine("<size=105%><b>Conversions</b></size>");
			sb.AppendLine($"• Ingot Conversions: {forge.IngotConversions:N0}");
			// Totals at top
			double totalCrystals = forge.CrystalsCraftedByResource?.Values.Sum() ?? forge.CrystalCrafted;
			double totalIngots = forge.IngotsCraftedByResource?.Values.Sum() ?? 0;
			sb.AppendLine($"• Total Crystals: {Blindsided.Utilities.CalcUtils.FormatNumber(totalCrystals, true)}");
			sb.AppendLine($"• Total Ingots: {Blindsided.Utilities.CalcUtils.FormatNumber(totalIngots, true)}");

			sb.AppendLine("Created:");
			// Group created resources by core and add a 20% spacer between core groups, mirroring Consumed spacing
			RenderCreatedByCore(sb, forge.CrystalsCraftedByResource, forge.ChunksCraftedByResource, null);
			if (forge.ConversionSpentByResource != null && forge.ConversionSpentByResource.Count > 0)
			{
				sb.AppendLine("Consumed:");
				RenderConversionSpends(sb, forge.ConversionSpentByResource);
			}

			return sb.ToString();
		}

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
			bool wroteAny = false;
			bool hasSlime = dict.TryGetValue("Slime", out var slime);
			bool hasStone = dict.TryGetValue("Stone", out var stone);
			if (hasSlime)
			{
				sb.Append($"• Slime: {Blindsided.Utilities.CalcUtils.FormatNumber(slime, true)}");
				if (hasCoreGroups && !hasStone)
					sb.Append("<line-height=20%>\n\u200B</line-height>\n");
				else sb.Append("\n");
				wroteAny = true;
			}
			if (hasStone)
			{
				sb.Append($"• Stone: {Blindsided.Utilities.CalcUtils.FormatNumber(stone, true)}");
				if (hasCoreGroups)
					sb.Append("<line-height=20%>\n\u200B</line-height>\n");
				else sb.Append("\n");
				wroteAny = true;
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

			// Contribution per stat = roll * comparisonScale
			var maxContribs = new List<float>();
			var minContribs = new List<float>();
			foreach (var s in stats)
			{
				float scale = Mathf.Max(0f, s.comparisonScale);
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
				sb.AppendLine($"  • {slot} crafts: {crafts:N0}");
				sb.AppendLine($"  • {slot} equips: {equips:N0}");
				sb.Append($"  • {slot} salvages: {salvages:N0}");
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


