#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TimelessEchoes.Tasks;
using UnityEditor;
using UnityEngine;

namespace TimelessEchoes.EditorTools
{
	/// <summary>
	/// Inspector-style tool to compare looting chest weights and probabilities across distances.
	/// - Loads all TaskData in Assets/Resources/Tasks/Looting
	/// - Sorts by taskID
	/// - Samples raw weights and shaped probabilities across a distance range
	/// - Applies an adjustable AnimationCurve as a multiplier (by normalized distance)
	/// Inspired by GearRollTesterWindow layout and graphing.
	/// </summary>
	public class LootingWeightTesterWindow : OdinEditorWindow
	{
		[MenuItem("Tools/Tasks/Looting Weight Tester")] private static void Open()
		{
			var wnd = GetWindow<LootingWeightTesterWindow>(utility: false, title: "Looting Weight Tester", focus: true);
			wnd.minSize = new Vector2(1080, 720);
			wnd.Show();
		}

		public enum DisplayMode { Probabilities, RawWeights }
		public enum NormalizeMode { GlobalRange, PerTaskRange }

		[BoxGroup("General"), ReadOnly]
		public string info = "Compare chest weights over distance; adjust curve to see shaped probabilities.";

		[BoxGroup("Sampling"), LabelText("Start X"), MinValue(0)]
		public float startDistance = 0f;
		[BoxGroup("Sampling"), LabelText("End X"), MinValue(1)]
		public float endDistance = 3000f;
		[BoxGroup("Sampling"), LabelText("Step"), MinValue(1)]
		public float step = 50f;
		[BoxGroup("Sampling"), LabelText("Display"), EnumToggleButtons]
		public DisplayMode displayMode = DisplayMode.Probabilities;
		[BoxGroup("Sampling"), LabelText("Normalize"), EnumToggleButtons]
		public NormalizeMode normalizeMode = NormalizeMode.PerTaskRange;

		[BoxGroup("Curve"), LabelText("Distance Curve"), PropertySpace(SpaceBefore = 4, SpaceAfter = 0)]
		public AnimationCurve distanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[BoxGroup("Curve")]
		public bool applyCurve = true;
		[BoxGroup("Curve"), InfoBox("Per-task normalization maps X to [minX,maxX]. If maxX is Infinity, uses [minX, End X] for normalization.")]
		[ShowInInspector, ReadOnly]
		private string curveNote => "Curve multiplies TaskData.GetWeight(x).";

		[BoxGroup("Filters"), LabelText("Only Include Tasks In Range"), Tooltip("Exclude tasks with zero weight at the sampled X (before curve).")]
		public bool onlyActiveAtX = false;
		[BoxGroup("Filters"), LabelText("Include All By Default"), Tooltip("Enable all tasks initially.")]
		public bool selectAllOnReload = true;

		private readonly List<TaskData> tasks = new List<TaskData>();
		private readonly Dictionary<TaskData, bool> selection = new Dictionary<TaskData, bool>();
		private readonly Dictionary<TaskData, Color> colorMap = new Dictionary<TaskData, Color>();

		private Vector2 listScroll;
		private Vector2 graphScroll;
		private readonly Color gridColor = new Color(1f, 1f, 1f, 0.08f);
		private readonly Color axisColor = new Color(1f, 1f, 1f, 0.35f);

		protected override void OnEnable()
		{
			base.OnEnable();
			ReloadTasks();
		}

		[Button(ButtonSizes.Medium)]
		private void ReloadTasks()
		{
			tasks.Clear();
			selection.Clear();
			colorMap.Clear();

			var guids = AssetDatabase.FindAssets("t:TaskData", new[] { "Assets/Resources/Tasks/Looting" });
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var td = AssetDatabase.LoadAssetAtPath<TaskData>(path);
				if (td != null) tasks.Add(td);
			}
			tasks.Sort((a, b) => a.taskID.CompareTo(b.taskID));

