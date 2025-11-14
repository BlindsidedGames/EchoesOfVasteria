using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Pathfinding.Util;

namespace Pathfinding {
	class PathReturnQueue {
		/// <summary>
		/// Holds all paths which are waiting to be flagged as completed.
		/// See: <see cref="ReturnPaths"/>
		/// </summary>
		Queue<Path> pathReturnQueueWriting = new Queue<Path>();
		Queue<Path> pathReturnQueueReading = new Queue<Path>();

		/// <summary>
		/// Paths are claimed silently by some object to prevent them from being recycled while still in use.
		/// This will be set to the AstarPath object.
		/// </summary>
		readonly System.Object pathsClaimedSilentlyBy;

		readonly System.Action OnReturnedPaths;

		public PathReturnQueue (System.Object pathsClaimedSilentlyBy, System.Action OnReturnedPaths) {
			this.pathsClaimedSilentlyBy = pathsClaimedSilentlyBy;
			this.OnReturnedPaths = OnReturnedPaths;
		}

		public void Enqueue (Path path) {
			lock (this) {
				pathReturnQueueWriting.Enqueue(path);
			}
		}

		/// <summary>
		/// Returns all paths in the return stack.
		/// Paths which have been processed are put in the return stack.
		/// This function will pop all items from the stack and return them to e.g the Seeker requesting them.
		/// </summary>
		/// <param name="timeSlice">Do not return all paths at once if it takes a long time, instead return some and wait until the next call.</param>
		public void ReturnPaths (bool timeSlice) {
			Profiler.BeginSample("Calling Path Callbacks");

			// Hard coded limit on 1.0 ms
			long targetTick = timeSlice ? System.DateTime.UtcNow.Ticks + 1 * 10000 : 0;

			int counter = 0;
			int totalReturned = 0;

			// Go through all paths that have been calculated by the pathfinding threads and call their callbacks to indicate that they are calculated
			while (true) {
				if (pathReturnQueueReading.Count == 0) {
					// Swap queues
					lock (this) {
						// We use double-buffering to avoid having to take a lock every single iteration.
						// This way, we only need to take the lock twice per frame.
						Memory.Swap(ref pathReturnQueueReading, ref pathReturnQueueWriting);
					}

					if (pathReturnQueueReading.Count == 0) break;
				}

				// Move to the next path
				Path path = pathReturnQueueReading.Dequeue();

				((IPathInternals)path).AdvanceState(PathState.Returning);

				try {
					// Return the path
					((IPathInternals)path).ReturnPath();
				} catch (System.Exception e) {
					Debug.LogException(e);
				}

				((IPathInternals)path).AdvanceState(PathState.Returned);

				path.Release(pathsClaimedSilentlyBy, true);

				counter++;
				totalReturned++;
				// At least 5 paths will be returned, even if timeSlice is enabled
				if (counter > 5 && timeSlice) {
					counter = 0;
					if (System.DateTime.UtcNow.Ticks >= targetTick) {
						break;
					}
				}
			}

			if (totalReturned > 0) OnReturnedPaths();
			Profiler.EndSample();
		}
	}
}
