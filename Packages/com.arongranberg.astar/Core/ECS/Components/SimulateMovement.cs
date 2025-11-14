#if MODULE_ENTITIES
using Unity.Entities;
using Unity.Mathematics;

namespace Pathfinding.ECS {
	using Pathfinding;

	/// <summary>
	/// Tag component to enable movement for an entity.
	/// Without this component, most systems will completely ignore the entity.
	///
	/// There are some more specific components that can be used to selectively enable/disable some jobs:
	/// - <see cref="SimulateMovementRepair"/>
	/// - <see cref="SimulateMovementControl"/>
	/// - <see cref="SimulateMovementFinalize"/>
	///
	/// Removing one of the above components can be useful if you want to override the movement of an agent in some way.
	/// </summary>
	public struct SimulateMovement : IComponentData {
	}

	/// <summary>
	/// Tag component to allow the agent to repair its path and recalculate various statistics.
	///
	/// Allows the <see cref="JobRepairPath"/> to run.
	///
	/// Note: <see cref="JobRepairPath"/> will, at the moment, run even if this component is not present, when manually setting some properties on the agent (e.g. ai.position, ai.destination, etc.).
	/// Create a forum post if you have a use case which isn't supported by this.
	/// </summary>
	public struct SimulateMovementRepair : IComponentData {
	}

	/// <summary>
	/// Tag component to allow the agent to calculate how it wants to move.
	///
	/// Allows the <see cref="JobControl"/> to run.
	/// </summary>
	public struct SimulateMovementControl : IComponentData {
	}

	/// <summary>
	/// Tag component to allow the agent to move according to its desired movement parameters.
	///
	/// Allows <see cref="AIMoveSystem"/> to run the <see cref="JobApplyGravity"/> and <see cref="JobMoveAgent"/> jobs.
	///
	/// By removing this, you can override how the agent's desired movement is converted to actual movement.
	///
	/// This snippet replicates most of the built-in movement:
	/// <code>
	/// var ai = GetComponent<FollowerEntity>();
	///
	/// // Prevent the agent from moving itself, so that we can override it.
	/// ai.world.EntityManager.RemoveComponent<SimulateMovementFinalize>(ai.entity);
	///
	/// // This will run once or more per frame, and allows you to hook into the movement logic
	/// ai.movementOverrides.AddBeforeMovementCallback((Unity.Entities.Entity entity, float dt, ref Unity.Transforms.LocalTransform localTransform, ref AgentCylinderShape shape, ref AgentMovementPlane movementPlane, ref DestinationPoint destination, ref MovementState movementState, ref MovementSettings movementSettings, ref MovementControl movementControl, ref ResolvedMovement resolvedMovement) => {
	///     // Just replicate the normal movement as an example, except for gravity and ground collision
	///     localTransform.Rotation = JobMoveAgent.ResolveRotation(localTransform.Rotation, ref movementState, in resolvedMovement, in movementSettings, in movementPlane, dt);
	///     localTransform.Position += JobMoveAgent.MoveWithoutGravity(localTransform.Position, in resolvedMovement, in movementPlane, dt);
	/// });
	/// </code>
	///
	/// Or if you prefer to handle more things yourself:
	/// <code>
	/// void Start () {
	///     var ai = GetComponent<FollowerEntity>();
	///
	///     // Prevent the agent from moving itself, so that we can override it.
	///     ai.world.EntityManager.RemoveComponent<SimulateMovementFinalize>(ai.entity);
	/// }
	///
	/// void Update () {
	///     var ai = GetComponent<FollowerEntity>();
	///
	///     // Read how the agent wants to move
	///     var resolved = ai.world.EntityManager.GetComponentData<ResolvedMovement>(ai.entity);
	///     var movementPlane = ai.world.EntityManager.GetComponentData<AgentMovementPlane>(ai.entity);
	///     var movementState = ai.world.EntityManager.GetComponentData<MovementState>(ai.entity);
	///     var targetRot = movementPlane.value.ToWorldRotation(resolved.targetRotation + resolved.targetRotationOffset);
	///     var movementSettings = ai.world.EntityManager.GetComponentData<MovementSettings>(ai.entity);
	///     var dt = Time.deltaTime;
	///
	///     // Move the agent.
	///     // This is a very simplified movement logic which has some limitations (it won't work well with local avoidance for example, and since it always runs exactly once per frame, it cannot handle higher time scales),
	///     // but it demonstrates the basic idea. Check out the source code for JobMoveAgent for more inspiration.
	///     ai.transform.rotation = Quaternion.RotateTowards(ai.transform.rotation, targetRot, resolved.rotationSpeed * dt * Mathf.Rad2Deg);
	///     ai.transform.position += Vector3.ClampMagnitude((Vector3)resolved.targetPoint - ai.transform.position, resolved.speed * dt);
	///
	///     // Write back the movement state if we have made any changes
	///     // In this example we don't, but it's common to want to do this.
	///     ai.world.EntityManager.SetComponentData(ai.entity, movementState);
	/// }
	/// </code>
	/// </summary>
	public struct SimulateMovementFinalize : IComponentData {
	}
}
#endif
