#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using TimelessEchoes.Enemies;

namespace TimelessEchoes.Editor
{
	public class EnemyBalanceWindow : EditorWindow
	{
		private enum ViewMode { Level, Distance }

		private const float FirstColumnWidth = 240f; // Enemy/Stat column width
		private const float BaseWidth = 80f;
		private const float PerWidth = 80f;
		private const float ValWidth = 80f;
		private static readonly int[] PreviewLevels = { 1, 500, 1000 };
		private static readonly float[] PreviewWorldX = { 50f, 500f, 1000f, 5000f, 10000f }; // absolute world distances

		private ViewMode mode = ViewMode.Distance;
		private float spawnOffset;
		private int customLevel = 250;
		private float customDistance = 250f; // absolute world distance
		private string customDistanceStr = string.Empty;
		private string search = string.Empty;
		private Vector2 scroll;

		private List<EnemyData> enemies = new();

		[MenuItem("Tools/Enemy Balance")] private static void Open()
		{
			GetWindow<EnemyBalanceWindow>().Show();
		}

		private void OnEnable()
		{
			Reload();
			customDistanceStr = FormatHeaderNumber(customDistance);
		}

		private void Reload()
		{
			var list = new List<EnemyData>();
			foreach (var guid in AssetDatabase.FindAssets("t:EnemyData"))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
				if (asset != null) list.Add(asset);
			}
			enemies = list
				.OrderBy(e => e.displayOrder)
				.ThenBy(e => e.enemyName)
				.ToList();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Enemy Balance", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			mode = (ViewMode)EditorGUILayout.EnumPopup("Mode", mode);
			spawnOffset = EditorGUILayout.FloatField(new GUIContent("Spawn Offset"), spawnOffset);
			if (GUILayout.Button("Refresh", GUILayout.Width(80))) Reload();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			if (mode == ViewMode.Level)
				customLevel = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Custom Level"), customLevel));
			search = EditorGUILayout.TextField("Search", search);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(6);
			scroll = EditorGUILayout.BeginScrollView(scroll);

			DrawHeaderRow();

			var filtered = string.IsNullOrWhiteSpace(search)
				? enemies
				: enemies.Where(e =>
					(e != null && !string.IsNullOrWhiteSpace(e.enemyName) && e.enemyName.ToLowerInvariant().Contains(search.ToLowerInvariant()))
				).ToList();

