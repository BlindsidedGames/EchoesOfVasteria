#pragma warning disable 0282 // Allows the 'partial' keyword without warnings
#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using UnityEngine;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;

namespace Pathfinding.ECS {
	using Pathfinding;

	/// <summary>
	/// Advances the off-mesh link traversal of every agent that is currently traversing one.
	///
	/// Run through JobChunkInterface.RunByRefWithoutJobs, which iterates chunks on the calling thread
	/// without entering the job system. Link traversal runs the user's coroutine and state machine, which
	/// are documented to run on the main thread and routinely touch Transforms, GameObjects and
	/// UnityEngine.Random, which Unity refuses inside a job context. IJobEntity over unmanaged components
	/// would schedule a real job, so it cannot be used here.
	///
	/// That entry point also installs Unity's guard against structural changes during iteration, which is
	/// why the components a finished traversal drops go through <see cref="commandBuffer"/>. The guard is compiled out
	/// of release players, along with the rest of ENABLE_UNITY_COLLECTIONS_CHECKS.
	///
	/// The caller completes all dependencies first, so reading chunk memory here is safe.
	/// </summary>
	public struct JobManagedOffMeshLinkTransition : IJobChunk {
		EntityTypeHandle entityHandle;
		// See JobRepairPath.Scheduler.AgentManagedRefTypeHandleRW for why this is not read-only.
		ComponentTypeHandle<AgentManagedRef> managedRefHandle;
		ComponentTypeHandle<LocalTransform> localTransformHandle;
		ComponentTypeHandle<AgentMovementPlane> movementPlaneHandle;
		ComponentTypeHandle<MovementControl> movementControlHandle;
		ComponentTypeHandle<MovementSettings> movementSettingsHandle;
		ComponentTypeHandle<AgentOffMeshLinkTraversal> linkTraversalHandle;
		ComponentTypeHandle<AgentOffMeshLinkMovementDisabled> movementDisabledHandle;
		ComponentTypeHandle<AgentOffMeshLinkLocalAvoidanceDisabled> localAvoidanceDisabledHandle;
		EntityCommandBuffer commandBuffer;
		float deltaTime;

		public JobManagedOffMeshLinkTransition (ref SystemState state) {
			entityHandle = state.GetEntityTypeHandle();
			managedRefHandle = state.GetComponentTypeHandle<AgentManagedRef>(false);
			localTransformHandle = state.GetComponentTypeHandle<LocalTransform>(false);
			movementPlaneHandle = state.GetComponentTypeHandle<AgentMovementPlane>(false);
			movementControlHandle = state.GetComponentTypeHandle<MovementControl>(false);
			movementSettingsHandle = state.GetComponentTypeHandle<MovementSettings>(false);
			linkTraversalHandle = state.GetComponentTypeHandle<AgentOffMeshLinkTraversal>(false);
			movementDisabledHandle = state.GetComponentTypeHandle<AgentOffMeshLinkMovementDisabled>(false);
			localAvoidanceDisabledHandle = state.GetComponentTypeHandle<AgentOffMeshLinkLocalAvoidanceDisabled>(false);
			// Set per run, by #Run.
			commandBuffer = default;
			deltaTime = 0;
		}

		/// <summary>
		/// The query this must be run over. IgnoreComponentEnabledState is required so that it does not
		/// filter out agents whose AgentOffMeshLinkMovementDisabled is currently disabled.
		/// </summary>
		public static EntityQuery GetEntityQuery (ref SystemState state) {
			return state.GetEntityQuery(new EntityQueryDesc {
				All = new ComponentType[] {
					ComponentType.ReadWrite<AgentManagedRef>(),
					ComponentType.ReadOnly<AgentOffMeshLinkTraversalCleanup>(),
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<AgentMovementPlane>(),
					ComponentType.ReadWrite<MovementControl>(),
					ComponentType.ReadWrite<MovementSettings>(),
					ComponentType.ReadWrite<AgentOffMeshLinkTraversal>(),
					ComponentType.ReadWrite<AgentOffMeshLinkMovementDisabled>(),
					ComponentType.ReadWrite<AgentOffMeshLinkLocalAvoidanceDisabled>(),
				},
				Options = EntityQueryOptions.IgnoreComponentEnabledState,
			});
		}

