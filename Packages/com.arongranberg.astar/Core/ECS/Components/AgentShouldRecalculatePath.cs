#if MODULE_ENTITIES
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Tag component which, when enabled, indicates that the agent should recalculate its path immediately.
	///
	/// The enabled state is updated every simulation loop. It cannot be set externally.
	/// </summary>
	public struct AgentShouldRecalculatePath : IComponentData, IEnableableComponent {}
}
#endif
