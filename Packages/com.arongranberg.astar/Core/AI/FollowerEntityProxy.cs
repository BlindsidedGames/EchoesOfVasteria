#if MODULE_ENTITIES
using Pathfinding;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Transforms;
using Pathfinding.RVO;
using Pathfinding.ECS.RVO;
using Pathfinding.Util;
using Pathfinding.PID;
using System.Collections.Generic;
using Pathfinding.Collections;
using UnityEngine.Assertions;

namespace Pathfinding.ECS {
	/// <summary>
	/// Proxy for a FollowerEntity using pure ECS components.
	///
	/// This struct behaves almost identically to the <see cref="FollowerEntity"/> MonoBehaviour component, but it wraps an Entity in a given world instead.
	/// It is used primarily when you are using subscenes, and therefore cannot use the <see cref="FollowerEntity"/> component at runtime.
	///
	/// This struct gives you the same high-level API as the <see cref="FollowerEntity"/> component, but without the overhead of dealing with GameObjects and MonoBehaviours.
	///
	/// Warning: This struct can only be used on the main thread outside of ECS jobs. When accessing ECS components from within jobs, you must use the ECS api directly.
	///
	/// \section followerentityproxy-usage Usage
	///
	/// -# Create a subscene (see unity entities documentation).
	/// -# Create an agent GameObject with a <see cref="FollowerEntity"/> component within that subscene.
	/// -# At runtime, the GameObject will be automatically converted to an entity.
	/// -# You can then use the <see cref="FollowerEntityProxy"/> struct to interact with the entity.
	///    <code>
	/// var follower = new FollowerEntityProxy(world, entity);
	/// follower.maxSpeed = 5;
	/// follower.destination = new Vector3(1, 2, 3);
	///
	/// if (follower.currentNode.Tag == 1) {
	///     Debug.Log("The agent is right now traversing a node with tag 1");
	/// }
	/// </code>
	///
	/// See: <see cref="FollowerEntity"/>
	/// </summary>
	public struct FollowerEntityProxy : IAstarAI {
		public Entity entity { [IgnoredByDeepProfiler] get; private set; }
		public World world { [IgnoredByDeepProfiler] get; private set; }

		class EntityDoesNotExistException : System.Exception {
			public EntityDoesNotExistException() : base("The entity does not exist. This can happen if the component is not enabled, or if the game is not running.") { }
		}

		static NativeList<float3> nextCornersScratch;
		static NativeArray<int> indicesScratch;

		internal static EntityAccess<DestinationPoint> destinationPointAccessRW = new EntityAccess<DestinationPoint>(false);
		internal static EntityAccess<DestinationPoint> destinationPointAccessRO = new EntityAccess<DestinationPoint>(true);
		internal static EntityAccess<AgentMovementPlane> movementPlaneAccessRW = new EntityAccess<AgentMovementPlane>(false);
		internal static EntityAccess<AgentMovementPlane> movementPlaneAccessRO = new EntityAccess<AgentMovementPlane>(true);
		internal static EntityAccess<MovementState> movementStateAccessRW = new EntityAccess<MovementState>(false);
		internal static EntityAccess<MovementState> movementStateAccessRO = new EntityAccess<MovementState>(true);
		internal static EntityAccess<MovementStatistics> movementOutputAccessRW = new EntityAccess<MovementStatistics>(false);
		internal static EntityAccess<ResolvedMovement> resolvedMovementAccessRO = new EntityAccess<ResolvedMovement>(true);
		internal static EntityAccess<ResolvedMovement> resolvedMovementAccessRW = new EntityAccess<ResolvedMovement>(false);
		internal static EntityAccess<MovementControl> movementControlAccessRO = new EntityAccess<MovementControl>(true);
		internal static EntityAccess<MovementControl> movementControlAccessRW = new EntityAccess<MovementControl>(false);
		internal static EntityAccess<MovementStatistics> movementStatisticsAccessRW = new EntityAccess<MovementStatistics>(false);
		internal static EntityAccess<MovementStatistics> movementStatisticsAccessRO = new EntityAccess<MovementStatistics>(true);
		internal static ManagedEntityAccess<ManagedState> managedStateAccessRO = new ManagedEntityAccess<ManagedState>(true);
		internal static ManagedEntityAccess<ManagedState> managedStateAccessRW = new ManagedEntityAccess<ManagedState>(false);
		internal static ManagedEntityAccess<ManagedSettings> managedSettingsAccessRO = new ManagedEntityAccess<ManagedSettings>(true);
		internal static ManagedEntityAccess<ManagedSettings> managedSettingsAccessRW = new ManagedEntityAccess<ManagedSettings>(false);
		internal static EntityAccess<ECS.AutoRepathPolicy> autoRepathPolicyRW = new EntityAccess<ECS.AutoRepathPolicy>(false);
		internal static EntityAccess<LocalTransform> localTransformAccessRO = new EntityAccess<LocalTransform>(true);
		internal static EntityAccess<LocalTransform> localTransformAccessRW = new EntityAccess<LocalTransform>(false);
		internal static EntityAccess<AgentCylinderShape> agentCylinderShapeAccessRO = new EntityAccess<AgentCylinderShape>(true);
		internal static EntityAccess<AgentCylinderShape> agentCylinderShapeAccessRW = new EntityAccess<AgentCylinderShape>(false);
		internal static EntityAccess<MovementSettings> movementSettingsAccessRO = new EntityAccess<MovementSettings>(true);
		internal static EntityAccess<MovementSettings> movementSettingsAccessRW = new EntityAccess<MovementSettings>(false);
		internal static EntityAccess<AgentOffMeshLinkTraversal> agentOffMeshLinkTraversalRO = new EntityAccess<AgentOffMeshLinkTraversal>(true);
		internal static EntityAccess<ReadyToTraverseOffMeshLink> readyToTraverseOffMeshLinkRW = new EntityAccess<ReadyToTraverseOffMeshLink>(false);
		internal static EntityAccess<RVOAgent> rvoSettingsAccessRO = new EntityAccess<RVOAgent>(true);
		internal static EntityAccess<RVOAgent> rvoSettingsAccessRW = new EntityAccess<RVOAgent>(false);

