#if MODULE_ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Pathfinding.ECS {
	[BurstCompile]
	[WithAll(typeof(SimulateMovement))]
	public partial struct JobUpdateMovementStatistics : IJobEntity {
		public float dt;

		static void UpdateStatistics (in LocalTransform transform, ref MovementStatistics movementStatistics, float dt) {
			if (dt > 0.000001f) {
				movementStatistics.estimatedVelocity = (transform.Position - movementStatistics.lastPosition) / dt;
			}
			movementStatistics.lastPosition = transform.Position;
		}

		public void Execute (in LocalTransform transform, ref MovementStatistics movementStatistics) {
			UpdateStatistics(in transform, ref movementStatistics, dt);
		}
	}
}
#endif
