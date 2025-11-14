#if MODULE_ENTITIES
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Holds an agent's destination as an entity.
	///
	/// Every frame, the position of this entity will be copied to the <see cref="DestinationPoint.destination"/> field of the agent, by the <see cref="DestinationEntitySystem"/>.
	///
	/// See: <see cref="AIDestinationSetter"/>
	/// See: <see cref="IAstarAI.destination"/>
	/// See: <see cref="DestinationPoint.destination"/>
	/// See: <see cref="DestinationEntitySystem"/>
	/// </summary>
	[System.Serializable]
	[Unity.Properties.GeneratePropertyBag]
	public struct DestinationEntity : IComponentData, IEnableableComponent {
		/// <summary>
		/// The entity whose position the agent is moving towards.
		///
		/// Every frame, the position of this entity will be copied to the <see cref="DestinationPoint.destination"/> field of the agent.
		///
		/// See: <see cref="AIDestinationSetter"/>
		/// See: <see cref="IAstarAI.destination"/>
		/// </summary>
		public Entity destination;

		/// <summary>
		/// If true, the agent will try to align itself with the rotation of the <see cref="destination"/> entity.
		///
		/// [Open online documentation to see videos]
		///
		/// See: <see cref="FollowerEntity.SetDestination"/>
		/// </summary>
		public bool useRotation;
	}
}
#endif