		internal static EntityStorageCache entityStorageCache;

		static void InitScratchData ( ) {
			if (!nextCornersScratch.IsCreated) {
				if (!Application.isPlaying) throw new System.InvalidOperationException("Cannot initialize scratch data outside of play mode.");
				nextCornersScratch = new NativeList<float3>(16, Allocator.Persistent);
				indicesScratch = new NativeArray<int>(16, Allocator.Persistent);
				Application.quitting += DisposeScratchData;
			}
		}

		static void DisposeScratchData () {
			if (nextCornersScratch.IsCreated) nextCornersScratch.Dispose();
			if (indicesScratch.IsCreated) indicesScratch.Dispose();
			Application.quitting -= DisposeScratchData;
		}

		public FollowerEntityProxy (World world, Entity entity) {
			this.world = world;
			this.entity = entity;
		}

		/// <summary>
		/// True if the <see cref="entity"/> exists.
		///
		/// This is typically true if the <see cref="FollowerEntity"/> component is active and enabled and the game is running.
		///
		/// See: <see cref="entity"/>
		/// </summary>
		public bool entityExists => world != null && world.IsCreated && world.EntityManager.Exists(entity);

		/// <summary>
		/// True if the entity seems to have the required components.
		///
		/// This is used to detect entities which still exist, but which have had all their FollowerEntity components removed.
		/// </summary>
		internal bool likelyHasReasonableComponents => entityExists && world.EntityManager.HasComponent<MovementState>(entity) && world.EntityManager.HasComponent<MovementSettings>(entity);

		void AssertEntityExists () {
			if (world == null || !world.IsCreated || !world.EntityManager.Exists(entity)) throw new System.InvalidOperationException("Entity does not exist. You can only access this if the component is active and enabled.");
		}

