#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using TimelessEchoes.Gear;

namespace TimelessEchoes.EditorTools
{
	public class RarityConfigWindow : OdinEditorWindow
	{
		private const int TierCount = 8;
		private const int MaxChartLevel = 100;

		[MenuItem("Tools/Gear/Rarity Config")] private static void Open()
		{
			var wnd = GetWindow<RarityConfigWindow>(utility: false, title: "Rarity Config", focus: true);
			wnd.minSize = new Vector2(980, 640);
			wnd.Show();
		}

		[BoxGroup("Settings"), PropertyRange(0, MaxChartLevel), LabelText("Level")] public int level = MaxChartLevel;
		[BoxGroup("Settings"), LabelText("Apply Global Multiplier")] public bool applyGlobalMultiplier = true;
		[BoxGroup("Settings"), LabelText("Show Zeros")] public bool showZeroRows = true;
		[BoxGroup("Settings"), LabelText("Autosave"), Tooltip("Save assets to disk immediately after edits")] public bool autoSave = false;

		[BoxGroup("Charts"), EnumToggleButtons, LabelText("Mode")] public ChartView chartMode = ChartView.PerCore;
		[BoxGroup("Charts"), ShowIf("@chartMode == ChartView.PerCore"), ValueDropdown(nameof(GetCoreDropdown)), LabelText("Core")] public CoreSO chartCore;
		[BoxGroup("Charts"), ShowIf("@chartMode == ChartView.PerRarity"), ValueDropdown(nameof(GetRarityDropdown)), LabelText("Rarity")] public RaritySO chartRarity;
		[BoxGroup("Charts"), ShowIf("@chartMode == ChartView.PerRarity"), EnumToggleButtons, LabelText("Compare")] public PerRarityCompare perRarityCompare = PerRarityCompare.ByTier;

		private readonly Vector2 chartSize = new Vector2(900, 220);
		private Vector2 scroll;
		private bool showCharts = true;
		private bool showPropagate;

		private readonly CoreSO[] coresByTier = new CoreSO[TierCount];
		private readonly List<RaritySO> raritiesByTier = new List<RaritySO>(TierCount);

		private readonly Color gridColor = new Color(1f, 1f, 1f, 0.08f);
		private readonly Color axisColor = new Color(1f, 1f, 1f, 0.35f);

		public enum ChartView { PerCore, PerRarity }
		public enum PerRarityCompare { ByCore, ByTier }

		protected override void OnEnable()
		{
			base.OnEnable();
			ReloadAssets();
		}

		protected override void Initialize()
		{
			// Odin's preferred initialization hook
			ReloadAssets();
		}

		private void ReloadAssets()
		{
			// Load rarities
			raritiesByTier.Clear();
			foreach (var guid in AssetDatabase.FindAssets("t:RaritySO"))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var r = AssetDatabase.LoadAssetAtPath<RaritySO>(path);
				if (r != null) raritiesByTier.Add(r);
			}
			raritiesByTier.Sort((a, b) => a.tierIndex.CompareTo(b.tierIndex));

			// Load cores and place by tierIndex 0..7
			Array.Clear(coresByTier, 0, coresByTier.Length);
			foreach (var guid in AssetDatabase.FindAssets("t:CoreSO"))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var c = AssetDatabase.LoadAssetAtPath<CoreSO>(path);
				if (c == null) continue;
				if (c.tierIndex >= 0 && c.tierIndex < TierCount)
					coresByTier[c.tierIndex] = c;
			}

