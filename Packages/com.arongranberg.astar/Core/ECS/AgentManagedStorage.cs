#if MODULE_ENTITIES
using Unity.Entities;

namespace Pathfinding.ECS {
	/// <summary>
	/// Handle to an agent's managed data.
	///
	/// Agents need to store some data that cannot live in an unmanaged component, such as the agent's
	/// current path and its traversal provider. That data lives in <see cref="AgentManagedStorage"/>, and this
	/// component is the agent's handle to it.
	///
	/// This is a cleanup component, which gives it two properties that the whole design rests on:
	///
	/// - It is never copied when an entity is cloned. A clone starts out without it, and only gains it once
	///   <see cref="AgentManagedDataRepairSystem"/> has given the clone its own copy of the data. Until then the
	///   clone is not a usable agent, and its lack of this component is what keeps it out of every query.
	/// - It survives entity destruction, so <see cref="AgentManagedDataCleanupSystem"/> can still reach the slot
	///   and release the agent's data after the entity is destroyed.
	///
	/// Guarantee: an entity that has this component owns the slot it holds. Queries that require this
	/// component can read the slot without any further ownership check.
	/// </summary>
	public struct AgentManagedRef : ICleanupComponentData {
		/// <summary>Index into <see cref="AgentManagedStorage"/></summary>
		internal int slot;
	}

	/// <summary>
	/// Copy of the agent's <see cref="AgentManagedRef"/> slot, which lets a clone find the data to clone.
	///
	/// Cleanup components are never copied when an entity is cloned, so <see cref="AgentManagedRef"/> alone
	/// would leave a clone with no link back to its source's data. This ordinary component is copied
	/// verbatim, and is the clone's only pointer to the data <see cref="AgentManagedDataRepairSystem"/> should
	/// clone for it.
	///
	/// Only agent initialization and <see cref="AgentManagedDataRepairSystem"/> read or write this component.
	/// Everything else reads the slot from <see cref="AgentManagedRef"/>, whose presence also proves ownership.
	/// </summary>
	public struct AgentManagedBackupRef : IComponentData {
		/// <summary>Index into <see cref="AgentManagedStorage"/>. Always equal to the agent's <see cref="AgentManagedRef"/> slot once repaired.</summary>
		internal int slot;
	}

	/// <summary>
	/// Marks an agent that has a movement override callback registered. Zero-sized.
	///
	/// The callbacks themselves live in <see cref="AgentManagedStorage"/>. This component exists so that the
	/// jobs which invoke them can select the few agents that have one with an ordinary query.
	///
	/// See: <see cref="ManagedMovementOverrides"/>
	/// </summary>
	public struct AgentHasBeforeControlOverride : IComponentData {}

	/// <summary>\copydocref{AgentHasBeforeControlOverride}</summary>
	public struct AgentHasAfterControlOverride : IComponentData {}

	/// <summary>\copydocref{AgentHasBeforeControlOverride}</summary>
	public struct AgentHasBeforeMovementOverride : IComponentData {}

	/// <summary>
	/// Storage for per-agent data that requires managed types.
	///
	/// The Unity ECS wants component data to be unmanaged, but an agent needs to keep hold of things that
	/// cannot be: its current path, a traversal provider, user callbacks. Historically these lived in
	/// class-based IComponentData components, but those are being removed from the Entities package.
	///
	/// Instead each agent owns one slot in a flat array here, and refers to it through an
	/// <see cref="AgentManagedRef"/> component holding nothing but an integer.
	///
	/// # Threading
	///
	/// Reads take a single snapshot of <see cref="entries"/> and index it. Growth allocates a larger array, copies, and
	/// publishes the new reference, so a reader racing with growth keeps using the pre-growth array; that
	/// array stays alive and every slot it already contained still holds the right data.
	///
	/// Invariant: every write to an entry goes through one of the locked methods here, never through a
	/// snapshot the caller is holding. Growth copies entry payloads rather than aliasing them, so a write
	/// through a snapshot that growth has already superseded lands in an array nobody reads any more and is
	/// silently lost. Taking the lock re-reads <see cref="entries"/>, which is what makes a write always land in the live
	/// array.
	/// </summary>
	internal static class AgentManagedStorage {
		/// <summary>
		/// All managed data for a single agent.
		///
		/// A struct rather than a class so that <see cref="entries"/> is one allocation instead of one per agent, and so
		/// the fields sit contiguously. Optional fields are null when the agent does not use them.
		/// </summary>
		internal struct Entry {
			/// <summary>
			/// The agent this data belongs to, or Entity.Null if the slot is free.
			///
			/// Stored as a real Entity, which is only safe because this array is not chunk memory.
			/// EntityRemapUtility rewrites Entity-typed fields inside components when an entity is cloned,
			/// so this same field inside <see cref="AgentManagedRef"/> would be silently repointed at the clone.
			/// </summary>
			internal Entity owner;
			internal ManagedState state;
			internal ManagedSettings settings;
			internal BeforeControlDelegate beforeControl;
			internal AfterControlDelegate afterControl;
			internal BeforeMovementDelegate beforeMovement;
			internal ManagedAgentOffMeshLinkTraversal linkTraversal;
		}