			var palette = BuildColorPalette(tasks.Count);
			for (int i = 0; i < tasks.Count; i++)
			{
				var t = tasks[i];
				selection[t] = selectAllOnReload;
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
				GUILayout.Label("Looting Weight Tester", EditorStyles.boldLabel, GUILayout.Width(180));
				displayMode = (DisplayMode)EditorGUILayout.EnumPopup(displayMode, GUILayout.Width(150));
				normalizeMode = (NormalizeMode)EditorGUILayout.EnumPopup(normalizeMode, GUILayout.Width(150));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(90))) ReloadTasks();
				if (GUILayout.Button("Copy CSV", EditorStyles.toolbarButton, GUILayout.Width(90))) CopyCsv();
			}
		}

		private void DrawLeftPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(280));
			GUILayout.Label("Tasks (sorted by taskID)", EditorStyles.boldLabel);
			using (var sv = new EditorGUILayout.ScrollViewScope(listScroll, GUILayout.ExpandHeight(true)))
			{
				listScroll = sv.scrollPosition;
				foreach (var t in tasks)
				{
					if (t == null) continue;
					using (new EditorGUILayout.HorizontalScope())
					{
						var col = colorMap.TryGetValue(t, out var c) ? c : Color.white;
						var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(16), GUILayout.Height(16));
						EditorGUI.DrawRect(rect, col);
						var prev = selection.TryGetValue(t, out var sel) ? sel : false;
						var next = EditorGUILayout.Toggle(prev, GUILayout.Width(18));
						if (next != prev) selection[t] = next;
						GUILayout.Label($"{t.taskID:00}  {t.taskName}", GUILayout.ExpandWidth(true));
					}
				}
			}

			EditorGUILayout.Space(6);
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Select All")) SetAllSelection(true);
				if (GUILayout.Button("Select None")) SetAllSelection(false);
			}

			EditorGUILayout.Space(10);
			GUILayout.Label("Sampling", EditorStyles.boldLabel);
			startDistance = EditorGUILayout.FloatField("Start X", startDistance);
			endDistance = EditorGUILayout.FloatField("End X", endDistance);
			step = EditorGUILayout.FloatField("Step", step);
			onlyActiveAtX = EditorGUILayout.Toggle("Only Active At X", onlyActiveAtX);

			EditorGUILayout.Space(10);
			GUILayout.Label("Curve", EditorStyles.boldLabel);
			applyCurve = EditorGUILayout.Toggle("Apply Curve", applyCurve);
			distanceCurve = EditorGUILayout.CurveField("Distance Curve", distanceCurve);
			if (GUILayout.Button("Reset Curve")) distanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

			EditorGUILayout.EndVertical();
		}

		private void DrawRightPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
			GUILayout.Label("Graph", EditorStyles.boldLabel);
			DrawGraph();

			EditorGUILayout.Space(10);
			GUILayout.Label("Snapshot Table", EditorStyles.boldLabel);
			DrawSnapshotTable();
			EditorGUILayout.EndVertical();
		}

		private void DrawGraph()
		{
			var rect = GUILayoutUtility.GetRect(1, 360, GUILayout.ExpandWidth(true));
			EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.25f));
			DrawGrid(rect);

			if (endDistance <= startDistance || step <= 0f) return;

			var active = tasks.Where(t => t != null && selection.TryGetValue(t, out var on) && on).ToList();
			if (active.Count == 0) return;

			var xs = SampleDistances();
			var weightsByTask = new Dictionary<TaskData, float[]>();
			var probsByTask = new Dictionary<TaskData, float[]>();

			// Sample raw and shaped weights
			foreach (var t in active)
			{
				var raw = new float[xs.Length];
				for (int i = 0; i < xs.Length; i++)
				{
					float x = xs[i];
					float w = t.GetWeight(x);
					if (onlyActiveAtX && w <= 0f) { raw[i] = 0f; continue; }
					if (applyCurve)
					{
						float tn = GetNormalizedT(t, x);
						w *= Mathf.Max(0f, distanceCurve != null ? distanceCurve.Evaluate(Mathf.Clamp01(tn)) : 1f);
					}
					raw[i] = Mathf.Max(0f, w);
				}
				weightsByTask[t] = raw;
			}

			// Compute probabilities
			for (int i = 0; i < xs.Length; i++)
			{
				float total = 0f;
				foreach (var t in active) total += weightsByTask[t][i];
				foreach (var t in active)
				{
					if (!probsByTask.TryGetValue(t, out var arr))
						arr = probsByTask[t] = new float[xs.Length];
					arr[i] = total > 0f ? weightsByTask[t][i] / total : 0f;
				}
			}

			// Determine y scale
			float yMin = 0f;
			float yMax = displayMode == DisplayMode.Probabilities
				? 1f
				: Mathf.Max(1e-5f, active.SelectMany(t => weightsByTask[t]).DefaultIfEmpty(0f).Max());

			// Draw axes labels
			DrawXAxisLabels(rect, xs);
			DrawYAxisLabels(rect, yMin, yMax);

			// Draw series
			Handles.BeginGUI();
			foreach (var t in active)
			{
				var col = colorMap.TryGetValue(t, out var c) ? c : Color.white;
				Handles.color = col;
				var pts = new List<Vector3>(xs.Length);
				for (int i = 0; i < xs.Length; i++)
				{
					float v = displayMode == DisplayMode.Probabilities ? probsByTask[t][i] : weightsByTask[t][i];
					float nx = Mathf.InverseLerp(startDistance, endDistance, xs[i]);
					float ny = Mathf.InverseLerp(yMin, yMax, v);
					float px = Mathf.Lerp(rect.x + 4, rect.xMax - 4, nx);
					float py = Mathf.Lerp(rect.yMax - 20, rect.y + 6, ny);
					pts.Add(new Vector3(px, py, 0));
				}
				if (pts.Count >= 2) Handles.DrawAAPolyLine(2.2f, pts.ToArray());
			}
			Handles.EndGUI();
		}

		private void DrawSnapshotTable()
		{
			var active = tasks.Where(t => t != null && selection.TryGetValue(t, out var on) && on).ToList();
			if (active.Count == 0) return;
			var xs = SampleDistances();
			if (xs.Length == 0) return;

			// Pick up to three snapshot Xs: start, mid, end
			var snapXs = new List<float>();
			snapXs.Add(xs.First());
			if (xs.Length >= 3) snapXs.Add(xs[xs.Length / 2]);
			snapXs.Add(xs.Last());
			snapXs = snapXs.Distinct().ToList();

			// Precompute weights
			var samplesAtX = new Dictionary<float, Dictionary<TaskData, (float w, float p)>>();
			foreach (var x in snapXs)
			{
				var weights = new Dictionary<TaskData, float>();
				foreach (var t in active)
				{
					float w = t.GetWeight(x);
					if (applyCurve)
					{
						float tn = GetNormalizedT(t, x);
						w *= Mathf.Max(0f, distanceCurve != null ? distanceCurve.Evaluate(Mathf.Clamp01(tn)) : 1f);
					}
					weights[t] = Mathf.Max(0f, w);
				}
				float total = Mathf.Max(0f, weights.Values.Sum());
				var dict = new Dictionary<TaskData, (float w, float p)>();
				foreach (var kv in weights)
					dict[kv.Key] = (kv.Value, total > 0f ? kv.Value / total : 0f);
				samplesAtX[x] = dict;
			}

			// Header
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("ID", GUILayout.Width(36));
				GUILayout.Label("Name", GUILayout.Width(160));
				foreach (var x in snapXs)
					GUILayout.Label(displayMode == DisplayMode.Probabilities ? $"P@{x:0}" : $"W@{x:0}", GUILayout.Width(80));
			}
			// Rows
			foreach (var t in active.OrderBy(t => t.taskID).ThenBy(t => t.taskName))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Label(t.taskID.ToString(), GUILayout.Width(36));
					GUILayout.Label(t.taskName, GUILayout.Width(160));
					foreach (var x in snapXs)
					{
						var pair = samplesAtX[x][t];
						if (displayMode == DisplayMode.Probabilities)
							GUILayout.Label(pair.p.ToString("0.###"), GUILayout.Width(80));
						else
							GUILayout.Label(pair.w.ToString("0.###"), GUILayout.Width(80));
					}
				}
			}
		}

		private void CopyCsv()
		{
			var active = tasks.Where(t => t != null && selection.TryGetValue(t, out var on) && on).ToList();
			if (active.Count == 0) return;
			if (endDistance <= startDistance || step <= 0f) return;

			var xs = SampleDistances();
			var lines = new List<string>();
			lines.Add("X," + string.Join(",", active.Select(t => SanitizeCsv($"{t.taskID}_{t.taskName}"))));

			// Precompute series
			var weightsByTask = new Dictionary<TaskData, float[]>();
			foreach (var t in active)
			{
				var arr = new float[xs.Length];
				for (int i = 0; i < xs.Length; i++)
				{
					float w = t.GetWeight(xs[i]);
					if (applyCurve)
					{
						float tn = GetNormalizedT(t, xs[i]);
						w *= Mathf.Max(0f, distanceCurve != null ? distanceCurve.Evaluate(Mathf.Clamp01(tn)) : 1f);
					}
					arr[i] = Mathf.Max(0f, w);
				}
				weightsByTask[t] = arr;
			}

			for (int i = 0; i < xs.Length; i++)
			{
				if (displayMode == DisplayMode.Probabilities)
				{
					float total = 0f;
					foreach (var t in active) total += weightsByTask[t][i];
					var probs = active.Select(t => total > 0f ? (weightsByTask[t][i] / total).ToString("0.######") : "0");
					lines.Add(xs[i].ToString("0.######") + "," + string.Join(",", probs));
				}
				else
				{
					var ws = active.Select(t => weightsByTask[t][i].ToString("0.######"));
					lines.Add(xs[i].ToString("0.######") + "," + string.Join(",", ws));
				}
			}

			EditorGUIUtility.systemCopyBuffer = string.Join("\n", lines);
			ShowNotification(new GUIContent("Series CSV copied"));
		}

		private float[] SampleDistances()
		{
			int count = Mathf.Max(1, Mathf.FloorToInt((endDistance - startDistance) / step) + 1);
			var xs = new float[count];
			for (int i = 0; i < count; i++) xs[i] = startDistance + i * step;
			return xs;
		}

		private float GetNormalizedT(TaskData t, float worldX)
		{
			if (normalizeMode == NormalizeMode.GlobalRange)
				return Mathf.InverseLerp(startDistance, endDistance, worldX);

			// Per-task: map [minX, maxX] to [0,1]. If maxX is Infinity or <= minX, fallback to [minX, End X]
			float min = t != null ? t.minX : startDistance;
			float max = t != null ? t.maxX : endDistance;
			if (float.IsInfinity(max) || max <= min) max = Mathf.Max(min + 1f, endDistance);
			return Mathf.InverseLerp(min, max, worldX);
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

		private void DrawXAxisLabels(Rect rect, float[] xs)
		{
			if (xs == null || xs.Length == 0) return;
			int tickCount = Mathf.Min(10, xs.Length);
			for (int i = 0; i <= tickCount; i++)
			{
				float t = i / (float)tickCount;
				float x = Mathf.Lerp(rect.x, rect.xMax, t);
				float v = Mathf.Lerp(startDistance, endDistance, t);
				GUI.Label(new Rect(x - 18, rect.yMax - 18, 60, 16), v.ToString("0"), EditorStyles.miniLabel);
			}
		}

		private void DrawYAxisLabels(Rect rect, float yMin, float yMax)
		{
			int tickCount = 4;
			for (int i = 0; i <= tickCount; i++)
			{
				float t = i / (float)tickCount;
				float y = Mathf.Lerp(rect.yMax - 20, rect.y + 6, t);
				float v = Mathf.Lerp(yMin, yMax, t);
				GUI.Label(new Rect(rect.x + 4, y - 8, 60, 16),
					displayMode == DisplayMode.Probabilities ? v.ToString("0.0#") : v.ToString("0.##"), EditorStyles.miniLabel);
			}
		}

		private void SetAllSelection(bool value)
		{
			foreach (var t in tasks)
				if (t != null) selection[t] = value;
		}

		private static Color[] BuildColorPalette(int count)
		{
			// Pleasant repeating palette
			var baseCols = new[]
			{
				new Color(0.91f, 0.30f, 0.24f), // red
				new Color(0.18f, 0.80f, 0.44f), // green
				new Color(0.20f, 0.60f, 0.86f), // blue
				new Color(0.91f, 0.76f, 0.23f), // yellow
				new Color(0.61f, 0.35f, 0.71f), // purple
				new Color(0.90f, 0.49f, 0.13f), // orange
				new Color(0.20f, 0.29f, 0.37f), // slate
				new Color(0.48f, 0.78f, 0.64f)  // teal
			};
			if (count <= baseCols.Length) return baseCols;
			// Expand slightly by shifting hues
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
	}
}
#endif


