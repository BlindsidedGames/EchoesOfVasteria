#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using UnityEngine.Profiling;
using Unity.Transforms;
using Unity.Burst;

namespace Pathfinding.ECS {
	using Pathfinding;
	using Unity.Burst.Intrinsics;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Mathematics;
	using Unity.Profiling;

	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(RepairPathSystem))]
	[UpdateBefore(typeof(TraverseOffMeshLinkSystem))]
	[BurstCompile]
	public partial struct SchedulePathSearchSystem : ISystem {
		public void OnUpdate (ref SystemState systemState) {
			// While the agent can technically discover that the path is stale during a simulation step,
			// only scheduling paths during the first substep is typically good enough.
			if (AstarPath.active == null || !AIMovementSystemGroup.TimeScaledRateManager.IsFirstSubstep) return;

			// Skip system if there are no ECS agents that use pathfinding
			if (SystemAPI.QueryBuilder().WithAll<ManagedState>().Build().IsEmptyIgnoreFilter) return;

			Profiler.BeginSample("Schedule search");
			var bits = new NativeBitArray(512, Allocator.TempJob);
			systemState.CompleteDependency();

			// Block the pathfinding threads from starting new path calculations while this loop is running.
			// This is done to reduce lock contention and significantly improve performance.
			// If we did not do this, all pathfinding threads would immediately wake up when a path was pushed to the queue.
			// Immediately when they wake up they will try to acquire a lock on the path queue.
			// If we are scheduling a lot of paths, this causes significant contention, and can make this loop take 100 times
			// longer to complete, compared to if we block the pathfinding threads.
			// TODO: Switch to a lock-free queue to avoid this issue altogether.
			var pathfindingLock = AstarPath.active.PausePathfindingSoon();

			// Propagate staleness
			Profiler.BeginSample("Check stale");
			new JobCheckStaleness {
				isPathStale = bits,
			}.Run();
			Profiler.EndSample();

			Profiler.BeginSample("Check should recalculate");
			// Calculate which agents want to recalculate their path (using burst)
			new JobShouldRecalculatePaths {
				time = (float)SystemAPI.Time.ElapsedTime,
				isPathStale = bits,
			}.Run();
			Profiler.EndSample();

			Profiler.BeginSample("Schedule path calculations");
			// Schedule the path calculations
			new JobRecalculatePaths {
				time = (float)SystemAPI.Time.ElapsedTime,
			}.Run();
			Profiler.EndSample();

			pathfindingLock.Release();
			bits.Dispose();
			Profiler.EndSample();
		}

		[WithAbsent(typeof(ManagedAgentOffMeshLinkTraversal))] // Do not recalculate the path of agents that are currently traversing an off-mesh link.
		[WithPresent(typeof(AgentShouldRecalculatePath))]
		partial struct JobCheckStaleness : IJobEntity, IJobEntityChunkBeginEnd {
			public NativeBitArray isPathStale;
			int index;

			public void Execute (ManagedState state) {
				isPathStale.Set(index++, state.pathTracer.isStale);
			}

			public bool OnChunkBegin (in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
				if (index + chunk.Count > isPathStale.Length) isPathStale.Resize(math.ceilpow2(index + chunk.Count), NativeArrayOptions.ClearMemory);
				return true;
			}

			public void OnChunkEnd (in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted) {}
		}


		[BurstCompile]
		[WithAbsent(typeof(ManagedAgentOffMeshLinkTraversal))] // Do not recalculate the path of agents that are currently traversing an off-mesh link.
		[WithPresent(typeof(AgentShouldRecalculatePath))]
		partial struct JobShouldRecalculatePaths : IJobEntity {
			public float time;
			public NativeBitArray isPathStale;
			int index;

			public void Execute (ref ECS.AutoRepathPolicy autoRepathPolicy, in LocalTransform transform, in AgentCylinderShape shape, in DestinationPoint destination, EnabledRefRW<AgentShouldRecalculatePath> shouldRecalculatePath) {
				var isPathStale = this.isPathStale.IsSet(index++);
				shouldRecalculatePath.ValueRW = autoRepathPolicy.ShouldRecalculatePath(transform.Position, shape.radius, destination.destination, time, isPathStale);
			}
		}

		[WithAbsent(typeof(ManagedAgentOffMeshLinkTraversal))] // Do not recalculate the path of agents that are currently traversing an off-mesh link.
		[WithAll(typeof(AgentShouldRecalculatePath))]
		public partial struct JobRecalculatePaths : IJobEntity {
			public float time;

			public void Execute (ManagedState state, ManagedSettings settings, ref ECS.AutoRepathPolicy autoRepathPolicy, ref LocalTransform transform, ref DestinationPoint destination, ref AgentMovementPlane movementPlane) {
				// If we reach this point, the agent always wants to recalculate its path, because the AgentShouldRecalculatePath component is enabled
				MaybeRecalculatePath(state, settings, ref autoRepathPolicy, ref transform, ref destination, ref movementPlane, time, true);
			}

			public static void MaybeRecalculatePath (ManagedState state, ManagedSettings settings, ref ECS.AutoRepathPolicy autoRepathPolicy, ref LocalTransform transform, ref DestinationPoint destination, ref AgentMovementPlane movementPlane, float time, bool wantsToRecalculatePath) {
				if (wantsToRecalculatePath && state.pendingPath == null) {
					var path = ABPath.Construct(transform.Position, destination.destination, null);
					path.UseSettings(settings.pathfindingSettings);
					path.nearestNodeDistanceMetric = DistanceMetric.ClosestAsSeenFromAboveSoft(movementPlane.value.up);
					ManagedState.SetPath(path, state, in movementPlane, ref destination);
					autoRepathPolicy.OnScheduledPathRecalculation(destination.destination, time);
				}
			}
		}
	}
}
#endif
