using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding {
	[CustomEditor(typeof(Seeker))]
	[CanEditMultipleObjects]
	public class SeekerEditor : EditorBase {
		static bool tagPenaltiesOpen;
		static List<Seeker> scripts = new List<Seeker>();

		GUIContent[] exactnessLabels = new [] { new GUIContent("Node Center (Snap To Node)"), new GUIContent("Original"), new GUIContent("Interpolate (deprecated)"), new GUIContent("Closest On Node Surface"), new GUIContent("Node Connection") };

		protected override void Inspector () {
			scripts.Clear();
			foreach (var script in targets) scripts.Add(script as Seeker);

			Undo.RecordObjects(targets, "Modify settings on Seeker");

			PropertyField("drawGizmos", "Draw Gizmos");
			PropertyField("detailedGizmos", "Detailed Gizmos");

			var startEndModifierProp = FindProperty("startEndModifier");
			startEndModifierProp.isExpanded = EditorGUILayout.Foldout(startEndModifierProp.isExpanded, startEndModifierProp.displayName);
			if (startEndModifierProp.isExpanded) {
				EditorGUI.indentLevel++;
				Popup("startEndModifier.exactStartPoint", exactnessLabels, "Start Point Snapping");
				Popup("startEndModifier.exactEndPoint", exactnessLabels, "End Point Snapping");
				PropertyField("startEndModifier.addPoints", "Add Points");

				if (FindProperty("startEndModifier.exactStartPoint").enumValueIndex == (int)StartEndModifier.Exactness.Original || FindProperty("startEndModifier.exactEndPoint").enumValueIndex == (int)StartEndModifier.Exactness.Original) {
					if (PropertyField("startEndModifier.useRaycasting", "Physics Raycasting")) {
						EditorGUI.indentLevel++;
						PropertyField("startEndModifier.mask", "Layer Mask");
						EditorGUI.indentLevel--;
						EditorGUILayout.HelpBox("Using raycasting to snap the start/end points has largely been superseded by the 'ClosestOnNode' snapping option. It is both faster and usually closer to what you want to achieve.", MessageType.Info);
					}

					if (PropertyField("startEndModifier.useGraphRaycasting", "Graph Raycasting")) {
						EditorGUILayout.HelpBox("Using raycasting to snap the start/end points has largely been superseded by the 'ClosestOnNode' snapping option. It is both faster and usually closer to what you want to achieve.", MessageType.Info);
					}
				}

				EditorGUI.indentLevel--;
			}

			PropertyField("graphMask", "Traversable Graphs");

			tagPenaltiesOpen = EditorGUILayout.Foldout(tagPenaltiesOpen, new GUIContent("Tags", "Settings for each tag"));
			if (tagPenaltiesOpen) {
				var traversableTags = scripts.Select(s => s.traversableTags).ToArray();
				EditorGUI.indentLevel++;
				TagsEditor(FindProperty("tagCostMultipliers"), FindProperty("tagEntryCosts"), traversableTags);
				for (int i = 0; i < scripts.Count; i++) {
					scripts[i].traversableTags = traversableTags[i];
				}
				EditorGUI.indentLevel--;
			}

			if (scripts.Count > 0 && scripts[0].traversalProvider != null) {
				EditorGUILayout.HelpBox("A custom traversal provider has been set", MessageType.None);
			}

			// Make sure we don't leak any memory
			scripts.Clear();
		}

		public static void TagsEditor (SerializedProperty costByDistanceProp, SerializedProperty entryCostsProp, int[] traversableTags) {
			string[] tagNames = AstarPath.FindTagNames();
			if (tagNames.Length != 32) {
				tagNames = new string[32];
				for (int i = 0; i < tagNames.Length; i++) tagNames[i] = "" + i;
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField("Tag", EditorStyles.boldLabel, GUILayout.MaxWidth(120));
			for (int i = 0; i < tagNames.Length; i++) {
				EditorGUILayout.LabelField(tagNames[i], GUILayout.MaxWidth(120));
			}

			if (GUILayout.Button("Edit names", EditorStyles.miniButton)) {
				AstarPathEditor.EditTags();
			}
			EditorGUILayout.EndVertical();

			// Prevent indent from affecting the other columns
			var originalIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

#if !ASTAR_NoTagPenalty
			// If navmesh/recast graph only, then only show traversal scale unless an entry cost is non zero
			bool hasNavmesh = false;
			bool hasGrid = false;
			bool hasOther = false;
			AstarPath.FindAstarPath();
			if (AstarPath.active != null && AstarPath.active.graphs != null) {
				for (int i = 0; i < AstarPath.active.data.graphs.Length; i++) {
					var graph = AstarPath.active.data.graphs[i];
					if (graph is NavmeshBase) {
						hasNavmesh = true;
					} else if (graph is GridGraph) {
						hasGrid = true;
					} else if (graph != null && !(graph is LinkGraph)) {
						hasOther = true;
					}
				}
			}
			bool anyEntryCosts = false;
			for (int i = 0; i < tagNames.Length; i++) {
				if (entryCostsProp.GetArrayElementAtIndex(i).intValue != 0) {
					anyEntryCosts = true;
					break;
				}
			}
			bool hideEntryCosts = hasNavmesh && !(hasGrid || hasOther) && !anyEntryCosts;

			static float LinearToSemiLog (float value) {
				if (value < 0) return -1;
				if (value < 1) return value - 1;
				return Mathf.Log10(value);
			}

			static float SemiLogToLinear (float value) {
				if (value < 0) return value + 1;
				return Mathf.Pow(10, value);
			}

			static bool? TagTraversable (int[] traversableTags, int i) {
				var anyFalse = false;
				var anyTrue = false;
				for (int j = 0; j < traversableTags.Length; j++) {
					var prevTraversable = ((traversableTags[j] >> i) & 0x1) != 0;
					anyTrue |= prevTraversable;
					anyFalse |= !prevTraversable;
				}
				if (anyTrue == anyFalse) return null;
				else return anyTrue;
			}

			static bool HerusticIsAdmissable (float costPerDistanceFactor, uint entryCost) {
				bool anyNotOk = false;
				if (AstarPath.active != null && AstarPath.active.graphs != null) {
					if (costPerDistanceFactor >= AstarPath.active.effectiveHeuristicScale) return true;

					for (int j = 0; j < AstarPath.active.data.graphs.Length; j++) {
						var graph = AstarPath.active.data.graphs[j];
						if (graph == null || graph is LinkGraph) continue;
						if (graph is GridGraph gridGraph) {
							// A too low cost per distance can be compensated by a high entry cost
							var cost = gridGraph.nodeSize * costPerDistanceFactor * Int3.Precision + entryCost;
							var heuristic = Int3.Precision*gridGraph.nodeSize*AstarPath.active.effectiveHeuristicScale;
							if (cost >= heuristic) {
								// Ok
							} else {
								anyNotOk = true;
							}
						} else {
							anyNotOk = true;
						}
					}
				}
				return !anyNotOk;
			}

			GUILayout.Space(5);
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField("Traversable", EditorStyles.boldLabel, GUILayout.MaxWidth(90));
			for (int i = 0; i < tagNames.Length; i++) {
				EditorGUI.BeginChangeCheck();
				var v = TagTraversable(traversableTags, i);
				EditorGUI.showMixedValue = v == null;
				var newTraversable = EditorGUILayout.Toggle(v.HasValue ? v.Value : false, GUILayout.MaxWidth(90));
				EditorGUI.showMixedValue = false;
				if (EditorGUI.EndChangeCheck()) {
					for (int j = 0; j < traversableTags.Length; j++) {
						traversableTags[j] = (traversableTags[j] & ~(1 << i)) | ((newTraversable ? 1 : 0) << i);
					}
				}
			}

			if (GUILayout.Button("Toggle all", EditorStyles.miniButton, GUILayout.MaxWidth(90))) {
				for (int j = traversableTags.Length - 1; j >= 0; j--) {
					traversableTags[j] = (traversableTags[0] & 0x1) == 0 ? -1 : 0;
				}
			}
			EditorGUILayout.EndVertical();

			GUILayout.Space(5);

			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField("Cost per world unit", EditorStyles.boldLabel, GUILayout.MaxWidth(130));
			var prop = costByDistanceProp;
			float lowestTraversalCostWarning = 1f;
			bool warnAboutEntryCostsOnNavmeshGraphs = hasNavmesh && !(hasOther || hasGrid) && anyEntryCosts;

			// On grid graphs we can check if the entry cost of a node is high enough to compensate for a low cost per distance
			// Other other graphs it is hard, because the distance an agent moves between nodes can vary a lot
			bool entryCostCanCompensateForTraversalCost = hasGrid && !(hasNavmesh || hasOther);

			if (costByDistanceProp.arraySize != 32) costByDistanceProp.arraySize = 32;
			for (int i = 0; i < tagNames.Length; i++) {
				var element = costByDistanceProp.GetArrayElementAtIndex(i);
				EditorGUI.BeginDisabledGroup(TagTraversable(traversableTags, i) == false);

				EditorGUILayout.BeginHorizontal();

				// Note: uintValue is not supported in Unity 2021.3, so we use intValue and cast instead
				bool heuristicIsAdmissable = EditorGUI.showMixedValue || HerusticIsAdmissable(element.floatValue, (uint)entryCostsProp.GetArrayElementAtIndex(i).intValue);
				if (!heuristicIsAdmissable) GUIUtilityx.PushTint(new Color(1, 0.9f, 0.5f));

				var r = EditorGUILayout.GetControlRect(false);
				EditorGUI.BeginProperty(r, GUIContent.none, element);
				EditorGUI.showMixedValue = element.hasMultipleDifferentValues;
				EditorGUI.BeginChangeCheck();
				var res = GUI.HorizontalSlider(r, LinearToSemiLog(element.floatValue), -1, 3);
				if (EditorGUI.EndChangeCheck()) {
					if (Mathf.Abs(res - Mathf.Round(res)) < 0.05f) res = Mathf.Round(res);
					var v = SemiLogToLinear(res);
					// Round to 1 decimal place
					v = Mathf.Round(v * 10) / 10;
					element.floatValue = v;
				}

				r = EditorGUILayout.GetControlRect(false, GUILayout.MinWidth(60), GUILayout.MaxWidth(70));
				EditorGUI.BeginChangeCheck();
				res = EditorGUI.FloatField(r, GUIContent.none, element.floatValue * Int3.Precision);
				if (EditorGUI.EndChangeCheck()) {
					element.floatValue = Mathf.Max(0, res / Int3.Precision);
				}
				EditorGUI.EndProperty();
				EditorGUI.showMixedValue = false;
				EditorGUILayout.EndHorizontal();

				EditorGUI.EndDisabledGroup();

				if (!heuristicIsAdmissable) GUIUtilityx.PopTint();

				if (!heuristicIsAdmissable && element.floatValue < lowestTraversalCostWarning) {
					lowestTraversalCostWarning = element.floatValue;
				}
			}
			if (GUILayout.Button("Reset all", EditorStyles.miniButton)) {
				for (int i = 0; i < tagNames.Length; i++) {
					costByDistanceProp.GetArrayElementAtIndex(i).floatValue = 1;
				}
			}
			EditorGUILayout.EndVertical();
			GUILayout.Space(5);

			if (!hideEntryCosts) {
				EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField("Cost per node", EditorStyles.boldLabel, GUILayout.MaxWidth(100));
				if (entryCostsProp.arraySize != 32) entryCostsProp.arraySize = 32;
				for (int i = 0; i < tagNames.Length; i++) {
					EditorGUI.BeginDisabledGroup(TagTraversable(traversableTags, i) == false);
					var element = entryCostsProp.GetArrayElementAtIndex(i);

					// Note: uintValue is not supported in Unity 2021.3, so we use intValue and cast instead
					bool heuristicIsAdmissable = EditorGUI.showMixedValue || HerusticIsAdmissable(costByDistanceProp.GetArrayElementAtIndex(i).floatValue, (uint)element.intValue);
					bool warn = (!heuristicIsAdmissable && entryCostCanCompensateForTraversalCost) || (warnAboutEntryCostsOnNavmeshGraphs && element.intValue != 0);
					if (warn) GUIUtilityx.PushTint(new Color(1, 0.9f, 0.5f));

					EditorGUILayout.PropertyField(element, GUIContent.none, false, GUILayout.MinWidth(100));
					// Penalties should not be negative
					if (!element.hasMultipleDifferentValues && element.intValue < 0) element.intValue = 0;

					if (warn) GUIUtilityx.PopTint();

					EditorGUI.EndDisabledGroup();
				}
				if (GUILayout.Button("Reset all", EditorStyles.miniButton)) {
					for (int i = 0; i < tagNames.Length; i++) {
						entryCostsProp.GetArrayElementAtIndex(i).intValue = 0;
					}
				}
				EditorGUILayout.EndVertical();
			}
#endif

			EditorGUILayout.EndHorizontal();

			if (AstarPath.active != null && lowestTraversalCostWarning < AstarPath.active.effectiveHeuristicScale) {
				var msg = "The cost per world unit for some tags is lower than the default. This may cause suboptimal paths to be calculated. You must reduce A* Inspector -> Pathfinding -> Heuristic Scale to at most " + lowestTraversalCostWarning;
				if (entryCostCanCompensateForTraversalCost) {
					msg += ", or increase the cost per node,";
				}
				msg += " to guarantee optimal paths.\n\nIn most cases, it is recommended to only make tags costlier to traverse than the default, not cheaper.";
				EditorGUILayout.HelpBox(msg, MessageType.Warning);
			}
			if (warnAboutEntryCostsOnNavmeshGraphs) {
				EditorGUILayout.HelpBox("It is recommended to only use costs that scale with distance on navmesh/recast graphs, instead of costs per node, since nodes sizes can vary significantly.\nBefore version 5.4 only costs per node were supported.", MessageType.Warning);
			}
			EditorGUI.indentLevel = originalIndent;
		}
	}
}
