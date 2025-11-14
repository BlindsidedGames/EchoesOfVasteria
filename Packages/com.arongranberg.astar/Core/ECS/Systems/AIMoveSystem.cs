#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

namespace Pathfinding.ECS {
	using Pathfinding.ECS.RVO;
	using Pathfinding.Drawing;
	using Pathfinding.Util;
	using Unity.Profiling;
	using UnityEngine.Profiling;

	[BurstCompile]
	[UpdateAfter(typeof(FollowerControlSystem))]
	[UpdateAfter(typeof(RVOSystem))]
	[UpdateAfter(typeof(FallbackResolveMovementSystem))]
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[RequireMatchingQueriesForUpdate]
	public partial struct AIMoveSystem : ISystem {
		EntityQuery entityQueryWithGravity;
		EntityQuery entityQueryMovementOverride;

		public void OnCreate (ref SystemState state) {
			entityQueryWithGravity = state.GetEntityQuery(
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadOnly<AgentCylinderShape>(),
				ComponentType.ReadWrite<AgentMovementPlane>(),
				ComponentType.ReadWrite<MovementState>(),
				ComponentType.ReadOnly<MovementSettings>(),
				ComponentType.ReadWrite<ResolvedMovement>(),
				ComponentType.ReadWrite<MovementStatistics>(),
				ComponentType.ReadOnly<MovementControl>(),
				ComponentType.ReadWrite<GravityState>(),
				ComponentType.ReadOnly<PhysicsSceneRef>(),

				// When in 2D mode, gravity is always disabled
				ComponentType.Exclude<OrientationYAxisForward>(),

				ComponentType.Exclude<AgentOffMeshLinkMovementDisabled>(),

				ComponentType.ReadOnly<AgentMovementPlaneSource>(),
				ComponentType.ReadOnly<SimulateMovement>(),
				ComponentType.ReadOnly<SimulateMovementFinalize>()
				);

			entityQueryMovementOverride = state.GetEntityQuery(
				ComponentType.ReadWrite<ManagedMovementOverrideBeforeMovement>(),

				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<AgentCylinderShape>(),
				ComponentType.ReadWrite<AgentMovementPlane>(),
				ComponentType.ReadWrite<DestinationPoint>(),
				ComponentType.ReadWrite<MovementState>(),
				ComponentType.ReadWrite<MovementStatistics>(),
				ComponentType.ReadWrite<ManagedState>(),
				ComponentType.ReadWrite<MovementSettings>(),
				ComponentType.ReadWrite<ResolvedMovement>(),
				ComponentType.ReadWrite<MovementControl>(),

				ComponentType.Exclude<AgentOffMeshLinkTraversal>(),
				ComponentType.ReadOnly<SimulateMovement>(),
				ComponentType.ReadOnly<SimulateMovementControl>()
				);
		}

		static readonly ProfilerMarker MarkerMovementOverride = new ProfilerMarker("MovementOverrideBeforeMovement");

		public void OnUpdate (ref SystemState systemState) {
			// This system is executed at least every frame to make sure the agent is moving smoothly even at high fps.
			// The control loop and local avoidance may be running less often.
			// So this is designated a "cheap" system, and we use the corresponding delta time for that.
			var dt = AIMovementSystemGroup.TimeScaledRateManager.CheapStepDeltaTime;

			systemState.Dependency = new JobAlignAgentWithMovementDirection {
				dt = dt,
			}.Schedule(systemState.Dependency);

			RunMovementOverrideBeforeMovement(ref systemState, dt);

			// Move all agents
			systemState.Dependency = new JobMoveAgent {
				dt = dt,
			}.ScheduleParallel(systemState.Dependency);

			ScheduleApplyGravity(ref systemState, dt);
		}

		void ScheduleApplyGravity (ref SystemState systemState, float dt) {
			Profiler.BeginSample("Gravity");
			// Allocate one raycast command and one hit for each entity that needs gravity.
			// Note: We don't want to use CalculateEntityCountWithoutFiltering here, because the GravityState component can be disabled.
			// We could get only an upper bound, but fortunately using CalculateEntityCount here is fine, because the dependencies it injects
			// are usually long done by this point, so it doesn't add much overhead.
			var count = entityQueryWithGravity.CalculateEntityCount();
			var raycastCommands = CollectionHelper.CreateNativeArray<RaycastCommand>(count, systemState.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
			var raycastHits = CollectionHelper.CreateNativeArray<RaycastHit>(count, systemState.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

			// Prepare raycasts for all entities that have a GravityState component
			systemState.Dependency = new JobPrepareAgentRaycasts {
				raycastQueryParameters = new QueryParameters(-1, false, QueryTriggerInteraction.Ignore, false),
				raycastCommands = raycastCommands,
				dt = dt,
				gravity = Physics.gravity.y,
			}.ScheduleParallel(entityQueryWithGravity, systemState.Dependency);

			var raycastJob = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 32, 1, systemState.Dependency);

			// Apply gravity and move all agents that have a GravityState component
			systemState.Dependency = new JobApplyGravity {
				raycastHits = raycastHits,
				raycastCommands = raycastCommands,
				dt = dt,
			}.ScheduleParallel(entityQueryWithGravity, JobHandle.CombineDependencies(systemState.Dependency, raycastJob));

			Profiler.EndSample();
		}

		void RunMovementOverrideBeforeMovement (ref SystemState systemState, float dt) {
			if (!entityQueryMovementOverride.IsEmptyIgnoreFilter) {
				MarkerMovementOverride.Begin();
				// The movement overrides always run on the main thread.
				// This adds a sync point, but only if people actually add a movement override (which is rare).
				systemState.CompleteDependency();
				new JobManagedMovementOverrideBeforeMovement {
					dt = dt,
					// TODO: Add unit test to make sure it fires/not fires when it should
				}.Run(entityQueryMovementOverride);
				MarkerMovementOverride.End();
			}
		}
	}
}
#endif