		/// <summary>
		/// Slot storage. Index with the slot from an <see cref="AgentManagedRef"/>.
		///
		/// Read this field exactly once per operation and index the local copy. Re-reading it partway
		/// through means a growth in between can move you to a different array.
		///
		/// Every slot handed out is in range for every array published from then on, so the methods below
		/// index it directly and leave the range check to the array. An out of range slot means the
		/// <see cref="AgentManagedRef"/> did not come from here.
		/// </summary>
		internal static Entry[] entries = new Entry[0];

		/// <summary>Slots that have been freed and can be handed out again. Guarded by <see cref="mutateLock"/>.</summary>
		static int[] freeSlots = new int[0];
		static int freeSlotCount;

		/// <summary>Number of slots in <see cref="entries"/> that have ever been handed out. Guarded by <see cref="mutateLock"/>.</summary>
		static int usedSlots;

		/// <summary>
		/// Guards allocation and release. Reads are lock-free.
		///
		/// Serializes writers against each other, which is what makes the growth in <see cref="AllocateSlotLocked"/> safe:
		/// two concurrent growths would each copy the old array, and one would discard the other's slots.
		/// </summary>
		static readonly object mutateLock = new object();

		/// <summary>
		/// Allocates a slot for a newly created agent.
		///
		/// The caller must add an <see cref="AgentManagedRef"/> and an <see cref="AgentManagedBackupRef"/> component
		/// holding the returned slot to the entity.
		/// </summary>
		internal static int Allocate (Entity owner, ManagedState state, ManagedSettings settings) {
			lock (mutateLock) {
				var slot = AllocateSlotLocked();
				entries[slot] = new Entry {
					owner = owner,
					state = state,
					settings = settings,
				};
				return slot;
			}
		}

		/// <summary>
		/// Gives a cloned agent its own copy of the managed data it currently shares with sourceSlot.
		///
		/// Clones what a new agent can reasonably re-use, and drops everything else.
		///
		/// - Path: cloned
		/// - Callback hooks: dropped
		/// - ITraversalProvider: dropped
		///
		/// Returns: The clone's own slot, or -1 if sourceSlot holds nothing worth cloning, in which case
		/// the caller must allocate a fresh entry instead.
		///
		/// The entity owning sourceSlot may already have been destroyed, since a
		/// destroyed agent's entry stays intact until <see cref="AgentManagedDataCleanupSystem"/> releases it,
		/// cloning from it is still correct. What this cannot detect is the source having been released
		/// *and* the slot handed to an unrelated agent, which is why
		/// <see cref="AgentManagedDataRepairSystem"/> must run before that cleanup system.
		/// </summary>
		internal static int CloneFrom (int sourceSlot, Entity newOwner) {
			lock (mutateLock) {
				// Read after taking the lock: a concurrent growth would otherwise move the array under us.
				var table = entries;
				var source = table[sourceSlot];
				if (source.owner == Entity.Null || source.state == null) return -1;

				var slot = AllocateSlotLocked();
				entries[slot] = new Entry {
					owner = newOwner,
					state = (ManagedState)((System.ICloneable)source.state).Clone(),
					settings = source.settings?.CloneAndSimplifyDefaults(false),
					// Callbacks and link traversal state are deliberately dropped. A delegate registered
					// for one agent is not meaningful for another, and an in-progress off-mesh link
					// traversal cannot be resumed by a second agent.
				};
				return slot;
			}
		}

		/// <summary>
		/// Releases a slot and disposes the data in it.
		///
		/// Passing the expected owner makes this a no-op when the slot has already been recycled to a
		/// different agent, so a stale slot reference cannot free live data.
		/// </summary>
		internal static void Free (int slot, Entity expectedOwner) {
			lock (mutateLock) {
				var table = entries;
				if (table[slot].owner != expectedOwner) return;

				table[slot].state?.Dispose();
				table[slot] = default;

				if (freeSlotCount == freeSlots.Length) {
					System.Array.Resize(ref freeSlots, System.Math.Max(16, freeSlots.Length*2));
				}
				freeSlots[freeSlotCount++] = slot;
			}
		}

		/// <summary>
		/// Takes a slot off the free list, growing <see cref="entries"/> if the list is empty.
		///
		/// Must be called while holding <see cref="mutateLock"/>.
		/// </summary>
		static int AllocateSlotLocked () {
			if (freeSlotCount > 0) return freeSlots[--freeSlotCount];

			if (usedSlots == entries.Length) {
				var grown = new Entry[System.Math.Max(16, entries.Length*2)];
				System.Array.Copy(entries, grown, entries.Length);
				// Publish only once fully populated, so a concurrent reader sees either the old array or a
				// complete new one, never a half-copied one.
				System.Threading.Volatile.Write(ref entries, grown);
			}
			return usedSlots++;
		}

