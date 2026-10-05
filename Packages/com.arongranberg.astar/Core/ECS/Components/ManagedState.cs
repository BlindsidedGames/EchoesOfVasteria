#if MODULE_ENTITIES
namespace Pathfinding.ECS {
	using Pathfinding;

	/// <summary>
	/// Runtime state for agent movement that requires managed types.
	///
	/// The Unity ECS in general wants everything in components to be unmanaged types.
	/// However, some things cannot be unmanaged types, for example delegates, interfaces and objects.
	/// There are also other things like path references and node references which are not unmanaged types at the moment.
	///
	/// This class is used to store those things.
	///
	/// It is created at runtime and does not store any settings that need to be persistent.
	///
	/// This is not a component. Each agent reaches its instance through an <see cref="AgentManagedRef"/>
	/// component, which indexes <see cref="AgentManagedStorage"/>.
	///
	/// See: <see cref="FollowerEntity"/>
	/// </summary>
	public class ManagedState : System.IDisposable, System.ICloneable {
		/// <summary>Calculates in which direction to move to follow the path</summary>
		public PathTracer pathTracer;

		/// <summary>Path that is being calculated, if any</summary>
		// Do not create a property visitor for this field, as otherwise the ECS infrastructure will try to patch entities inside it, and get very confused.
		// I haven't been able to replicate this issue recently, but it has caused problems in the past.
		// [Unity.Properties.DontCreateProperty]
		public Path pendingPath { get; private set; }

		/// <summary>
		/// Path that is being followed, if any.
		///
		/// The agent may have moved away from this path since it was calculated. So it may not be up to date.
		/// </summary>
		// Do not create a property visitor for this field, as otherwise the ECS infrastructure will try to patch entities inside it, and get very confused.
		// [Unity.Properties.DontCreateProperty]
		public Path activePath { get; private set; }

		/// <summary>
		/// \copydocref{IAstarAI.SetPath}.
		///
		/// Warning: In almost all cases you should use <see cref="FollowerEntity.SetPath"/> instead of this method.
		/// </summary>
		public static void SetPath (Path path, ManagedState state, in AgentMovementPlane movementPlane, ref DestinationPoint destination) {
			if (path == null) {
				state.CancelCurrentPathRequest();
				state.ClearPath();
			} else if (path.PipelineState == PathState.Created) {
				// Path has not started calculation yet
				state.CancelCurrentPathRequest();
				state.pendingPath = path;
				path.Claim(state);
				AstarPath.StartPath(path);
			} else if (path.PipelineState >= PathState.ReturnQueue) {
				// Path has already been calculated

				if (state.pendingPath == path) {
					// The pending path is now obviously no longer pending
					state.pendingPath = null;
				} else {
					// We might be calculating another path at the same time, and we don't want that path to override this one. So cancel it.
					state.CancelCurrentPathRequest();

					// Increase the refcount on the path.
					// If the path was already our pending path, then the refcount will have already been incremented
					path.Claim(state);
				}

				var abPath = path as ABPath;
				if (abPath == null) throw new System.ArgumentException("This function only works with ABPaths, or paths inheriting from ABPath");

				if (!abPath.error) {
					try {
						state.pathTracer.SetPath(abPath, movementPlane.value);

						// Release the previous path back to the pool, to reduce GC pressure
						if (state.activePath != null) state.activePath.Release(state);

						state.activePath = abPath;
					} catch (System.Exception e) {
						// If the path was so invalid that the path tracer throws an exception, then we should not use it.
						abPath.Release(state);
						state.ClearPath();
						UnityEngine.Debug.LogException(e);
					}

					// If a RandomPath or MultiTargetPath have just been calculated, then we need
					// to patch our destination point, to ensure the agent continues to move towards the end of the path.
					// For these path types, the end point of the path is not known before the calculation starts.
					if (!abPath.endPointKnownBeforeCalculation) {
						destination = new DestinationPoint { destination = abPath.originalEndPoint, facingDirection = default };
					}

					// Right now, the pathTracer is almost fully up to date.
					// To make it fully up to date, we'd also have to call pathTracer.UpdateStart and pathTracer.UpdateEnd after this function.
					// During normal path recalculations, the JobRepairPath will be scheduled right after this function, and it will
					// call those functions. The incomplete state will not be observable outside the system.
					// When called from FollowerEntity, the SetPath method on that component will ensure that these methods are called.
				} else {
					// The path had an error, so we should stop moving.
					// However, we keep the agent anchored to the graph it was traversing before (if any).
					// Marking the path as unrepairable prevents the agent from immediately trying to locally repair its path,
					// which may give incorrect results, or just use up a ton of cpu power.
					// The behavior of the agent will be similar to if ai.isStopped is set: the agent will smoothly slow down until stationary.
					// The next time a path is successfully calculated, the unrepairable state will be cleared.
					abPath.Release(state);
					state.pathTracer.SetEmptyAndUnrepairable();
				}
			} else {
				// Path calculation has been started, but it is not yet complete. Cannot really handle this.
				throw new System.ArgumentException("You must call the SetPath method with a path that either has been completely calculated or one whose path calculation has not been started at all. It looks like the path calculation for the path you tried to use has been started, but is not yet finished.");
			}
		}

		public void ClearPath () {
			pathTracer.Clear();
			if (activePath != null) {
				activePath.Release(this);
				activePath = null;
			}
		}

		public void CancelCurrentPathRequest () {
			if (pendingPath != null) {
				pendingPath.FailWithError("Canceled by script");
				pendingPath.Release(this);
				pendingPath = null;
			}
		}

		public void Dispose () {
			pathTracer.Dispose();
			if (pendingPath != null) {
				pendingPath.FailWithError("Canceled because entity was destroyed");
				pendingPath.Release(this);
				pendingPath = null;
			}
			if (activePath != null) {
				activePath.Release(this);
				activePath = null;
			}
		}

		/// <summary>
		/// Pops the current part, and the next part from the start of the path.
		///
		/// Called when an agent finishes traversing an off-mesh link, to make it start following the part
		/// of the path that comes after the link.
		///
		/// Invariant: an agent's path keeps the off-mesh link it is traversing as its second path part for the
		/// whole traversal. Nothing may replace or truncate the path in between, which is why
		/// <see cref="FollowerEntity.SetPath"/>, <see cref="FollowerEntity.destination"/> and
		/// <see cref="FollowerEntity.position"/> all leave the path alone while the agent is on a link.
		/// Throws if the invariant has been broken.
		/// </summary>
		public void PopNextLinkFromPath () {
			if (pathTracer.partCount < 2 || pathTracer.GetPartType(1) != Funnel.PartType.OffMeshLink) {
				throw new System.InvalidOperationException("The next part in the path is not an off-mesh link.");
			}
			pathTracer.PopParts(2);
		}

		/// <summary>
		/// Clones the managed state for when an entity is duplicated.
		///
		/// Some fields are cleared instead of being cloned, such as the pending path,
		/// which cannot reasonably be cloned.
		/// </summary>
		object System.ICloneable.Clone () {
			return new ManagedState {
					   pathTracer = pathTracer.Clone(),
					   pendingPath = null,  // Cannot be safely cloned or copied
					   activePath = null,  // Cannot be safely cloned or copied
			};
		}
	}
}
#endif
