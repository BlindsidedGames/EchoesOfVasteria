#pragma warning disable IDE0051
#pragma warning disable IDE0052
#pragma warning disable CS0649

using System.Collections;
using UnityEngine;
using Pathfinding.Util;

namespace Pathfinding.Examples {
	/// <summary>Shows a helpful warning message if additional dependencies or a higher version of Unity is required to run the example scene</summary>
	[ExecuteInEditMode]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/minimumunityversionwarning.html")]
	public class MinimumUnityVersionWarning : MonoBehaviour {
		bool requiresUnity2022_2;
		bool requiresUnity2022_3;
		bool requiresEntities;
		bool requires_entities_graphics;
		bool requires_render_pipeline;


		void Awake () {
			requiresEntities = UnityCompatibility.FindAnyObjectByType<Pathfinding.FollowerEntity>() != null || UnityCompatibility.FindAnyObjectByType<Pathfinding.Examples.LightweightRVO>() != null;
			// Box colliders from scenes created in Unity 2022+ are not compatible with older versions of Unity. They will end with the wrong size.
			// The minimum version of the entitites package also requires Unity 2022
			requiresUnity2022_2 = UnityCompatibility.FindAnyObjectByType<BoxCollider>() != null || requiresEntities;
			// Navmesh cutting requires Unity 2022.3 or newer due to unity bugs in earlier versions
			requiresUnity2022_3 = UnityCompatibility.FindAnyObjectByType<NavmeshCut>() != null || UnityCompatibility.FindAnyObjectByType<NavmeshAdd>() != null;
			requiresEntities |= GameObject.Find("ECSSubScene") != null;

#if MODULE_ENTITIES
			requires_entities_graphics |= UnityCompatibility.FindAnyObjectByType<Unity.Scenes.SubScene>() != null;
			requires_render_pipeline = requires_entities_graphics;
#endif
		}

		IEnumerator Start () {
			// Catch dynamically spawned prefabs
			yield return null;
			Awake();
		}

		void OnGUI () {
#if !UNITY_2022_3_OR_NEWER
			if (requiresUnity2022_3) {
				var rect = new Rect(Screen.width/2 - 325, Screen.height/2 - 30, 650, 60);
				GUILayout.BeginArea(rect, "", "box");
				GUILayout.Label($"<b>Unity version too low</b>\nThis example scene can unfortunately not be played in your version of Unity, due to a Unity bug.\nYou must upgrade to Unity 2022.3 or later.");
				GUILayout.EndArea();
				return;
			}
#endif

#if !UNITY_2022_2_OR_NEWER
			if (requiresUnity2022_2) {
				var rect = new Rect(Screen.width/2 - 325, Screen.height/2 - 30, 650, 60);
				GUILayout.BeginArea(rect, "", "box");
				GUILayout.Label($"<b>Unity version too low</b>\nThis example scene can unfortunately not be played in your version of Unity, due to compatibility issues.\nYou must upgrade to Unity 2022.2 or later.");
				GUILayout.EndArea();
				return;
			}
#endif

#if !MODULE_ENTITIES
			if (requiresEntities) {
				var rect = new Rect(Screen.width/2 - 325, Screen.height/2 - 30, 650, 80);
				GUILayout.BeginArea(rect, "", "box");
#if UNITY_EDITOR
				GUILayout.Label("<b>Just one more step</b>\nThis example scene requires version 1.0 or higher of the <b>Entities</b> package to be installed.");
				if (GUILayout.Button("Install")) {
					UnityEditor.PackageManager.Client.Add("com.unity.entities");
				}
#else
				GUILayout.Label("<b>Just one more step</b>\nThis example scene requires version 1.0 or higher of the <b>Entities</b> package to be installed\nYou can install it from the Unity Package Manager");
#endif
				GUILayout.EndArea();
				return;
			}
#endif

#if !MODULE_ENTITIES_GRAPHICS
			if (requires_entities_graphics) {
				var rect = new Rect(Screen.width/2 - 325, Screen.height/2 - 30, 650, 80);
				GUILayout.BeginArea(rect, "", "box");
				GUILayout.Label("This example scene requires version 1.0 or higher of the <b>Entities Graphics</b> package to be installed, in order to render the entities during runtime.");
#if UNITY_EDITOR
				if (GUILayout.Button("Install")) {
					UnityEditor.PackageManager.Client.Add("com.unity.entities.graphics");
				}
#endif
				GUILayout.EndArea();
				return;
			}
#endif

			if (requires_render_pipeline) {
				var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
				if (rp == null) {
					var rect = new Rect(Screen.width/2 - 325, Screen.height/2 - 30, 650, 80);
					GUILayout.BeginArea(rect, "", "box");
					GUILayout.Label("This example scene requires a render pipeline to be set up in order to render the entities during runtime. Unity's built-in render pipeline cannot render ECS entities. The materials in this scene are configured for the Universal Render Pipeline.");
					if (GUILayout.Button("Read more")) {
						Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.4/manual/creating-a-new-entities-graphics-project.html");
					}
					GUILayout.EndArea();
					return;
				}
			}
		}
	}
}
