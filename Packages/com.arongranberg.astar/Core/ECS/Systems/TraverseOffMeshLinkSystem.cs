#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using UnityEngine.Profiling;

namespace Pathfinding.ECS {
	using Pathfinding;

	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(FollowerControlSystem))]
	[UpdateBefore(typeof(RepairPathSystem))] // Must run before RepairPathSystem to allow the agent to instantly start moving correctly after an agent finishes traversing an off-mesh link.
	public partial struct TraverseOffMeshLinkSystem : ISystem {
		EntityQuery entityQueryOffMeshLinkCleanup;
		EntityQuery entityQueryOffMeshLinkTransition;
		JobManagedOffMeshLinkTransition offMeshLinkTransitionRunner;
		public JobRepairPath.Scheduler jobRepairPathScheduler;

		public void OnCreate (ref SystemState state) {
			jobRepairPathScheduler = new JobRepairPath.Scheduler(ref state);
			offMeshLinkTransitionRunner = new JobManagedOffMeshLinkTransition(ref state);
			entityQueryOffMeshLinkTransition = JobManagedOffMeshLinkTransition.GetEntityQuery(ref state);

			entityQueryOffMeshLinkCleanup = state.GetEntityQuery(
				// AgentOffMeshLinkTraversalCleanup is a cleanup component.
				// If it exists, but the AgentOffMeshLinkTraversal does not exist,
				// then the agent must have been destroyed while traversing the off-mesh link.
				ComponentType.ReadOnly<AgentOffMeshLinkTraversalCleanup>(),
				// AgentManagedRef is also a cleanup component, so it is still present for a destroyed
				// agent, and it is what tells us which storage slot holds the traversal state.
				ComponentType.ReadOnly<AgentManagedRef>(),
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
			foreach (var(managedRef, entity) in SystemAPI.Query<RefRW<AgentManagedRef> >().WithAll<ReadyToTraverseOffMeshLink>()
					 .WithEntityAccess()
			         // Do not try to add another off-mesh link component to agents that already have one.
					 .WithNone<AgentOffMeshLinkTraversal>()) {
				var slot = managedRef.ValueRO.slot;
				ref readonly var entry = ref AgentManagedStorage.entries[slot];
				var state = entry.state;
				// UnityEngine.Assertions.Assert.IsTrue(movementState.ValueRO.reachedEndOfPart && state.pathTracer.isNextPartValidLink);
				if (!state.pathTracer.isNextPartValidLink) {
					// The ReadyToTraverseOffMeshLink component is set at the end of a frame by the RepairPathSystem.
					// In rare cases, the link may have been invalidated between then and now.
					// In that case, just skip this agent and let the RepairPathSystem add the component again later if needed.
					continue;
				}
				var linkInfo = NextLinkToTraverse(state);
				var ctx = new AgentOffMeshLinkTraversalContext(linkInfo.link);
				// Add the AgentOffMeshLinkTraversal component when the agent should start traversing an off-mesh link.
				AgentManagedStorage.SetLinkTraversal(slot, entity, new ManagedAgentOffMeshLinkTraversal(ctx, ResolveOffMeshLinkHandler(entry.settings, ctx)));
				commandBuffer.AddComponent(entity, new AgentOffMeshLinkTraversal(linkInfo));
				commandBuffer.AddComponent(entity, new AgentOffMeshLinkTraversalCleanup());
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
			// Both branches below run on the main thread and so need every dependency completed first. That
			// sync point, and the command buffer, are worth skipping in the common case where agents are
			// merely approaching links rather than traversing one.
			if (entityQueryOffMeshLinkTransition.IsEmptyIgnoreFilter && entityQueryOffMeshLinkCleanup.IsEmptyIgnoreFilter) return;

			var commandBuffer = new EntityCommandBuffer(systemState.WorldUpdateAllocator);
			systemState.CompleteDependency();

			if (!entityQueryOffMeshLinkTransition.IsEmptyIgnoreFilter) {
				offMeshLinkTransitionRunner.Run(ref systemState, entityQueryOffMeshLinkTransition, commandBuffer, AIMovementSystemGroup.TimeScaledRateManager.CheapStepDeltaTime);
			}

			if (!entityQueryOffMeshLinkCleanup.IsEmptyIgnoreFilter) {
				JobManagedOffMeshLinkTransitionCleanup.Run(entityQueryOffMeshLinkCleanup);
#if MODULE_ENTITIES_1_0_8_OR_NEWER
				commandBuffer.RemoveComponent<AgentOffMeshLinkTraversalCleanup>(entityQueryOffMeshLinkCleanup, EntityQueryCaptureMode.AtPlayback);
				commandBuffer.RemoveComponent<AgentOffMeshLinkMovementDisabled>(entityQueryOffMeshLinkCleanup, EntityQueryCaptureMode.AtPlayback);
				commandBuffer.RemoveComponent<AgentOffMeshLinkLocalAvoidanceDisabled>(entityQueryOffMeshLinkCleanup, EntityQueryCaptureMode.AtPlayback);
#else
				commandBuffer.RemoveComponent<AgentOffMeshLinkTraversalCleanup>(entityQueryOffMeshLinkCleanup);
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
