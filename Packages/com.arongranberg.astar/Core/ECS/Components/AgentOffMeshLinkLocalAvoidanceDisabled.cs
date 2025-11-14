#if MODULE_ENTITIES
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Component which is enabled when the agent is traversing an off-mesh link, and the local avoidance logic should be disabled.
	///
	/// You can toggle this using <see cref="AgentOffMeshLinkTraversalContext.DisableLocalAvoidance"/>.
	///
	/// This component is added to the agent when it starts traversing an off-mesh link.
	/// The enabled/disabled state is managed by <see cref="JobManagedOffMeshLinkTransition"/>.
	/// </summary>
	public struct AgentOffMeshLinkLocalAvoidanceDisabled : IComponentData, IEnableableComponent {
	}
}
#endif
