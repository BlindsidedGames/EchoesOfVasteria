#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using UnityEngine;

namespace Pathfinding.ECS {
	using Pathfinding;

	[UpdateBefore(typeof(SchedulePathSearchSystem))]
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[RequireMatchingQueriesForUpdate]
	public partial struct SyncDestinationTransformSystem : ISystem {
		public void OnUpdate (ref SystemState systemState) {
			// If there will be multiple simulation steps during this frame, only update the destination points on the first step.
			// It cannot change between simulation steps anyway.
			if (!AIMovementSystemGroup.TimeScaledRateManager.IsFirstSubstep) return;

#if MODULE_ENTITIES_6_6_0_OR_NEWER
			foreach (var(point, destinationSetterRef) in SystemAPI.Query<RefRW<DestinationPoint>, RefRO<AIDestinationSetterRef> >()) {
				// Resolves to null if the AIDestinationSetter has been destroyed without OnDisable removing the component first
				var destinationSetter = destinationSetterRef.ValueRO.value.Value;
#else
			foreach (var(point, destinationSetterWrapper) in SystemAPI.Query<RefRW<DestinationPoint>, SystemAPI.ManagedAPI.UnityEngineComponent<AIDestinationSetter> >()) {
				var destinationSetter = destinationSetterWrapper.Value;
#endif
				if (destinationSetter != null && destinationSetter.target != null) {
					point.ValueRW = new DestinationPoint {
						destination = destinationSetter.target.position,
						facingDirection = destinationSetter.useRotation ? destinationSetter.target.forward : Vector3.zero
					};
				}
			}
		}
	}
}
#endif
