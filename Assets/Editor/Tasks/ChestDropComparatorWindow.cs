#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;

namespace TimelessEchoes.EditorTools
{
	/// <summary>
        /// Compare per-item drop outcomes between all chest tasks at a given distance using weighted slot rolls.
        /// - Loads Looting tasks (TaskData) and sorts by taskID
        /// - Simulates GenerateDrops logic (ResourceGeneratingTask) for accuracy
        /// - Reports per-resource: probability of any drop and expected count per chest
	/// </summary>
	public class ChestDropComparatorWindow : OdinEditorWindow
	{
		[MenuItem("Tools/Tasks/Chest Drop Comparator")] private static void Open()
		{
			var wnd = GetWindow<ChestDropComparatorWindow>(utility: false, title: "Chest Drop Comparator", focus: true);
			wnd.minSize = new Vector2(1100, 720);
			wnd.Show();
		}

		[BoxGroup("Sampling"), LabelText("Distance X"), MinValue(0)]
		public float distanceX = 1000f;
		[BoxGroup("Sampling"), LabelText("Samples"), MinValue(100), MaxValue(200000)]
		public int samples = 5000;
		[BoxGroup("Sampling"), LabelText("Assume Quests Completed")] 
		public bool assumeQuestsCompleted = true;
		[BoxGroup("Filters"), LabelText("Resource Filter (name contains)"), PropertySpace(SpaceBefore = 4, SpaceAfter = 0)]
		public string resourceNameFilter = string.Empty;
		[BoxGroup("Filters"), LabelText("Only Show Resources With Any Chance")] 
		public bool onlyShowWithAnyChance = true;
		[BoxGroup("Display"), LabelText("Sort Resources By"), EnumToggleButtons]
		public ResourceSort resourceSort = ResourceSort.ById;
		[BoxGroup("Display"), LabelText("Value"), EnumToggleButtons]
		public ValueMode valueMode = ValueMode.Probability; // Toggle per-cell main value
		[BoxGroup("Display"), LabelText("Weight By Chest Spawn Probability"), Tooltip("Compute an Overall column by weighting each chest's stats by its spawn probability at Distance X.")]
		public bool weightBySpawnProbability = false;
		[BoxGroup("Display"), LabelText("Expected as Hits/Samples"), Tooltip("If enabled and Value = ExpectedCount, show number of chests that dropped the resource (hits) over Samples, instead of the average amount.")]
		public bool showExpectedAsFraction = false;
		[BoxGroup("Display"), LabelText("Show Additional Loot Chances"), Tooltip("Display each chest's configured additional loot chances under the table header.")]
		public bool showAdditionalLootChances = true;
		[BoxGroup("Display"), LabelText("Wrap Loot Rolls (2 lines)"), Tooltip("Wrap the Loot Rolls row to up to two lines and increase column width by 50% for readability.")]
		public bool wrapLootRolls = false;

		private readonly List<TaskData> chests = new List<TaskData>();
		private readonly Dictionary<TaskData, bool> selection = new Dictionary<TaskData, bool>();
		private readonly Dictionary<TaskData, Color> colorMap = new Dictionary<TaskData, Color>();

		private Vector2 leftScroll;
		private Vector2 tableScroll;

		public enum ResourceSort { ById, ByName }
		public enum ValueMode { Probability, ExpectedCount }

		protected override void OnEnable()
		{
			base.OnEnable();
			ReloadChests();
		}

		[Button(ButtonSizes.Medium)]
		private void ReloadChests()
		{
			chests.Clear();
			selection.Clear();
			colorMap.Clear();
			var guids = AssetDatabase.FindAssets("t:TaskData", new[] { "Assets/Resources/Tasks/Looting" });
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var td = AssetDatabase.LoadAssetAtPath<TaskData>(path);
				if (td != null) chests.Add(td);
			}
			chests.Sort((a, b) => a.taskID.CompareTo(b.taskID));
			var palette = BuildColorPalette(chests.Count);
			for (int i = 0; i < chests.Count; i++)
			{
				var t = chests[i];
				selection[t] = true;
				colorMap[t] = palette[i % palette.Length];
			}
		}

