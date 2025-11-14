#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Profiling;
using Pathfinding.Util;
using UnityEngine.Jobs;

namespace Pathfinding.ECS {
	/// <summary>
	/// System that runs after all movement systems and repairs the agent's path and syncs its orientation to the associated Transform component, if one exists.
	///
	/// Repairing the path after running all the movement simulations is not strictly necessary, but it ensures that any other scripts querying properties like the remaining path, or if the agent has reached its destination,
	/// will see up to date information immediately.
	///
	/// If the <see cref="FollowerEntity"/> was not in a subscene, we need to sync the internal entity's position and rotation to the Transform that the FollowerEntity component is attached to.
	/// However, if it was placed in a subscene, the Transform and FollowerEntity component won't even exist at runtime (except when debugging in the unity editor), and so the sync can be skipped.
	/// </summary>
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateAfter(typeof(AIMoveSystem))]
	[UpdateAfter(typeof(MovementStatisticsSystem))]
	[RequireMatchingQueriesForUpdate]
	public partial struct LateSyncSystem : ISystem {
		JobRepairPath.Scheduler jobRepairPathScheduler;
		EntityQuery entityQueryPrepareMovement;

		public void OnCreate (ref SystemState systemState) {
			jobRepairPathScheduler = new JobRepairPath.Scheduler(ref systemState);
			entityQueryPrepareMovement = jobRepairPathScheduler.GetEntityQuery(Allocator.Temp).WithAll<SimulateMovement, SimulateMovementRepair>().Build(ref systemState);
		}

		public void OnDestroy (ref SystemState systemState) {
			jobRepairPathScheduler.Dispose();
		}

		public void OnUpdate (ref SystemState systemState) {
			systemState.Dependency = ScheduleRepairPaths(ref systemState, systemState.Dependency);
			systemState.Dependency = ScheduleSyncEntitiesToTransforms(ref systemState, systemState.Dependency);
			systemState.Dependency = new JobClearTemporaryData().Schedule(systemState.Dependency);
		}

		JobHandle ScheduleRepairPaths (ref SystemState systemState, JobHandle dependency) {
			Profiler.BeginSample("RepairPaths");
			// This job accesses graph data, but this is safe because the AIMovementSystemGroup
			// holds a read lock on the graph data while its subsystems are running.
			dependency = jobRepairPathScheduler.ScheduleParallel(ref systemState, entityQueryPrepareMovement, dependency);
			Profiler.EndSample();
			return dependency;
		}

		JobHandle ScheduleSyncEntitiesToTransforms (ref SystemState systemState, JobHandle dependency) {
			Profiler.BeginSample("SyncEntitiesToTransforms");
			int numComponents = BatchedEvents.GetComponents<FollowerEntity>(BatchedEvents.Event.None, out var transforms, out var components);
			if (numComponents > 0) {
				var entities = CollectionHelper.CreateNativeArray<Entity>(numComponents, systemState.WorldUpdateAllocator);
				for (int i = 0; i < numComponents; i++) entities[i] = components[i].entity;

				dependency = new JobSyncEntitiesToTransforms {
					entities = entities,
					syncPositionWithTransform = SystemAPI.GetComponentLookup<SyncPositionWithTransform>(true),
					syncRotationWithTransform = SystemAPI.GetComponentLookup<SyncRotationWithTransform>(true),
					orientationYAxisForward = SystemAPI.GetComponentLookup<OrientationYAxisForward>(true),
					entityPositions = SystemAPI.GetComponentLookup<LocalTransform>(true),
					movementState = SystemAPI.GetComponentLookup<MovementState>(true),
				}.Schedule(transforms, dependency);
			}
			Profiler.EndSample();
			return dependency;
		}
	}
}
#endif