		/// <summary>\copydocref{FollowerEntity.radius}</summary>
		public float radius {
			get => entityStorageCache.GetComponentData(world, entity, ref agentCylinderShapeAccessRW, out var shape) ? shape.value.radius : 0;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref agentCylinderShapeAccessRW, out var shape)) {
					shape.value.radius = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.height}</summary>
		public float height {
			get => entityStorageCache.GetComponentData(world, entity, ref agentCylinderShapeAccessRO, out var shape) ? shape.value.height : 0;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref agentCylinderShapeAccessRW, out var shape)) {
					shape.value.height = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.pathfindingSettings}</summary>
		public ref PathRequestSettings pathfindingSettings {
			get {
				// Complete any job dependencies
				// Need RW because this getter has a ref return.
				if (entityStorageCache.GetComponentData(world, entity, ref managedSettingsAccessRW, out var managedSettings)) {
					return ref managedSettings.pathfindingSettings;
				} else {
					throw new EntityDoesNotExistException();
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.rvoSettings}</summary>
		public RVOAgent rvoSettings {
			get {
				// Note: Cannot use GetComponentData, because it assumes that the component exists on the entity, but the RVOAgent component is optional
				if (entityStorageCache.Update(world, entity, out var entityManager, out var storage)) {
					rvoSettingsAccessRO.Update(entityManager);
					if (rvoSettingsAccessRO.HasComponent(storage)) {
						return rvoSettingsAccessRO[storage];
					}
				}
				throw new System.Exception("Local avoidance is not enabled for the entity, cannot get local avoidance settings. The RVOAgent component is missing on the entity.");
			}
			set {
				if (entityStorageCache.Update(world, entity, out var entityManager, out var storage)) {
					rvoSettingsAccessRW.Update(entityManager);
					if (rvoSettingsAccessRW.HasComponent(storage)) {
						rvoSettingsAccessRW[storage] = value;
					}
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.position}</summary>
		public Vector3 position {
			// Make sure we are not waiting for a job to update the world position
			get => entityStorageCache.GetComponentData(world, entity, ref localTransformAccessRO, out var localTransform) ? localTransform.value.Position : default;
			set {
				if (entityStorageCache.Update(World.DefaultGameObjectInjectionWorld, entity, out var entityManager, out var storage)) {
					movementStateAccessRW.Update(entityManager);
					managedStateAccessRW.Update(entityManager);
					agentCylinderShapeAccessRO.Update(entityManager);
					movementSettingsAccessRO.Update(entityManager);
					destinationPointAccessRO.Update(entityManager);
					movementPlaneAccessRO.Update(entityManager);
					localTransformAccessRW.Update(entityManager);
					readyToTraverseOffMeshLinkRW.Update(entityManager);
					autoRepathPolicyRW.Update(entityManager);

					ref var localTransform = ref localTransformAccessRW[storage];
					localTransform.Position = value;
					ref var movementState = ref movementStateAccessRW[storage];
					movementState.positionOffset = float3.zero;
					var managedState = managedStateAccessRW[storage];
					if (managedState.pathTracer.hasPath) {
						Profiler.BeginSample("RepairStart");
						ref var movementPlane = ref movementPlaneAccessRO[storage];
						var oldVersion = managedState.pathTracer.version;
						managedState.pathTracer.UpdateStart(value, PathTracer.RepairQuality.High, movementPlane.value);
						Profiler.EndSample();
						if (managedState.pathTracer.version != oldVersion) {
							Profiler.BeginSample("EstimateNative");
							ref var shape = ref agentCylinderShapeAccessRO[storage];
							ref var movementSettings = ref movementSettingsAccessRO[storage];
							ref var destinationPoint = ref destinationPointAccessRO[storage];
							ref var autoRepath = ref autoRepathPolicyRW[storage];
							var readyToTraverseOffMeshLink = storage.Chunk.GetEnabledMask(ref readyToTraverseOffMeshLinkRW.handle).GetEnabledRefRW<ReadyToTraverseOffMeshLink>(storage.IndexInChunk);
							InitScratchData();
							JobRepairPath.Execute(
								ref localTransform,
								ref movementState,
								ref shape,
								ref movementPlane,
								ref autoRepath,
								ref destinationPoint,
								readyToTraverseOffMeshLink,
								managedState,
								in movementSettings,
								nextCornersScratch,
								ref indicesScratch,
								Allocator.Persistent,
								false
								);
							Profiler.EndSample();
						}
					}
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.isTraversingOffMeshLink}</summary>
		public bool isTraversingOffMeshLink {
			get => entityExists && world.EntityManager.HasComponent<AgentOffMeshLinkTraversal>(entity);
		}


		/// <summary>\copydocref{FollowerEntity.offMeshLink}</summary>
		public OffMeshLinks.OffMeshLinkTracer offMeshLink {
			get {
				if (entityStorageCache.Update(World.DefaultGameObjectInjectionWorld, entity, out var entityManager, out var storage) && entityManager.HasComponent<AgentOffMeshLinkTraversal>(entity)) {
					agentOffMeshLinkTraversalRO.Update(entityManager);
					var linkTraversal = agentOffMeshLinkTraversalRO[storage];
					var linkTraversalManaged = entityManager.GetComponentData<ManagedAgentOffMeshLinkTraversal>(entity);
					return new OffMeshLinks.OffMeshLinkTracer(linkTraversalManaged.context.concreteLink, linkTraversal.relativeStart, linkTraversal.relativeEnd, linkTraversal.isReverse);
				} else {
					return default;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.onTraverseOffMeshLink}</summary>
		public IOffMeshLinkHandler onTraverseOffMeshLink {
			get => entityStorageCache.GetComponentData(world, entity, ref managedSettingsAccessRO, out var managedSettings) ? managedSettings.onTraverseOffMeshLink : null;
			set {
				// Complete any job dependencies
				if (entityStorageCache.GetComponentData(world, entity, ref managedSettingsAccessRO, out var managedSettings)) {
					managedSettings.onTraverseOffMeshLink = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.currentNode}</summary>
		public GraphNode currentNode {
			get {
				if (entityStorageCache.GetComponentData(world, entity, ref managedStateAccessRO, out var managedState)) {
					var node = managedState.pathTracer.startNode;
					if (node == null || node.Destroyed) return null;
					return node;
				} else {
					return null;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.nearestNavmeshBorder}</summary>
		public GraphHitInfo nearestNavmeshBorder {
			get {
				if (AstarPath.active != null) {
					AstarPath.active.GetNearestBorder(position, currentNode, out var hit);
					return hit;
				} else {
					GraphHitInfo hit = default;
					hit.origin = position;
					hit.point = Vector3.positiveInfinity;
					return hit;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.rotation}</summary>
		public Quaternion rotation {
			get => entityStorageCache.GetComponentData(world, entity, ref localTransformAccessRO, out var localTransform) ? localTransform.value.Rotation : Quaternion.identity;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref localTransformAccessRW, out var localTransform)) {
					localTransform.value.Rotation = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.movementPlaneSource}</summary>
		public MovementPlaneSource movementPlaneSource {
			get => entityExists ? world.EntityManager.GetSharedComponent<AgentMovementPlaneSource>(entity).value : default;
			set {
				if (entityExists) {
					world.EntityManager.SetSharedComponent(entity, new AgentMovementPlaneSource { value = value });
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.groundMask}</summary>
		public LayerMask groundMask {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.groundMask : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.groundMask = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.debugFlags}</summary>
		public PIDMovement.DebugFlags debugFlags {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.debugFlags : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.debugFlags = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.maxSpeed}</summary>
		public float maxSpeed {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.follower.speed : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.follower.speed = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.rotationSpeed}</summary>
		public float rotationSpeed {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.follower.rotationSpeed : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.follower.rotationSpeed = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.maxRotationSpeed}</summary>
		public float maxRotationSpeed {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.follower.maxRotationSpeed : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.follower.maxRotationSpeed = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.velocity}</summary>
		public Vector3 velocity {
			get {
				return entityStorageCache.GetComponentData(world, entity, ref movementStatisticsAccessRO, out var statistics) ? (Vector3)statistics.value.estimatedVelocity : Vector3.zero;
			}
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementStatisticsAccessRW, out var statistics)) {
					statistics.value.estimatedVelocity = (float3)value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.desiredVelocity}</summary>
		public Vector3 desiredVelocity {
			get {
				if (entityStorageCache.GetComponentData(world, entity, ref resolvedMovementAccessRO, out var resolvedMovement)) {
					var dt = Mathf.Max(Time.deltaTime, 0.0001f);
					return Vector3.ClampMagnitude((Vector3)resolvedMovement.value.targetPoint - position, dt * resolvedMovement.value.speed) / dt;
				} else {
					return Vector3.zero;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.desiredVelocityWithoutLocalAvoidance}</summary>
		public Vector3 desiredVelocityWithoutLocalAvoidance {
			get {
				if (entityStorageCache.GetComponentData(world, entity, ref movementControlAccessRO, out var movementControl)) {
					var dt = Mathf.Max(Time.deltaTime, 0.0001f);
					return Vector3.ClampMagnitude((Vector3)movementControl.value.targetPoint - position, dt * movementControl.value.speed) / dt;
				} else {
					return Vector3.zero;
				}
			}
			set => throw new System.NotImplementedException("The FollowerEntity does not support setting this property. If you want to override the movement, you'll need to write a custom entity component system.");
		}

		/// <summary>\copydocref{FollowerEntity.remainingDistance}</summary>
		public float remainingDistance {
			get {
				if (!entityStorageCache.Update(World.DefaultGameObjectInjectionWorld, entity, out var entityManager, out var storage)) return float.PositiveInfinity;

				movementStateAccessRO.Update(entityManager);
				managedStateAccessRO.Update(entityManager);
				var managedState = managedStateAccessRO[storage];
				// TODO: Should this perhaps only check if the start/end points are stale, and ignore the case when the graph is updated and some nodes are destroyed?
				if (managedState.pathTracer.hasPath && !managedState.pathTracer.isStale) {
					ref var movementState = ref movementStateAccessRO[storage];
					return movementState.remainingDistanceToEndOfPart + Vector3.Distance(managedState.pathTracer.endPointOfFirstPart, managedState.pathTracer.endPoint);
				} else {
					return float.PositiveInfinity;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.stopDistance}</summary>
		public float stopDistance {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.stopDistance : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.stopDistance = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.positionSmoothing}</summary>
		public float positionSmoothing {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.positionSmoothing : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.positionSmoothing = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.rotationSmoothing}</summary>
		public float rotationSmoothing {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.rotationSmoothing : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.rotationSmoothing = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.reachedDestination}</summary>
		public bool reachedDestination => entityStorageCache.GetComponentData(world, entity, ref movementStateAccessRW, out var movementState) ? movementState.value.reachedDestinationAndOrientation : false;

		/// <summary>\copydocref{FollowerEntity.reachedEndOfPath}</summary>
		public bool reachedEndOfPath => entityStorageCache.GetComponentData(world, entity, ref movementStateAccessRW, out var movementState) ? movementState.value.reachedEndOfPathAndOrientation : false;

		/// <summary>\copydocref{FollowerEntity.reachedCrowdedEndOfPath}</summary>
		public bool reachedCrowdedEndOfPath {
			get {
				if (reachedEndOfPath) return true;
				if (!hasPath) return false;

				var entityManager = world.EntityManager;
				if (RVOSimulator.active != null && entityManager.HasComponent<AgentIndex>(entity)) {
					var agentIndex = entityManager.GetComponentData<AgentIndex>(entity);
					var simulator = RVOSimulator.active.GetSimulator();
					if (agentIndex.TryGetIndex(ref simulator.simulationData, out var index)) {
						var effectivelyReachedDestination = simulator.outputData.effectivelyReachedDestination[index];
						if (effectivelyReachedDestination == ReachedEndOfPath.Reached) {
							managedStateAccessRO.Update(entityManager);
							var managedState = managedStateAccessRO[entityManager.GetStorageInfo(entity)];

							// Check if the RVO simulator state is roughly in sync with the path tracer
							var rvoEndOfPath = (Vector3)simulator.simulationData.endOfPath[index];
							var endOfPath = managedState.pathTracer.endPoint;
							const float MaxChangeRadians = 0.1f;
							if ((rvoEndOfPath - endOfPath).sqrMagnitude < (endOfPath - position).sqrMagnitude*MaxChangeRadians*MaxChangeRadians) {
								return true;
							} else {
								// The RVO simulator has a different end of path than the path tracer.
								// This can happen if the destination has just changed, but the rvo simulation has not yet run another iteration.
								// In that case we should not consider it reached.
							}
						}
					}
				}
				return false;
			}
		}

		/// <summary>\copydocref{FollowerEntity.endOfPath}</summary>
		public Vector3 endOfPath {
			get {
				if (entityStorageCache.GetComponentData(world, entity, ref managedStateAccessRO, out var managedState)) {
					if (managedState.pathTracer.hasPath) {
						return managedState.pathTracer.endPoint;
					} else {
						var d = destination;
						if (float.IsFinite(d.x)) return d;
					}
				}
				return position;
			}
		}

		/// <summary>\copydocref{FollowerEntity.destination}</summary>
		public Vector3 destination {
			get => entityStorageCache.GetComponentData(world, entity, ref destinationPointAccessRO, out var destination) ? (Vector3)destination.value.destination : Vector3.positiveInfinity;
			set => SetDestination(value, default);
		}

		/// <summary>\copydocref{FollowerEntity.destinationFacingDirection}</summary>
		public Vector3 destinationFacingDirection {
			get => entityStorageCache.GetComponentData(world, entity, ref destinationPointAccessRO, out var destination) ? (Vector3)destination.value.facingDirection : Vector3.zero;
		}

		/// <summary>\copydocref{FollowerEntity.SetDestination}</summary>
		public void SetDestination (float3 destination, float3 facingDirection = default) {
			AssertEntityExists();
			var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
			movementStateAccessRW.Update(entityManager);
			managedStateAccessRW.Update(entityManager);
			agentCylinderShapeAccessRO.Update(entityManager);
			movementSettingsAccessRO.Update(entityManager);
			localTransformAccessRO.Update(entityManager);
			autoRepathPolicyRW.Update(entityManager);
			destinationPointAccessRW.Update(entityManager);
			movementPlaneAccessRO.Update(entityManager);
			readyToTraverseOffMeshLinkRW.Update(entityManager);

			var storage = entityManager.GetStorageInfo(entity);
			destinationPointAccessRW[storage] = new DestinationPoint {
				destination = destination,
				facingDirection = facingDirection,
			};

			var managedState = managedStateAccessRW[storage];

			// If we already have a path, we try to repair it immediately.
			// This ensures that the #reachedDestination and #reachedEndOfPath flags are as up to date as possible.
			if (managedState.pathTracer.hasPath) {
				Profiler.BeginSample("RepairEnd");
				ref var movementPlane = ref movementPlaneAccessRO[storage];
				managedState.pathTracer.UpdateEnd(destination, PathTracer.RepairQuality.High, movementPlane.value);
				Profiler.EndSample();
				ref var movementState = ref movementStateAccessRW[storage];
				if (movementState.pathTracerVersion != managedState.pathTracer.version) {
					Profiler.BeginSample("EstimateNative");
					ref var shape = ref agentCylinderShapeAccessRO[storage];
					ref var movementSettings = ref movementSettingsAccessRO[storage];
					ref var localTransform = ref localTransformAccessRO[storage];
					ref var autoRepath = ref autoRepathPolicyRW[storage];
					ref var destinationPoint = ref destinationPointAccessRW[storage];
					var readyToTraverseOffMeshLink = storage.Chunk.GetEnabledMask(ref readyToTraverseOffMeshLinkRW.handle).GetEnabledRefRW<ReadyToTraverseOffMeshLink>(storage.IndexInChunk);
					InitScratchData();
					JobRepairPath.Execute(
						ref localTransform,
						ref movementState,
						ref shape,
						ref movementPlane,
						ref autoRepath,
						ref destinationPoint,
						readyToTraverseOffMeshLink,
						managedState,
						in movementSettings,
						nextCornersScratch,
						ref indicesScratch,
						Allocator.Persistent,
						false
						);
					Profiler.EndSample();
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.autoRepath}</summary>
		public ECS.AutoRepathPolicy autoRepath {
			get => entityStorageCache.GetComponentData(world, entity, ref autoRepathPolicyRW, out var component) ? component.value : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref autoRepathPolicyRW, out var component)) {
					component.value = value;
				}
			}
		}

		/// <summary>
		/// \copydoc Pathfinding::IAstarAI::canSearch
		/// Deprecated: This has been superseded by <see cref="autoRepath.mode"/>.
		/// </summary>
		[System.Obsolete("This has been superseded by autoRepath.mode")]
		public bool canSearch {
			get {
				return autoRepath.mode != Pathfinding.AutoRepathPolicy.Mode.Never;
			}
			set {
				if (value) {
					var v = autoRepath;
					if (v.mode == Pathfinding.AutoRepathPolicy.Mode.Never) {
						v.mode = Pathfinding.AutoRepathPolicy.Mode.EveryNSeconds;
						autoRepath = v;
					}
				} else {
					var v = autoRepath;
					v.mode = Pathfinding.AutoRepathPolicy.Mode.Never;
					autoRepath = v;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.simulateMovement}</summary>
		public bool simulateMovement {
			get => entityExists && world.EntityManager.HasComponent<SimulateMovement>(entity);
			set => ToggleComponent<SimulateMovement>(world, entity, value, true);
		}

		/// <summary>\copydocref{FollowerEntity.movementPlane}</summary>
		public NativeMovementPlane movementPlane => entityStorageCache.GetComponentData(world, entity, ref movementPlaneAccessRO, out var movementPlane) ? movementPlane.value.value : new NativeMovementPlane(rotation);

		/// <summary>\copydocref{FollowerEntity.enableGravity}</summary>
		public bool enableGravity {
			get => entityExists && world.EntityManager.IsComponentEnabled<GravityState>(entity);
			set {
				ToggleComponentEnabled<GravityState>(world, entity, value, false);
			}
		}

		/// <summary>
		/// \copydocref{ManagedState.enableLocalAvoidance}
		///
		/// Note: Setting this property cannot be done via this proxy. Instead, you must add or remove the <see cref="RVOAgent"/> component from the entity.
		///
		/// See: <see cref="FollowerEntity.enableLocalAvoidance"/>, which supports both get and set.
		/// See: <see cref="rvoSettings"/>
		/// </summary>
		public bool enableLocalAvoidance {
			get => entityExists && world.EntityManager.HasComponent<RVOAgent>(entity);
		}

		/// <summary>\copydocref{FollowerEntity.localAvoidanceTemporarilyDisabled}</summary>
		public bool localAvoidanceTemporarilyDisabled {
			get => entityExists && world.EntityManager.HasComponent<AgentOffMeshLinkLocalAvoidanceDisabled>(entity) && world.EntityManager.IsComponentEnabled<AgentOffMeshLinkLocalAvoidanceDisabled>(entity);
		}

		/// <summary>\copydocref{FollowerEntity.updatePosition}</summary>
		public bool updatePosition {
			get => entityExists && world.EntityManager.HasComponent<SyncPositionWithTransform>(entity);
			set {
				ToggleComponent<SyncPositionWithTransform>(world, entity, value, false);
			}
		}

		/// <summary>\copydocref{FollowerEntity.updateRotation}</summary>

		public bool updateRotation {
			get => entityExists && world.EntityManager.HasComponent<SyncRotationWithTransform>(entity);
			set {
				ToggleComponent<SyncRotationWithTransform>(world, entity, value, false);
			}
		}

		/// <summary>\copydocref{FollowerEntity.orientation}</summary>
		public OrientationMode orientation {
			get => entityExists && world.EntityManager.HasComponent<OrientationYAxisForward>(entity) ? OrientationMode.YAxisForward : OrientationMode.ZAxisForward;
			set {
				ToggleComponent<OrientationYAxisForward>(world, entity, value == OrientationMode.YAxisForward, false);
			}
		}

		/// <summary>\copydocref{FollowerEntity.hasPath}</summary>
		public bool hasPath {
			get => entityStorageCache.GetComponentData(world, entity, ref managedStateAccessRO, out var managedState) && !managedState.pathTracer.isStale;
		}

		/// <summary>\copydocref{FollowerEntity.pathPending}</summary>
		public bool pathPending {
			get => entityStorageCache.GetComponentData(world, entity, ref managedStateAccessRO, out var managedState) && managedState.pendingPath != null;
		}

		/// <summary>\copydocref{FollowerEntity.isStopped}</summary>
		public bool isStopped {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value.isStopped : false;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value.isStopped = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.movementSettings}</summary>
		public MovementSettings movementSettings {
			get => entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRO, out var movementSettings) ? movementSettings.value : default;
			set {
				if (entityStorageCache.GetComponentData(world, entity, ref movementSettingsAccessRW, out var movementSettings)) {
					movementSettings.value = value;
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.steeringTarget}</summary>
		public Vector3 steeringTarget => entityStorageCache.GetComponentData(world, entity, ref movementStateAccessRO, out var movementState) ? (Vector3)movementState.value.nextCorner : position;

		/// <summary>\copydocref{FollowerEntity.movementOverrides}</summary>
		public ManagedMovementOverrides movementOverrides => new ManagedMovementOverrides(entity, world);

		/// <summary>\copydoc Pathfinding::IAstarAI::onSearchPath</summary>
		System.Action IAstarAI.onSearchPath {
			get => null;
			set => throw new System.NotImplementedException("The FollowerEntity does not support this property.");
		}

		/// <summary>\copydoc Pathfinding::IAstarAI::FinalizeMovement</summary>
		void IAstarAI.FinalizeMovement (Vector3 nextPosition, Quaternion nextRotation) {
			throw new System.InvalidOperationException("The FollowerEntity component does not support FinalizeMovement. Use an ECS system to override movement instead, or use the movementOverrides property. If you just want to move the agent to a position, set ai.position or call ai.Teleport.");
		}

		/// <summary>\copydocref{FollowerEntity.GetRemainingPath(List<Vector3>,bool)}</summary>
		public void GetRemainingPath (List<Vector3> buffer, out bool stale) {
			GetRemainingPath(buffer, null, out stale);
		}

		/// <summary>\copydocref{FollowerEntity.GetRemainingPath(List<Vector3>,List<PathPartWithLinkInfo>,bool)}</summary>
		public void GetRemainingPath (List<Vector3> buffer, List<PathPartWithLinkInfo> partsBuffer, out bool stale) {
			buffer.Clear();
			if (partsBuffer != null) partsBuffer.Clear();
			if (!entityExists) {
				buffer.Add(position);
				if (partsBuffer != null) partsBuffer.Add(new PathPartWithLinkInfo { startIndex = 0, endIndex = 0 });
				stale = true;
				return;
			}

			var ms = world.EntityManager.GetComponentData<ManagedState>(entity);
			stale = false;
			if (ms.pathTracer.hasPath) {
				var nativeBuffer = new NativeList<float3>(Allocator.Temp);
				var scratch = new NativeArray<int>(8, Allocator.Temp);
				ms.pathTracer.GetNextCorners(nativeBuffer, int.MaxValue, ref scratch, Allocator.Temp);
				if (partsBuffer != null) partsBuffer.Add(new PathPartWithLinkInfo(0, nativeBuffer.Length - 1));

				if (ms.pathTracer.partCount > 1) {
					// There are more parts in the path. We need to create a new PathTracer to get the other parts.
					// This can be comparatively expensive, since it needs to generate all the other types from scratch.
					var pathTracer = ms.pathTracer.Clone();
					while (pathTracer.partCount > 1) {
						pathTracer.PopParts(1);
						var startIndex = nativeBuffer.Length;
						if (pathTracer.GetPartType() == Funnel.PartType.NodeSequence) {
							pathTracer.GetNextCorners(nativeBuffer, int.MaxValue, ref scratch, Allocator.Temp);
							if (partsBuffer != null) partsBuffer.Add(new PathPartWithLinkInfo(startIndex, nativeBuffer.Length - 1));
						} else {
							// If the link contains destroyed nodes, we cannot get a valid link object.
							// In that case, we stop here and mark the path as stale.
							if (pathTracer.PartContainsDestroyedNodes()) {
								stale = true;
								break;
							}
							// Note: startIndex will refer to the last point in the previous part, and endIndex will refer to the first point in the next part
							Assert.IsTrue(startIndex > 0);
							if (partsBuffer != null) partsBuffer.Add(new PathPartWithLinkInfo(startIndex - 1, startIndex, pathTracer.GetLinkInfo()));
						}
						// We need to check if the path is stale after each part because the path tracer may have realized that some nodes are destroyed
						stale |= pathTracer.isStale;
					}
					pathTracer.Dispose();
				}

				nativeBuffer.AsUnsafeSpan().Reinterpret<Vector3>().CopyTo(buffer);
			} else {
				buffer.Add(position);
				if (partsBuffer != null) partsBuffer.Add(new PathPartWithLinkInfo { startIndex = 0, endIndex = 0 });
			}
			stale |= ms.pathTracer.isStale;
		}

		/// <summary>\copydocref{FollowerEntity.Move}</summary>
		public void Move (Vector3 deltaPosition) {
			position += deltaPosition;
		}

		/// <summary>
		/// Calculate how the agent will move during this frame.
		///
		/// Warning: The FollowerEntity component does not support MovementUpdate. Use an ECS system to override movement instead, or use the <see cref="movementOverrides"/> property.
		/// </summary>
		void IAstarAI.MovementUpdate (float deltaTime, out Vector3 nextPosition, out Quaternion nextRotation) {
			throw new System.InvalidOperationException("The FollowerEntity component does not support MovementUpdate. Use an ECS system to override movement instead, or use the movementOverrides property");
		}

		/// <summary>\copydocref{FollowerEntity.SearchPath}</summary>
		public void SearchPath () {
			var dest = destination;
			if (!float.IsFinite(dest.x)) return;

			if (entityStorageCache.GetComponentData(world, entity, ref managedSettingsAccessRO, out var managedSettings)) {
				var movementPlane = this.movementPlane;
				var path = ABPath.Construct(position, dest, null);
				path.UseSettings(managedSettings.pathfindingSettings);
				SetPath(path, false);
			}
		}

		internal void CancelCurrentPathRequest () {
			if (entityStorageCache.GetComponentData(world, entity, ref managedStateAccessRO, out var managedState)) {
				managedState.CancelCurrentPathRequest();
			}
		}

		internal void ClearPath () {
			if (entityStorageCache.Update(world, entity, out var entityManager, out var storage)) {
				agentOffMeshLinkTraversalRO.Update(entityManager);

				if (agentOffMeshLinkTraversalRO.HasComponent(storage)) {
					// Agent is traversing an off-mesh link. We must abort this link traversal.
					var managedInfo = entityManager.GetComponentData<ManagedAgentOffMeshLinkTraversal>(entity);
					if (managedInfo.stateMachine != null) managedInfo.stateMachine.OnAbortTraversingOffMeshLink();
					managedInfo.context.Restore();
					entityManager.RemoveComponent<AgentOffMeshLinkTraversal>(entity);
					entityManager.RemoveComponent<ManagedAgentOffMeshLinkTraversal>(entity);
					// We need to get the storage info again, because the entity will have been moved to another chunk
					entityStorageCache.Update(world, entity, out entityManager, out storage);
				}

				entityManager.SetComponentEnabled<ReadyToTraverseOffMeshLink>(entity, false);

				managedStateAccessRW.Update(entityManager);
				movementStateAccessRW.Update(entityManager);
				localTransformAccessRO.Update(entityManager);
				movementPlaneAccessRO.Update(entityManager);
				resolvedMovementAccessRW.Update(entityManager);
				movementControlAccessRW.Update(entityManager);

				ref var movementState = ref movementStateAccessRW[storage];
				ref var localTransform = ref localTransformAccessRO[storage];
				ref var movementPlane = ref movementPlaneAccessRO[storage];
				ref var resolvedMovement = ref resolvedMovementAccessRW[storage];
				ref var controlOutput = ref movementControlAccessRW[storage];
				var managedState = managedStateAccessRW[storage];

				managedState.ClearPath();
				managedState.CancelCurrentPathRequest();
				movementState.SetPathIsEmpty(localTransform.Position);

				// This emulates what JobControl does when the agent has no path.
				// This ensures that properties like #desiredVelocity return the correct value immediately after the path has been cleared.
				ResetControl(ref resolvedMovement, ref controlOutput, ref movementPlane, localTransform.Position, localTransform.Rotation, movementState.endOfPath);
			}
		}

		/// <summary>Adds or removes a component from an entity</summary>
		internal static void ToggleComponent<T>(World world, Entity entity, bool enabled, bool mustExist) where T : struct, IComponentData {
			if (world == null || !world.IsCreated || !world.EntityManager.Exists(entity)) {
				if (!mustExist) throw new System.InvalidOperationException("Entity does not exist. You can only access this if the component is active and enabled.");
				return;
			}
			if (enabled) {
				world.EntityManager.AddComponent<T>(entity);
			} else {
				world.EntityManager.RemoveComponent<T>(entity);
			}
		}

		/// <summary>Enables or disables a component on an entity</summary>
		internal static void ToggleComponentEnabled<T>(World world, Entity entity, bool enabled, bool mustExist) where T : struct, IComponentData, IEnableableComponent {
			if (world == null || !world.IsCreated || !world.EntityManager.Exists(entity)) {
				if (!mustExist) throw new System.InvalidOperationException("Entity does not exist. You can only access this if the component is active and enabled.");
				return;
			}
			world.EntityManager.SetComponentEnabled<T>(entity, enabled);
		}

		internal static void ResetControl (ref ResolvedMovement resolvedMovement, ref MovementControl controlOutput, ref AgentMovementPlane movementPlane, float3 position, quaternion rotation, float3 endOfPath) {
			resolvedMovement.targetPoint = position;
			resolvedMovement.speed = 0;
			resolvedMovement.targetRotation = resolvedMovement.targetRotationHint = controlOutput.targetRotation = controlOutput.targetRotationHint = movementPlane.value.ToPlane(rotation);
			controlOutput.endOfPath = endOfPath;
			controlOutput.speed = 0f;
			controlOutput.targetPoint = position;
			controlOutput.hierarchicalNodeIndex = -1;
		}

		/// <summary>\copydocref{FollowerEntity.SetPath}</summary>
		public void SetPath (Path path, bool updateDestinationFromPath = true) {
			var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
			if (!entityManager.Exists(entity)) throw new System.InvalidOperationException("Entity does not exist. You can only assign a path if the component is active and enabled.");

			managedStateAccessRW.Update(entityManager);
			movementPlaneAccessRO.Update(entityManager);
			agentOffMeshLinkTraversalRO.Update(entityManager);
			movementStateAccessRW.Update(entityManager);
			localTransformAccessRO.Update(entityManager);
			destinationPointAccessRW.Update(entityManager);
			autoRepathPolicyRW.Update(entityManager);

			var storage = entityManager.GetStorageInfo(entity);

			bool isTraversingOffMeshLink = agentOffMeshLinkTraversalRO.HasComponent(storage);
			if (isTraversingOffMeshLink) {
				// Agent is traversing an off-mesh link. We ignore any path updates during this time.
				// TODO: Race condition when adding off mesh link component?
				// TODO: If passing null, should we clear the whole path after the off-mesh link?
				return;
			}

			if (path == null) {
				ClearPath();
				return;
			}

			var managedState = managedStateAccessRW[storage];
			ref var movementPlane = ref movementPlaneAccessRO[storage];
			ref var movementState = ref movementStateAccessRW[storage];
			ref var localTransform = ref localTransformAccessRO[storage];
			ref var destination = ref destinationPointAccessRW[storage];
			ref var autoRepathPolicy = ref autoRepathPolicyRW[storage];

			if (updateDestinationFromPath && path is ABPath abPath) {
				// If the user supplies a new ABPath manually, they probably want the agent to move to that point.
				// So by default we update the destination to match the path.
				if (abPath.endPointKnownBeforeCalculation || abPath.IsDone()) {
					destination = new DestinationPoint { destination = abPath.originalEndPoint, facingDirection = default };
				} else {
					// If the destination is not known, we set it to positive infinity.
					// This is the case for MultiTargetPath and RandomPath, for example.
					destination = new DestinationPoint { destination = Vector3.positiveInfinity, facingDirection = default };
				}
			}

			// The FollowerEntity works best with a ClosestAsSeenFromAboveSoft distance metric
			path.nearestNodeDistanceMetric = DistanceMetric.ClosestAsSeenFromAboveSoft(movementPlane.value.up);

			autoRepathPolicy.OnScheduledPathRecalculation(destination.destination, (float)World.DefaultGameObjectInjectionWorld.Time.ElapsedTime);
			if (path.IsDone()) autoRepathPolicy.OnPathCalculated(path.error);
			ManagedState.SetPath(path, managedState, in movementPlane, ref destination);

			// Check if we have started to follow the path.
			// If it wasn't calculated yet, it will have just been scheduled to be calculated, and will be applied later.
			if (managedState.activePath == path) {
				agentCylinderShapeAccessRO.Update(entityManager);
				movementSettingsAccessRO.Update(entityManager);
				readyToTraverseOffMeshLinkRW.Update(entityManager);

				// This remaining part ensures that the path tracer is fully up to date immediately after the path has been assigned.
				// So that things like GetRemainingPath, and various properties like reachedDestination are up to date immediately.
				managedState.pathTracer.UpdateStart(localTransform.Position, PathTracer.RepairQuality.High, movementPlane.value);
				managedState.pathTracer.UpdateEnd(destination.destination, PathTracer.RepairQuality.High, movementPlane.value);

				if (movementState.pathTracerVersion != managedState.pathTracer.version) {
					InitScratchData();
					ref var shape = ref agentCylinderShapeAccessRO[storage];
					ref var movementSettings = ref movementSettingsAccessRO[storage];
					var readyToTraverseOffMeshLink = storage.Chunk.GetEnabledMask(ref readyToTraverseOffMeshLinkRW.handle).GetEnabledRefRW<ReadyToTraverseOffMeshLink>(storage.IndexInChunk);
					JobRepairPath.Execute(
						ref localTransform,
						ref movementState,
						ref shape,
						ref movementPlane,
						ref autoRepathPolicy,
						ref destination,
						readyToTraverseOffMeshLink,
						managedState,
						in movementSettings,
						nextCornersScratch,
						ref indicesScratch,
						Allocator.Persistent,
						false
						);
				}
			}
		}

		/// <summary>\copydocref{FollowerEntity.Teleport}</summary>
		public void Teleport (Vector3 newPosition, bool clearPath = true) {
			if (!entityExists) return;

			if (clearPath) ClearPath();

			var entityManager = world.EntityManager;
			movementOutputAccessRW.Update(entityManager);
			managedStateAccessRW.Update(entityManager);
			managedSettingsAccessRO.Update(entityManager);
			movementPlaneAccessRW.Update(entityManager);
			resolvedMovementAccessRW.Update(entityManager);
			movementControlAccessRW.Update(entityManager);
			var storage = entityManager.GetStorageInfo(entity);

			ref var movementOutput = ref movementOutputAccessRW[storage];
			movementOutput.lastPosition = newPosition;
			var managedState = managedStateAccessRW[storage];
			var managedSettings = managedSettingsAccessRO[storage];
			if (clearPath) managedState.CancelCurrentPathRequest();

			if (!managedState.pathTracer.hasPath && AstarPath.active != null) {
				// Since we haven't calculated a path yet,
				var nearest = AstarPath.active.GetNearest(newPosition, managedSettings.pathfindingSettings.ToNearestNodeConstraint());

				if (nearest.node != null) {
					ref var movementPlane = ref movementPlaneAccessRW[storage];
					ref var resolvedMovement = ref resolvedMovementAccessRW[storage];
					ref var movementControl = ref movementControlAccessRW[storage];

					// If we are using the graph's natural movement plane, we need to update our movement plane from the graph
					// before we start repairing the path. Otherwise the agent can get snapped to a weird point on the navmesh,
					// if its initial rotation was not aligned with the graph.
					if (movementPlaneSource == MovementPlaneSource.Graph) {
						// The target rotations are relative to the movement plane, so we need to patch it, to make sure it stays constant in world space.
						// This is important when the agent starts with isStopped=true, because the targetRotation will not be recalculated every frame.
						// TODO: Alternatively we could make sure to always make the new movement plane as similar as possible to the old one,
						// but this has a minor performance impact every frame.
						var targetRotation = movementPlane.value.ToWorldRotation(resolvedMovement.targetRotation);
						var targetRotationHint = movementPlane.value.ToWorldRotation(resolvedMovement.targetRotationHint);
						var targetRotation2 = movementPlane.value.ToWorldRotation(movementControl.targetRotation);
						var targetRotationHint2 = movementPlane.value.ToWorldRotation(movementControl.targetRotationHint);

						movementPlane = new AgentMovementPlane(MovementPlaneFromGraphSystem.MovementPlaneFromGraph(nearest.node.Graph));
						// TODO: Do we need to do a similar thing for the raycast and navmesh normal cases?

						resolvedMovement.targetRotation = movementPlane.value.ToPlane(targetRotation);
						resolvedMovement.targetRotationHint = movementPlane.value.ToPlane(targetRotationHint);
						movementControl.targetRotation = movementPlane.value.ToPlane(targetRotation2);
						movementControl.targetRotationHint = movementPlane.value.ToPlane(targetRotationHint2);
					}

					// Make the agent's path consist of a single node at the current position.
					// This is temporary and will be replaced by the actual path when it is calculated.
					// This allows it to be clamped to the navmesh immediately, instead of waiting for a destination to be set and a path to be calculated.
					managedState.pathTracer.SetFromSingleNode(nearest.node, nearest.position, movementPlane.value, managedSettings.pathfindingSettings.ToTraversalConstraint(), managedSettings.pathfindingSettings.ToTraversalCosts());
				}
			}

			// Note: Since we are starting from a completely new path,
			// setting the position will also cause the path tracer to repair the destination.
			// Therefore we don't have to also set the destination here.
			position = newPosition;
		}

		/// <summary>
		/// Destroys the entity and clears the proxy.
		///
		/// If the entity does not exist, this does nothing.
		/// </summary>
		public void Destroy () {
			if (entityExists) world.EntityManager.DestroyEntity(entity);
			this = default; // Clear the proxy to avoid dangling references
		}
	}
}
#endif
