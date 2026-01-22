#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;

namespace Pathfinding.ECS {
	using Pathfinding;
	using Unity.Burst.Intrinsics;
	using Unity.Collections;
	using Unity.Mathematics;
	using Unity.Profiling;
	using UnityEngine;

	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(RepairPathSystem))]
	[UpdateBefore(typeof(TraverseOffMeshLinkSystem))]
	[BurstCompile]
	public partial struct SchedulePathSearchSystem : ISystem {
		static readonly ProfilerMarker MarkerSchedulePathSearch = new ProfilerMarker("Schedule Path Search");
		static readonly ProfilerMarker MarkerCheckStaleness = new ProfilerMarker("Check Path Staleness");
		static readonly ProfilerMarker MarkerShouldRecalculatePaths = new ProfilerMarker("Check Should Recalculate Paths");
		static readonly ProfilerMarker MarkerRecalculatePaths = new ProfilerMarker("Schedule Path Calculations");

		public void OnUpdate (ref SystemState systemState) {
			// While the agent can technically discover that the path is stale during a simulation step,
			// only scheduling paths during the first substep is typically good enough.
			if (AstarPath.active == null || !AIMovementSystemGroup.TimeScaledRateManager.IsFirstSubstep) return;

			// Skip system if there are no ECS agents that use pathfinding
			if (SystemAPI.QueryBuilder().WithAll<ManagedState>().Build().IsEmptyIgnoreFilter) return;

			MarkerSchedulePathSearch.Begin();
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
			MarkerCheckStaleness.Begin();
			new JobCheckStaleness {
				isPathStale = bits,
			}.Run();
			MarkerCheckStaleness.End();

			MarkerShouldRecalculatePaths.Begin();
			// Calculate which agents want to recalculate their path (using burst)
			new JobShouldRecalculatePaths {
				time = (float)SystemAPI.Time.ElapsedTime,
				isPathStale = bits,
			}.Run();
			MarkerShouldRecalculatePaths.End();

			MarkerRecalculatePaths.Begin();
			// Schedule the path calculations
			new JobRecalculatePaths {
				time = (float)SystemAPI.Time.ElapsedTime,
			}.Run();
			MarkerRecalculatePaths.End();

			pathfindingLock.Release();
			bits.Dispose();
			MarkerSchedulePathSearch.End();
		}

		[WithAbsent(typeof(ManagedAgentOffMeshLinkTraversal))] // Do not recalculate the path of agents that are currently traversing an off-mesh link.
		[WithPresent(typeof(AgentShouldRecalculatePath))]
		partial struct JobCheckStaleness : IJobEntity, IJobEntityChunkBeginEnd {
			public NativeBitArray isPathStale;
			int index;

			public void Execute (ManagedState state) {
				isPathStale.Set(index++, state.pathTracer.isStale);
				isPathStale.Set(index++, state.pendingPath != null);
			}

			public bool OnChunkBegin (in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
				if (index + chunk.Count*2 > isPathStale.Length) isPathStale.Resize(math.ceilpow2(index + chunk.Count*2), NativeArrayOptions.ClearMemory);
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
				// If a path is pending, we always want to run JobRecalculatePaths for that agent, to refresh the path endpoints.
				var isPathPending = this.isPathStale.IsSet(index++);
				shouldRecalculatePath.ValueRW = isPathPending || autoRepathPolicy.ShouldRecalculatePath(transform.Position, shape.radius, destination.destination, time, isPathStale);
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
				if (wantsToRecalculatePath) {
					if (state.pendingPath == null) {
						var path = ABPath.Construct(transform.Position, destination.destination, null);
						path.UseSettings(settings.pathfindingSettings);
						path.nearestNodeDistanceMetric = DistanceMetric.ClosestAsSeenFromAboveSoft(movementPlane.value.up);
						ManagedState.SetPath(path, state, in movementPlane, ref destination);
						autoRepathPolicy.OnScheduledPathRecalculation(destination.destination, time);
					} else if (state.pendingPath is ABPath aBPath) {
						// Refresh the endpoints of the pending path.
						// This is useful if the agent has moved significantly since the path was requested.
						aBPath.RefreshPathEndpoints(transform.Position, destination.destination);
					}
				}
			}
		}
	}
}
#endif
