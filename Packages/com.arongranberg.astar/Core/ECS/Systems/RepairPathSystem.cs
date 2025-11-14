#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Burst;

namespace Pathfinding.ECS {
	using Pathfinding;

	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(FollowerControlSystem))]
	[BurstCompile]
	public partial struct RepairPathSystem : ISystem {
		EntityQuery entityQueryPrepare;
		JobRepairPath.Scheduler jobRepairPathScheduler;

		public void OnCreate (ref SystemState state) {
			jobRepairPathScheduler = new JobRepairPath.Scheduler(ref state);
			entityQueryPrepare = jobRepairPathScheduler.GetEntityQuery(Unity.Collections.Allocator.Temp).WithAll<SimulateMovement, SimulateMovementRepair>().Build(ref state);
		}

		public void OnDestroy (ref SystemState state) {
			jobRepairPathScheduler.Dispose();
		}

		public void OnUpdate (ref SystemState systemState) {
			if (AstarPath.active == null) return;

			// Skip system if there are no ECS agents that need path repair
			if (SystemAPI.QueryBuilder().WithAll<MovementState>().Build().IsEmptyIgnoreFilter) return;

			// This job accesses managed component data in a somewhat unsafe way.
			// It should be safe to run it in parallel with other systems, but I'm not 100% sure.
			// This job also accesses graph data, but this is safe because the AIMovementSystemGroup
			// holds a read lock on the graph data while its subsystems are running.
			systemState.Dependency = jobRepairPathScheduler.ScheduleParallel(ref systemState, entityQueryPrepare, systemState.Dependency);
		}

		[System.Obsolete("Use TraverseOffMeshLinkSystem.NextLinkToTraverse instead")]
		public static OffMeshLinks.OffMeshLinkTracer NextLinkToTraverse (ManagedState state) {
			return TraverseOffMeshLinkSystem.NextLinkToTraverse(state);
		}

		[System.Obsolete("Use TraverseOffMeshLinkSystem.ResolveOffMeshLinkHandler instead")]
		public static IOffMeshLinkHandler ResolveOffMeshLinkHandler (ManagedSettings settings, AgentOffMeshLinkTraversalContext ctx) {
			return TraverseOffMeshLinkSystem.ResolveOffMeshLinkHandler(settings, ctx);
		}
	}
}
#endif
