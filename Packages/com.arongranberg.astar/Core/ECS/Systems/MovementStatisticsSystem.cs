#if MODULE_ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
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
	[UpdateAfter(typeof(AIMoveSystem))]
	[RequireMatchingQueriesForUpdate]
	public partial struct MovementStatisticsSystem : ISystem {
		public void OnUpdate (ref SystemState state) {
			// TODO: Can be bursted if we provide the cheap delta time via some burst-readable field
			state.Dependency = new JobUpdateMovementStatistics {
				// This system is executed at least every frame to make sure the agent is moving smoothly even at high fps.
				// The control loop and local avoidance may be running less often.
				// So this is designated a "cheap" system, and we use the corresponding delta time for that.
				dt = AIMovementSystemGroup.TimeScaledRateManager.CheapStepDeltaTime
			}.Schedule(state.Dependency);
		}
	}
}
#endif
