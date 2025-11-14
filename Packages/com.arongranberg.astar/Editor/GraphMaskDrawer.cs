using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	[CustomPropertyDrawer(typeof(GraphMask))]
	public class GraphMaskDrawer : PropertyDrawer {
		string[] graphLabels = new string[31];

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			// Make sure the AstarPath object is initialized and the graphs are loaded, this is required to be able to show graph names in the mask popup
			AstarPath.FindAstarPath();

			for (int i = 0; i < graphLabels.Length; i++) {
				if (AstarPath.active == null || AstarPath.active.data.graphs == null || i >= AstarPath.active.data.graphs.Length || AstarPath.active.data.graphs[i] == null) graphLabels[i] = "Graph " + i + (i == 30 ? "+" : "");
				else {
					graphLabels[i] = AstarPath.active.data.graphs[i].name + " (graph " + i + ")";
				}
			}

			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
			var valueProp = property.FindPropertyRelative("value");
			var mask = new GraphMask((uint)valueProp.intValue); // Note: .uintValue is not supported in Unity 2021.3
			if (mask.isPureBitmask) {
				EditorGUI.BeginChangeCheck();
				int newVal = EditorGUI.MaskField(position, label, valueProp.intValue, graphLabels);
				if (EditorGUI.EndChangeCheck()) {
					valueProp.intValue = newVal;
				}
			} else {
				var tooltip = "";
				var cnt = 0;
				for (uint i = 0; i <= GraphNode.MaxGraphIndex; i++) {
					if (mask.Contains(i)) {
						if (cnt > 0) tooltip += ", ";
						if (AstarPath.active == null || AstarPath.active.data.graphs == null || i >= AstarPath.active.data.graphs.Length || AstarPath.active.data.graphs[i] == null) {
							tooltip += "Graph " + i;
						} else {
							tooltip += AstarPath.active.data.graphs[i].name + " (graph " + i + ")";
						}
						cnt++;
					}
				}
				EditorGUI.LabelField(position, label, new GUIContent(cnt > 2 ? "Mixed..." : tooltip, tooltip + "\nCannot be edited in the inspector because the mask contains large graph indices"));
				if (GUI.Button(new Rect(position.xMax - 60, position.y, 60, 16), new GUIContent("Reset"), EditorStyles.miniButton)) {
					valueProp.intValue = -1;
				}
			}
			EditorGUI.showMixedValue = false;
		}
	}
}
