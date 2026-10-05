#if MODULE_ENTITIES
using Unity.Entities;

namespace Pathfinding.ECS {
	using Unity.Transforms;

	public delegate void BeforeControlDelegate(Entity entity, float dt, ref LocalTransform localTransform, ref AgentCylinderShape shape, ref AgentMovementPlane movementPlane, ref DestinationPoint destination, ref MovementState movementState, ref MovementSettings movementSettings);
	public delegate void AfterControlDelegate(Entity entity, float dt, ref LocalTransform localTransform, ref AgentCylinderShape shape, ref AgentMovementPlane movementPlane, ref DestinationPoint destination, ref MovementState movementState, ref MovementSettings movementSettings, ref MovementControl movementControl);
	public delegate void BeforeMovementDelegate(Entity entity, float dt, ref LocalTransform localTransform, ref AgentCylinderShape shape, ref AgentMovementPlane movementPlane, ref DestinationPoint destination, ref MovementState movementState, ref MovementSettings movementSettings, ref MovementControl movementControl, ref ResolvedMovement resolvedMovement);

	/// <summary>
	/// Helper for adding and removing hooks to the FollowerEntity component.
	///
	/// This is used to allow other systems to override the movement of the agent.
	///
	/// The callbacks are stored in <see cref="AgentManagedStorage"/>. A zero-sized marker component on the
	/// entity records that a callback exists, so the jobs that invoke them can skip the vast majority of
	/// agents with an ordinary query.
	///
	/// Registering and unregistering are queued, and take effect at the start of the next phase of the
	/// movement pipeline. This allows adding/removing callbacks from within the callback itself.
	///
	/// Callbacks are not transferred to a cloned agent, since a delegate registered for one agent is not
	/// meaningful for another.
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
			ValidateAdd(value);
			PendingMovementOverrideChanges.Enqueue(world, entity, MovementOverrideRunner.Phase.BeforeControl, value, true);
		}

		/// <summary>Removes a callback previously added with <see cref="AddBeforeControlCallback"/></summary>
		public void RemoveBeforeControlCallback (BeforeControlDelegate value) {
			ValidateRemove(value);
			PendingMovementOverrideChanges.Enqueue(world, entity, MovementOverrideRunner.Phase.BeforeControl, value, false);
		}

		/// <summary>Registers a callback that runs after the agent has calculated how it wants to move, except for local avoidance.</summary>
		public void AddAfterControlCallback (AfterControlDelegate value) {
			ValidateAdd(value);
			PendingMovementOverrideChanges.Enqueue(world, entity, MovementOverrideRunner.Phase.AfterControl, value, true);
		}

		/// <summary>Removes a callback previously added with <see cref="AddAfterControlCallback"/></summary>
		public void RemoveAfterControlCallback (AfterControlDelegate value) {
			ValidateRemove(value);
			PendingMovementOverrideChanges.Enqueue(world, entity, MovementOverrideRunner.Phase.AfterControl, value, false);
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
			ValidateAdd(value);
			PendingMovementOverrideChanges.Enqueue(world, entity, MovementOverrideRunner.Phase.BeforeMovement, value, true);
		}

		/// <summary>Removes a callback previously added with <see cref="AddBeforeMovementCallback"/></summary>
		public void RemoveBeforeMovementCallback (BeforeMovementDelegate value) {
			ValidateRemove(value);
			PendingMovementOverrideChanges.Enqueue(world, entity, MovementOverrideRunner.Phase.BeforeMovement, value, false);
		}

		/// <summary>
		/// Checks that this entity is an agent that can hold a callback.
		///
		/// Checks <see cref="AgentManagedBackupRef"/> rather than <see cref="AgentManagedRef"/>, so that registering
		/// on a freshly cloned agent is allowed. The slot is not read until the change is applied, which is
		/// always after the clone has been repaired.
		/// </summary>
		void ValidateAdd (System.Delegate callback) {
			if (callback == null) throw new System.ArgumentNullException(nameof(callback));
			if (world == null || !world.EntityManager.Exists(entity) || !world.EntityManager.HasComponent<AgentManagedBackupRef>(entity)) throw new System.InvalidOperationException("The entity does not exist, or is not an agent. You can only set a callback when the FollowerEntity is active and has been enabled. If you are trying to set this during Awake or OnEnable, try setting it during Start instead.");
		}

		/// <summary>
		/// Unregistering is tolerant of a dead agent, unlike <see cref="ValidateAdd"/>, so that a component can remove
		/// its callback in OnDisable without caring whether the agent outlived it.
		/// </summary>
		static void ValidateRemove (System.Delegate callback) {
			if (callback == null) throw new System.ArgumentNullException(nameof(callback));
		}
	}

	/// <summary>Movement override registrations that have not been applied yet</summary>
	static class PendingMovementOverrideChanges {
		struct Change {
			public World world;
			public Entity entity;
			public System.Delegate callback;
			public MovementOverrideRunner.Phase phase;
			public bool add;
		}

		/// <summary>
		/// Queued changes, oldest first.
		///
		/// Replayed in order, so that repeated registration and unregistration of one callback resolve to
		/// whatever the last call asks for.
		/// </summary>
		static Change[] changes = new Change[0];
		static int count;

		internal static void Enqueue (World world, Entity entity, MovementOverrideRunner.Phase phase, System.Delegate callback, bool add) {
			if (count == changes.Length) System.Array.Resize(ref changes, System.Math.Max(4, changes.Length*2));
			changes[count++] = new Change {
				world = world,
				entity = entity,
				callback = callback,
				phase = phase,
				add = add,
			};
		}

		/// <summary>
		/// Brings every agent's callbacks and marker components up to date with the calls made since this last ran.
		///
		/// Contract: every phase of the movement pipeline calls this before it inspects its query, and
		/// before it takes any chunk pointer or type handle. Applying a change is a structural change, which
		/// invalidates those, and it is what adds the marker component the query selects on.
		/// </summary>
		internal static void Apply () {
			if (count == 0) return;

			var retained = 0;
			for (int i = 0; i < count; i++) {
				if (TryApply(ref changes[i])) continue;
				changes[retained++] = changes[i];
			}
			// Clearing the slots releases the delegates, which would otherwise keep their targets alive.
			System.Array.Clear(changes, retained, count - retained);
			if (count > 100 && retained == 0) changes = new Change[0];
			count = retained;
		}

		/// <summary>
		/// Applies one change, unless its agent is a clone whose managed data has not been repaired yet.
		///
		/// Returns false only while the agent still has to be repaired, which asks the caller to keep the
		/// change queued. Without this, an agent instantiated from inside a movement override callback would
		/// silently lose a callback registered on it before the change could be applied, because
		/// <see cref="AgentManagedDataRepairSystem"/> runs first in the group and so not again until next frame.
		/// </summary>
		static bool TryApply (ref Change change) {
			// The agent may have been destroyed since the change was queued.
			if (change.world == null || !change.world.IsCreated) return true;
			var entityManager = change.world.EntityManager;
			if (!entityManager.Exists(change.entity)) return true;
			// An entity clone has no AgentManagedRef until it is repaired, so its change stays queued. An agent
			// destroyed this frame still has one and gets applied to: harmless, since cleanup frees its
			// storage entry at the end of the frame, and no query matches the corpse before then.
			if (!entityManager.HasComponent<AgentManagedRef>(change.entity)) return false;
			var slot = entityManager.GetComponentData<AgentManagedRef>(change.entity).slot;
			// Skips an agent that was destroyed and had its slot handed to a different one.
			if (!AgentManagedStorage.TryGet(slot, change.entity, out var entry)) return true;

			switch (change.phase) {
			case MovementOverrideRunner.Phase.BeforeControl: {
				var remaining = (BeforeControlDelegate)Recombine(entry.beforeControl, ref change);
				AgentManagedStorage.SetBeforeControl(slot, change.entity, remaining);
				if (remaining != null) entityManager.AddComponent<AgentHasBeforeControlOverride>(change.entity);
				else entityManager.RemoveComponent<AgentHasBeforeControlOverride>(change.entity);
				break;
			}
			case MovementOverrideRunner.Phase.AfterControl: {
				var remaining = (AfterControlDelegate)Recombine(entry.afterControl, ref change);
				AgentManagedStorage.SetAfterControl(slot, change.entity, remaining);
				if (remaining != null) entityManager.AddComponent<AgentHasAfterControlOverride>(change.entity);
				else entityManager.RemoveComponent<AgentHasAfterControlOverride>(change.entity);
				break;
			}
			case MovementOverrideRunner.Phase.BeforeMovement: {
				var remaining = (BeforeMovementDelegate)Recombine(entry.beforeMovement, ref change);
				AgentManagedStorage.SetBeforeMovement(slot, change.entity, remaining);
				if (remaining != null) entityManager.AddComponent<AgentHasBeforeMovementOverride>(change.entity);
				else entityManager.RemoveComponent<AgentHasBeforeMovementOverride>(change.entity);
				break;
			}
			}
			return true;
		}

		static System.Delegate Recombine (System.Delegate existing, ref Change change) {
			return change.add ? System.Delegate.Combine(existing, change.callback) : System.Delegate.Remove(existing, change.callback);
		}
	}
}
#endif
