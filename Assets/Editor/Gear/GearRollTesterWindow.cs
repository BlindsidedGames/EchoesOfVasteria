#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using TimelessEchoes.Gear;

namespace TimelessEchoes.EditorTools
{
	/// <summary>
	/// Editor window to simulate gear rolls and visualize stat distributions.
	/// Provides two modes:
	///  - Affix Value Tester: sample one stat inside a chosen rarity band.
	///  - Full Craft Simulator: roll full items using Core → Rarity → Slot → Affixes pipeline.
	/// </summary>
	public class GearRollTesterWindow : OdinEditorWindow
	{
		private const int DefaultSamples = 1000;
		private const int MaxBins = 80;
		private const int TierCount = 8;

		[MenuItem("Tools/Gear/Gear Roll Tester")] private static void Open()
		{
			var wnd = GetWindow<GearRollTesterWindow>(utility: false, title: "Gear Roll Tester", focus: true);
			wnd.minSize = new Vector2(1060, 720);
			wnd.Show();
		}

		public enum Mode { AffixValueTester, FullCraftSimulator }
		public enum RarityMode { UseCoreWeights, ForceSpecific }

		[BoxGroup("General"), EnumToggleButtons]
		public Mode mode = Mode.AffixValueTester;

		[BoxGroup("General"), PropertySpace(SpaceBefore = 2, SpaceAfter = 0)]
		[InfoBox("This tool runs entirely in-editor and doesn't spend resources. It mirrors the runtime roll logic for bands, within-tier curves, and rarity floors.")]
		[InlineButton("ReloadAssets", "Reload Assets")]
		public string info = "";

		// Shared asset caches
		private readonly List<RaritySO> rarities = new List<RaritySO>();
		private readonly List<StatDefSO> stats = new List<StatDefSO>();
		private readonly List<CoreSO> cores = new List<CoreSO>();

		// Affix Value Tester
		[ShowIf("@mode == Mode.AffixValueTester"), BoxGroup("Affix Value Tester"), LabelText("Stat")]
		public StatDefSO stat;
		[ShowIf("@mode == Mode.AffixValueTester"), BoxGroup("Affix Value Tester"), LabelText("Rarity")]
		public RaritySO rarity;
		[ShowIf("@mode == Mode.AffixValueTester"), BoxGroup("Affix Value Tester"), MinValue(10), MaxValue(500000), LabelText("Samples")]
		public int samples = DefaultSamples;
		[ShowIf("@mode == Mode.AffixValueTester"), BoxGroup("Affix Value Tester"), Range(10, MaxBins), LabelText("Bins")]
		public int bins = 50;
		[ShowIf("@mode == Mode.AffixValueTester"), BoxGroup("Affix Value Tester"), LabelText("Overlay Bands for All Rarities"), Tooltip("Draw min/max band windows atop the histogram")] 
		public bool overlayBands = true;
		[ShowIf("@mode == Mode.AffixValueTester"), BoxGroup("Affix Value Tester"), LabelText("Show Floor"), Tooltip("Draw rarity floor value and report what % of samples were clamped to it.")]
		public bool overlayFloor = true;
		[ShowIf("@mode == Mode.AffixValueTester"), BoxGroup("Affix Value Tester"), LabelText("Ignore Jackpot Fields"), Tooltip("Fields removed from code; kept for legacy assets, always ignored."), ReadOnly]
		public bool jackpotIgnored = true;