			foreach (var e in filtered)
			{
				if (e == null) continue;
				DrawEnemyBlock(e);
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawHeaderRow()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Enemy / Stat", EditorStyles.boldLabel, GUILayout.Width(FirstColumnWidth));
			EditorGUILayout.LabelField("Base", EditorStyles.boldLabel, GUILayout.Width(BaseWidth));
			EditorGUILayout.LabelField("Per Lv", EditorStyles.boldLabel, GUILayout.Width(PerWidth));
			// Previews: 1, 500, 1000 use same numbers for Level and Distance modes
			if (mode == ViewMode.Level)
			{
				EditorGUILayout.LabelField($"@L{FormatHeaderNumber(PreviewLevels[0])}", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
				EditorGUILayout.LabelField($"@L{FormatHeaderNumber(PreviewLevels[1])}", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
				EditorGUILayout.LabelField($"@L{FormatHeaderNumber(PreviewLevels[2])}", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
				EditorGUILayout.LabelField("@L*", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
			}
			else
			{
				for (int i = 0; i < PreviewWorldX.Length; i++)
					EditorGUILayout.LabelField($"@{FormatHeaderNumber(PreviewWorldX[i])}", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
				// Custom world distance editor in header
				EditorGUI.BeginChangeCheck();
				string newStr = EditorGUILayout.TextField(customDistanceStr, GUILayout.Width(ValWidth));
				if (EditorGUI.EndChangeCheck())
				{
					if (TryParseNumber(newStr, out float parsed))
					{
						customDistance = Mathf.Max(0f, parsed);
						customDistanceStr = FormatHeaderNumber(customDistance);
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawEnemyBlock(EnemyData e)
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.BeginHorizontal();
			// First column cell: icon + title, fixed width
			{
				EditorGUILayout.BeginHorizontal(GUILayout.Width(FirstColumnWidth));
				var icon = e.icon;
				if (icon != null && icon.texture != null)
				{
					var drawRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
					var tex = icon.texture;
					var tr = icon.textureRect;
					var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
					GUI.DrawTextureWithTexCoords(drawRect, tex, uv);
				}
				var title = string.IsNullOrWhiteSpace(e.enemyName) ? e.name : e.enemyName;
				EditorGUILayout.LabelField($"{title}  (#{e.displayOrder})");
				EditorGUILayout.EndHorizontal();
				// Right-side quick actions/info (outside fixed grid columns)
				GUILayout.FlexibleSpace();
				EditorGUILayout.LabelField($"minX: {e.minX:0.##}", GUILayout.Width(120));
				var dpl = float.IsInfinity(e.distancePerLevel) ? "∞" : e.distancePerLevel.ToString("0.##");
				EditorGUILayout.LabelField($"dist/level: {dpl}", GUILayout.Width(120));
				if (GUILayout.Button("Select", GUILayout.Width(70)))
				{
					Selection.activeObject = e;
					EditorGUIUtility.PingObject(e);
				}
			}
			EditorGUILayout.EndHorizontal();

			// Health
			DrawStatRow(
				e,
				label: "Health",
				baseDrawer: () =>
				{
					Undo.RecordObject(e, "Edit Enemy Health");
					int v = EditorGUILayout.IntField(e.maxHealth, GUILayout.Width(BaseWidth));
					if (v != e.maxHealth) { e.maxHealth = Mathf.Max(0, v); EditorUtility.SetDirty(e); }
				},
				perDrawer: () =>
				{
					Undo.RecordObject(e, "Edit Enemy Health Per Level");
					int v = EditorGUILayout.IntField(e.healthPerLevel, GUILayout.Width(PerWidth));
					if (v != e.healthPerLevel) { e.healthPerLevel = Mathf.Max(0, v); EditorUtility.SetDirty(e); }
				},
				valueGetter: level => e.GetMaxHealthForLevel(level),
				valueGetterAtWorldX: worldX => e.GetMaxHealthForLevel(GetLevelAtWorldX(e, worldX))
			);

			// Damage
			DrawStatRow(
				e,
				label: "Damage",
				baseDrawer: () =>
				{
					Undo.RecordObject(e, "Edit Enemy Damage");
					float v = EditorGUILayout.FloatField(e.damage, GUILayout.Width(BaseWidth));
					if (!Mathf.Approximately(v, e.damage)) { e.damage = Mathf.Max(0f, v); EditorUtility.SetDirty(e); }
				},
				perDrawer: () =>
				{
					Undo.RecordObject(e, "Edit Enemy Damage Per Level");
					float v = EditorGUILayout.FloatField(e.damagePerLevel, GUILayout.Width(PerWidth));
					if (!Mathf.Approximately(v, e.damagePerLevel)) { e.damagePerLevel = Mathf.Max(0f, v); EditorUtility.SetDirty(e); }
				},
				valueGetter: level => e.GetDamageForLevel(level),
				valueGetterAtWorldX: worldX => e.GetDamageForLevel(GetLevelAtWorldX(e, worldX))
			);

			// Defense
			DrawStatRow(
				e,
				label: "Defense",
				baseDrawer: () =>
				{
					Undo.RecordObject(e, "Edit Enemy Defense");
					float v = EditorGUILayout.FloatField(e.defense, GUILayout.Width(BaseWidth));
					if (!Mathf.Approximately(v, e.defense)) { e.defense = Mathf.Max(0f, v); EditorUtility.SetDirty(e); }
				},
				perDrawer: () =>
				{
					Undo.RecordObject(e, "Edit Enemy Defense Per Level");
					float v = EditorGUILayout.FloatField(e.defensePerLevel, GUILayout.Width(PerWidth));
					if (!Mathf.Approximately(v, e.defensePerLevel)) { e.defensePerLevel = Mathf.Max(0f, v); EditorUtility.SetDirty(e); }
				},
				valueGetter: level => e.GetDefenseForLevel(level),
				valueGetterAtWorldX: worldX => e.GetDefenseForLevel(GetLevelAtWorldX(e, worldX)),
				valueStringAtWorldX: worldX =>
				{
					float def = e.GetDefenseForLevel(GetLevelAtWorldX(e, worldX));
					float after = TimelessEchoes.Combat.ApplyDefense(1f, Mathf.Max(0f, def));
					float pct = Mathf.Clamp01(1f - after) * 100f;
					return pct.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + "%";
				}
			);

			EditorGUILayout.EndVertical();
		}

		private void DrawStatRow(EnemyData e, string label, System.Action baseDrawer, System.Action perDrawer, System.Func<int, float> valueGetter, System.Func<float, float> valueGetterAtWorldX, System.Func<float, string> valueStringAtWorldX = null)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label, GUILayout.Width(FirstColumnWidth));
			baseDrawer?.Invoke();
			perDrawer?.Invoke();
			if (mode == ViewMode.Level)
			{
				for (int i = 0; i < PreviewLevels.Length; i++)
				{
					float val = valueGetter != null ? valueGetter(Mathf.Max(1, PreviewLevels[i])) : 0f;
					EditorGUILayout.LabelField(FormatNumber(val), GUILayout.Width(ValWidth));
				}
				{
					float val = valueGetter != null ? valueGetter(Mathf.Max(1, customLevel)) : 0f;
					EditorGUILayout.LabelField(FormatNumber(val), GUILayout.Width(ValWidth));
				}
			}
			else
			{
				for (int i = 0; i < PreviewWorldX.Length; i++)
				{
					if (IsBeforeSpawn(e, PreviewWorldX[i]))
					{
						EditorGUILayout.LabelField("-", GUILayout.Width(ValWidth));
						continue;
					}
					if (valueStringAtWorldX != null)
						EditorGUILayout.LabelField(valueStringAtWorldX(PreviewWorldX[i]), GUILayout.Width(ValWidth));
					else
					{
						float val = valueGetterAtWorldX != null ? valueGetterAtWorldX(PreviewWorldX[i]) : 0f;
						EditorGUILayout.LabelField(FormatNumber(val), GUILayout.Width(ValWidth));
					}
				}
				{
					if (IsBeforeSpawn(e, customDistance))
						EditorGUILayout.LabelField("-", GUILayout.Width(ValWidth));
					else
					{
						if (valueStringAtWorldX != null)
							EditorGUILayout.LabelField(valueStringAtWorldX(customDistance), GUILayout.Width(ValWidth));
						else
						{
							float val = valueGetterAtWorldX != null ? valueGetterAtWorldX(customDistance) : 0f;
							EditorGUILayout.LabelField(FormatNumber(val), GUILayout.Width(ValWidth));
						}
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private bool IsBeforeSpawn(EnemyData e, float worldX)
		{
			if (e == null) return false;
			// Consider map-wide spawn offset; enemies spawn after (minX + offset)
			return worldX < (e.minX + spawnOffset);
		}

		private int GetLevelAtWorldX(EnemyData e, float worldX)
		{
			if (e == null) return 1;
			// Convert absolute world X to relative distance from the enemy's start, factoring spawn offset
			float relative = Mathf.Max(0f, worldX - (e.minX + spawnOffset));
			return e.GetLevel(relative);
		}

		private static string FormatNumber(float v)
		{
			// Int-like numbers show with thousand separators; others with two decimals
			if (Mathf.Approximately(v, Mathf.Round(v))) return Mathf.RoundToInt(v).ToString("N0", CultureInfo.InvariantCulture);
			return v.ToString("N2", CultureInfo.InvariantCulture);
		}

		private static string FormatHeaderNumber(float v)
		{
			return Mathf.RoundToInt(v).ToString("N0", CultureInfo.InvariantCulture);
		}

		private static bool TryParseNumber(string s, out float value)
		{
			value = 0f;
			if (string.IsNullOrWhiteSpace(s)) return false;
			s = s.Replace(",", string.Empty);
			return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}
	}
}
#endif


