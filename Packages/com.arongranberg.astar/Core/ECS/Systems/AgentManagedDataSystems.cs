#if MODULE_ENTITIES
using Unity.Collections;
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Gives cloned agents their own copy of their managed data.
	///
	/// When an entity is cloned, its <see cref="AgentManagedBackupRef"/> is copied verbatim, so the clone starts
	/// out pointing at data owned by its source. Its <see cref="AgentManagedRef"/> is not copied, because
	/// cleanup components never are. An entity with the former and not the latter is therefore exactly an
	/// unrepaired clone, and this system is what turns it into a real agent.
	///
	/// A clone is not a usable agent until this has run. Every A* query reads the slot from
	/// <see cref="AgentManagedRef"/>, so no other system can observe one in the meantime.
	///
	/// See: <see cref="AgentManagedStorage"/>
	/// </summary>
	[UpdateInGroup(typeof(AIMovementSystemGroup), OrderFirst = true)]
	[RequireMatchingQueriesForUpdate]
	public partial struct AgentManagedDataRepairSystem : ISystem {
		EntityQuery unrepairedClones;

		public void OnCreate (ref SystemState state) {
			unrepairedClones = new EntityQueryBuilder(Allocator.Temp)
							   .WithAll<AgentManagedBackupRef>()
							   .WithNone<AgentManagedRef>()
							   .Build(ref state);
		}

		public void OnUpdate (ref SystemState state) {
			var entities = unrepairedClones.ToEntityArray(Allocator.Temp);
			var refs = unrepairedClones.ToComponentDataArray<AgentManagedBackupRef>(Allocator.Temp);
			var slots = new NativeArray<int>(entities.Length, Allocator.Temp);

			for (int i = 0; i < entities.Length; i++) {
				var slot = AgentManagedStorage.CloneFrom(refs[i].slot, entities[i]);
				if (slot < 0) {
					// The source's data was already released. Nothing to clone from, so start the agent
					// from scratch rather than leaving it in a state where it has no path tracer at all.
					slot = AgentManagedStorage.Allocate(entities[i], new ManagedState {
						pathTracer = new PathTracer(Allocator.Persistent),
					}, new ManagedSettings { pathfindingSettings = PathRequestSettings.Default });
				}
				slots[i] = slot;
				// The backup ref must be repointed at the clone's new slot.
				state.EntityManager.SetComponentData(entities[i], new AgentManagedBackupRef { slot = slot });
			}

			// Callbacks are not carried over to a clone, so the markers that say it has them must go too.
			state.EntityManager.RemoveComponent<AgentHasBeforeControlOverride>(unrepairedClones);
			state.EntityManager.RemoveComponent<AgentHasAfterControlOverride>(unrepairedClones);
			state.EntityManager.RemoveComponent<AgentHasBeforeMovementOverride>(unrepairedClones);

			// Adding the cleanup component is what marks these entities as repaired, so it must happen
			// after every slot above has been written. Done as one batched structural change per frame.
			for (int i = 0; i < entities.Length; i++) {
				state.EntityManager.AddComponentData(entities[i], new AgentManagedRef { slot = slots[i] });
			}
		}
	}

	/// <summary>
	/// Releases the managed data of agents that have been destroyed.
	///
	/// <see cref="AgentManagedRef"/> is a cleanup component, so an agent that is destroyed does not disappear
	/// immediately: everything else about it is stripped, that component remains, and the entity lingers
	/// until this system removes it. That window is what lets the agent's path be released and its path
	/// tracer disposed instead of leaking.
	///
	/// Invariant: this system runs after <see cref="AgentManagedDataRepairSystem"/>. Reversed, it would return a
	/// slot to the free list that repair could hand to a different clone within the same frame, and a clone
	/// repaired later in that pass would deep-clone an unrelated agent's path tracer.
	///
	/// Running last in the group is also what lets other systems still see a destroyed agent's data during
	/// the frame it died. <see cref="JobManagedOffMeshLinkTransitionCleanup"/> depends on that to tell a state
	/// machine its link traversal was aborted.
	///
	/// See: <see cref="AgentManagedStorage"/>
	/// </summary>
	[UpdateInGroup(typeof(AIMovementSystemGroup), OrderLast = true)]
	[RequireMatchingQueriesForUpdate]
	public partial struct AgentManagedDataCleanupSystem : ISystem {
		EntityQuery destroyedAgents;
		EntityQuery allAgents;

		public void OnCreate (ref SystemState state) {
			destroyedAgents = new EntityQueryBuilder(Allocator.Temp)
							  .WithAll<AgentManagedRef>()
							  .WithNone<AgentManagedBackupRef>()
							  .Build(ref state);
			allAgents = new EntityQueryBuilder(Allocator.Temp)
						.WithAll<AgentManagedRef>()
						.Build(ref state);
		}

		public void OnUpdate (ref SystemState state) {
			// allAgents is non-empty whenever any agent exists, so RequireMatchingQueriesForUpdate cannot
			// skip this system on its own.
			if (destroyedAgents.IsEmptyIgnoreFilter) return;
			Release(ref state, destroyedAgents);
			state.EntityManager.RemoveComponent<AgentManagedRef>(destroyedAgents);
		}

		public void OnDestroy (ref SystemState state) {
			// A world being torn down does not run its cleanup systems, so without this every agent still
			// alive in it would keep its path claimed for the rest of the process. The entities are still
			// queryable here, because systems are destroyed before the entity store is.
			Release(ref state, allAgents);
		}

		static void Release (ref SystemState state, EntityQuery query) {
			var entities = query.ToEntityArray(Allocator.Temp);
			var data = query.ToComponentDataArray<AgentManagedRef>(Allocator.Temp);
			for (int i = 0; i < entities.Length; i++) {
				AgentManagedStorage.Free(data[i].slot, entities[i]);
			}
		}
	}
}
#endif