		protected override void OnImGUI()
		{
			DrawToolbar();
			EditorGUILayout.Space(6);
			using (new EditorGUILayout.HorizontalScope())
			{
				DrawLeftPanel();
				DrawRightPanel();
			}
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUILayout.Label("Chest Drop Comparator", EditorStyles.boldLabel, GUILayout.Width(200));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(90))) ReloadChests();
				if (GUILayout.Button("Copy CSV", EditorStyles.toolbarButton, GUILayout.Width(90))) CopyCsv();
			}
		}

		private void DrawLeftPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(300));
			GUILayout.Label("Chests (by taskID)", EditorStyles.boldLabel);
			using (var sv = new EditorGUILayout.ScrollViewScope(leftScroll, GUILayout.ExpandHeight(true)))
			{
				leftScroll = sv.scrollPosition;
				foreach (var t in chests)
				{
					if (t == null) continue;
					using (new EditorGUILayout.HorizontalScope())
					{
						var col = colorMap.TryGetValue(t, out var c) ? c : Color.white;
						var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(16), GUILayout.Height(16));
						EditorGUI.DrawRect(rect, col);
						var prev = selection.TryGetValue(t, out var on) && on;
						var next = EditorGUILayout.Toggle(prev, GUILayout.Width(18));
						if (next != prev) selection[t] = next;
						GUILayout.Label($"{t.taskID:00}  {t.taskName}", GUILayout.ExpandWidth(true));
					}
				}
			}

			EditorGUILayout.Space(8);
			GUILayout.Label("Sampling", EditorStyles.boldLabel);
			distanceX = EditorGUILayout.FloatField("Distance X", distanceX);
			samples = EditorGUILayout.IntSlider("Samples", samples, 10, 10000);
			assumeQuestsCompleted = EditorGUILayout.Toggle("Assume Quests Completed", assumeQuestsCompleted);

			EditorGUILayout.Space(8);
			GUILayout.Label("Filters", EditorStyles.boldLabel);
			resourceNameFilter = EditorGUILayout.TextField("Resource Filter", resourceNameFilter);
			onlyShowWithAnyChance = EditorGUILayout.Toggle("Only Show With Any Chance", onlyShowWithAnyChance);

			EditorGUILayout.Space(8);
			GUILayout.Label("Display", EditorStyles.boldLabel);
			resourceSort = (ResourceSort)EditorGUILayout.EnumPopup("Sort Resources By", resourceSort);
			valueMode = (ValueMode)EditorGUILayout.EnumPopup("Value", valueMode);
			if (valueMode == ValueMode.ExpectedCount)
				showExpectedAsFraction = EditorGUILayout.Toggle("Expected as Hits/Samples", showExpectedAsFraction);
			weightBySpawnProbability = EditorGUILayout.Toggle("Weight By Chest Spawn Probability", weightBySpawnProbability);
			showAdditionalLootChances = EditorGUILayout.Toggle("Show Additional Loot Chances", showAdditionalLootChances);
			if (showAdditionalLootChances)
				wrapLootRolls = EditorGUILayout.Toggle("Wrap Loot Rolls (2 lines)", wrapLootRolls);

			EditorGUILayout.Space(8);
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Select All")) SetAllSelection(true);
				if (GUILayout.Button("Select None")) SetAllSelection(false);
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawRightPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
			GUILayout.Label("Drop Comparison Table", EditorStyles.boldLabel);
			DrawComparisonTable();
			EditorGUILayout.EndVertical();
		}

		private void DrawComparisonTable()
		{
			var sel = chests.Where(t => t != null && selection.TryGetValue(t, out var on) && on).ToList();
			if (sel.Count == 0) return;

			// Build unified resource list
			var resourceSet = new Dictionary<Resource, (string name, int id)>();
			foreach (var t in sel)
				foreach (var d in t.resourceDrops)
					if (d != null && d.resource != null && !resourceSet.ContainsKey(d.resource))
						resourceSet[d.resource] = (d.resource.name, d.resource.resourceID);

			var resources = resourceSet.Keys.ToList();
			if (resourceSort == ResourceSort.ById)
				resources.Sort((a, b) => resourceSet[a].id.CompareTo(resourceSet[b].id));
			else
				resources.Sort((a, b) => string.Compare(resourceSet[a].name, resourceSet[b].name, StringComparison.Ordinal));

			if (!string.IsNullOrWhiteSpace(resourceNameFilter))
				resources = resources.Where(r => resourceSet[r].name.IndexOf(resourceNameFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

			// Simulate
			var results = SimulateDrops(sel, resources, distanceX, samples, assumeQuestsCompleted, showAdditionalLootChances);

			// Optionally filter out rows with all zeros
			if (onlyShowWithAnyChance)
				resources = resources.Where(r => results.Any(kv => kv.Value.TryGetValue(r, out var s) && s.probability > 0f)).ToList();

			// Header
			var chestColWidth = showAdditionalLootChances ? (wrapLootRolls ? 225f : 180f) : 150f;
			using (var sv = new EditorGUILayout.ScrollViewScope(tableScroll))
			{
				tableScroll = sv.scrollPosition;
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Label("Resource", GUILayout.Width(220));
					foreach (var t in sel)
					{
						GUILayout.Label($"{t.taskID:00} {t.taskName}", GUILayout.Width(chestColWidth));
					}
					if (weightBySpawnProbability)
						GUILayout.Label("Overall", GUILayout.Width(150));
				}

				// Optional row: show configured additional loot chances per chest
				if (showAdditionalLootChances)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Label("Loot Rolls", GUILayout.Width(220));
						GUIStyle lootStyle = null;
						if (wrapLootRolls)
						{
							lootStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
						}
						foreach (var t in sel)
						{
							if (lootStyle != null)
								GUILayout.Label(FormatLootChances(t), lootStyle, GUILayout.Width(chestColWidth));
							else
								GUILayout.Label(FormatLootChances(t), GUILayout.Width(chestColWidth));
						}
						if (weightBySpawnProbability)
							GUILayout.Label("—", GUILayout.Width(150));
					}
				}

				// Rows
				foreach (var r in resources)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Label($"{resourceSet[r].id:000}  {resourceSet[r].name}", GUILayout.Width(220));
						foreach (var t in sel)
						{
							if (!results.TryGetValue(t, out var map) || !map.TryGetValue(r, out var stat))
							{
								GUILayout.Label("-", GUILayout.Width(150));
								continue;
							}
							string text;
							if (valueMode == ValueMode.Probability)
							{
								text = $"{stat.probability * 100f:0.###}%";
							}
							else
							{
								text = showExpectedAsFraction
									? $"{FormatInt(Mathf.RoundToInt(stat.probability * samples))} / {FormatInt(samples)}"
									: FormatDouble(stat.expectedCount);
							}
							GUILayout.Label(text, GUILayout.Width(chestColWidth));
						}
						if (weightBySpawnProbability)
						{
							var overall = ComputeWeightedOverall(sel, results, r, distanceX);
							string text;
							if (valueMode == ValueMode.Probability)
							{
								text = $"{overall.probability * 100f:0.###}%";
							}
							else
							{
								text = showExpectedAsFraction
									? $"{FormatInt(Mathf.RoundToInt(overall.probability * samples))} / {FormatInt(samples)}"
									: FormatDouble(overall.expectedCount);
							}
							GUILayout.Label(text, GUILayout.Width(150));
						}
					}
				}
			}
		}

		private (float probability, float expectedCount) ComputeWeightedOverall(
			List<TaskData> sel,
			Dictionary<TaskData, Dictionary<Resource, (float probability, float expectedCount)>> stats,
			Resource r,
			float x)
		{
			float totalW = 0f;
			var weights = new List<(TaskData t, float w)>();
			foreach (var t in sel)
			{
				float w = Mathf.Max(0f, t.GetWeight(x));
				weights.Add((t, w));
				totalW += w;
			}
			if (totalW <= 0f) return (0f, 0f);
			float p = 0f;
			float e = 0f;
			foreach (var pair in weights)
			{
				float pi = pair.w / totalW;
				if (stats.TryGetValue(pair.t, out var map) && map.TryGetValue(r, out var s))
				{
					p += pi * s.probability;
					e += pi * s.expectedCount;
				}
			}
			return (p, e);
		}

		private Dictionary<TaskData, Dictionary<Resource, (float probability, float expectedCount)>> SimulateDrops(
			List<TaskData> sel,
			List<Resource> resources,
			float x,
			int count,
			bool assumeQuests,
			bool includeAdditionalLootChances)
		{
			var rng = new System.Random(12345);
			float Rand01() => (float)rng.NextDouble();

			var results = new Dictionary<TaskData, Dictionary<Resource, (float p, float e)>>();
			foreach (var t in sel)
			{
				var map = new Dictionary<Resource, (float p, float e)>();
				results[t] = map;
				foreach (var r in resources) map[r] = (0f, 0f);

                                for (int i = 0; i < count; i++)
                                {
                                        var produced = new Dictionary<Resource, int>();
                                        var rolled = DropResolver.RollDrops(t.resourceDrops, includeAdditionalLootChances ? t.additionalLootChances : null, x, assumeQuests, Rand01);
                                        foreach (var res in rolled)
                                        {
                                                produced.TryGetValue(res.resource, out var prev);
                                                produced[res.resource] = prev + res.count;
                                        }

                                        foreach (var r in resources)
                                        {
                                                produced.TryGetValue(r, out var c);
                                                var s = map[r];
                                                map[r] = (s.p + (c > 0 ? 1f : 0f), s.e + c);
                                        }
                                }

				// Normalize to probabilities / expected per run
				foreach (var r in resources)
				{
					var s = map[r];
					map[r] = (count > 0 ? s.p / count : 0f, count > 0 ? s.e / count : 0f);
				}
			}

			return results;
		}

		private void CopyCsv()
		{
			var sel = chests.Where(t => t != null && selection.TryGetValue(t, out var on) && on).ToList();
			if (sel.Count == 0) return;

			// Build unified resource list
			var resourceSet = new Dictionary<Resource, (string name, int id)>();
			foreach (var t in sel)
				foreach (var d in t.resourceDrops)
					if (d != null && d.resource != null && !resourceSet.ContainsKey(d.resource))
						resourceSet[d.resource] = (d.resource.name, d.resource.resourceID);

			var resources = resourceSet.Keys.ToList();
			resources.Sort((a, b) => resourceSet[a].id.CompareTo(resourceSet[b].id));

			var stats = SimulateDrops(sel, resources, distanceX, samples, assumeQuestsCompleted, showAdditionalLootChances);

			var lines = new List<string>();
			lines.Add("Resource," + string.Join(",", sel.Select(t => SanitizeCsv($"{t.taskID}_{t.taskName}"))));
			foreach (var r in resources)
			{
				var row = new List<string> { SanitizeCsv($"{resourceSet[r].id}_{resourceSet[r].name}") };
				foreach (var t in sel)
				{
					if (!stats.TryGetValue(t, out var map) || !map.TryGetValue(r, out var s)) { row.Add("0"); continue; }
					row.Add(valueMode == ValueMode.Probability ? (s.probability * 100f).ToString("0.######") : s.expectedCount.ToString("0.######"));
				}
				lines.Add(string.Join(",", row));
			}

			EditorGUIUtility.systemCopyBuffer = string.Join("\n", lines);
			ShowNotification(new GUIContent("Drop table CSV copied"));
		}

		private void SetAllSelection(bool v)
		{
			foreach (var t in chests) if (t != null) selection[t] = v;
		}

		private static Color[] BuildColorPalette(int count)
		{
			var baseCols = new[]
			{
				new Color(0.91f, 0.30f, 0.24f),
				new Color(0.18f, 0.80f, 0.44f),
				new Color(0.20f, 0.60f, 0.86f),
				new Color(0.91f, 0.76f, 0.23f),
				new Color(0.61f, 0.35f, 0.71f),
				new Color(0.90f, 0.49f, 0.13f),
				new Color(0.20f, 0.29f, 0.37f),
				new Color(0.48f, 0.78f, 0.64f)
			};
			if (count <= baseCols.Length) return baseCols;
			var list = new List<Color>();
			for (int i = 0; i < count; i++)
			{
				var c = baseCols[i % baseCols.Length];
				Color.RGBToHSV(c, out var h, out var s, out var v);
				float shift = ((i / baseCols.Length) % 4) * 0.06f;
				var nc = Color.HSVToRGB(Mathf.Repeat(h + shift, 1f), s, v);
				list.Add(nc);
			}
			return list.ToArray();
		}

		private static string SanitizeCsv(string s)
		{
			if (string.IsNullOrEmpty(s)) return string.Empty;
			s = s.Replace(',', ';');
			s = s.Replace('\n', ' ');
			s = s.Replace('\r', ' ');
			return s;
		}

		private static string FormatLootChances(TaskData t)
		{
			if (t == null || t.additionalLootChances == null || t.additionalLootChances.Count == 0)
				return "1"; // one guaranteed slot
			var parts = t.additionalLootChances.Select(p => $"{Mathf.Clamp01(p) * 100f:0.###}%");
			return $"1 + {string.Join(", ", parts)}";
		}

		private static string FormatInt(int v)
		{
			return v.ToString("N0", CultureInfo.InvariantCulture);
		}

		private static string FormatDouble(float v)
		{
			return ((double)v).ToString("#,0.###", CultureInfo.InvariantCulture);
		}
	}
}
#endif


