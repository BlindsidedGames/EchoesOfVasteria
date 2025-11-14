#if MODULE_ENTITIES
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Settings for agent movement that require managed types.
	///
	/// This component is used to store settings for agent movement that cannot be put anywhere else.
	/// For example, it can store delegates, interfaces and objects.
	///
	/// It is used by the <see cref="FollowerEntity"/> component to store settings for how the agent should move.
	/// Fortunately, the settings here are not used often, and so putting them in a managed component does not affect performance much.
	///
	/// In contrast to <see cref="ManagedState"/>, these settings are persistent.
	///
	/// See: <see cref="FollowerEntity"/>
	/// </summary>
	[System.Serializable]
	// Generate source code for a property bag for this struct. This improves performance for some ECS operations. Otherwise it will fall back on a slower reflection-based implementation.
	[Unity.Properties.GeneratePropertyBag]
	// Unity cannot guarantee that this struct does not contain any entity references (because we have, for example, some interface fields),
	// so it will try to patch entity references sometimes (in particular when live-patching entities), which is slow. So we promise that we will not use entity references in this struct to improve performance.
#if MODULE_ENTITIES_1_3_0_OR_NEWER
	[Unity.Entities.TypeManager.TypeOverrides(hasNoEntityReferences: true, hasNoBlobReferences: true, hasNoUnityObjectReferences: true)]
#else
	[Unity.Entities.TypeManager.TypeOverrides(hasNoEntityReferences: true, hasNoBlobReferences: true)]
#endif
	public class ManagedSettings : IComponentData, System.ICloneable, System.IEquatable<ManagedSettings> {
		/// <summary>
		/// Callback for when the agent starts to traverse an off-mesh link.
		///
		/// See: <see cref="IOffMeshLinkStateMachine.OnTraverseOffMeshLink"/>
		/// See: <see cref="FollowerEntity.onTraverseOffMeshLink"/>
		/// </summary>
		[System.NonSerialized]
		public IOffMeshLinkHandler onTraverseOffMeshLink;

		/// <summary>
		/// Settings for how an agent searches for paths.
		///
		/// This struct contains information about which graphs the agent can use, which nodes it can traverse, and if any nodes should be easier or harder to traverse.
		///
		/// A good default value to start from is <see cref="PathRequestSettings.Default"/>.
		///
		/// See: <see cref="FollowerEntity.pathfindingSettings"/>
		/// See: <see cref="Path.UseSettings"/>
		/// </summary>
		public PathRequestSettings pathfindingSettings;

		public object Clone () {
			return CloneAndSimplifyDefaults(false);
		}

		public ManagedSettings CloneAndSimplifyDefaults (bool simplify) {
			// Replace some arrays with null if they are all default values.
			// This saves some memory and makes the entity smaller.
			// This has a side effect of making live-patching of entities in the editor quite a lot faster
			var tagCostMultipliers = pathfindingSettings.tagCostMultipliers;
			if (simplify && tagCostMultipliers != null) {
				bool allOnes = true;
				for (int i = 0; i < pathfindingSettings.tagCostMultipliers.Length; i++) {
					allOnes &= pathfindingSettings.tagCostMultipliers[i] == 1;
				}
				if (allOnes) tagCostMultipliers = null;
			}
			if (tagCostMultipliers != null) tagCostMultipliers = (float[])tagCostMultipliers.Clone();

			var tagEntryCosts = pathfindingSettings.tagEntryCosts;
			if (simplify && tagEntryCosts != null) {
				bool allZero = true;
				for (int i = 0; i < pathfindingSettings.tagEntryCosts.Length; i++) {
					allZero &= pathfindingSettings.tagEntryCosts[i] == 0;
				}
				if (allZero) tagEntryCosts = null;
			}
			if (tagEntryCosts != null) tagEntryCosts = (uint[])tagEntryCosts.Clone();

			return new ManagedSettings {
					   pathfindingSettings = new PathRequestSettings {
						   graphMask = pathfindingSettings.graphMask,
						   tagEntryCosts = tagEntryCosts,
						   tagCostMultipliers = tagCostMultipliers,
						   traversableTags = pathfindingSettings.traversableTags,
						   traversalProvider = null,  // Cannot be safely cloned or copied
					   },
					   onTraverseOffMeshLink = null,  // Cannot be safely cloned or copied
			};
		}

		// Used by the unity editor when patching baked entities. If not defined it has to fall back to a slower method.
		public bool Equals (ManagedSettings other) {
			if (other == null) return false;

			return pathfindingSettings.Equals(other.pathfindingSettings) &&
				   onTraverseOffMeshLink == other.onTraverseOffMeshLink; // Reference equality check
		}
	}
}
#endif
