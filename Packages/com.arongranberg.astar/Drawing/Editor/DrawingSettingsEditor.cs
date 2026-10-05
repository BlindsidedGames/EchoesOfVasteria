using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Pathfinding.Drawing {
	/// <summary>Helper for adding project settings</summary>
	static class ALINESettingsRegister {
		const string PROVIDER_PATH = "Project/AstarGizmos";
		const string SETTINGS_LABEL = "A* Gizmos";


		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider () {
			// First parameter is the path in the Settings window.
			// Second parameter is the scope of this setting: it only appears in the Project Settings window.
			var provider = new SettingsProvider(PROVIDER_PATH, SettingsScope.Project) {
				// By default the last token of the path is used as display name if no label is provided.
				label = SETTINGS_LABEL,
				guiHandler = (searchContext) =>
				{
					var settings = new SerializedObject(DrawingSettings.GetSettingsAsset());
					EditorGUILayout.HelpBox("Opacity of lines, solid objects and text drawn using ALINE. When drawing behind other objects, an additional opacity multiplier is applied.", MessageType.None);
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Lines", EditorStyles.boldLabel);
					EditorGUILayout.Slider(settings.FindProperty("settings.lineOpacity"), 0, 1, new GUIContent("Opacity", "Opacity of lines when in front of objects"));
					EditorGUILayout.Slider(settings.FindProperty("settings.lineOpacityBehindObjects"), 0, 1, new GUIContent("Opacity (occluded)", "Additional opacity multiplier of lines when behind or inside objects"));
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Solids", EditorStyles.boldLabel);
					EditorGUILayout.Slider(settings.FindProperty("settings.solidOpacity"), 0, 1, new GUIContent("Opacity", "Opacity of solid objects when in front of other objects"));
					EditorGUILayout.Slider(settings.FindProperty("settings.solidOpacityBehindObjects"), 0, 1, new GUIContent("Opacity (occluded)", "Additional opacity multiplier of solid objects when behind or inside other objects"));
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
					EditorGUILayout.Slider(settings.FindProperty("settings.textOpacity"), 0, 1, new GUIContent("Opacity", "Opacity of text when in front of other objects"));
					EditorGUILayout.Slider(settings.FindProperty("settings.textOpacityBehindObjects"), 0, 1, new GUIContent("Opacity (occluded)", "Additional opacity multiplier of text when behind or inside other objects"));
					EditorGUILayout.Separator();
					EditorGUILayout.Slider(settings.FindProperty("settings.curveResolution"), 0.1f, 3f, new GUIContent("Curve resolution", "Higher values will make curves smoother, but also a bit slower to draw."));

					EditorGUILayout.Separator();

					settings.ApplyModifiedProperties();
					if (GUILayout.Button("Reset to default")) {
						var def = DrawingSettings.DefaultSettings;
						var current = DrawingSettings.GetSettingsAsset();
						current.settings.lineOpacity = def.lineOpacity;
						current.settings.lineOpacityBehindObjects = def.lineOpacityBehindObjects;
						current.settings.solidOpacity = def.solidOpacity;
						current.settings.solidOpacityBehindObjects = def.solidOpacityBehindObjects;
						current.settings.textOpacity = def.textOpacity;
						current.settings.textOpacityBehindObjects = def.textOpacityBehindObjects;
						current.settings.curveResolution = def.curveResolution;
						EditorUtility.SetDirty(current);
					}

					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Builds", EditorStyles.boldLabel);
					DrawExcludeFromBuildsToggle();
				},

				// Populate the search keywords to enable smart search filtering and label highlighting:
				keywords = new HashSet<string>(new[] { "Drawing", "Wire", "aline", "opacity", "build", "exclude", "strip" })
			};

			return provider;
		}

		const string EXCLUDE_DEFINE = "ALINE_EXCLUDED_IN_BUILD";

		/// <summary>
		/// The value the user last requested, or null if the toggle already matches the compiled state.
		///
		/// Toggling the define only takes effect after Unity has recompiled all scripts, which takes several seconds.
		/// The domain reload ending that recompilation resets this to null, which is exactly when the request has taken effect.
		/// </summary>
		static bool? pendingExclude;

		static void DrawExcludeFromBuildsToggle () {
#if ALINE_EXCLUDED_IN_BUILD
			bool excluded = true;
#else
			bool excluded = false;
#endif

			var waiting = pendingExclude.HasValue || EditorApplication.isCompiling;

			var label = new GUIContent("Exclude from builds", "Removes drawing code from standalone builds. Drawing still works in the editor and in play mode.");
			bool newValue;
			// Disabled while waiting, since a second toggle would show a state that no recompilation is going to produce.
			using (new EditorGUI.DisabledScope(waiting)) {
				newValue = EditorGUILayout.Toggle(label, pendingExclude ?? excluded);
			}

			if (waiting) {
				EditorGUILayout.HelpBox("Recompiling...", MessageType.Info);
			} else if (excluded) {
				EditorGUILayout.HelpBox("Drawing code is removed from standalone builds. Draw.ingame will not render anything in a build.", MessageType.Info);
			}

			if (!waiting && newValue != excluded) {
				pendingExclude = newValue;
				SetDefineForAllBuildTargets(EXCLUDE_DEFINE, newValue);
			}
		}

		/// <summary>
		/// Adds or removes a scripting define symbol for every build target group.
		///
		/// Applying it to all groups avoids the define being set for one platform but not another,
		/// which would silently change what a build contains depending on the active platform.
		/// </summary>
		static void SetDefineForAllBuildTargets (string define, bool enable) {
			foreach (BuildTargetGroup group in System.Enum.GetValues(typeof(BuildTargetGroup))) {
				if (group == BuildTargetGroup.Unknown) continue;

				// Unity throws for build target groups it has deprecated, and there is no API to enumerate only the supported ones.
				var field = typeof(BuildTargetGroup).GetField(group.ToString());
				if (field == null || field.IsDefined(typeof(System.ObsoleteAttribute), false)) continue;

				string[] symbols;
				try {
					PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), out symbols);
				} catch (System.Exception) {
					continue;
				}

				var parts = new List<string>(symbols);
				parts.RemoveAll(s => s.Trim().Length == 0);
				var contains = parts.Contains(define);
				if (enable == contains) continue;

				if (enable) parts.Add(define);
				else parts.RemoveAll(s => s == define);

				PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), parts.ToArray());
			}
		}
	}
}