		// Full craft simulator
		[ShowIf("@mode == Mode.FullCraftSimulator"), BoxGroup("Craft Simulator"), LabelText("Core")]
		public CoreSO core;
		[ShowIf("@mode == Mode.FullCraftSimulator"), BoxGroup("Craft Simulator"), EnumToggleButtons, LabelText("Rarity Source")]
		public RarityMode rarityMode = RarityMode.UseCoreWeights;
		[ShowIf("@mode == Mode.FullCraftSimulator && rarityMode == RarityMode.ForceSpecific"), BoxGroup("Craft Simulator"), LabelText("Force Rarity")]
		public RaritySO forcedRarity;
		[ShowIf("@mode == Mode.FullCraftSimulator"), BoxGroup("Craft Simulator"), MinValue(1), MaxValue(100000), LabelText("Sim Crafts")]
		public int simCrafts = DefaultSamples;
		[ShowIf("@mode == Mode.FullCraftSimulator"), BoxGroup("Craft Simulator"), LabelText("Use Level Scaling"), Tooltip("Adds Core.weightPerLevel × level to each rarity weight")]
		public bool useLevelScaling = false;
		[ShowIf("@mode == Mode.FullCraftSimulator && useLevelScaling"), BoxGroup("Craft Simulator"), MinValue(0), MaxValue(100), LabelText("Level")] 
		public int level = 0;
		[ShowIf("@mode == Mode.FullCraftSimulator"), BoxGroup("Craft Simulator"), LabelText("Apply Global Mult"), Tooltip("Multiply by Rarity.globalWeightMultiplier")] 
		public bool applyGlobalMultiplier = true;
		[ShowIf("@mode == Mode.FullCraftSimulator"), BoxGroup("Craft Simulator"), LabelText("Fixed Slot (optional)"), Tooltip("Leave empty to use default Weapon/Helmet/Chest/Boots distribution.")]
		public string fixedSlot = string.Empty;

		private Vector2 scroll;
		private readonly Color gridColor = new Color(1f, 1f, 1f, 0.08f);
		private readonly Color axisColor = new Color(1f, 1f, 1f, 0.35f);

		// Results
		private float[] lastHistogram = Array.Empty<float>();
		private float lastMin;
		private float lastMax;
		private float lastAvg;
		private float lastMedian;
		private float lastP10;
		private float lastP90;
		private float lastFloorValue;
		private int lastClampedCount;
		private string lastSummary = string.Empty;
		private readonly Dictionary<RaritySO, int> rarityCounts = new Dictionary<RaritySO, int>();

		protected override void OnEnable()
		{
			base.OnEnable();
			ReloadAssets();
		}

		protected override void Initialize()
		{
			ReloadAssets();
		}

		private void ReloadAssets()
		{
			rarities.Clear();
			foreach (var guid in AssetDatabase.FindAssets("t:RaritySO"))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var r = AssetDatabase.LoadAssetAtPath<RaritySO>(path);
				if (r != null) rarities.Add(r);
			}
			rarities.Sort((a, b) => a.tierIndex.CompareTo(b.tierIndex));

			stats.Clear();
			foreach (var guid in AssetDatabase.FindAssets("t:StatDefSO"))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var s = AssetDatabase.LoadAssetAtPath<StatDefSO>(path);
				if (s != null) stats.Add(s);
			}

