#if MODULE_ENTITIES
using Unity.Collections;
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Creates a ManagedState component for every entity with a ManagedSettings component.
	///
	/// This ensures all baked FollowerEntity entities get a <see cref="ManagedState"/> component when they are created.
	///
	/// See: <see cref="ManagedState"/>
	/// See: <see cref="PathTracer"/>
	/// See: <see cref="FollowerEntity"/>
	/// </summary>
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(MovementPlaneFromGraphSystem))]
	[UpdateBefore(typeof(SchedulePathSearchSystem))]
	[RequireMatchingQueriesForUpdate]
	public partial struct InitManagedStateSystem : ISystem {
		public void OnUpdate (ref SystemState state) {
			var query = SystemAPI.QueryBuilder().WithAll<ManagedSettings>().WithNone<ManagedState>().Build();
			var entities = query.ToEntityArray(Allocator.Temp);
			state.EntityManager.AddComponent<ManagedState>(entities);
			for (int i = 0; i < entities.Length; i++) {
				state.EntityManager.SetComponentData(entities[i], new ManagedState {
					pathTracer = new PathTracer(Allocator.Persistent),
				});
			}

			for (int i = 0; i < entities.Length; i++) {
				// This will attach the entity to the navmesh (if one exists), to make things like #currentNode and being snapped to the graph surface work immediately.
				// Otherwise, we'd have to wait for the first path calculation to finish.
				var proxy = new FollowerEntityProxy(state.World, entities[i]);
				proxy.Teleport(proxy.position, false);
			}
		}
	}
}
#endif