		/// <summary>
		/// Advances every matched agent's link traversal by one step.
		///
		/// An agent whose traversal has finished has its link components removed through commandBuffer,
		/// since a structural change here would invalidate the chunk this is iterating.
		/// </summary>
		public void Run (ref SystemState state, EntityQuery query, EntityCommandBuffer commandBuffer, float deltaTime) {
			entityHandle.Update(ref state);
			managedRefHandle.Update(ref state);
			localTransformHandle.Update(ref state);
			movementPlaneHandle.Update(ref state);
			movementControlHandle.Update(ref state);
			movementSettingsHandle.Update(ref state);
			linkTraversalHandle.Update(ref state);
			movementDisabledHandle.Update(ref state);
			localAvoidanceDisabledHandle.Update(ref state);
			this.commandBuffer = commandBuffer;
			this.deltaTime = deltaTime;
			Unity.Entities.Internal.InternalCompilerInterface.JobChunkInterface.RunByRefWithoutJobs(ref this, query);
		}

		/// <summary>Called once per chunk by RunByRefWithoutJobs.</summary>
		public unsafe void Execute (in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
			var managedData = AgentManagedStorage.entries;
			var entities = (Entity*)chunk.GetNativeArray(entityHandle).GetUnsafeReadOnlyPtr();
			var managedRefs = (AgentManagedRef*)chunk.GetNativeArray(ref managedRefHandle).GetUnsafeReadOnlyPtr();
			var transforms = (LocalTransform*)chunk.GetNativeArray(ref localTransformHandle).GetUnsafePtr();
			var movementPlanes = (AgentMovementPlane*)chunk.GetNativeArray(ref movementPlaneHandle).GetUnsafePtr();
			var movementControls = (MovementControl*)chunk.GetNativeArray(ref movementControlHandle).GetUnsafePtr();
			var movementSettings = (MovementSettings*)chunk.GetNativeArray(ref movementSettingsHandle).GetUnsafePtr();
			var linkInfos = (AgentOffMeshLinkTraversal*)chunk.GetNativeArray(ref linkTraversalHandle).GetUnsafePtr();
			var movementDisabledMask = chunk.GetEnabledMask(ref movementDisabledHandle);
			var localAvoidanceDisabledMask = chunk.GetEnabledMask(ref localAvoidanceDisabledHandle);

			// A plain index loop rather than ChunkEntityEnumerator, for the reason given on
			// \reflink{MovementOverrideRunner.Execute}.
			// Contract: #GetEntityQuery sets IgnoreComponentEnabledState, which zeroes the query's
			// HasEnableableComponents, so Unity never asks for a mask.
			if (useEnabledMask) throw new System.InvalidOperationException("This query must keep EntityQueryOptions.IgnoreComponentEnabledState, or this loop must start honouring chunkEnabledMask.");

			for (int i = 0; i < chunk.Count; i++) {
				var entity = entities[i];
				var slot = managedRefs[i].slot;
				ref readonly var entry = ref managedData[slot];
				if (!MoveNext(entity, entry.state, ref transforms[i], ref movementPlanes[i], ref movementControls[i], ref movementSettings[i], ref linkInfos[i], entry.linkTraversal,
					movementDisabledMask.GetEnabledRefRW<AgentOffMeshLinkMovementDisabled>(i),
					localAvoidanceDisabledMask.GetEnabledRefRW<AgentOffMeshLinkLocalAvoidanceDisabled>(i), deltaTime)) {
					AgentManagedStorage.SetLinkTraversal(slot, entity, null);
					commandBuffer.RemoveComponent<AgentOffMeshLinkTraversal>(entity);
					commandBuffer.RemoveComponent<AgentOffMeshLinkTraversalCleanup>(entity);
					commandBuffer.RemoveComponent<AgentOffMeshLinkMovementDisabled>(entity);
					commandBuffer.RemoveComponent<AgentOffMeshLinkLocalAvoidanceDisabled>(entity);
				}
			}
		}

