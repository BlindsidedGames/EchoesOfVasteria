#if MODULE_ENTITIES
using Unity.Entities;

namespace Pathfinding.ECS {
	using Unity.Transforms;

	public delegate void BeforeControlDelegate(Entity entity, float dt, ref LocalTransform localTransform, ref AgentCylinderShape shape, ref AgentMovementPlane movementPlane, ref DestinationPoint destination, ref MovementState movementState, ref MovementSettings movementSettings);
	public delegate void AfterControlDelegate(Entity entity, float dt, ref LocalTransform localTransform, ref AgentCylinderShape shape, ref AgentMovementPlane movementPlane, ref DestinationPoint destination, ref MovementState movementState, ref MovementSettings movementSettings, ref MovementControl movementControl);
	public delegate void BeforeMovementDelegate(Entity entity, float dt, ref LocalTransform localTransform, ref AgentCylinderShape shape, ref AgentMovementPlane movementPlane, ref DestinationPoint destination, ref MovementState movementState, ref MovementSettings movementSettings, ref MovementControl movementControl, ref ResolvedMovement resolvedMovement);

	/// <summary>
	/// Helper for adding and removing hooks to the FollowerEntity component.
	/// This is used to allow other systems to override the movement of the agent.
	///
	/// See: <see cref="FollowerEntity.movementOverrides"/>
	/// </summary>
	public struct ManagedMovementOverrides {
		Entity entity;
		World world;

		public ManagedMovementOverrides (Entity entity, World world) {
			this.entity = entity;
			this.world = world;
		}

		/// <summary>
		/// Registers a callback that runs before the agent calculates how it wants to move, but after it has repaired its path.
		///
		/// You can use this to tweak the agent's movement slightly.
		///
		/// See: <see cref="FollowerEntity.movementOverrides"/> for example code.
		/// </summary>
		public void AddBeforeControlCallback (BeforeControlDelegate value) {
			AddCallback<ManagedMovementOverrideBeforeControl, BeforeControlDelegate>(value);
		}

		/// <summary>Removes a callback previously added with <see cref="AddBeforeControlCallback"/></summary>
		public void RemoveBeforeControlCallback (BeforeControlDelegate value) {
			RemoveCallback<ManagedMovementOverrideBeforeControl, BeforeControlDelegate>(value);
		}

		/// <summary>Registers a callback that runs after the agent has calculated how it wants to move, except for local avoidance.</summary>
		public void AddAfterControlCallback (AfterControlDelegate value) {
			AddCallback<ManagedMovementOverrideAfterControl, AfterControlDelegate>(value);
		}

		/// <summary>Removes a callback previously added with <see cref="AddAfterControlCallback"/></summary>
		public void RemoveAfterControlCallback (AfterControlDelegate value) {
			RemoveCallback<ManagedMovementOverrideAfterControl, AfterControlDelegate>(value);
		}

		/// <summary>
		/// Registers a callback that will be called before the agent is moved, but after it has calculated how it wants to move.
		///
		/// You can use this to tweak the agent's desired movement slightly (<see cref="ResolvedMovement"/>), or by also removing the <see cref="SimulateMovementFinalize"/> component, you can take over the actual movement completely.
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
		public void AddBeforeMovementCallback (BeforeMovementDelegate value) {
			AddCallback<ManagedMovementOverrideBeforeMovement, BeforeMovementDelegate>(value);
		}

		/// <summary>Removes a callback previously added with <see cref="AddBeforeMovementCallback"/></summary>
		public void RemoveBeforeMovementCallback (BeforeMovementDelegate value) {
			RemoveCallback<ManagedMovementOverrideBeforeMovement, BeforeMovementDelegate>(value);
		}

		void AddCallback<C, T>(T callback) where T : System.Delegate where C : ManagedMovementOverride<T>, IComponentData, new() {
			if (callback == null) throw new System.ArgumentNullException(nameof(callback));
			if (world == null || !world.EntityManager.Exists(entity)) throw new System.InvalidOperationException("The entity does not exist. You can only set a callback when the FollowerEntity is active and has been enabled. If you are trying to set this during Awake or OnEnable, try setting it during Start instead.");
			if (!world.EntityManager.HasComponent<C>(entity)) world.EntityManager.AddComponentData(entity, new C());
			world.EntityManager.GetComponentData<C>(entity).AddCallback(callback);
		}

		void RemoveCallback<C, T>(T callback) where T : System.Delegate where C : ManagedMovementOverride<T>, IComponentData, new() {
			if (callback == null) throw new System.ArgumentNullException(nameof(callback));
			if (world == null || !world.EntityManager.Exists(entity)) return;
			if (!world.EntityManager.HasComponent<C>(entity)) return;

			var comp = world.EntityManager.GetComponentData<C>(entity);
			if (!comp.RemoveCallback(callback)) {
				world.EntityManager.RemoveComponent<C>(entity);
			}
		}
	}

	/// <summary>
	/// Component that stores a delegate that can be used to override movement control and movement settings for a specific entity.
	/// This is used by the FollowerEntity to allow other systems to override the movement of the entity.
	///
	/// See: <see cref="FollowerEntity.movementOverrides"/>
	/// </summary>
	public class ManagedMovementOverride<T> : IComponentData where T : class, System.Delegate {
		public T callback;

		public void AddCallback(T callback) => this.callback = (T)System.Delegate.Combine(this.callback, callback);
		public bool RemoveCallback(T callback) => (this.callback = (T)System.Delegate.Remove(this.callback, callback)) != null;
	}

	// IJobEntity does not support generic jobs yet, so we have to make concrete component types for each delegate type
	public class ManagedMovementOverrideBeforeControl : ManagedMovementOverride<BeforeControlDelegate>, System.ICloneable {
		// No fields in this class can be cloned safely
		public object Clone() => new ManagedMovementOverrideBeforeControl();
	}
	public class ManagedMovementOverrideAfterControl : ManagedMovementOverride<AfterControlDelegate> {
		public object Clone() => new ManagedMovementOverrideAfterControl();
	}
	public class ManagedMovementOverrideBeforeMovement : ManagedMovementOverride<BeforeMovementDelegate> {
		public object Clone() => new ManagedMovementOverrideBeforeMovement();
	}
}
#endif
