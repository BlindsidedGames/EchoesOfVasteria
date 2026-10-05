#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Mathematics;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using GCHandle = System.Runtime.InteropServices.GCHandle;

namespace Pathfinding.ECS.RVO {
	using Pathfinding.RVO;
	using Unity.Jobs;

	/// <summary>
	/// Simulates local avoidance in an ECS context.
	///
	/// All agent entities must have the following ECS components:
	/// - LocalTransform
	/// - <see cref="AgentCylinderShape"/>
	/// - <see cref="AgentMovementPlane"/>
	/// - <see cref="RVOAgent"/>
	/// - <see cref="MovementControl"/>: where you store how you want the agent to move
	/// - <see cref="ResolvedMovement"/>: where this system will output how the agent should move, when using RVO
	///
	/// The system will use the data from <see cref="MovementControl"/>, and output the following fields to <see cref="ResolvedMovement"/>:
	///
	/// <see cref="ResolvedMovement.targetPoint"/>: Where the agent should move to.
	/// <see cref="ResolvedMovement.speed"/>: At what speed the agent should move, in world units.
	/// <see cref="ResolvedMovement.turningRadiusMultiplier"/>: This will go up if its more crowded, to indicate that the agent should try to take wider turns to improve crowd flow.
	///
	/// The <see cref="AgentIndex"/> component will be added to the agent automatically by this system. You do not need to care about it.
	/// </summary>
	[BurstCompile]
	[UpdateAfter(typeof(FollowerControlSystem))]
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	public partial struct RVOSystem : ISystem {
		/// <summary>
		/// Actual infinity is not handled well by some algorithms, but very large values are ok.
		/// This should be larger than any reasonable value a user might want to use.
		/// </summary>
		const float VERY_LARGE = 100000;

		/// <summary>
		/// Keeps track of the last simulator that this RVOSystem saw.
		/// This is a weak GCHandle to allow it to be stored in an ISystem.
		/// </summary>
		GCHandle lastSimulator;

		/// <summary>
		/// Which slice of the agents refreshes its crowd density this simulation step.
		///
		/// See: <see cref="JobCopyFromRVOSimulatorToEntities.DensityUpdateInterval"/>
		/// </summary>
		uint densityPhase;

		public void OnCreate (ref SystemState state) {
			lastSimulator = GCHandle.Alloc(null, System.Runtime.InteropServices.GCHandleType.Weak);
		}

		public void OnDestroy (ref SystemState state) {
			lastSimulator.Free();
		}

		public void OnUpdate (ref SystemState systemState) {
			var simulator = RVOSimulator.active?.GetSimulator();

			if (simulator != lastSimulator.Target) {
				// If the simulator has been destroyed, we need to remove all AgentIndex components
				RemoveAllAgentsFromSimulation(ref systemState);
				lastSimulator.Target = simulator;
			}
			if (simulator == null) return;

			AddAndRemoveAgentsFromSimulation(ref systemState, simulator);

			// This runs on every update, even ones that skip the simulation below,
			// because its change filter is evaluated against SystemState.LastSystemVersion, which advances on
			// every update.
			CopyRVOSettingsToSimulator(ref systemState, simulator);

			// The full movement calculations do not necessarily need to be done every frame if the fps is high
			if (AIMovementSystemGroup.TimeScaledRateManager.CheapSimulationOnly) {
				return;
			}

			CopyFromEntitiesToRVOSimulator(ref systemState, simulator, SystemAPI.Time.DeltaTime);

			// Schedule RVO update
			simulator.Update(
				systemState.Dependency,
				SystemAPI.Time.DeltaTime,
				AIMovementSystemGroup.TimeScaledRateManager.IsLastSubstep,
				systemState.WorldUpdateAllocator
				);

			CopyFromRVOSimulatorToEntities(ref systemState, simulator);
		}

		void RemoveAllAgentsFromSimulation (ref SystemState systemState) {
			var buffer = new EntityCommandBuffer(Allocator.Temp);
			var entities = SystemAPI.QueryBuilder()
						   .WithAllRW<AgentIndex>()
						   .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
						   .Build()
						   .ToEntityArray(systemState.WorldUpdateAllocator);
			buffer.RemoveComponent<AgentIndex>(entities);
			buffer.Playback(systemState.EntityManager);
			buffer.Dispose();
		}

		void AddAndRemoveAgentsFromSimulation (ref SystemState systemState, SimulatorBurst simulator) {
			var shouldBeRemovedFromSimulation = SystemAPI.QueryBuilder()
												.WithAll<AgentIndex>()
												.WithNone<RVOAgent>()
												.WithOptions(EntityQueryOptions.IncludeDisabledEntities)
												.Build();

			var shouldBeRemovedFromSimulation2 = SystemAPI.QueryBuilder()
												 .WithAll<AgentIndex, AgentOffMeshLinkLocalAvoidanceDisabled>()
												 .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
												 .Build();

			var shouldBeAddedToSimulation = SystemAPI.QueryBuilder()
											.WithAll<RVOAgent>()
											.WithNone<AgentIndex, AgentOffMeshLinkLocalAvoidanceDisabled>()
											.Build();

			// Remove all agents from the simulation that do not have an RVOAgent component, but have an AgentIndex
			var indicesToRemove = shouldBeRemovedFromSimulation.ToComponentDataArray<AgentIndex>(systemState.WorldUpdateAllocator);
			var indicesToRemove2 = shouldBeRemovedFromSimulation2.ToComponentDataArray<AgentIndex>(systemState.WorldUpdateAllocator);
			// Add all agents to the simulation that have an RVOAgent component, but not AgentIndex component
			var entitiesToAdd = shouldBeAddedToSimulation.ToEntityArray(systemState.WorldUpdateAllocator);
			// Avoid a sync point in the common case
			if (indicesToRemove.Length > 0 || indicesToRemove2.Length > 0 || entitiesToAdd.Length > 0) {
				var buffer = new EntityCommandBuffer(Allocator.Temp);
#if MODULE_ENTITIES_1_0_8_OR_NEWER
				buffer.RemoveComponent<AgentIndex>(shouldBeRemovedFromSimulation, EntityQueryCaptureMode.AtPlayback);
				buffer.RemoveComponent<AgentIndex>(shouldBeRemovedFromSimulation2, EntityQueryCaptureMode.AtPlayback);
#else
				buffer.RemoveComponent<AgentIndex>(shouldBeRemovedFromSimulation);
				buffer.RemoveComponent<AgentIndex>(shouldBeRemovedFromSimulation2);
#endif
				for (int i = 0; i < indicesToRemove.Length; i++) {
					simulator.RemoveAgent(indicesToRemove[i]);
				}
				for (int i = 0; i < indicesToRemove2.Length; i++) {
					// Note: In very rare cases, we might have already removed the agent in the first loop.
					simulator.RemoveAgent(indicesToRemove2[i], true);
				}
				for (int i = 0; i < entitiesToAdd.Length; i++) {
					buffer.AddComponent<AgentIndex>(entitiesToAdd[i], simulator.AddAgentBurst(UnityEngine.Vector3.zero));
				}

				buffer.Playback(systemState.EntityManager);
				buffer.Dispose();
			}
		}

		void CopyRVOSettingsToSimulator (ref SystemState systemState, SimulatorBurst simulator) {
			var writeLock = simulator.LockSimulationDataReadWrite();
			systemState.Dependency = new JobCopyRVOSettingsToSimulator {
				agentData = simulator.simulationData,
			}.ScheduleParallel(JobHandle.CombineDependencies(writeLock.dependency, systemState.Dependency));
			writeLock.UnlockAfter(systemState.Dependency);
		}

		void CopyFromEntitiesToRVOSimulator (ref SystemState systemState, SimulatorBurst simulator, float dt) {
			var writeLock = simulator.LockSimulationDataReadWrite();
			systemState.Dependency = new JobCopyFromEntitiesToRVOSimulator {
				agentData = simulator.simulationData,
				agentOutputData = simulator.outputData,
				movementPlaneMode = simulator.movementPlane,
				dt = dt,
			}.ScheduleParallel(JobHandle.CombineDependencies(writeLock.dependency, systemState.Dependency));

			systemState.Dependency = new JobDisableLocalAvoidanceDuringLinkTraversal {
				agentDataVersions = simulator.simulationData.version,
				manuallyControlled = simulator.simulationData.manuallyControlled,
			}.ScheduleParallel(systemState.Dependency);
			writeLock.UnlockAfter(systemState.Dependency);
		}

		void CopyFromRVOSimulatorToEntities (ref SystemState systemState, SimulatorBurst simulator) {
			var writeLock = simulator.LockSimulationDataReadWrite();
			densityPhase++;
			systemState.Dependency = new JobCopyFromRVOSimulatorToEntities {
				quadtree = simulator.quadtree,
				agentDataVersions = simulator.simulationData.version,
				agentOutputData = simulator.outputData,
				densityPhase = densityPhase,
			}.ScheduleParallel(JobHandle.CombineDependencies(writeLock.dependency, systemState.Dependency));
			writeLock.UnlockAfter(systemState.Dependency);
		}

		[BurstCompile]
		public partial struct JobCopyFromEntitiesToRVOSimulator : IJobEntity {
			[NativeDisableParallelForRestriction]
			public SimulatorBurst.AgentData agentData;
			[ReadOnly]
			public SimulatorBurst.AgentOutputData agentOutputData;
			public MovementPlane movementPlaneMode;
			public float dt;

			public void Execute (in LocalTransform transform, in AgentCylinderShape shape, in AgentMovementPlane movementPlane, in AgentIndex agentIndex, in RVOAgent controller, in MovementControl target) {
				var scale = math.abs(transform.Scale);
				if (!agentIndex.TryGetIndex(ref agentData, out var index)) throw new System.InvalidOperationException("RVOAgent has an invalid entity index");

				// Copy all fields to the rvo simulator, and clamp them to reasonable values
				agentData.radius[index] = math.clamp(shape.radius * scale, 0.001f, VERY_LARGE);
				agentData.targetPoint[index] = target.targetPoint;
				agentData.desiredSpeed[index] = math.clamp(target.speed, 0, VERY_LARGE);
				agentData.maxSpeed[index] = math.clamp(target.maxSpeed, 0, VERY_LARGE);
				agentData.manuallyControlled[index] = target.overrideLocalAvoidance;
				agentData.endOfPath[index] = target.endOfPath;
				agentData.hierarchicalNodeIndex[index] = target.hierarchicalNodeIndex;
				agentData.movementPlane[index] = movementPlane.value;

				// Use the position from the movement script if one is attached
				// as the movement script's position may not be the same as the transform's position
				// (in particular if IAstarAI.updatePosition is false).
				if (movementPlaneMode == MovementPlane.XY) {
					// In 2D it is assumed the Z coordinate differences of agents is ignored.
					agentData.height[index] = 1;
					agentData.position[index] = movementPlane.value.ToWorld(movementPlane.value.ToPlane(transform.Position), 0);
				} else {
					agentData.height[index] = math.clamp(shape.height * scale, 0, VERY_LARGE);
					agentData.position[index] = transform.Position;
				}


				// TODO: Move this to a separate file
				var reached = agentOutputData.effectivelyReachedDestination[index];
				var prio = math.clamp(controller.priority * controller.priorityMultiplier, 0, VERY_LARGE);
				var flow = math.clamp(controller.flowFollowingStrength, 0, 1);
				// TODO: This is gettting overriden every frame, right?
				if (reached == ReachedEndOfPath.Reached) {
					// Override flow following strength and make it go towards 1
					flow = math.lerp(agentData.flowFollowingStrength[index], 1.0f, 6.0f * dt);
					prio *= 0.3f;
				} else if (reached == ReachedEndOfPath.ReachedSoon) {
					// Override flow following strength and make it go towards 1
					flow = math.lerp(agentData.flowFollowingStrength[index], 1.0f, 6.0f * dt);
					prio *= 0.45f;
				}
				agentData.priority[index] = prio;
				agentData.flowFollowingStrength[index] = flow;
			}
		}

		/// <summary>
		/// Copies the agent's local avoidance settings to the simulator.
		///
		/// The RVOAgent fields change rarely, so we can use a change filter to improve performance.
		/// We also key on AgentIndex, which changes if the agent is added/removed from the simulation.
		/// </summary>
		[BurstCompile]
		[WithChangeFilter(typeof(RVOAgent), typeof(AgentIndex))]
		public partial struct JobCopyRVOSettingsToSimulator : IJobEntity {
			[NativeDisableParallelForRestriction]
			public SimulatorBurst.AgentData agentData;

			public void Execute (in AgentIndex agentIndex, in RVOAgent controller) {
				if (!agentIndex.TryGetIndex(ref agentData, out var index)) throw new System.InvalidOperationException("RVOAgent has an invalid entity index");

				agentData.agentTimeHorizon[index] = math.clamp(controller.agentTimeHorizon, 0, VERY_LARGE);
				agentData.obstacleTimeHorizon[index] = math.clamp(controller.obstacleTimeHorizon, 0, VERY_LARGE);
				agentData.locked[index] = controller.locked;
				agentData.maxNeighbours[index] = math.max(controller.maxNeighbours, 0);
				agentData.debugFlags[index] = controller.debug;
				agentData.layer[index] = controller.layer;
				agentData.collidesWith[index] = controller.collidesWith;
			}
		}

		/// <summary>
		/// Stops agents from avoiding others while they traverse an off-mesh link.
		///
		/// Other agents may still avoid them.
		/// </summary>
		[BurstCompile]
		[WithAll(typeof(AgentOffMeshLinkTraversal))]
		public partial struct JobDisableLocalAvoidanceDuringLinkTraversal : IJobEntity {
			[ReadOnly]
			public NativeArray<AgentIndex> agentDataVersions;
			[NativeDisableParallelForRestriction]
			public NativeArray<bool> manuallyControlled;

			public void Execute (in AgentIndex agentIndex) {
				if (!agentIndex.TryGetIndex(ref agentDataVersions, out var index)) throw new System.InvalidOperationException("RVOAgent has an invalid entity index");

				manuallyControlled[index] = true;
			}
		}

		[BurstCompile]
		public partial struct JobCopyFromRVOSimulatorToEntities : IJobEntity {
			[ReadOnly]
			public NativeArray<AgentIndex> agentDataVersions;
			[ReadOnly]
			public RVOQuadtreeBurst quadtree;
			[ReadOnly]
			public SimulatorBurst.AgentOutputData agentOutputData;

			public uint densityPhase;

			/// <summary>See https://en.wikipedia.org/wiki/Circle_packing</summary>
			const float MaximumCirclePackingDensity = 0.9069f;

			/// <summary>
			/// How many simulation steps pass between an agent's crowd density updates.
			///
			/// Must be a power of two, since the agents are spread over the steps using a bitmask.
			/// </summary>
			const uint DensityUpdateInterval = 4;

			public void Execute (in LocalTransform transform, in AgentCylinderShape shape, in AgentIndex agentIndex, in RVOAgent controller, in MovementControl control, ref ResolvedMovement resolved) {
				if (!agentIndex.TryGetIndex(ref agentDataVersions, out var index)) return;

				resolved.targetPoint = agentOutputData.targetPoint[index];
				resolved.speed = agentOutputData.speed[index];

				// Stagger density checks over a few steps, since the data is slow-changing and relatively slow to calculate.
				// turningRadiusMultiplier starts at 0, and then we force a recalculation immediately.
				if (resolved.turningRadiusMultiplier < 1f || (((uint)index + densityPhase) & (DensityUpdateInterval - 1)) == 0) {
					var scale = math.abs(transform.Scale);
					var r = shape.radius * scale * 3f;
					var area = quadtree.QueryArea(transform.Position, r);

					// Calculate the agent density in a circle around the agent and compare it to optimal circle packing
					// This should be between 0 and 1, but if agents are overlapping it can be larger than 1, which is why we clamp it.
					var density = math.min(1.0f, area / (MaximumCirclePackingDensity * math.PI * r * r));

					var rnd = 1.0f; // (agentIndex.Index % 1024) / 1024f;
					resolved.turningRadiusMultiplier = math.max(1f, math.pow(density * 2.0f, 4.0f) * rnd);
				}
			}
		}
	}
}
#endif