		public static bool MoveNext (Entity entity, ManagedState state, ref LocalTransform transform, ref AgentMovementPlane movementPlane, ref MovementControl movementControl, ref MovementSettings movementSettings, ref AgentOffMeshLinkTraversal linkInfo, ManagedAgentOffMeshLinkTraversal managedLinkInfo, EnabledRefRW<AgentOffMeshLinkMovementDisabled> movementDisabled, EnabledRefRW<AgentOffMeshLinkLocalAvoidanceDisabled> localAvoidanceDisabled, float deltaTime) {
			unsafe {
				managedLinkInfo.context.SetInternalData(entity, ref transform, ref movementPlane, ref movementControl, ref movementSettings, ref linkInfo, movementDisabled, localAvoidanceDisabled, state, deltaTime);
			}

			// Initialize the coroutine during the first step.
			// This can also happen if the entity is duplicated, since the coroutine cannot be cloned.
			if (managedLinkInfo.coroutine == null) {
				// If we are calculating a path right now, cancel that path calculation.
				// We don't want to calculate a path while we are traversing an off-mesh link.
				state.CancelCurrentPathRequest();

				if (managedLinkInfo.stateMachine == null) {
					managedLinkInfo.stateMachine = managedLinkInfo.handler != null? managedLinkInfo.handler.GetOffMeshLinkStateMachine(managedLinkInfo.context) : null;
				}
				managedLinkInfo.coroutine = managedLinkInfo.stateMachine != null? managedLinkInfo.stateMachine.OnTraverseOffMeshLink(managedLinkInfo.context).GetEnumerator() : JobStartOffMeshLinkTransition.DefaultOnTraverseOffMeshLink(managedLinkInfo.context).GetEnumerator();

				// Don't disable local avoidance during off-mesh links by default. The link traversal code can do that itself if it wants to.
				if (localAvoidanceDisabled.IsValid) localAvoidanceDisabled.ValueRW = false;
			}

			bool finished;
			bool error = false;
			bool popParts = true;

			// Disable the agent's normal movement logic while traversing the off-mesh link
			// This can be re-enabled by the state machine if it wants to, but it needs to do it every tick.
			// It is enabled automatically by the AgentOffMeshLinkTraversal.MoveTowards method.
			// The reference could be invalid when called from the project dawn navigation package
			if (movementDisabled.IsValid) movementDisabled.ValueRW = true;

			try {
				finished = !managedLinkInfo.coroutine.MoveNext();
			} catch (AgentOffMeshLinkTraversalContext.AbortOffMeshLinkTraversal) {
				error = true;
				finished = true;
				popParts = false;
			}
			catch (System.Exception e) {
				Debug.LogException(e, managedLinkInfo.context.gameObject);
				// Teleport the agent to the end of the link as a fallback, if there's an exception
				managedLinkInfo.context.Teleport(managedLinkInfo.context.link.relativeEnd);
				finished = true;
				error = true;
			}

			if (finished) {
				try {
					if (managedLinkInfo.stateMachine != null) {
						if (error) managedLinkInfo.stateMachine.OnAbortTraversingOffMeshLink();
						else managedLinkInfo.stateMachine.OnFinishTraversingOffMeshLink(managedLinkInfo.context);
					}
				} catch (System.Exception e) {
					// If an exception happens when exiting the state machine, log it, and then continue with the cleanup
					Debug.LogException(e, managedLinkInfo.context.gameObject);
				}

				managedLinkInfo.context.Restore();
				if (popParts) {
					// Pop the part leading up to the link, and the link itself
					state.PopNextLinkFromPath();
				}
			}
			return !finished;
		}
	}

	/// <summary>
	/// Tells the state machine of an agent that died mid-traversal that its link traversal was aborted.
	///
	/// Runs on the main thread for the same reason as <see cref="JobManagedOffMeshLinkTransition"/>.
	/// </summary>
	public struct JobManagedOffMeshLinkTransitionCleanup {
		// The agent has been destroyed, so its ordinary components are gone. AgentManagedRef is a cleanup
		// component and outlives them, which is what still makes the storage slot reachable here.
		public static void Run (EntityQuery query) {
			var entities = query.ToEntityArray(Allocator.Temp);
			var data = query.ToComponentDataArray<AgentManagedRef>(Allocator.Temp);
			for (int i = 0; i < entities.Length; i++) {
				var linkTraversal = AgentManagedStorage.entries[data[i].slot].linkTraversal;
				if (linkTraversal == null) continue;
				// The state machine may be null if the default off-mesh link logic is used, or if the entity is destroyed on the first frame
				// that it starts to traverse an off-mesh link.
				if (linkTraversal.stateMachine != null) linkTraversal.stateMachine.OnAbortTraversingOffMeshLink();
				AgentManagedStorage.SetLinkTraversal(data[i].slot, entities[i], null);
			}
		}
	}
}
#endif
