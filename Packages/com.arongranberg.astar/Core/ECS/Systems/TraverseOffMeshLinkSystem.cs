#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using UnityEngine.Profiling;

namespace Pathfinding.ECS {
	using Pathfinding;

	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(FollowerControlSystem))]
	[UpdateBefore(typeof(RepairPathSystem))]
	public partial struct TraverseOffMeshLinkSystem : ISystem {
		EntityQuery entityQueryPrepare;
		EntityQuery entityQueryOffMeshLinkCleanup;
		public JobRepairPath.Scheduler jobRepairPathScheduler;

		public void OnCreate (ref SystemState state) {
			jobRepairPathScheduler = new JobRepairPath.Scheduler(ref state);

			entityQueryPrepare = jobRepairPathScheduler.GetEntityQuery(Unity.Collections.Allocator.Temp).WithAll<SimulateMovement, SimulateMovementRepair>().Build(ref state);

			entityQueryOffMeshLinkCleanup = state.GetEntityQuery(
				// ManagedAgentOffMeshLinkTraversal is a cleanup component.
				// If it exists, but the AgentOffMeshLinkTraversal does not exist,
				// then the agent must have been destroyed while traversing the off-mesh link.
				ComponentType.ReadOnly<ManagedAgentOffMeshLinkTraversal>(),
				ComponentType.Exclude<AgentOffMeshLinkTraversal>()
				);
		}

		public void OnDestroy (ref SystemState state) {
			jobRepairPathScheduler.Dispose();
		}

		public void OnUpdate (ref SystemState systemState) {
			if (AstarPath.active == null) return;

			// Skip system if there are no agents with support for using off-mesh links
			if (SystemAPI.QueryBuilder().WithAny<AgentOffMeshLinkTraversal, ReadyToTraverseOffMeshLink>().Build().IsEmptyIgnoreFilter) return;

			var commandBuffer = new EntityCommandBuffer(systemState.WorldUpdateAllocator);
			StartOffMeshLinkTraversal(ref systemState, commandBuffer);

			commandBuffer.Playback(systemState.EntityManager);
			commandBuffer.Dispose();

			ProcessActiveOffMeshLinkTraversal(ref systemState);
		}

		void StartOffMeshLinkTraversal (ref SystemState systemState, EntityCommandBuffer commandBuffer) {
			Profiler.BeginSample("Start off-mesh link traversal");
			foreach (var(state, settings, entity) in SystemAPI.Query<ManagedState, ManagedSettings>().WithAll<ReadyToTraverseOffMeshLink>()
					 .WithEntityAccess()
			         // Do not try to add another off-mesh link component to agents that already have one.
					 .WithNone<AgentOffMeshLinkTraversal>()) {
				// UnityEngine.Assertions.Assert.IsTrue(movementState.ValueRO.reachedEndOfPart && state.pathTracer.isNextPartValidLink);
				var linkInfo = NextLinkToTraverse(state);
				var ctx = new AgentOffMeshLinkTraversalContext(linkInfo.link);
				// Add the AgentOffMeshLinkTraversal and ManagedAgentOffMeshLinkTraversal components when the agent should start traversing an off-mesh link.
				commandBuffer.AddComponent(entity, new AgentOffMeshLinkTraversal(linkInfo));
				commandBuffer.AddComponent(entity, new ManagedAgentOffMeshLinkTraversal(ctx, ResolveOffMeshLinkHandler(settings, ctx)));
				commandBuffer.AddComponent(entity, new AgentOffMeshLinkMovementDisabled());
				commandBuffer.AddComponent(entity, new AgentOffMeshLinkLocalAvoidanceDisabled());
			}
			Profiler.EndSample();
		}

		public static OffMeshLinks.OffMeshLinkTracer NextLinkToTraverse (ManagedState state) {
			return state.pathTracer.GetLinkInfo(1);
		}

		public static IOffMeshLinkHandler ResolveOffMeshLinkHandler (ManagedSettings settings, AgentOffMeshLinkTraversalContext ctx) {
			var handler = settings.onTraverseOffMeshLink ?? ctx.concreteLink.handler;
			return handler;
		}

		void ProcessActiveOffMeshLinkTraversal (ref SystemState systemState) {
			var commandBuffer = new EntityCommandBuffer(systemState.WorldUpdateAllocator);
			systemState.CompleteDependency();

			new JobManagedOffMeshLinkTransition {
				commandBuffer = commandBuffer,
				deltaTime = AIMovementSystemGroup.TimeScaledRateManager.CheapStepDeltaTime,
			}.Run();

			if (!entityQueryOffMeshLinkCleanup.IsEmptyIgnoreFilter) {
				new JobManagedOffMeshLinkTransitionCleanup().Run(entityQueryOffMeshLinkCleanup);
#if MODULE_ENTITIES_1_0_8_OR_NEWER
				commandBuffer.RemoveComponent<ManagedAgentOffMeshLinkTraversal>(entityQueryOffMeshLinkCleanup, EntityQueryCaptureMode.AtPlayback);
				commandBuffer.RemoveComponent<AgentOffMeshLinkMovementDisabled>(entityQueryOffMeshLinkCleanup, EntityQueryCaptureMode.AtPlayback);
				commandBuffer.RemoveComponent<AgentOffMeshLinkLocalAvoidanceDisabled>(entityQueryOffMeshLinkCleanup, EntityQueryCaptureMode.AtPlayback);
#else
				commandBuffer.RemoveComponent<ManagedAgentOffMeshLinkTraversal>(entityQueryOffMeshLinkCleanup);
				commandBuffer.RemoveComponent<AgentOffMeshLinkMovementDisabled>(entityQueryOffMeshLinkCleanup);
				commandBuffer.RemoveComponent<AgentOffMeshLinkLocalAvoidanceDisabled>(entityQueryOffMeshLinkCleanup);
#endif
			}

			commandBuffer.Playback(systemState.EntityManager);
			commandBuffer.Dispose();
		}
	}
}
#endif