			cores.Clear();
			foreach (var guid in AssetDatabase.FindAssets("t:CoreSO"))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var c = AssetDatabase.LoadAssetAtPath<CoreSO>(path);
				if (c != null) cores.Add(c);
			}
			cores.Sort((a, b) => a.tierIndex.CompareTo(b.tierIndex));

			if (stat == null) stat = stats.FirstOrDefault();
			if (rarity == null) rarity = rarities.FirstOrDefault();
			if (core == null) core = cores.FirstOrDefault();
		}

		protected override void OnImGUI()
		{
			DrawToolbar();
			EditorGUILayout.Space(6);
			scroll = EditorGUILayout.BeginScrollView(scroll);
			if (mode == Mode.AffixValueTester)
				DrawAffixValueTester();
			else
				DrawCraftSimulator();
			EditorGUILayout.EndScrollView();
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUILayout.Label("Gear Roll Tester", EditorStyles.boldLabel, GUILayout.Width(140));
				mode = (Mode)EditorGUILayout.EnumPopup(mode, GUILayout.Width(200));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(80))) ReloadAssets();
				if (GUILayout.Button("Copy CSV", EditorStyles.toolbarButton, GUILayout.Width(90))) CopyCsv();
			}
		}

		private void DrawAffixValueTester()
		{
			EditorGUILayout.BeginVertical("box");
			stat = (StatDefSO)EditorGUILayout.ObjectField("Stat", stat, typeof(StatDefSO), false);
			rarity = (RaritySO)EditorGUILayout.ObjectField("Rarity", rarity, typeof(RaritySO), false);
			samples = EditorGUILayout.IntSlider("Samples", samples, 10, 500000);
			bins = EditorGUILayout.IntSlider("Bins", bins, 10, MaxBins);
			overlayBands = EditorGUILayout.Toggle("Overlay Bands", overlayBands);

			EditorGUILayout.Space(4);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Run", GUILayout.Width(120), GUILayout.Height(26)))
				{
					RunAffixSampling();
				}
			}

			EditorGUILayout.Space(6);
			DrawHistogram();
			DrawSummaryBox();
			EditorGUILayout.EndVertical();
		}

		private void DrawCraftSimulator()
		{
			EditorGUILayout.BeginVertical("box");
			core = (CoreSO)EditorGUILayout.ObjectField("Core", core, typeof(CoreSO), false);
			rarityMode = (RarityMode)EditorGUILayout.EnumPopup("Rarity Source", rarityMode);
			if (rarityMode == RarityMode.ForceSpecific)
				forcedRarity = (RaritySO)EditorGUILayout.ObjectField("Force Rarity", forcedRarity, typeof(RaritySO), false);
			simCrafts = EditorGUILayout.IntSlider("Sim Crafts", simCrafts, 1, 100000);
			useLevelScaling = EditorGUILayout.Toggle("Use Level Scaling", useLevelScaling);
			if (useLevelScaling) level = EditorGUILayout.IntSlider("Level", level, 0, 100);
			applyGlobalMultiplier = EditorGUILayout.Toggle("Apply Global Mult", applyGlobalMultiplier);
			fixedSlot = EditorGUILayout.TextField("Fixed Slot (optional)", fixedSlot);

			EditorGUILayout.Space(4);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Simulate", GUILayout.Width(120), GUILayout.Height(26)))
				{
					RunCraftSimulation();
				}
			}

			EditorGUILayout.Space(6);
			DrawRarityResults();
			EditorGUILayout.EndVertical();
		}

		private void DrawHistogram()
		{
			if (lastHistogram == null || lastHistogram.Length == 0) return;
			Rect rect = GUILayoutUtility.GetRect(980, 260);
			EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.25f));
			DrawGrid(rect);

			float maxCount = Mathf.Max(1f, lastHistogram.Max());
			float barW = (rect.width - 6) / lastHistogram.Length;
			for (int i = 0; i < lastHistogram.Length; i++)
			{
				float t = i / (float)lastHistogram.Length;
				float nextT = (i + 1) / (float)lastHistogram.Length;
				float x = Mathf.Lerp(rect.x + 3, rect.xMax - barW - 3, t);
				float h = Mathf.Lerp(0, rect.height - 24, lastHistogram[i] / maxCount);
				var br = new Rect(x, rect.yMax - 18 - h, barW * 0.95f, h);
				EditorGUI.DrawRect(br, new Color(0.3f, 0.8f, 0.4f, 0.9f));
				// Bin labels at bottom grid every ~10 bins
				if (i % Mathf.Max(1, lastHistogram.Length / 10) == 0)
				{
					string label = Mathf.Lerp(lastMin, lastMax, t).ToString("0.###");
					GUI.Label(new Rect(x, rect.yMax - 16, barW + 4, 16), label, EditorStyles.miniLabel);
				}
			}

			// Overlay band window for the selected rarity
			if (overlayBands && stat != null && rarity != null)
			{
				var band = stat.GetBandForRarity(rarity);
				if (band != null)
				{
					float minV = Mathf.Lerp(stat.minRoll, stat.maxRoll, band.GetClampedMin());
					float maxV = Mathf.Lerp(stat.minRoll, stat.maxRoll, band.GetClampedMax());
					float x0 = Mathf.Lerp(rect.x, rect.xMax, Mathf.InverseLerp(lastMin, lastMax, minV));
					float x1 = Mathf.Lerp(rect.x, rect.xMax, Mathf.InverseLerp(lastMin, lastMax, maxV));
					var overlay = new Rect(Mathf.Min(x0, x1), rect.y + 4, Mathf.Abs(x1 - x0), rect.height - 24);
					EditorGUI.DrawRect(overlay, new Color(1f, 1f, 1f, 0.06f));
					Handles.BeginGUI();
					Handles.color = new Color(1f, 1f, 1f, 0.2f);
					Handles.DrawAAPolyLine(2f, new Vector3(x0, rect.y + 4, 0), new Vector3(x0, rect.yMax - 20, 0));
					Handles.DrawAAPolyLine(2f, new Vector3(x1, rect.y + 4, 0), new Vector3(x1, rect.yMax - 20, 0));
					Handles.EndGUI();
				}
			}

			// Floor line
			if (overlayFloor && stat != null && rarity != null)
			{
				float xF = Mathf.Lerp(rect.x, rect.xMax, Mathf.InverseLerp(lastMin, lastMax, lastFloorValue));
				Handles.BeginGUI();
				Handles.color = new Color(1f, 0.4f, 0.2f, 0.7f);
				Handles.DrawAAPolyLine(2.5f, new Vector3(xF, rect.y + 2, 0), new Vector3(xF, rect.yMax - 18, 0));
				Handles.EndGUI();
			}
		}

		private void DrawGrid(Rect rect)
		{
			Handles.BeginGUI();
			Handles.color = axisColor;
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
		}

		private void DrawSummaryBox()
		{
			if (string.IsNullOrEmpty(lastSummary)) return;
			EditorGUILayout.HelpBox(lastSummary, MessageType.Info);
		}

		private void DrawRarityResults()
		{
			if (rarityCounts.Count == 0) return;
			int total = rarityCounts.Values.Sum();
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label($"Results for {total} crafts:", EditorStyles.boldLabel);
			foreach (var r in rarities)
			{
				int c = rarityCounts.TryGetValue(r, out var v) ? v : 0;
				float p = total > 0 ? (c / (float)total) * 100f : 0f;
				var style = new GUIStyle(EditorStyles.label);
				style.normal.textColor = r != null ? r.color : Color.white;
				GUILayout.Label($"{r.GetName()}: {c} ({p:0.###}%)", style);
			}
			EditorGUILayout.EndVertical();
		}

		private void RunAffixSampling()
		{
			if (stat == null || rarity == null) return;
			var values = new List<float>(samples);
			lastClampedCount = 0;
			lastFloorValue = Mathf.Lerp(stat.minRoll, stat.maxRoll, rarity != null ? Mathf.Clamp01(rarity.floorPercent / 100f) : 0f);
			for (int i = 0; i < samples; i++)
			{
				float v = RollAffixValue(stat, rarity);
				values.Add(v);
				if (Mathf.Abs(v - lastFloorValue) <= 1e-5f) lastClampedCount++;
			}

			values.Sort();
			lastMin = values.FirstOrDefault();
			lastMax = values.LastOrDefault();
			lastAvg = values.Average();
			lastMedian = values[values.Count / 2];
			lastP10 = Percentile(values, 0.10f);
			lastP90 = Percentile(values, 0.90f);

			lastHistogram = BuildHistogram(values, bins, out float histMin, out float histMax);
			lastMin = histMin; lastMax = histMax;

			var range = stat.maxRoll - stat.minRoll;
			float pctOfMaxAvg = range > 0.0001f ? Mathf.InverseLerp(stat.minRoll, stat.maxRoll, lastAvg) * 100f : 0f;
			float clampedPct = samples > 0 ? (lastClampedCount / (float)samples) * 100f : 0f;
			lastSummary = $"Avg: {lastAvg:0.###}  |  Median: {lastMedian:0.###}  |  P10: {lastP10:0.###}  |  P90: {lastP90:0.###}  |  Min: {lastMin:0.###}  |  Max: {lastMax:0.###}  |  Avg% of Max: {pctOfMaxAvg:0.##}%  |  Clamped@Floor: {lastClampedCount} ({clampedPct:0.##}%)";
		}

		private void RunCraftSimulation()
		{
			rarityCounts.Clear();
			if (core == null) return;
			for (int i = 0; i < simCrafts; i++)
			{
				var r = rarityMode == RarityMode.ForceSpecific ? (forcedRarity ?? rarities.FirstOrDefault()) : RollRarityForCore(core);
				if (r != null)
				{
					int count;
					rarityCounts.TryGetValue(r, out count);
					rarityCounts[r] = count + 1;
				}
				// Optionally roll affixes, but for summary we only show rarity distribution. Could be extended to per-stat charts.
			}
		}

		private RaritySO RollRarityForCore(CoreSO c)
		{
			if (c == null) return rarities.FirstOrDefault();
			var weights = new List<(RaritySO r, float w)>();
			foreach (var r in rarities)
			{
				float baseW = c.GetRarityWeight(r);
				float perLevel = useLevelScaling ? c.GetRarityWeightPerLevel(r) * level : 0f;
				float mult = applyGlobalMultiplier && r != null ? r.globalWeightMultiplier : 1f;
				float w = Mathf.Max(0f, (baseW + perLevel) * mult);
				if (w > 0f) weights.Add((r, w));
			}
			float total = weights.Sum(t => t.w);
			if (total <= 0f) return rarities.FirstOrDefault();
			float roll = UnityEngine.Random.value * total;
			foreach (var (r, w) in weights)
			{
				if (roll <= w) return r;
				roll -= w;
			}
			return weights.Last().r;
		}

		private float RollAffixValue(StatDefSO def, RaritySO r)
		{
			if (def == null) return 0f;
			var band = def.GetBandForRarity(r);
			float t;
			// Jackpot disabled: always sample within band
			float u = UnityEngine.Random.value;
			float shaped = (band != null && band.withinTierCurve != null) ? band.withinTierCurve.Evaluate(u) : u;
			float minQ = band != null ? band.GetClampedMin() : 0f;
			float maxQ = band != null ? band.GetClampedMax() : 1f;
			if (maxQ < minQ) { var tmp = minQ; minQ = maxQ; maxQ = tmp; }
			t = Mathf.Lerp(minQ, maxQ, Mathf.Clamp01(shaped));
			float v = def.RemapRoll(t);
			float floorQ = r != null ? Mathf.Clamp01(r.floorPercent / 100f) : 0f;
			float floorValue = Mathf.Lerp(def.minRoll, def.maxRoll, floorQ);
			return Mathf.Max(v, floorValue);
		}

		private static float[] BuildHistogram(List<float> sortedValues, int binCount, out float histMin, out float histMax)
		{
			if (sortedValues == null || sortedValues.Count == 0) { histMin = 0f; histMax = 1f; return Array.Empty<float>(); }
			histMin = sortedValues.First();
			histMax = sortedValues.Last();
			binCount = Mathf.Clamp(binCount, 1, MaxBins);
			var bins = new float[binCount];
			float range = Mathf.Max(0.000001f, histMax - histMin);
			for (int i = 0; i < sortedValues.Count; i++)
			{
				int idx = Mathf.Clamp((int)((sortedValues[i] - histMin) / range * (binCount - 1)), 0, binCount - 1);
				bins[idx] += 1f;
			}
			return bins;
		}

		private static float Percentile(IReadOnlyList<float> sortedValues, float pct)
		{
			if (sortedValues == null || sortedValues.Count == 0) return 0f;
			pct = Mathf.Clamp01(pct);
			float pos = pct * (sortedValues.Count - 1);
			int lo = Mathf.FloorToInt(pos);
			int hi = Mathf.CeilToInt(pos);
			if (lo == hi) return sortedValues[lo];
			float lerp = pos - lo;
			return Mathf.Lerp(sortedValues[lo], sortedValues[hi], lerp);
		}

		private void CopyCsv()
		{
			if (mode == Mode.AffixValueTester)
			{
				if (lastHistogram == null || lastHistogram.Length == 0 || stat == null || rarity == null) return;
				var lines = new List<string> { "Value,Count" };
				for (int i = 0; i < lastHistogram.Length; i++)
				{
					float t = i / (float)lastHistogram.Length;
					float v = Mathf.Lerp(lastMin, lastMax, t);
					lines.Add($"{v:0.######},{lastHistogram[i]:0}");
				}
				EditorGUIUtility.systemCopyBuffer = string.Join("\n", lines);
				ShowNotification(new GUIContent("Histogram CSV copied"));
			}
			else
			{
				if (rarityCounts.Count == 0) return;
				var lines = new List<string> { "Rarity,Count,Percent" };
				int total = rarityCounts.Values.Sum();
				foreach (var r in rarities)
				{
					int c = rarityCounts.TryGetValue(r, out var v) ? v : 0;
					float p = total > 0 ? (c / (float)total) * 100f : 0f;
					lines.Add($"{r.GetName()},{c},{p:0.###}");
				}
				EditorGUIUtility.systemCopyBuffer = string.Join("\n", lines);
				ShowNotification(new GUIContent("Rarity CSV copied"));
			}
		}
	}
}
#endif


