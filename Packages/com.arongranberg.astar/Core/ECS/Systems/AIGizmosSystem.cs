#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Transforms;
using Pathfinding.Drawing;
using Unity.Jobs;

namespace Pathfinding.ECS {
	/// <summary>
	/// Draws gizmos and debug information for <see cref="FollowerEntity"/> agents.
	///
	/// When outside of play mode, the <see cref="FollowerEntity.DrawGizmos"/> method will draw some of the same gizmos,
	/// since this system cannot be used then (entities only exist in play mode).
	/// </summary>
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	public partial struct AIGizmosSystem : ISystem {
		static bool manuallyTriggered;
		JobRepairPath.Scheduler jobRepairPathScheduler;
		ComponentTypeHandle<MovementState> MovementStateTypeHandleRO;
		ComponentTypeHandle<ResolvedMovement> ResolvedMovementHandleRO;

		class DrawerCallback : IDrawGizmos {
			World world;

			public DrawerCallback(World world) {
				this.world = world;
			}

			public void DrawGizmos () {
				if (!world.IsCreated) return;
				var handle = world.GetExistingSystem<AIGizmosSystem>();
				if (handle != SystemHandle.Null) {
					manuallyTriggered = true;
					try {
						handle.Update(world.Unmanaged);
					} finally {
						manuallyTriggered = false;
					}
				}
			}

			public bool Exists => world.IsCreated && world.GetExistingSystem<AIGizmosSystem>() != SystemHandle.Null;
		}

		public void OnCreate (ref SystemState state) {
			jobRepairPathScheduler = new JobRepairPath.Scheduler(ref state);
			MovementStateTypeHandleRO = state.GetComponentTypeHandle<MovementState>(true);
			ResolvedMovementHandleRO = state.GetComponentTypeHandle<ResolvedMovement>(true);

			DrawingManager.Register(new DrawerCallback(state.World), typeof(FollowerEntity));
		}

		public void OnUpdate (ref SystemState systemState) {
			if (manuallyTriggered) DrawGizmos(ref systemState);
		}

		void DrawGizmos (ref SystemState systemState) {
			var entityQueryGizmos = SystemAPI.QueryBuilder()
									.WithAll<LocalTransform, AgentCylinderShape, MovementSettings, AgentMovementPlane>()
									.WithAll<ManagedState, MovementState, ResolvedMovement>()
									.WithAll<SimulateMovement>().Build();

			if (entityQueryGizmos.IsEmptyIgnoreFilter) return;

			jobRepairPathScheduler.Update(ref systemState);
			MovementStateTypeHandleRO.Update(ref systemState);
			ResolvedMovementHandleRO.Update(ref systemState);

			var draw = DrawingManager.GetBuilder();

			var job1 = new JobDrawFollowerGizmos {
				draw = draw,
				entityManagerHandle = jobRepairPathScheduler.entityManagerHandle,
				LocalTransformTypeHandleRO = jobRepairPathScheduler.LocalTransformTypeHandleRO,
				AgentCylinderShapeHandleRO = jobRepairPathScheduler.AgentCylinderShapeTypeHandleRO,
				MovementSettingsHandleRO = jobRepairPathScheduler.MovementSettingsTypeHandleRO,
				AgentMovementPlaneHandleRO = jobRepairPathScheduler.AgentMovementPlaneTypeHandleRO,
				ManagedStateHandleRW = jobRepairPathScheduler.ManagedStateTypeHandleRW,
				MovementStateHandleRO = MovementStateTypeHandleRO,
				ResolvedMovementHandleRO = ResolvedMovementHandleRO,
			}.ScheduleParallel(entityQueryGizmos, systemState.Dependency);

			// This can actually run in parallel with the first job, because the command builder is thread-safe, and we only read the same components.
			var job2 = new JobDrawFollowerGizmosBase {
				draw = draw,
			}.ScheduleParallel(systemState.Dependency);
			systemState.Dependency = JobHandle.CombineDependencies(job1, job2);

			draw.DisposeAfter(systemState.Dependency, AllowedDelay.EndOfFrame);
		}
	}
}
#endif
