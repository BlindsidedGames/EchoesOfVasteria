#if MODULE_ENTITIES
using UnityEditor;
using UnityEngine;
using Pathfinding.RVO;
using Pathfinding.ECS;
using System.Linq;
using Unity.Entities;
using Pathfinding.ECS.RVO;

namespace Pathfinding {
	[CustomEditor(typeof(FollowerEntity), true)]
	[CanEditMultipleObjects]
	public class FollowerEntityEditor : EditorBase {
		bool debug = false;
		bool tagPenaltiesOpen;
		bool legend = false;

		static readonly GUIContent[] PositionSyncOptions = new [] {
			new GUIContent("Move independently of transform"),
			new GUIContent("Move agent with transform")
		};

		static readonly GUIContent[] RotationSyncOptions = new [] {
			new GUIContent("Rotate independently of transform"),
			new GUIContent("Rotate agent with transform")
		};

		protected override void OnDisable () {
			base.OnDisable();
			EditorPrefs.SetBool("FollowerEntity.debug", debug);
			EditorPrefs.SetBool("FollowerEntity.tagPenaltiesOpen", tagPenaltiesOpen);
		}

		protected override void OnEnable () {
			base.OnEnable();
			debug = EditorPrefs.GetBool("FollowerEntity.debug", false);
			tagPenaltiesOpen = EditorPrefs.GetBool("FollowerEntity.tagPenaltiesOpen", false);
		}

		public override bool RequiresConstantRepaint () {
			// When the debug inspector is open we want to update it every frame
			// as the agent can move
			return debug && Application.isPlaying;
		}

		protected void AutoRepathInspector () {
			var mode = FindProperty("autoRepathBacking.mode");

			PropertyField(mode, "Recalculate Paths Automatically");
			if (!mode.hasMultipleDifferentValues) {
				var modeValue = (AutoRepathPolicy.Mode)mode.enumValueIndex;
				EditorGUI.indentLevel++;
				var period = FindProperty("autoRepathBacking.period");
				if (modeValue == AutoRepathPolicy.Mode.EveryNSeconds || modeValue == AutoRepathPolicy.Mode.Dynamic) {
					FloatField(period, min: 0f);
				}
				if (modeValue == AutoRepathPolicy.Mode.Dynamic) {
					EditorGUILayout.HelpBox("The path will be recalculated at least every " + period.floatValue.ToString("0.0") + " seconds, but more often if the destination changes quickly", MessageType.None);
				}
				EditorGUI.indentLevel--;
			}
		}

		protected void DebugInspector () {
			debug = EditorGUILayout.Foldout(debug, "Debug info");
			if (debug) {
				EditorGUI.indentLevel++;
				DebugInspectorContents();
				EditorGUI.indentLevel--;
			}
		}

