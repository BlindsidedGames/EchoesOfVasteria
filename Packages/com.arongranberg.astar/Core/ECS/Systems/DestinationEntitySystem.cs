#if MODULE_ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace Pathfinding.ECS {
	/// <summary>
	/// System that updates the destination of agents with a <see cref="DestinationEntity"/> component.
	///
	/// Every frame, the position of the target entity will be copied to the <see cref="DestinationPoint.destination"/> field of the agent.
	/// If <see cref="DestinationEntity.useRotation"/> is true, the facing direction will also be copied.
	///
	/// See: <see cref="AIDestinationSetter"/>
	/// </summary>
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[UpdateBefore(typeof(SchedulePathSearchSystem))]
	[BurstCompile]
	[RequireMatchingQueriesForUpdate]
	public partial struct DestinationEntitySystem : ISystem {
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

			public void Execute (ref DestinationPoint destPoint, in DestinationEntity destEntity) {
				if (TransformLookup.HasComponent(destEntity.destination)) {
					var transform = TransformLookup[destEntity.destination];
					destPoint.destination = transform.Position;
					destPoint.facingDirection = destEntity.useRotation ? transform.Forward : float3.zero;
				}
			}
		}
	}
}
#endif
