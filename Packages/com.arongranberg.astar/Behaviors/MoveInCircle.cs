using UnityEngine;
using Pathfinding.Drawing;

namespace Pathfinding {
	/// <summary>
	/// Moves an agent in a circle around a point.
	///
	/// This script is intended as an example of how you can make an agent move in a circle.
	/// In a real game, you may want to replace this script with your own custom script that is tailored to your game.
	/// The code in this script is simple enough to copy and paste wherever you need it.
	///
	/// When used in an ECS subscene, it will automatically be baked into a <see cref="DestinationMoveInCircle"/> component.
	///
	/// [Open online documentation to see videos]
	///
	/// See: move_in_circle (view in online documentation for working links)
	/// See: <see cref="AIDestinationSetter"/>
	/// See: <see cref="FollowerEntity"/>
	/// See: <see cref="AIPath"/>
	/// See: <see cref="RichAI"/>
	/// See: <see cref="AILerp"/>
	/// </summary>
	[UniqueComponent(tag = "ai.destination")]
	[AddComponentMenu("Pathfinding/AI/Behaviors/MoveInCircle")]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/moveincircle.html")]
	public class MoveInCircle : VersionedMonoBehaviour {
		/// <summary>[MoveInCircle]</summary>
		/// <summary>Target point to rotate around</summary>
		public Transform target;
		/// <summary>Radius of the circle</summary>
		public float radius = 5;
		/// <summary>Distance between the agent's current position, and the destination it will get. Use a negative value to make the agent move in the opposite direction around the circle.</summary>
		public float offset = 2;

		IAstarAI ai;

		void OnEnable () {
			ai = GetComponent<IAstarAI>();
		}

		public static Vector3 CalculateDestination (Vector3 position, Vector3 target, Vector3 targetUp, float radius, float offset) {
			var normal = (position - target).normalized;
			var tangent = Vector3.Cross(normal, targetUp);
			return target + normal * radius + tangent * offset;
		}

		void Update () {
			ai.destination = CalculateDestination(ai.position, target.position, target.up, radius, offset);
		}

		/// <summary>[MoveInCircle]</summary>

		public override void DrawGizmos () {
			if (target) Draw.Circle(target.position, target.up, radius, Color.white);
		}

#if MODULE_ENTITIES
		// The code below is only used when this component is used in an ECS subscene

		/// <summary>
		/// ECS component corresponding to <see cref="MoveInCircle"/>.
		/// See: <see cref="DestinationMoveInCircleSystem"/>
		/// </summary>
		public struct DestinationMoveInCircle : Unity.Entities.IComponentData, Unity.Entities.IEnableableComponent {
			public Unity.Entities.Entity target;
			public float radius;
			public float offset;
		}

#if UNITY_EDITOR
		public class MoveInCircleBaker : Unity.Entities.Baker<MoveInCircle> {
			public override void Bake (MoveInCircle authoring) {
				var entity = GetEntity(Unity.Entities.TransformUsageFlags.Dynamic);
				AddComponent(entity, new DestinationMoveInCircle {
					target = GetEntity(authoring.target, Unity.Entities.TransformUsageFlags.Dynamic),
					radius = authoring.radius,
					offset = authoring.offset
				});
			}
		}
#endif
#endif
	}
}
