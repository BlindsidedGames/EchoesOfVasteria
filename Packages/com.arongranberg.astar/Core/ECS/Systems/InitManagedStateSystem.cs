#if MODULE_ENTITIES
using Unity.Collections;
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Initializes managed state for baked agents.
	///
	/// A baked entity cannot carry a storage slot in <see cref="AgentManagedStorage"/>, because baking happens ahead of time and its output is
	/// serialized while the storage only exists at runtime. So a baked <see cref="FollowerEntity"/> arrives with
	/// an <see cref="AgentBakedSettings"/> component and no <see cref="AgentManagedRef"/>.
	///
	/// Agents created from a <see cref="FollowerEntity"/> MonoBehaviour do not go through here.
	/// <see cref="FollowerEntity.CreateEntity"/> allocates their slot directly.
	///
	/// See: <see cref="AgentManagedStorage"/>
	/// See: <see cref="PathTracer"/>
	/// See: <see cref="FollowerEntity"/>
	/// </summary>
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(MovementPlaneFromGraphSystem))]
	[UpdateBefore(typeof(SchedulePathSearchSystem))]
	[RequireMatchingQueriesForUpdate]
	public partial struct InitManagedStateSystem : ISystem {
		public void OnUpdate (ref SystemState state) {
			// AgentManagedBackupRef rather than AgentManagedRef: a clone of an initialized baked agent has
			// the former but not the latter, and it must be handled by AgentManagedDataRepairSystem, not
			// re-initialized from its baked settings here.
			var query = SystemAPI.QueryBuilder().WithAll<AgentBakedSettings>().WithNone<AgentManagedBackupRef>().Build();
			var entities = query.ToEntityArray(Allocator.Temp);

			var slots = new NativeArray<int>(entities.Length, Allocator.Temp);
			for (int i = 0; i < entities.Length; i++) {
				slots[i] = AgentManagedStorage.Allocate(entities[i], new ManagedState {
					pathTracer = new PathTracer(Allocator.Persistent),
				}, BuildSettings(ref state, entities[i]));
			}

			// One batched structural change rather than one per entity, then fill in the slots.
			state.EntityManager.AddComponent<AgentManagedRef>(entities);
			state.EntityManager.AddComponent<AgentManagedBackupRef>(entities);
			for (int i = 0; i < entities.Length; i++) {
				state.EntityManager.SetComponentData(entities[i], new AgentManagedRef { slot = slots[i] });
				state.EntityManager.SetComponentData(entities[i], new AgentManagedBackupRef { slot = slots[i] });
			}

			for (int i = 0; i < entities.Length; i++) {
				// This will attach the entity to the navmesh (if one exists), to make things like #currentNode and being snapped to the graph surface work immediately.
				// Otherwise, we'd have to wait for the first path calculation to finish.
				var proxy = new FollowerEntityProxy(state.World, entities[i]);
				proxy.Teleport(proxy.position, false);
			}
		}

		/// <summary>Reconstructs the settings that were flattened into unmanaged components during baking.</summary>
		static ManagedSettings BuildSettings (ref SystemState state, Entity entity) {
			var baked = state.EntityManager.GetComponentData<AgentBakedSettings>(entity);
			var settings = new ManagedSettings {
				pathfindingSettings = new PathRequestSettings {
					graphMask = baked.graphMask,
					traversableTags = baked.traversableTags,
				},
			};

			// The buffers are absent when the agent left these at their defaults, in which case null is the
			// representation the pathfinding code expects.
			if (state.EntityManager.HasBuffer<AgentBakedTagEntryCost>(entity)) {
				var buffer = state.EntityManager.GetBuffer<AgentBakedTagEntryCost>(entity);
				var costs = new uint[buffer.Length];
				for (int i = 0; i < buffer.Length; i++) costs[i] = buffer[i].value;
				settings.pathfindingSettings.tagEntryCosts = costs;
			}
			if (state.EntityManager.HasBuffer<AgentBakedTagCostMultiplier>(entity)) {
				var buffer = state.EntityManager.GetBuffer<AgentBakedTagCostMultiplier>(entity);
				var multipliers = new float[buffer.Length];
				for (int i = 0; i < buffer.Length; i++) multipliers[i] = buffer[i].value;
				settings.pathfindingSettings.tagCostMultipliers = multipliers;
			}
			return settings;
		}
	}
}
#endif
