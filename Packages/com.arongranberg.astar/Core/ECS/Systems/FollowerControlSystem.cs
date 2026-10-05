#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using UnityEngine.Profiling;
using Unity.Profiling;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using GCHandle = System.Runtime.InteropServices.GCHandle;

namespace Pathfinding.ECS {
	using Pathfinding;
	using Pathfinding.ECS.RVO;
	using Pathfinding.Drawing;
	using Pathfinding.RVO;
	using Unity.Collections;
	using Unity.Burst.Intrinsics;
	using System.Diagnostics;

	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[BurstCompile]
	public partial struct FollowerControlSystem : ISystem {
		EntityQuery entityQueryControlManaged;
		EntityQuery entityQueryControlManaged2;
		RedrawScope redrawScope;
		MovementOverrideRunner movementOverrideRunner;

		static readonly ProfilerMarker MarkerMovementOverrideBeforeControl = new ProfilerMarker("MovementOverrideBeforeControl");
		static readonly ProfilerMarker MarkerMovementOverrideAfterControl = new ProfilerMarker("MovementOverrideAfterControl");

		public void OnCreate (ref SystemState state) {
			redrawScope = DrawingManager.GetRedrawScope();
			movementOverrideRunner = new MovementOverrideRunner(ref state);

			entityQueryControlManaged = state.GetEntityQuery(
				ComponentType.ReadOnly<AgentHasBeforeControlOverride>(),

				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<AgentCylinderShape>(),
				ComponentType.ReadWrite<AgentMovementPlane>(),
				ComponentType.ReadWrite<DestinationPoint>(),
				ComponentType.ReadWrite<MovementState>(),
				ComponentType.ReadWrite<MovementStatistics>(),
				ComponentType.ReadWrite<AgentManagedRef>(),
				ComponentType.ReadWrite<MovementSettings>(),
				ComponentType.ReadWrite<ResolvedMovement>(),
				ComponentType.ReadWrite<MovementControl>(),

				ComponentType.Exclude<AgentOffMeshLinkTraversal>(),
				ComponentType.ReadOnly<SimulateMovement>(),
				ComponentType.ReadOnly<SimulateMovementControl>()
				);

			entityQueryControlManaged2 = state.GetEntityQuery(
				ComponentType.ReadOnly<AgentHasAfterControlOverride>(),

				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<AgentCylinderShape>(),
				ComponentType.ReadWrite<AgentMovementPlane>(),
				ComponentType.ReadWrite<DestinationPoint>(),
				ComponentType.ReadWrite<MovementState>(),
				ComponentType.ReadWrite<MovementStatistics>(),
				ComponentType.ReadWrite<AgentManagedRef>(),
				ComponentType.ReadWrite<MovementSettings>(),
				ComponentType.ReadWrite<ResolvedMovement>(),
				ComponentType.ReadWrite<MovementControl>(),

				ComponentType.Exclude<AgentOffMeshLinkTraversal>(),
				ComponentType.ReadOnly<SimulateMovement>(),
				ComponentType.ReadOnly<SimulateMovementControl>()
				);
		}

		public void OnDestroy (ref SystemState state) {
			redrawScope.Dispose();
		}

		public void OnUpdate (ref SystemState systemState) {
			if (AstarPath.active == null) return;

			// Skip system if there are no ECS agents that need movement control
			if (SystemAPI.QueryBuilder().WithAll<MovementState>().Build().IsEmptyIgnoreFilter) return;

			ProcessControlLoop(ref systemState, AIMovementSystemGroup.TimeScaledRateManager.CheapStepDeltaTime);
		}

		void ProcessControlLoop (ref SystemState systemState, float dt) {
			// This is a hook for other systems to modify the movement of agents.
			// Normally it is not used.
			PendingMovementOverrideChanges.Apply();
			if (!entityQueryControlManaged.IsEmpty) {
				MarkerMovementOverrideBeforeControl.Begin();
				systemState.Dependency.Complete();
				movementOverrideRunner.Run(ref systemState, entityQueryControlManaged, MovementOverrideRunner.Phase.BeforeControl, dt);
				MarkerMovementOverrideBeforeControl.End();
			}

			redrawScope.Rewind();
			var draw = DrawingManager.GetBuilder(redrawScope);
			var navmeshEdgeData = AstarPath.active.GetNavmeshBorderData(out var readLock);
			systemState.Dependency = new JobControl {
				navmeshEdgeData = navmeshEdgeData,
				draw = draw,
				dt = dt,
			}.ScheduleParallel(JobHandle.CombineDependencies(systemState.Dependency, readLock.dependency));
			readLock.UnlockAfter(systemState.Dependency);
			draw.DisposeAfter(systemState.Dependency);

			PendingMovementOverrideChanges.Apply();
			if (!entityQueryControlManaged2.IsEmpty) {
				MarkerMovementOverrideAfterControl.Begin();
				systemState.Dependency.Complete();
				movementOverrideRunner.Run(ref systemState, entityQueryControlManaged2, MovementOverrideRunner.Phase.AfterControl, dt);
				MarkerMovementOverrideAfterControl.End();
			}
		}
	}
}
#endif
