#if MODULE_ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace Pathfinding.ECS {
	/// <summary>
	/// System that updates the destination of agents with a <see cref="DestinationMoveInCircle"/> component.
	///
	/// Every frame, the calculated destination will be copied to the <see cref="DestinationPoint.destination"/> field of the agent.
	///
	/// See: <see cref="MoveInCircle"/>
	/// </summary>
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(SchedulePathSearchSystem))]
	[BurstCompile]
	[RequireMatchingQueriesForUpdate]
	public partial struct DestinationMoveInCircleSystem : ISystem {
		[BurstCompile]
		public void OnUpdate (ref SystemState state) {
			state.Dependency = new UpdateDestinationJob {
				TransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
			}.Schedule(state.Dependency);
		}

		[BurstCompile]
		partial struct UpdateDestinationJob : IJobEntity {
			[ReadOnly]
			public ComponentLookup<LocalToWorld> TransformLookup;

			public void Execute (ref DestinationPoint destPoint, in LocalTransform localTransform, in MoveInCircle.DestinationMoveInCircle destCircle) {
				if (TransformLookup.HasComponent(destCircle.target)) {
					var targetTransform = TransformLookup[destCircle.target];
					destPoint.destination = MoveInCircle.CalculateDestination(localTransform.Position, targetTransform.Position, targetTransform.Up, destCircle.radius, destCircle.offset);
					destPoint.facingDirection = float3.zero;
				}
			}
		}
	}
}
#endif