		void DebugInspectorContents () {
			EditorGUI.BeginDisabledGroup(true);

			if (!Application.isPlaying) {
				EditorGUILayout.HelpBox("Debug info is only available while playing.", MessageType.Info);
				return;
			}

			if (targets.Length == 1) {
				var ai = target as FollowerEntity;

				if (!ai.enabled) {
					EditorGUILayout.HelpBox("FollowerEntity is disabled.", MessageType.Info);
					return;
				}

				// The FollowerEntity could be baked, or have an internal entity
				FollowerEntityProxy proxy;
				if (ai.entityExists) {
					proxy = new FollowerEntityProxy(ai.world, ai.entity);
				} else {
					// For a baked entity, try to get the entities created from this authoring object
					var ls = new Unity.Collections.NativeList<Entity>(Unity.Collections.Allocator.Temp);
					var world = World.DefaultGameObjectInjectionWorld; // Guess the default world
					world?.EntityManager.Debug.GetEntitiesForAuthoringObject(ai.gameObject, ls);

					if (ls.Length == 0) {
						EditorGUILayout.HelpBox("No entities found for this object. Cannot display debug info.", MessageType.Warning);
						return;
					} else if (ls.Length != 1) {
						EditorGUILayout.HelpBox("Multiple entities found for this object. Cannot display debug info.", MessageType.Warning);
						return;
					}

					proxy = new FollowerEntityProxy(world, ls[0]);

					if (!proxy.likelyHasReasonableComponents) {
						EditorGUILayout.HelpBox("The entity is missing some required components.", MessageType.Warning);
						return;
					}
				}

				EditorGUILayout.Toggle("Reached Destination", proxy.reachedDestination);
				EditorGUILayout.Toggle("Reached End Of Path", proxy.reachedEndOfPath);
				if (proxy.enableLocalAvoidance) {
					EditorGUILayout.Toggle("Reached (maybe crowded) End Of Path", proxy.reachedCrowdedEndOfPath);
				}
				EditorGUILayout.Toggle("Has Path", proxy.hasPath);
				EditorGUILayout.Toggle("Path Pending", proxy.pathPending);
				if (proxy.isTraversingOffMeshLink) {
					EditorGUILayout.Toggle("Traversing Off-Mesh Link", true);
				}
				if (proxy.isStopped) {
					EditorGUILayout.Toggle("IsStopped (user controlled)", proxy.isStopped);
				}
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginChangeCheck();
				var newDestination = EditorGUILayout.Vector3Field("Destination", proxy.destination);
				if (EditorGUI.EndChangeCheck() && proxy.entityExists) proxy.SetDestination(newDestination, proxy.destinationFacingDirection);

				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.LabelField("Remaining Distance", proxy.remainingDistance.ToString("0.00"));
				EditorGUILayout.LabelField("Speed", proxy.velocity.magnitude.ToString("0.00"));
			} else {
				var ls = new Unity.Collections.NativeList<Entity>(Unity.Collections.Allocator.Temp);
				var world = World.DefaultGameObjectInjectionWorld; // Guess the default world

				int nReachedDestination = 0;
				int nReachedEndOfPath = 0;
				int nReachedCrowdedEndOfPath = 0;
				int nPending = 0;
				int nBaked = 0;
				for (int i = 0; i < targets.Length; i++) {
					var ai = targets[i] as FollowerEntity;

					var origCount = ls.Length;
					world?.EntityManager.Debug.GetEntitiesForAuthoringObject(targets[i], ls);
					if (ls.Length == origCount) {
						if (ai.reachedDestination) nReachedDestination++;
						if (ai.reachedEndOfPath) nReachedEndOfPath++;
						if (ai.reachedCrowdedEndOfPath) nReachedCrowdedEndOfPath++;
						if (ai.pathPending) nPending++;
					} else {
						nBaked++;
					}
				}
				if (nBaked == ls.Length) {
					for (int i = 0; i < ls.Length; i++) {
						var ai = new FollowerEntityProxy(world, ls[i]);
						if (ai.reachedDestination) nReachedDestination++;
						if (ai.reachedEndOfPath) nReachedEndOfPath++;
						if (ai.reachedCrowdedEndOfPath) nReachedCrowdedEndOfPath++;
						if (ai.pathPending) nPending++;
					}
				} else {
					EditorGUILayout.HelpBox("Some authoring script baked multiple entities. Cannot determine which ones are relevant. Cannot display debug info.", MessageType.Warning);
					return;
				}
				EditorGUILayout.LabelField("Reached Destination", nReachedDestination + " of " + targets.Length);
				EditorGUILayout.LabelField("Reached End Of Path", nReachedEndOfPath + " of " + targets.Length);
				EditorGUILayout.LabelField("Reached (maybe crowded) End Of Path", nReachedCrowdedEndOfPath + " of " + targets.Length);
				EditorGUILayout.LabelField("Path Pending", nPending + " of " + targets.Length);
			}
			EditorGUI.EndDisabledGroup();

			legend = EditorGUILayout.Foldout(legend, "Debug rendering legend");
			if (legend) {
				EditorGUI.indentLevel++;
				EditorGUI.BeginDisabledGroup(true);
				Section("General");
				EditorGUILayout.ColorField("Destination", Color.blue);
				EditorGUILayout.ColorField("Path", JobDrawFollowerGizmos.Path);

				var debugRendering = (Pathfinding.PID.PIDMovement.DebugFlags)FindProperty("movement.debugFlags").intValue;
				if ((debugRendering & PID.PIDMovement.DebugFlags.Rotation) != 0) {
					Section("Rotation");
					EditorGUILayout.ColorField("Visual Rotation", JobDrawFollowerGizmos.VisualRotationColor);
					EditorGUILayout.ColorField("Unsmoothed Rotation", JobDrawFollowerGizmos.UnsmoothedRotation);
					EditorGUILayout.ColorField("Internal Rotation", JobDrawFollowerGizmos.InternalRotation);
					EditorGUILayout.ColorField("Target Internal Rotation", JobDrawFollowerGizmos.TargetInternalRotation);
					EditorGUILayout.ColorField("Target Internal Rotation Hint", JobDrawFollowerGizmos.TargetInternalRotationHint);
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.indentLevel--;
			}
		}

		void PathfindingSettingsInspector () {
			bool anyCustomTraversalProvider = this.targets.Any(s => (s as FollowerEntity).pathfindingSettings.traversalProvider != null);
			if (anyCustomTraversalProvider) {
				EditorGUILayout.HelpBox("Custom traversal provider active", MessageType.None);
			}

			PropertyField("managedSettings.pathfindingSettings.graphMask", "Traversable Graphs");

			tagPenaltiesOpen = EditorGUILayout.Foldout(tagPenaltiesOpen, new GUIContent("Tags", "Settings for each tag"));
			if (tagPenaltiesOpen) {
				EditorGUI.indentLevel++;
				var traversableTags = this.targets.Select(s => (s as FollowerEntity).pathfindingSettings.traversableTags).ToArray();
				SeekerEditor.TagsEditor(FindProperty("managedSettings.pathfindingSettings.tagCostMultipliers"), FindProperty("managedSettings.pathfindingSettings.tagEntryCosts"), traversableTags);
				for (int i = 0; i < targets.Length; i++) {
					(targets[i] as FollowerEntity).pathfindingSettings.traversableTags = traversableTags[i];
				}
				EditorGUI.indentLevel--;
			}
		}

		protected override void Inspector () {
			Undo.RecordObjects(targets, "Modify FollowerEntity settings");
			EditorGUI.BeginChangeCheck();
			Section("Shape");
			FloatField("shape.radius", min: 0.01f);
			FloatField("shape.height", min: 0.01f);
			Popup("orientationBacking", new [] { new GUIContent("ZAxisForward (for 3D games)"), new GUIContent("YAxisForward (for 2D games)") }, "Orientation");
			var orientationProperty = FindProperty("orientationBacking");
			bool is2D = (OrientationMode)orientationProperty.enumValueIndex == OrientationMode.YAxisForward;

			Section("Movement");
			FloatField("movement.follower.speed", min: 0f);
			FloatField("movement.follower.rotationSpeed", min: 0f);
			var maxRotationSpeed = FindProperty("movement.follower.rotationSpeed");
			FloatField("movement.follower.maxRotationSpeed", min: maxRotationSpeed.hasMultipleDifferentValues ? 0f : maxRotationSpeed.floatValue);
			if (ByteAsToggle("movement.follower.allowRotatingOnSpotBacking", "Allow Rotating On The Spot")) {
				EditorGUI.indentLevel++;
				FloatField("movement.follower.maxOnSpotRotationSpeed", min: 0f);
				FloatField("movement.follower.slowdownTimeWhenTurningOnSpot", min: 0f);
				EditorGUI.indentLevel--;
			}
			Slider("movement.positionSmoothing", left: 0f, right: 0.5f);
			Slider("movement.rotationSmoothing", left: 0f, right: 0.5f);
			FloatField("movement.follower.slowdownTime", min: 0f);
			FloatField("movement.stopDistance", min: 0f);
			FloatField("movement.follower.leadInRadiusWhenApproachingDestination", min: 0f);
			FloatField("movement.follower.desiredWallDistance", min: 0f);

			if (!is2D && PropertyField("enableGravityBacking", "Gravity")) {
				EditorGUI.indentLevel++;
				PropertyField("movement.groundMask", "Raycast Ground Mask");
				EditorGUI.indentLevel--;
			}
			var movementPlaneSource = FindProperty("movementPlaneSourceBacking");
			PropertyField(movementPlaneSource, "Movement Plane Source");
			if (AstarPath.active != null && AstarPath.active.data.graphs != null) {
				var possiblySpherical = AstarPath.active.data.navmeshGraph != null && !AstarPath.active.data.navmeshGraph.RecalculateNormals;
				if (!possiblySpherical && !movementPlaneSource.hasMultipleDifferentValues && (MovementPlaneSource)movementPlaneSource.intValue == MovementPlaneSource.Raycast) {
					EditorGUILayout.HelpBox("Using raycasts as the movement plane source is only recommended if you have a spherical or otherwise non-planar world. It has a performance overhead.", MessageType.Info);
				}
				if (!possiblySpherical && !movementPlaneSource.hasMultipleDifferentValues && (MovementPlaneSource)movementPlaneSource.intValue == MovementPlaneSource.NavmeshNormal) {
					EditorGUILayout.HelpBox("Using the navmesh normal as the movement plane source is only recommended if you have a spherical or otherwise non-planar world. It has a performance overhead.", MessageType.Info);
				}
			}

			if ((target as MonoBehaviour).gameObject.scene.isSubScene) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.Popup(new GUIContent("Position Sync", "Position sync cannot be changed when the FollowerEntity is in a sub scene"), 0, PositionSyncOptions);
				EditorGUILayout.Popup(new GUIContent("Rotation Sync", "Rotation sync cannot be changed when the FollowerEntity is in a sub scene"), 0, RotationSyncOptions);
				EditorGUI.EndDisabledGroup();
			} else {
				this.Popup("syncPosition", PositionSyncOptions, "Position Sync");
				this.Popup("syncRotation", RotationSyncOptions, "Rotation Sync");
			}

			Section("Pathfinding");
			PathfindingSettingsInspector();
			AutoRepathInspector();


			if (SectionEnableable("Local Avoidance", "enableLocalAvoidanceBacking")) {
				if (Application.isPlaying && RVOSimulator.active == null && !EditorUtility.IsPersistent(target)) {
					EditorGUILayout.HelpBox("There is no enabled RVOSimulator component in the scene. A single global RVOSimulator component is required for local avoidance.", MessageType.Warning);
				}
				if (targets.Length == 1) {
					var ai = target as FollowerEntity;
					if (ai.localAvoidanceTemporarilyDisabled) {
						EditorGUILayout.HelpBox("Local avoidance is temporarily disabled while traversing an off-mesh link.", MessageType.Warning);
					}
				}
				FloatField("rvoSettingsBacking.agentTimeHorizon", min: 0f, max: 20.0f);
				FloatField("rvoSettingsBacking.obstacleTimeHorizon", min: 0f, max: 20.0f);
				PropertyField("rvoSettingsBacking.maxNeighbours");
				ClampInt("rvoSettingsBacking.maxNeighbours", min: 0, max: SimulatorBurst.MaxNeighbourCount);
				PropertyField("rvoSettingsBacking.layer");
				PropertyField("rvoSettingsBacking.collidesWith");
				Slider("rvoSettingsBacking.priority", left: 0f, right: 1.0f);
				PropertyField("rvoSettingsBacking.locked");

				if (targets.Length == 1) {
					var ai = target as FollowerEntity;
					var simulator = RVOSimulator.active?.GetSimulator();
					if (simulator != null && ai.entityExists && World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<AgentIndex>(ai.entity)) {
						var agentIndex = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AgentIndex>(ai.entity);
						simulator.BlockUntilSimulationStepDone();
						if (agentIndex.TryGetIndex(ref simulator.simulationData, out var index)) {
							if (simulator.outputData.numNeighbours[index] >= simulator.simulationData.maxNeighbours[index]) {
								EditorGUILayout.HelpBox("Limit of how many neighbours to consider (Max Neighbours) has been reached. Some nearby agents may have been ignored. " +
									"To ensure all agents are taken into account you can raise the 'Max Neighbours' value at a cost to performance.", MessageType.Warning);
							}
						}
					}
				}
			}

			Section("Debug");
			PropertyField("movement.debugFlags", "Movement Debug Rendering");
			PropertyField("rvoSettingsBacking.debug", "Local Avoidance Debug Rendering");
			DebugInspector();

			if (EditorGUI.EndChangeCheck()) {
				for (int i = 0; i < targets.Length; i++) {
					var script = targets[i] as FollowerEntity;
					script.SyncWithEntity();
				}
			}
		}

