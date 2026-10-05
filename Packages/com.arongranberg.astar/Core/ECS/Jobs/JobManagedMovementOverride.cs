#if MODULE_ENTITIES
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace Pathfinding.ECS {
	/// <summary>
	/// Invokes the user's movement override callbacks.
	///
	/// Run through JobChunkInterface.RunByRefWithoutJobs, which iterates chunks on the calling thread
	/// without entering the job system. Movement override callbacks are documented to run on the main
	/// thread, and routinely touch Transforms, GameObjects and UnityEngine.Random, which Unity refuses
	/// inside a job context. IJobEntity over unmanaged components would schedule a real job, so it cannot
	/// be used here.
	///
	/// That entry point also installs Unity's guard against structural changes during iteration, so a
	/// callback which adds or removes a component gets an exception naming the problem. The guard is
	/// compiled out of release players, along with the rest of ENABLE_UNITY_COLLECTIONS_CHECKS.
	///
	/// The callers complete all dependencies first, so reading chunk memory here is safe.
	///
	/// See: <see cref="ManagedMovementOverrides"/>
	/// </summary>
	struct MovementOverrideRunner : IJobChunk {
		EntityTypeHandle entityHandle;
		// See JobRepairPath.Scheduler.AgentManagedRefTypeHandleRW for why this is not read-only.
		ComponentTypeHandle<AgentManagedRef> managedRefHandle;
		ComponentTypeHandle<LocalTransform> localTransformHandle;
		ComponentTypeHandle<AgentCylinderShape> shapeHandle;
		ComponentTypeHandle<AgentMovementPlane> movementPlaneHandle;
		ComponentTypeHandle<DestinationPoint> destinationHandle;
		ComponentTypeHandle<MovementState> movementStateHandle;
		ComponentTypeHandle<MovementSettings> movementSettingsHandle;
		ComponentTypeHandle<MovementControl> movementControlHandle;
		ComponentTypeHandle<ResolvedMovement> resolvedMovementHandle;
		Phase phase;
		float dt;

		public MovementOverrideRunner (ref SystemState state) {
			entityHandle = state.GetEntityTypeHandle();
			managedRefHandle = state.GetComponentTypeHandle<AgentManagedRef>(false);
			localTransformHandle = state.GetComponentTypeHandle<LocalTransform>(false);
			shapeHandle = state.GetComponentTypeHandle<AgentCylinderShape>(false);
			movementPlaneHandle = state.GetComponentTypeHandle<AgentMovementPlane>(false);
			destinationHandle = state.GetComponentTypeHandle<DestinationPoint>(false);
			movementStateHandle = state.GetComponentTypeHandle<MovementState>(false);
			movementSettingsHandle = state.GetComponentTypeHandle<MovementSettings>(false);
			movementControlHandle = state.GetComponentTypeHandle<MovementControl>(false);
			resolvedMovementHandle = state.GetComponentTypeHandle<ResolvedMovement>(false);
			// Set per run, by #Run.
			phase = default;
			dt = 0;
		}

		void Update (ref SystemState state) {
			entityHandle.Update(ref state);
			managedRefHandle.Update(ref state);
			localTransformHandle.Update(ref state);
			shapeHandle.Update(ref state);
			movementPlaneHandle.Update(ref state);
			destinationHandle.Update(ref state);
			movementStateHandle.Update(ref state);
			movementSettingsHandle.Update(ref state);
			movementControlHandle.Update(ref state);
			resolvedMovementHandle.Update(ref state);
		}

		/// <summary>Which point in the movement pipeline to invoke callbacks for</summary>
		public enum Phase {
			BeforeControl,
			AfterControl,
			BeforeMovement,
		}

		/// <summary>
		/// Invokes every registered callback for phase on the agents matched by query.
		///
		/// Contract: the caller has applied <see cref="PendingMovementOverrideChanges"/> first. An agent matched
		/// here then holds the callback its marker component claims it holds, so the delegates below are
		/// invoked without a null check.
		///
		/// Destroying an agent from inside a callback is unsupported however it is done, being a structural
		/// change while the pointers taken below are live, which leaves this loop reading a chunk whose
		/// contents have moved. <see cref="FollowerEntityProxy.Destroy"/> also clears the agent's entry on the
		/// spot. Destroy an agent once the movement pipeline has run, or through GameObject.Destroy, which
		/// Unity defers to the end of the frame.
		///
		/// Each phase is its own loop, so the branch on phase is taken once per chunk instead of once per
		/// agent, and a phase only takes the chunk pointers it passes to its callback.
		/// </summary>
		public void Run (ref SystemState state, EntityQuery query, Phase phase, float dt) {
			Update(ref state);
			this.phase = phase;
			this.dt = dt;
			Unity.Entities.Internal.InternalCompilerInterface.JobChunkInterface.RunByRefWithoutJobs(ref this, query);
		}

		/// <summary>\copydocref{Run} Called once per chunk by RunByRefWithoutJobs.</summary>
		public unsafe void Execute (in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
			var managedData = AgentManagedStorage.entries;
			var entities = (Entity*)chunk.GetNativeArray(entityHandle).GetUnsafeReadOnlyPtr();
			var managedRefs = (AgentManagedRef*)chunk.GetNativeArray(ref managedRefHandle).GetUnsafeReadOnlyPtr();
			var localTransforms = (LocalTransform*)chunk.GetNativeArray(ref localTransformHandle).GetUnsafePtr();
			var shapes = (AgentCylinderShape*)chunk.GetNativeArray(ref shapeHandle).GetUnsafePtr();
			var movementPlanes = (AgentMovementPlane*)chunk.GetNativeArray(ref movementPlaneHandle).GetUnsafePtr();
			var destinations = (DestinationPoint*)chunk.GetNativeArray(ref destinationHandle).GetUnsafePtr();
			var movementStates = (MovementState*)chunk.GetNativeArray(ref movementStateHandle).GetUnsafePtr();
			var movementSettings = (MovementSettings*)chunk.GetNativeArray(ref movementSettingsHandle).GetUnsafePtr();

			// A plain index loop rather than ChunkEntityEnumerator. These loops invoke managed delegates, so
			// they are not Burst compiled, and outside Burst the enumerator's per-entity tzcnt costs about
			// 8ns per agent.
			// Contract: none of the three queries contains an IEnableableComponent, so Unity never asks for a
			// mask. Adding one without honouring it here would silently run callbacks for disabled agents.
			if (useEnabledMask) throw new System.InvalidOperationException("A movement override query gained an enableable component. This loop must start honouring chunkEnabledMask.");

			switch (phase) {
			case Phase.BeforeControl: {
				for (int i = 0; i < chunk.Count; i++) {
					var callback = managedData[managedRefs[i].slot].beforeControl;
					callback(entities[i], dt, ref localTransforms[i], ref shapes[i], ref movementPlanes[i], ref destinations[i], ref movementStates[i], ref movementSettings[i]);
					InvalidateMovementState(ref movementStates[i]);
				}
				break;
			}
			case Phase.AfterControl: {
				var movementControls = (MovementControl*)chunk.GetNativeArray(ref movementControlHandle).GetUnsafePtr();
				for (int i = 0; i < chunk.Count; i++) {
					var callback = managedData[managedRefs[i].slot].afterControl;
					callback(entities[i], dt, ref localTransforms[i], ref shapes[i], ref movementPlanes[i], ref destinations[i], ref movementStates[i], ref movementSettings[i], ref movementControls[i]);
					InvalidateMovementState(ref movementStates[i]);
				}
				break;
			}
			case Phase.BeforeMovement: {
				var movementControls = (MovementControl*)chunk.GetNativeArray(ref movementControlHandle).GetUnsafePtr();
				var resolvedMovements = (ResolvedMovement*)chunk.GetNativeArray(ref resolvedMovementHandle).GetUnsafePtr();
				for (int i = 0; i < chunk.Count; i++) {
					var callback = managedData[managedRefs[i].slot].beforeMovement;
					callback(entities[i], dt, ref localTransforms[i], ref shapes[i], ref movementPlanes[i], ref destinations[i], ref movementStates[i], ref movementSettings[i], ref movementControls[i], ref resolvedMovements[i]);
					InvalidateMovementState(ref movementStates[i]);
				}
				break;
			}
			}
		}

		/// <summary>
		/// Tells the repair job that the movement state may no longer match the path tracer.
		///
		/// A callback is free to have moved the agent. Without this the repair job would optimise away the
		/// update that recomputes the agent's corners.
		/// </summary>
		static void InvalidateMovementState (ref MovementState movementState) {
			movementState.pathTracerVersion--;
		}
	}
}
#endif