			// Defaults for charts
			if (chartCore == null)
				chartCore = coresByTier.FirstOrDefault(c => c != null);
			if (chartRarity == null)
				chartRarity = raritiesByTier.FirstOrDefault();
		}

		protected override void OnImGUI()
		{
			DrawToolbar();
			EditorGUILayout.Space(4);
			scroll = EditorGUILayout.BeginScrollView(scroll);
			for (int tier = 0; tier < TierCount; tier++)
			{
				DrawCorePanel(tier);
			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(6);
			DrawChartsSection();
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUILayout.Label("Rarity Config", EditorStyles.boldLabel, GUILayout.Width(110));
				GUILayout.Space(12);
				level = (int)EditorGUILayout.Slider(level, 0, MaxChartLevel, GUILayout.Width(220));
				applyGlobalMultiplier = GUILayout.Toggle(applyGlobalMultiplier, new GUIContent("Global Mult"), EditorStyles.toolbarButton, GUILayout.Width(90));
				showZeroRows = GUILayout.Toggle(showZeroRows, new GUIContent("Show Zeros"), EditorStyles.toolbarButton, GUILayout.Width(90));
				autoSave = GUILayout.Toggle(autoSave, new GUIContent("Autosave"), EditorStyles.toolbarButton, GUILayout.Width(80));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) ReloadAssets();
				if (GUILayout.Button("Copy CSV", EditorStyles.toolbarButton, GUILayout.Width(80))) CopyCsv();
				if (GUILayout.Button("Propagate Weights", EditorStyles.toolbarButton, GUILayout.Width(140))) showPropagate = !showPropagate;
				if (GUILayout.Button("Save All", EditorStyles.toolbarButton, GUILayout.Width(80))) AssetDatabase.SaveAssets();
			}

			if (showPropagate)
			{
				DrawPropagateBox();
			}
		}

		private void DrawPropagateBox()
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Propagate Rarity Weights", EditorStyles.boldLabel);

			var sources = GetCoreList().Where(c => c != null).ToList();
			var sourceIdx = Mathf.Max(0, sources.IndexOf(chartCore));
			sourceIdx = EditorGUILayout.Popup(new GUIContent("Source Core"), sourceIdx, sources.Select(c => c.name).ToArray());
			var source = sources.Count > 0 ? sources[sourceIdx] : null;

			var targets = GetCoreList().ToArray();
			var targetFlags = new bool[targets.Length];
			for (int i = 0; i < targets.Length; i++)
			{
				if (targets[i] == null || targets[i] == source) { targetFlags[i] = false; continue; }
				targetFlags[i] = true;
			}

			EditorGUILayout.LabelField("Targets (tiers)");
			using (new EditorGUILayout.HorizontalScope())
			{
				for (int i = 0; i < targets.Length; i++)
				{
					EditorGUI.BeginDisabledGroup(targets[i] == null || targets[i] == source);
					targetFlags[i] = GUILayout.Toggle(targetFlags[i], new GUIContent($"{i}"), "Button", GUILayout.Width(28));
					EditorGUI.EndDisabledGroup();
				}
			}

			bool copyBase = true;
			bool copyPerLevel = true;
			bool overwriteOnlyMissing = false;

			copyBase = GUILayout.Toggle(copyBase, new GUIContent("Base Weights"), "Button", GUILayout.Width(110));
			copyPerLevel = GUILayout.Toggle(copyPerLevel, new GUIContent("Weight/Level"), "Button", GUILayout.Width(110));
			overwriteOnlyMissing = GUILayout.Toggle(overwriteOnlyMissing, new GUIContent("Overwrite Missing Only"), "Button", GUILayout.Width(170));

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Apply", GUILayout.Width(90)))
				{
					ApplyPropagation(source, targets, targetFlags, copyBase, copyPerLevel, overwriteOnlyMissing);
					showPropagate = false;
				}
				if (GUILayout.Button("Cancel", GUILayout.Width(90))) showPropagate = false;
			}

			EditorGUILayout.EndVertical();
		}

		private void ApplyPropagation(CoreSO source, CoreSO[] targets, bool[] targetFlags, bool copyBase, bool copyPerLevel, bool overwriteOnlyMissing)
		{
			if (source == null) return;
			Undo.SetCurrentGroupName("Propagate Rarity Weights");
			int group = Undo.GetCurrentGroup();

			foreach (var (target, idx) in targets.Select((t, i) => (t, i)))
			{
				if (target == null || !targetFlags[idx] || target == source) continue;
				Undo.RecordObject(target, "Propagate Weights");
				foreach (var rarity in raritiesByTier)
				{
					if (rarity == null) continue;
					var srcRW = GetOrCreateWeight(source, rarity, createIfMissing: false);
					var tgtRW = GetOrCreateWeight(target, rarity, createIfMissing: true);
					if (srcRW == null || tgtRW == null) continue;

					if (overwriteOnlyMissing)
					{
						bool missing = Mathf.Approximately(tgtRW.weight, 0f) && Mathf.Approximately(tgtRW.weightPerLevel, 0f);
						if (!missing) continue;
					}

					if (copyBase) tgtRW.weight = srcRW.weight;
					if (copyPerLevel) tgtRW.weightPerLevel = srcRW.weightPerLevel;
				}
				EditorUtility.SetDirty(target);
			}

			Undo.CollapseUndoOperations(group);
			if (autoSave) AssetDatabase.SaveAssets();
		}

		private void DrawCorePanel(int tierIndex)
		{
			var core = coresByTier[tierIndex];
			EditorGUILayout.BeginVertical("box");
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label($"Tier {tierIndex}", EditorStyles.boldLabel, GUILayout.Width(70));
				if (core != null)
					GUILayout.Label(core.name, EditorStyles.largeLabel);
				else
					GUILayout.Label("(No Core)", EditorStyles.miniBoldLabel);
				GUILayout.FlexibleSpace();
				if (core != null && GUILayout.Button("Fix Missing Rarities", GUILayout.Width(170)))
				{
					AddMissingRarities(core);
				}
			}

			DrawWeightsTable(core);
			DrawCoreSummary(core);
			EditorGUILayout.EndVertical();
		}

		private void DrawWeightsTable(CoreSO core)
		{
			// Header
			using (new EditorGUILayout.HorizontalScope())
			{
                               GUILayout.Label("Rarity", GUILayout.Width(150));
                               GUILayout.Label("Base", GUILayout.Width(70));
                               GUILayout.Label("/Level", GUILayout.Width(70));
                               GUILayout.Label("Global", GUILayout.Width(70));
                               GUILayout.Label("Eff@100", GUILayout.Width(80));
                               GUILayout.Label("Eff@1000", GUILayout.Width(80));
                               GUILayout.Label("Chance@0", GUILayout.Width(80));
                               GUILayout.Label($"Chance@{level}", GUILayout.Width(90));
                               GUILayout.Label("Chance@1000", GUILayout.Width(100));
                       }

                       // Precompute totals
                       float totalAtLevel = 0f;
                       float totalAt0 = 0f;
                       float totalAt1000 = 0f;
                       var effByRarity = new Dictionary<RaritySO, float>();
                       var effAt0ByRarity = new Dictionary<RaritySO, float>();
                       var effAt100ByRarity = new Dictionary<RaritySO, float>();
                       var effAt1000ByRarity = new Dictionary<RaritySO, float>();
                       foreach (var r in raritiesByTier)
                       {
                               float effL = ComputeEffective(core, r, level);
                               effByRarity[r] = effL;
                               totalAtLevel += effL;
                               float e0 = ComputeEffective(core, r, 0);
                               effAt0ByRarity[r] = e0;
                               totalAt0 += e0;
                               float e100 = ComputeEffective(core, r, 100);
                               effAt100ByRarity[r] = e100;
                               float e1000 = ComputeEffective(core, r, 1000);
                               effAt1000ByRarity[r] = e1000;
                               totalAt1000 += e1000;
                       }

			foreach (var rarity in raritiesByTier)
			{
				if (!showZeroRows && Mathf.Approximately(effByRarity[rarity], 0f))
					continue;

				float baseW = GetWeight(core, rarity);
				float perLvl = GetWeightPerLevel(core, rarity);
				float mult = rarity != null ? rarity.globalWeightMultiplier : 1f;
                               float effLevel = effByRarity[rarity];
                               float eff100 = effAt100ByRarity[rarity];
                               float eff1000 = effAt1000ByRarity[rarity];
                               float eff0 = effAt0ByRarity[rarity];
                               float p0 = totalAt0 > 0f ? (eff0 / totalAt0) * 100f : 0f;
                               float pLevel = totalAtLevel > 0f ? (effLevel / totalAtLevel) * 100f : 0f;
                               float p1000 = totalAt1000 > 0f ? (eff1000 / totalAt1000) * 100f : 0f;

                               using (new EditorGUILayout.HorizontalScope())
                               {
                                       var c = rarity != null ? rarity.color : Color.white;
                                       var colorRect = GUILayoutUtility.GetRect(8, 16, GUILayout.Width(16));
                                       EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y + 2, 12, 12), c);
                                       GUILayout.Label(rarity != null ? rarity.GetName() : "(null)", GUILayout.Width(120));

					EditorGUI.BeginChangeCheck();
					float newBase = EditorGUILayout.FloatField(baseW, GUILayout.Width(70));
					float newPer = EditorGUILayout.FloatField(perLvl, GUILayout.Width(70));
					if (EditorGUI.EndChangeCheck() && core != null)
					{
						Undo.RecordObject(core, "Edit Rarity Weight");
						SetWeight(core, rarity, newBase, newPer);
						EditorUtility.SetDirty(core);
						if (autoSave) AssetDatabase.SaveAssets();
					}

                                       GUILayout.Label(applyGlobalMultiplier ? mult.ToString("0.###") : "1", GUILayout.Width(70));
                                       GUILayout.Label(eff100.ToString("0.###"), GUILayout.Width(80));
                                       GUILayout.Label(eff1000.ToString("0.###"), GUILayout.Width(80));
                                       GUILayout.Label(p0.ToString("0.0000"), GUILayout.Width(80));
                                       GUILayout.Label(pLevel.ToString("0.0000"), GUILayout.Width(90));
                                       GUILayout.Label(p1000.ToString("0.0000"), GUILayout.Width(100));
                               }
                       }
               }

		private void DrawCoreSummary(CoreSO core)
		{
			// Summary lines of chances
			var parts = new List<string>(TierCount);
			float total = 0f;
			var effByRarity = new float[raritiesByTier.Count];
			for (int i = 0; i < raritiesByTier.Count; i++)
			{
				var r = raritiesByTier[i];
				var eff = ComputeEffective(core, r, level);
				effByRarity[i] = eff;
				total += eff;
			}

			for (int i = 0; i < raritiesByTier.Count; i++)
			{
				var r = raritiesByTier[i];
				if (!showZeroRows && Mathf.Approximately(effByRarity[i], 0f)) continue;
				float p = total > 0f ? (effByRarity[i] / total) * 100f : 0f;
				parts.Add($"{r.GetName()}: {p:0.###}%");
			}
			EditorGUILayout.LabelField(string.Join("    ", parts));
		}

		private void DrawChartsSection()
		{
			showCharts = EditorGUILayout.Foldout(showCharts, "Charts", true);
			if (!showCharts) return;

			using (new EditorGUILayout.HorizontalScope())
			{
				chartMode = (ChartView)EditorGUILayout.EnumPopup("Mode", chartMode);
				if (chartMode == ChartView.PerCore)
				{
					chartCore = (CoreSO)EditorGUILayout.ObjectField("Core", chartCore, typeof(CoreSO), false);
				}
				else
				{
					chartRarity = (RaritySO)EditorGUILayout.ObjectField("Rarity", chartRarity, typeof(RaritySO), false);
					perRarityCompare = (PerRarityCompare)EditorGUILayout.EnumPopup("Compare", perRarityCompare);
				}
			}

			Rect rect = GUILayoutUtility.GetRect(chartSize.x, chartSize.y);
			EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.3f));
			DrawGrid(rect);

			if (chartMode == ChartView.PerCore)
				DrawPerCoreChart(rect, chartCore);
			else
				DrawPerRarityChart(rect, chartRarity);

			EditorGUILayout.Space(4);
		}

		private void DrawGrid(Rect rect)
		{
			Handles.BeginGUI();
			Handles.color = axisColor;
			// Axes
			Handles.DrawLine(new Vector3(rect.x, rect.yMax, 0), new Vector3(rect.xMax, rect.yMax, 0));
			Handles.DrawLine(new Vector3(rect.x, rect.y, 0), new Vector3(rect.x, rect.yMax, 0));

			Handles.color = gridColor;
			int hLines = 4;
			for (int i = 1; i <= hLines; i++)
			{
				float y = Mathf.Lerp(rect.yMax, rect.y, i / (float)(hLines + 1));
				Handles.DrawLine(new Vector3(rect.x, y, 0), new Vector3(rect.xMax, y, 0));
			}
			int vLines = 10;
			for (int i = 1; i <= vLines; i++)
			{
				float x = Mathf.Lerp(rect.x, rect.xMax, i / (float)(vLines + 1));
				Handles.DrawLine(new Vector3(x, rect.y, 0), new Vector3(x, rect.yMax, 0));
			}
			Handles.EndGUI();

			// Labels
			var labelRect = new Rect(rect.x + 4, rect.y + 4, 120, 18);
			GUI.Label(labelRect, "0% – 100%", EditorStyles.miniLabel);
		}

		private void DrawPerCoreChart(Rect rect, CoreSO core)
		{
			if (core == null) return;
			// Prepare series: 8 rarities
			for (int rIdx = 0; rIdx < raritiesByTier.Count; rIdx++)
			{
				var rarity = raritiesByTier[rIdx];
				var color = rarity != null ? rarity.color : Color.white;
				var pts = new List<Vector3>(MaxChartLevel + 1);
				for (int lv = 0; lv <= MaxChartLevel; lv++)
				{
					var chances = GetChancesForCoreAtLevel(core, lv);
					float yVal = rIdx < chances.Length ? chances[rIdx] : 0f; // 0..1
					float x = Mathf.Lerp(rect.x, rect.xMax, lv / (float)MaxChartLevel);
					float y = Mathf.Lerp(rect.yMax, rect.y, yVal);
					pts.Add(new Vector3(x, y, 0));
				}
				Handles.BeginGUI();
				Handles.color = color;
				Handles.DrawAAPolyLine(2f, pts.ToArray());
				Handles.EndGUI();
			}

			// Legend
			using (new EditorGUILayout.HorizontalScope())
			{
				foreach (var rarity in raritiesByTier)
				{
					var color = rarity != null ? rarity.color : Color.white;
					var rectSwatch = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(18));
					EditorGUI.DrawRect(new Rect(rectSwatch.x, rectSwatch.y + 2, 12, 10), color);
					var style = new GUIStyle(EditorStyles.miniLabel);
					style.normal.textColor = color;
					GUILayout.Label(rarity.GetName(), style, GUILayout.Width(110));
				}
			}
		}

		private void DrawPerRarityChart(Rect rect, RaritySO rarity)
		{
			if (rarity == null) return;
			if (perRarityCompare == PerRarityCompare.ByTier)
			{
				// Compare the same rarity across tiers (0..7). One line per tier, colored by that tier's rarity color.
				for (int tier = 0; tier < TierCount; tier++)
				{
					var core = tier >= 0 && tier < coresByTier.Length ? coresByTier[tier] : null;
					if (core == null) continue;
					// color is the selected rarity's color for consistency across views
					Color lineColor = GetRarityColor(rarity);
					var pts = new List<Vector3>(MaxChartLevel + 1);
					for (int lv = 0; lv <= MaxChartLevel; lv++)
					{
						var chances = GetChancesForCoreAtLevel(core, lv);
						int rIdx = raritiesByTier.IndexOf(rarity);
						float yVal = (rIdx >= 0 && rIdx < chances.Length) ? chances[rIdx] : 0f;
						float x = Mathf.Lerp(rect.x, rect.xMax, lv / (float)MaxChartLevel);
						float y = Mathf.Lerp(rect.yMax, rect.y, yVal);
						pts.Add(new Vector3(x, y, 0));
					}
					Handles.BeginGUI();
					Handles.color = lineColor;
					Handles.DrawAAPolyLine(2f, pts.ToArray());
					Handles.EndGUI();
				}

				// Legend: One swatch using the selected rarity color, labels per tier
				using (new EditorGUILayout.HorizontalScope())
				{
					var color = GetRarityColor(rarity);
					var rectSwatch = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(16));
					EditorGUI.DrawRect(new Rect(rectSwatch.x, rectSwatch.y + 2, 10, 8), color);
					var style = new GUIStyle(EditorStyles.miniLabel);
					style.normal.textColor = color;
					GUILayout.Label($"{rarity.GetName()} across tiers:", style, GUILayout.Width(180));
					for (int tier = 0; tier < TierCount; tier++)
					{
						var core = tier >= 0 && tier < coresByTier.Length ? coresByTier[tier] : null;
						if (core == null) continue;
						GUILayout.Label($"T{tier}", style, GUILayout.Width(28));
					}
				}
			}
			else // ByCore (previous behavior): compare the same rarity across cores with unique per-core colors
			{
				var cores = GetCoreList();
				for (int cIdx = 0; cIdx < cores.Count; cIdx++)
				{
					var core = cores[cIdx];
					if (core == null) continue;
					Color lineColor = Color.HSVToRGB((cIdx / (float)Mathf.Max(1, cores.Count)), 0.7f, 1f);
					var pts = new List<Vector3>(MaxChartLevel + 1);
					for (int lv = 0; lv <= MaxChartLevel; lv++)
					{
						var chances = GetChancesForCoreAtLevel(core, lv);
						int rIdx = raritiesByTier.IndexOf(rarity);
						float yVal = (rIdx >= 0 && rIdx < chances.Length) ? chances[rIdx] : 0f;
						float x = Mathf.Lerp(rect.x, rect.xMax, lv / (float)MaxChartLevel);
						float y = Mathf.Lerp(rect.yMax, rect.y, yVal);
						pts.Add(new Vector3(x, y, 0));
					}
					Handles.BeginGUI();
					Handles.color = lineColor;
					Handles.DrawAAPolyLine(2f, pts.ToArray());
					Handles.EndGUI();
				}

				// Legend: rarity swatch once + per-core colored labels
				using (new EditorGUILayout.HorizontalScope())
				{
					var rarityColor = GetRarityColor(rarity);
					var sw = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(16));
					EditorGUI.DrawRect(new Rect(sw.x, sw.y + 2, 10, 8), rarityColor);
					GUILayout.Label($"{rarity.GetName()}:", GUILayout.Width(120));
					var cores2 = GetCoreList();
					for (int cIdx = 0; cIdx < cores2.Count; cIdx++)
					{
						var core = cores2[cIdx];
						if (core == null) continue;
						Color c = Color.HSVToRGB((cIdx / (float)Mathf.Max(1, cores2.Count)), 0.7f, 1f);
						var style = new GUIStyle(EditorStyles.miniLabel);
						style.normal.textColor = c;
						GUILayout.Label($"T{core.tierIndex}", style, GUILayout.Width(40));
					}
				}
			}
		}

		private Color GetRarityColor(RaritySO rarity)
		{
			return rarity != null ? rarity.color : Color.white;
		}

		private float[] GetChancesForCoreAtLevel(CoreSO core, int lv)
		{
			var eff = new float[raritiesByTier.Count];
			float sum = 0f;
			for (int i = 0; i < raritiesByTier.Count; i++)
			{
				var r = raritiesByTier[i];
				float e = ComputeEffective(core, r, lv);
				eff[i] = e;
				sum += e;
			}
			if (sum <= 0f) return eff.Select(_ => 0f).ToArray();
			for (int i = 0; i < eff.Length; i++) eff[i] = Mathf.Clamp01(eff[i] / sum);
			return eff;
		}

		private float ComputeEffective(CoreSO core, RaritySO rarity, int lv)
		{
			if (core == null || rarity == null) return 0f;
			float baseW = GetWeight(core, rarity);
			float perLvl = GetWeightPerLevel(core, rarity);
			float mult = applyGlobalMultiplier && rarity != null ? rarity.globalWeightMultiplier : 1f;
			return Mathf.Max(0f, baseW * mult + perLvl * lv);
		}

		private float GetWeight(CoreSO core, RaritySO rarity)
		{
			return core != null && rarity != null ? core.GetRarityWeight(rarity) : 0f;
		}

		private float GetWeightPerLevel(CoreSO core, RaritySO rarity)
		{
			return core != null && rarity != null ? core.GetRarityWeightPerLevel(rarity) : 0f;
		}

		private CoreSO[] AddMissingRarities(CoreSO core)
		{
			if (core == null) return Array.Empty<CoreSO>();
			Undo.RecordObject(core, "Add Missing Rarities");
			foreach (var r in raritiesByTier)
			{
				GetOrCreateWeight(core, r, createIfMissing: true);
			}
			EditorUtility.SetDirty(core);
			if (autoSave) AssetDatabase.SaveAssets();
			return new[] { core };
		}

		private RarityWeight GetOrCreateWeight(CoreSO core, RaritySO rarity, bool createIfMissing)
		{
			if (core == null || rarity == null) return null;
			if (core.rarityWeights == null) core.rarityWeights = new List<RarityWeight>();
			for (int i = 0; i < core.rarityWeights.Count; i++)
			{
				var rw = core.rarityWeights[i];
				if (rw != null && rw.rarity == rarity) return rw;
			}
			if (!createIfMissing) return null;
			var created = new RarityWeight { rarity = rarity, weight = 0f, weightPerLevel = 0f };
			core.rarityWeights.Add(created);
			return created;
		}

		private void SetWeight(CoreSO core, RaritySO rarity, float baseWeight, float perLevel)
		{
			var rw = GetOrCreateWeight(core, rarity, createIfMissing: true);
			if (rw == null) return;
			rw.weight = baseWeight;
			rw.weightPerLevel = perLevel;
		}

		private IEnumerable<ValueDropdownItem<CoreSO>> GetCoreDropdown()
		{
			foreach (var c in GetCoreList())
			{
				if (c != null) yield return new ValueDropdownItem<CoreSO>($"{c.tierIndex}: {c.name}", c);
			}
		}

		private IEnumerable<ValueDropdownItem<RaritySO>> GetRarityDropdown()
		{
			for (int i = 0; i < raritiesByTier.Count; i++)
			{
				var r = raritiesByTier[i];
				yield return new ValueDropdownItem<RaritySO>($"{i}: {r.GetName()}", r);
			}
		}

		private List<CoreSO> GetCoreList()
		{
			return coresByTier.ToList();
		}

		private void CopyCsv()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Core,Tier,Rarity,Base,PerLevel,Global,Eff@Level,Chance%");
			for (int t = 0; t < TierCount; t++)
			{
				var core = coresByTier[t];
				if (core == null) continue;
				float total = 0f;
				var eff = new float[raritiesByTier.Count];
				for (int i = 0; i < raritiesByTier.Count; i++)
				{
					eff[i] = ComputeEffective(core, raritiesByTier[i], level);
					total += eff[i];
				}
				for (int i = 0; i < raritiesByTier.Count; i++)
				{
					var r = raritiesByTier[i];
					float baseW = GetWeight(core, r);
					float perLvl = GetWeightPerLevel(core, r);
					float mult = applyGlobalMultiplier ? r.globalWeightMultiplier : 1f;
					float e = eff[i];
					float p = total > 0f ? (e / total) * 100f : 0f;
					sb.AppendLine(string.Join(",", new[]
					{
						Escape(core.name),
						t.ToString(),
						Escape(r.GetName()),
						baseW.ToString("0.###"),
						perLvl.ToString("0.###"),
						mult.ToString("0.###"),
						e.ToString("0.###"),
						p.ToString("0.###")
					}));
				}
			}
			EditorGUIUtility.systemCopyBuffer = sb.ToString();
			ShowNotification(new GUIContent("CSV copied"));
		}

		private static string Escape(string s)
		{
			if (string.IsNullOrEmpty(s)) return "";
			if (s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0)
				return '"' + s.Replace("\"", "\"\"") + '"';
			return s;
		}
	}
}
#endif


