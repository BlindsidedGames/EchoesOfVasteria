using System.Runtime.CompilerServices;

namespace Pathfinding {
	/// <summary>
	/// Constraint for the <see cref="AstarPath.GetNearest"/> method.
	/// This is used to filter out nodes you are not interested in.
	///
	/// For example, you can use this to only find walkable nodes, or only nodes with a specific tag.
	///
	/// See: <see cref="AstarPath.GetNearest"/>
	///
	/// <b>Why is this struct passed around with the ref keyword all the time?</b>
	///
	/// Structs are passed by value in C#, and copying them can be expensive when they are large (like this one at around 44 bytes).
	/// Using the ref keyword allows it to be passed by reference instead, which is much faster.
	///
	/// <b>But then why is it not a class?</b>
	///
	/// Firstly, because classes involve the garbage collector, and since this struct is created very often (often many times per frame), we want to avoid that.
	/// Garbage collections are very expensive operations, and can cause stuttering in the game.
	/// </summary>
	public struct NearestNodeConstraint {
		/// <summary>
		/// Determines how to measure distances to the navmesh.
		///
		/// The default is a euclidean distance, which works well for most things.
		///
		/// See: <see cref="DistanceMetric"/>
		/// </summary>
		public DistanceMetric distanceMetric;

		/// <summary>
		/// Area ID to constrain to.
		/// Will not affect anything if negative.
		///
		/// See: <see cref="GraphNode.Area"/>
		/// </summary>
		public int area;

		/// <summary>
		/// Squared <see cref="maxDistance"/>.
		/// Negative value means to use the default A* limit (<see cref="AstarPath.maxNearestNodeDistance"/>).
		/// Positive value means to use a custom limit.
		///
		/// Typically you'll modify <see cref="maxDistance"/> instead of this.
		///
		/// See: <see cref="maxDistance"/>
		/// </summary>
		public float maxDistanceSqr;

		/// <summary>Sub-filter with more constraints</summary>
		internal TraversalConstraint traversal;

		/// <summary>
		/// Constraint to only search only walkable nodes, only unwalkable nodes, or both.
		/// See: <see cref="GraphNode.Walkable"/>
		/// </summary>
		public WalkabilityConstraint walkable;

		/// <summary>\copydocref{TraversalConstraint::tags}</summary>
		public int tags {
			get => traversal.tags;
			set => traversal.tags = value;
		}

		/// <summary>\copydocref{TraversalConstraint::graphMask}</summary>
		public GraphMask graphMask {
			get => traversal.graphMask;
			set => traversal.graphMask = value;
		}

		/// <summary>
		/// Allow only nodes which the given filter function returns true for.
		///
		/// The other constraints on this struct will still apply, in addition to the filter function.
		///
		/// Note: Either an ITraversalProvider (<see cref="traversalProvider)"/> or a filter function can be set, but not both.
		/// Setting this will clear <see cref="traversalProvider"/>, and vice versa.
		///
		/// <code>
		/// var nearestNodeConstraint = NearestNodeConstraint.None;
		///
		/// // Set a custom filter. This can be an arbitrary function which takes a GraphNode and returns true or false.
		/// nearestNodeConstraint.filter = (GraphNode node) => ((Vector3)node.position).y > 50;
		///
		/// // Find the closest node to our position which is above y=50
		/// var nearest = AstarPath.active.GetNearest(transform.position, nearestNodeConstraint);
		/// </code>
		///
		/// See: <see cref="traversalProvider"/>
		/// </summary>
		public System.Func<GraphNode, bool> filter {
			get => traversal.filter;
			set => traversal.filter = value;
		}

		/// <summary>\copydocref{TraversalConstraint::traversalProvider}</summary>
		public ITraversalProvider traversalProvider {
			get => traversal.traversalProvider;
			set => traversal.traversalProvider = value;
		}

		/// <summary>
		/// True if all nodes are suitable with respect to the constraints set in this struct.
		/// The <see cref="Suitable"/> method will return true for all nodes if this is true.
		/// </summary>
		public bool allNodesAreSuitable => area < 0 && tags == ~0 && graphMask.containsAllGraphs && walkable == WalkabilityConstraint.DontCare && traversal.filterType == TraversalConstraint.FilterType.None;

		/// <summary>
		/// The maximum distance to the navmesh to search before the search fails.
		/// This is the maximum distance from the query point to the closest point on the navmesh.
		/// If null (the default), the default limit set on the <see cref="AstarPath"/> component will be used (<see cref="AstarPath.maxNearestNodeDistance"/>).
		///
		/// Positive infinity is a valid value, meaning it will search for the closest node regardless of distance.
		/// 0 is also a valid value, but it usually only makes sense with a different distance metric than the default.
		///
		/// Note: The distance depends on the distance metric used.
		///
		/// See: <see cref="AstarPath::maxNearestNodeDistance"/>
		/// See: <see cref="distanceMetric"/>
		/// </summary>
		public float? maxDistance {
			get => maxDistanceSqr < 0 ? null : UnityEngine.Mathf.Sqrt(maxDistanceSqr);
			set => maxDistanceSqr = value.HasValue ? value.Value * value.Value : -1;
		}

		public enum WalkabilityConstraint : byte {
			Walkable,
			Unwalkable,
			DontCare,
		}

		internal float maxDistanceSqrOrDefault (AstarPath astar) {
			return maxDistanceSqr < 0 ? astar.maxNearestNodeDistanceSqr : maxDistanceSqr;
		}

		/// <summary>True if the node is ok with respect to the constraints set in this struct</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Suitable (GraphNode node) {
			if (area >= 0 && node.Area != area) return false;
			if (node.Walkable) {
				if (walkable == WalkabilityConstraint.Unwalkable) return false;
			} else {
				if (walkable == WalkabilityConstraint.Walkable) return false;
			}

			// The following part is the same as traversal.CanTraverse(node),
			// but it doesn't require that the node is walkable.
			// When searching for the nearest node, we allow that check to be customized (see #walkable).

			if ((traversal.tags >> (int)node.Tag & 0x1) == 0) return false;

			switch (traversal.filterType) {
			case TraversalConstraint.FilterType.TraversalProvider:
				// This is a hot method, so we want to avoid all checks that we possibly can.
				// This cast is guaranteed to succeed, so we can skip the check using an unsafe cast.
				return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<System.Object, ITraversalProvider>(ref traversal.filterObj).CanTraverse(ref traversal, node);
			case TraversalConstraint.FilterType.Func:
				return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<System.Object, System.Func<GraphNode, bool> >(ref traversal.filterObj)(node);
			default:
				return true;
			}
		}

		/// <summary>Constraint which filters out all unwalkable nodes</summary>
		public static readonly NearestNodeConstraint Walkable = new NearestNodeConstraint {
			traversal = TraversalConstraint.None,
			distanceMetric = DistanceMetric.Euclidean,
			area = -1,
			maxDistanceSqr = -1,
			walkable = WalkabilityConstraint.Walkable,
		};

		/// <summary>Constraint which allows all nodes</summary>
		public static readonly NearestNodeConstraint None = new NearestNodeConstraint {
			traversal = TraversalConstraint.None,
			distanceMetric = DistanceMetric.Euclidean,
			area = -1,
			maxDistanceSqr = -1,
			walkable = WalkabilityConstraint.DontCare,
		};
	}
}
