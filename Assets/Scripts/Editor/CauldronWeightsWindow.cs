#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Editor
{
	public class CauldronWeightsWindow : EditorWindow
	{
		private CauldronConfig config;
		private int evaLevel = 1;
		private Vector2 scroll;

		private const float LabelWidth = 180f;
		private const float BaseWidth = 70f;
		private const float PerWidth = 80f;
		private const float ValWidth = 70f;
		private const float ChanceWidth = 70f;
		private static readonly int[] PreviewLevels = { 1, 10, 100, 1000 };

		[MenuItem("Tools/Cauldron Weights")] private static void Open()
		{
			GetWindow<CauldronWeightsWindow>().Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Cauldron Weights", EditorStyles.boldLabel);
			config = (CauldronConfig)EditorGUILayout.ObjectField("Config", config, typeof(CauldronConfig), false);
			evaLevel = Mathf.Max(1, EditorGUILayout.IntField("Eva Level (for Total)", evaLevel));

			if (config == null)
			{
				EditorGUILayout.HelpBox("Assign a CauldronConfig asset.", MessageType.Info);
				return;
			}

			scroll = EditorGUILayout.BeginScrollView(scroll);
			EditorGUILayout.Space(4);
			DrawHeaderRow();
			DrawWeightedRow("Nothing", ref config.weightNothing);
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Alter-Echo Subcategories", EditorStyles.boldLabel);
			DrawHeaderRow();
			DrawWeightedRow("AE - Farming", ref config.weightAEFarming);
			DrawWeightedRow("AE - Fishing", ref config.weightAEFishing);
			DrawWeightedRow("AE - Mining", ref config.weightAEMining);
			DrawWeightedRow("AE - Woodcutting", ref config.weightAEWoodcutting);
			DrawWeightedRow("AE - Looting", ref config.weightAELooting);
			DrawWeightedRow("AE - Combat", ref config.weightAECombat);
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Other Rolls", EditorStyles.boldLabel);
			DrawHeaderRow();
			DrawWeightedRow("Buff", ref config.weightBuffCard);
			DrawWeightedRow("Lowest", ref config.weightLowestCountCard);
			DrawWeightedRow("Eva's Blessing x2", ref config.weightEvasBlessingX2);
			DrawWeightedRow("Vast Surge x10", ref config.weightVastSurgeX10);

			EditorGUILayout.Space(8);
			var total = config.GetTotalWeight(evaLevel);
			EditorGUILayout.LabelField($"Total Weight @ L{evaLevel}: {total:0.##}");
			EditorGUILayout.EndScrollView();

			if (GUI.changed)
			{
				EditorUtility.SetDirty(config);
			}
		}

		private void DrawHeaderRow()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Group", EditorStyles.boldLabel, GUILayout.Width(LabelWidth));
			EditorGUILayout.LabelField("Base", EditorStyles.boldLabel, GUILayout.Width(BaseWidth));
			EditorGUILayout.LabelField("Per Lv", EditorStyles.boldLabel, GUILayout.Width(PerWidth));
			EditorGUILayout.LabelField("@1", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
			EditorGUILayout.LabelField("@10", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
			EditorGUILayout.LabelField("@100", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
			EditorGUILayout.LabelField("@1000", EditorStyles.boldLabel, GUILayout.Width(ValWidth));
			EditorGUILayout.LabelField("Ch@1", EditorStyles.boldLabel, GUILayout.Width(ChanceWidth));
			EditorGUILayout.LabelField("Ch@10", EditorStyles.boldLabel, GUILayout.Width(ChanceWidth));
			EditorGUILayout.LabelField("Ch@100", EditorStyles.boldLabel, GUILayout.Width(ChanceWidth));
			EditorGUILayout.LabelField("Ch@1000", EditorStyles.boldLabel, GUILayout.Width(ChanceWidth));
			EditorGUILayout.EndHorizontal();
		}

		private void DrawWeightedRow(string label, ref TimelessEchoes.Utilities.WeightedValue w)
		{
			EditorGUILayout.BeginHorizontal("box");
			EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
			w.baseWeight = Mathf.Max(0f, EditorGUILayout.FloatField(w.baseWeight, GUILayout.Width(BaseWidth)));
			w.weightPerLevel = EditorGUILayout.FloatField(w.weightPerLevel, GUILayout.Width(PerWidth));
			for (int i = 0; i < PreviewLevels.Length; i++)
			{
				var val = w.Evaluate(PreviewLevels[i]);
				EditorGUILayout.LabelField(val.ToString("0.##"), GUILayout.Width(ValWidth));
			}
			for (int i = 0; i < PreviewLevels.Length; i++)
			{
				var total = config != null ? config.GetTotalWeight(PreviewLevels[i]) : 0f;
				var val = w.Evaluate(PreviewLevels[i]);
				var pct = total > 0f ? Mathf.Clamp01(val / total) * 100f : 0f;
				EditorGUILayout.LabelField(pct.ToString("0.##") + "%", GUILayout.Width(ChanceWidth));
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}
#endif