		public void OnSceneGUI () {
			if (EditorApplication.isPaused) {
				var script = target as FollowerEntity;
				if (script.position != script.transform.position && script.updatePosition) {
					// Force sync the position with the entity.
					// The user may have moved the entity in the scene view.
					// If the game is paused, we still want the entity to be in the correct position immediately,
					// in order to draw gizmos correctly.
					script.position = script.transform.position;
				}
			}
		}
	}
}
#else
using UnityEditor;
using UnityEngine;
using Pathfinding.ECS;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Pathfinding {
	// This inspector is only used if the Entities package is not installed
	[CustomEditor(typeof(FollowerEntity), true)]
	[CanEditMultipleObjects]
	public class FollowerEntityEditor : EditorBase {
		static AddRequest addRequest;

		protected override void Inspector () {
			if (addRequest != null) {
				if (addRequest.Status == StatusCode.Success) {
					addRequest = null;

					// If we get this far, unity did not successfully reload the assemblies.
					// Who knows what went wrong. Quite possibly restarting Unity will resolve the issue.
					EditorUtility.DisplayDialog("Installed Entities package", "The entities package has been installed. You may have to restart the editor for changes to take effect.", "Ok");
				} else if (addRequest.Status == StatusCode.Failure) {
					EditorGUILayout.HelpBox("Failed to install the Entities package. Please install it manually using the Package Manager." + (addRequest.Error != null ? "\n" + addRequest.Error.message : ""), MessageType.Error);
				} else {
					EditorGUILayout.HelpBox("Installing entities package...", MessageType.None);
				}
			} else {
				EditorGUILayout.HelpBox("This component requires the Entities package (1.1.0+) to be installed. Please install it using the Package Manager.", MessageType.Error);
				if (GUILayout.Button("Install entities package")) {
					addRequest = Client.Add("com.unity.entities");
				}
			}
		}
	}
}
#endif
