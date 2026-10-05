#if MODULE_ENTITIES
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Baked form of the persistent parts of <see cref="ManagedSettings"/>.
	///
	/// Baking runs ahead of time and its output is serialized, so a baked agent cannot be given a slot in
	/// <see cref="AgentManagedStorage"/> directly. Instead the settings that survive baking are written to this
	/// unmanaged component, and <see cref="InitManagedStateSystem"/> turns it into a real
	/// <see cref="ManagedSettings"/> the first time the entity is seen at runtime.
	///
	/// The parts of <see cref="ManagedSettings"/> that cannot be baked at all, namely
	/// <see cref="ManagedSettings.onTraverseOffMeshLink"/> and
	/// <see cref="PathRequestSettings.traversalProvider"/>, are not represented here. They were already dropped
	/// by <see cref="ManagedSettings.CloneAndSimplifyDefaults"/> before baking.
	/// </summary>
	public struct AgentBakedSettings : IComponentData {
		/// <summary>\copydocref{PathRequestSettings.graphMask}</summary>
		public GraphMask graphMask;

		/// <summary>\copydocref{PathRequestSettings.traversableTags}</summary>
		public int traversableTags;
	}

	/// <summary>
	/// Baked <see cref="PathRequestSettings.tagEntryCosts"/>, one element per tag.
	///
	/// Absent when the agent leaves every entry cost at zero, which is the common case. A buffer is used
	/// rather than a fixed-size array in the component so that those agents pay nothing.
	/// </summary>
	[InternalBufferCapacity(0)]
	public struct AgentBakedTagEntryCost : IBufferElementData {
		public uint value;
	}

	/// <summary>
	/// Baked <see cref="PathRequestSettings.tagCostMultipliers"/>, one element per tag.
	///
	/// \copydetails AgentBakedTagEntryCost
	/// </summary>
	[InternalBufferCapacity(0)]
	public struct AgentBakedTagCostMultiplier : IBufferElementData {
		public float value;
	}

	/// <summary>
	/// Settings for agent movement that require managed types.
	///
	/// This class is used to store settings for agent movement that cannot be put anywhere else.
	/// For example, it can store delegates, interfaces and objects.
	///
	/// It is used by the <see cref="FollowerEntity"/> component to store settings for how the agent should move.
	///
	/// In contrast to <see cref="ManagedState"/>, these settings are persistent.
	///
	/// This is not a component. Each agent reaches its instance through an <see cref="AgentManagedRef"/>
	/// component, which indexes <see cref="AgentManagedStorage"/>.
	///
	/// See: <see cref="FollowerEntity"/>
	/// </summary>
	[System.Serializable]
	public class ManagedSettings : System.ICloneable, System.IEquatable<ManagedSettings> {
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

		public bool Equals (ManagedSettings other) {
			if (other == null) return false;

			return pathfindingSettings.Equals(other.pathfindingSettings) &&
				   onTraverseOffMeshLink == other.onTraverseOffMeshLink; // Reference equality check
		}
	}
}
#endif
