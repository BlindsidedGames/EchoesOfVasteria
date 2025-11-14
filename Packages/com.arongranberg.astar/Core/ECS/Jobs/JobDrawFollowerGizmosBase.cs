#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Pathfinding.Drawing;
using Unity.Burst.Intrinsics;

namespace Pathfinding.ECS {
	[BurstCompile]
	public partial struct JobDrawFollowerGizmosBase : IJobEntity, IJobEntityChunkBeginEnd {
		public CommandBuilder draw;
		OrientationMode orientation;

		internal static readonly UnityEngine.Color ShapeGizmoColor = new UnityEngine.Color(240/255f, 213/255f, 30/255f);

		public bool OnChunkBegin (in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
			orientation = chunk.Has<OrientationYAxisForward>() ? OrientationMode.YAxisForward : OrientationMode.ZAxisForward;
			return true;
		}

		public void OnChunkEnd (in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted) {
		}

		public void Execute (
			in LocalTransform transform,
			in AgentCylinderShape shape,
			in DestinationPoint destination
			) {
			// Note: The entity's internal rotation always uses Z-axis forward. But Y axis forward is a hint to use a 2D visualization
			DrawGizmos(ref draw, in transform, in shape, in destination, orientation == OrientationMode.YAxisForward);
		}

		[BurstCompile]
		public static void DrawGizmos (
			ref CommandBuilder draw,
			in LocalTransform transform,
			in AgentCylinderShape shape,
			in DestinationPoint destination,
			bool draw2D
			) {
			var radius = shape.radius;
			var color = ShapeGizmoColor;

			draw.PushMatrix(transform.ToMatrix());
			if (draw2D) {
				draw.Circle(float3.zero, new float3(0, 1, 0), radius, color);
			} else {
				draw.WireCylinder(float3.zero, new float3(0, 1, 0), shape.height, radius, color);
			}

			draw.ArrowheadArc(float3.zero, new float3(0, 0, 1), radius * 1.05f, color);
			draw.PopMatrix();

			if (math.all(math.isfinite(destination.destination))) {
				var dir = destination.facingDirection;
				if (math.any(dir != float3.zero)) {
					draw.xz.ArrowheadArc(destination.destination, dir, 0.25f, UnityEngine.Color.blue);
				}
				draw.xz.Circle(destination.destination, 0.2f, UnityEngine.Color.blue);
			}
		}
	}
}
#endif