		/// <summary>
		/// Destroys an agent entity, releasing its managed data first so that it dies immediately.
		///
		/// DestroyEntity on its own leaves the entity alive until <see cref="AgentManagedDataCleanupSystem"/>
		/// removes its <see cref="AgentManagedRef"/>, because that is a cleanup component. The entity would
		/// still be observable through EntityManager.Exists for the rest of the frame, with every other
		/// component already stripped. A* destroys its own agents through here so that destruction stays
		/// immediate, as it was before the managed data moved out of components.
		/// </summary>
		internal static void DestroyAgent (EntityManager entityManager, Entity entity) {
			if (!entityManager.Exists(entity)) return;

			if (entityManager.HasComponent<AgentManagedRef>(entity)) {
				var slot = entityManager.GetComponentData<AgentManagedRef>(entity).slot;

				// Removing the cleanup components below means JobManagedOffMeshLinkTransitionCleanup will
				// never see this entity, so tell the state machine its traversal was aborted here instead.
				if (entityManager.HasComponent<AgentOffMeshLinkTraversalCleanup>(entity)) {
					if (TryGet(slot, entity, out var entry) && entry.linkTraversal?.stateMachine != null) {
						entry.linkTraversal.stateMachine.OnAbortTraversingOffMeshLink();
					}
					entityManager.RemoveComponent<AgentOffMeshLinkTraversalCleanup>(entity);
				}

				Free(slot, entity);
				entityManager.RemoveComponent<AgentManagedRef>(entity);
			}

			entityManager.DestroyEntity(entity);
		}

		/// <summary>
		/// Returns the entry belonging to owner.
		///
		/// Uses ref returns for performance, since readers typically only want one field.
		///
		/// Throws: System.InvalidOperationException If the slot does not belong to owner. In practice
		/// that means the slot was read from an <see cref="AgentManagedBackupRef"/> of a cloned entity whose
		/// managed data has not been repaired yet.
		/// </summary>
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		internal static ref readonly Entry GetChecked (int slot, Entity owner) {
			// See #entries for why the slot is not range checked here.
			// One read of the field, then index the local copy, so a concurrent growth cannot move us.
			var table = entries;
			if (table[slot].owner == owner) return ref table[slot];
			throw NotOwnerException(owner);
		}

		/// <summary>
		/// Reads the entry belonging to owner without throwing.
		///
		/// Returns: False if the slot does not belong to owner.
		/// </summary>
		internal static bool TryGet (int slot, Entity owner, out Entry entry) {
			var table = entries;
			if (table[slot].owner == owner) {
				entry = table[slot];
				return true;
			}
			entry = default;
			return false;
		}

		internal static void SetSettings (int slot, Entity owner, ManagedSettings value) {
			lock (mutateLock) {
				var table = entries;
				if (table[slot].owner != owner) throw NotOwnerException(owner);
				table[slot].settings = value;
			}
		}

		internal static void SetLinkTraversal (int slot, Entity owner, ManagedAgentOffMeshLinkTraversal value) {
			lock (mutateLock) {
				var table = entries;
				if (table[slot].owner != owner) throw NotOwnerException(owner);
				table[slot].linkTraversal = value;
			}
		}

		internal static void SetBeforeControl (int slot, Entity owner, BeforeControlDelegate value) {
			lock (mutateLock) {
				var table = entries;
				if (table[slot].owner != owner) throw NotOwnerException(owner);
				table[slot].beforeControl = value;
			}
		}

		internal static void SetAfterControl (int slot, Entity owner, AfterControlDelegate value) {
			lock (mutateLock) {
				var table = entries;
				if (table[slot].owner != owner) throw NotOwnerException(owner);
				table[slot].afterControl = value;
			}
		}

		internal static void SetBeforeMovement (int slot, Entity owner, BeforeMovementDelegate value) {
			lock (mutateLock) {
				var table = entries;
				if (table[slot].owner != owner) throw NotOwnerException(owner);
				table[slot].beforeMovement = value;
			}
		}

		/// <summary>
		/// Explains the one mistake that can produce a slot which does not belong to its entity.
		///
		/// Not gated on ENABLE_UNITY_COLLECTIONS_CHECKS. Silently letting this through would have two agents
		/// share a <see cref="PathTracer"/>, which corrupts both in ways that surface far from the cause.
		/// </summary>
		internal static System.InvalidOperationException NotOwnerException (Entity entity) {
			return new System.InvalidOperationException(
				"The managed data for the entity belongs to a different agent.\n" +
				"This happens when an agent entity is cloned (for example with EntityManager.Instantiate) and then used " +
				"before " + nameof(AgentManagedDataRepairSystem) + " has given the clone its own copy of that data.\n" +
				"Let one frame pass after cloning before touching the new agent, or run " + nameof(AgentManagedDataRepairSystem) + " manually.\n"
				);
		}
	}
}
#endif
