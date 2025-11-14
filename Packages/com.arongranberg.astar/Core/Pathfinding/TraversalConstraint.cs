using System.Runtime.CompilerServices;

namespace Pathfinding {
	/// <summary>
	/// Specifies which nodes are traversable and which are not.
	///
	/// This struct is used to specify which nodes are traversable when searching for paths and when doing linecasts on graphs, among other things.
	///
	/// It can be used to filter out nodes based on their tags, or even using completely custom filters.
	///
	/// Note: For performance reasons, this struct is often passed by reference (using the ref keyword). It's a moderately large struct, and it is often used in performance-critical code.
	/// Passing it by reference avoids copying the struct, and gives us some moderate performance improvements.
	///
	/// <code>
	/// // Custom filter
	/// var traversalConstraint = new TraversalConstraint((GraphNode node) => ((Vector3)node.position).y > 50);
	///
	/// traversalConstraint.tags = 1 << 3; // Only allow traversing nodes with tag 3
	///
	/// // Convert to nearest node constraint
	/// var nearestNodeConstraint = traversalConstraint.ToNearestNodeConstraint();
	/// var node = AstarPath.active.GetNearest(transform.position, nearestNodeConstraint).node;
	///
	/// // Check if the node is traversable
	/// traversalConstraint.CanTraverse(node); // True
	/// </code>
	///
	/// <code>
	/// var graph = AstarPath.active.data.recastGraph;
	/// var start = transform.position;
	/// var end = start + Vector3.forward * 10;
	/// var trace = new List<GraphNode>();
	///
	/// var traversalConstraint = TraversalConstraint.None;
	/// traversalConstraint.tags = 1 << 3; // Only allow traversing nodes with tag 3
	///
	/// if (graph.Linecast(start, end, out GraphHitInfo hit, ref traversalConstraint, trace)) {
	///     Debug.Log("Linecast traversed " + trace.Count + " nodes before hitting an obstacle");
	///     Debug.DrawLine(start, hit.point, Color.red);
	///     Debug.DrawLine(hit.point, end, Color.blue);
	/// } else {
	///     Debug.Log("Linecast traversed " + trace.Count + " nodes");
	///     Debug.DrawLine(start, end, Color.green);
	/// }
	/// </code>
	///
	/// <code>
	/// ABPath path = ABPath.Construct(currentPosition, destination, null);
	///
	/// path.traversalConstraint.traversalProvider = GridShapeTraversalProvider.SquareShape(3);
	///
	/// // If you are writing your own movement script
	/// seeker.StartPath(path);
	///
	/// // If you are using an existing movement script (you may also want to set ai.canSearch to false)
	/// // ai.SetPath(path);
	/// </code>
	///
	/// See: <see cref="Path.traversalConstraint"/>
	/// See: <see cref="IRaycastableGraph"/>
	/// See: <see cref="NearestNodeConstraint"/>
	/// </summary>
	public struct TraversalConstraint {
		internal object filterObj;

		/// <summary>
		/// Set of tags which are traversable.
		///
		/// This is a bitmask, i.e bit 0 indicates that tag 0 is traversable, bit 3 indicates tag 3 is traversable etc.
		///
		/// By default all tags are traversable, which corresponds to the value -1.
		///
		/// <code>
		/// var traversalConstraint = TraversalConstraint.None;
		///
		/// // Allows the first and third tags to be searched, but not the rest
		/// traversalConstraint.tags = (1 << 0) | (1 << 2);
		///
		/// // Allows the two named tags to be searched, but not the rest
		/// traversalConstraint.tags = PathfindingTag.FromName("Grass").ToMask() | PathfindingTag.FromName("Lava").ToMask();
		/// </code>
		///
		/// If you are using the Seeker or FollowerEntity components, you can access this in the inspector via the tags foldout:
		/// [Open online documentation to see images]
		///
		/// See: <see cref="graphMask"/>
		/// See: tags (view in online documentation for working links)
		/// See: bitmasks (view in online documentation for working links)
		/// See: <see cref="PathfindingTag.ToMask"/>
		/// </summary>
		public int tags;

		/// <summary>
		/// %Graphs which are traversable.
		///
		/// This is a bitmask, meaning that bit 0 specifies whether or not the first graph in the graphs list should be included in the search,
		/// bit 1 specifies whether or not the second graph should be included, and so on.
		///
		/// <code>
		/// var traversalConstraint = TraversalConstraint.None;
		///
		/// // Allows the first and fourth graphs to be searched, but not the rest
		/// traversalConstraint.graphMask = GraphMask.FromGraphIndex(0) | GraphMask.FromGraphIndex(3);
		///
		/// // Allows the two named graphs to be searched, but not the rest
		/// traversalConstraint.graphMask = GraphMask.FromGraphName("My Custom Graph Name") | GraphMask.FromGraphName("My Other Graph");
		/// </code>
		///
		/// Note: When used for pathfinding, this does only affects which nodes the path starts and ends on. If a valid graph is connected to an "invalid" graph, it may get searched anyway.
		///
		/// See: <see cref="AstarPath.GetNearest"/>
		/// See: <see cref="GraphMask"/>
		/// See: bitmasks (view in online documentation for working links)
		/// </summary>
		public GraphMask graphMask;
		internal FilterType filterType;

		internal enum FilterType {
			None,
			TraversalProvider,
			Func,
		}

		/// <summary>
		/// Allow traversing only nodes that are also traversable by the given <see cref="ITraversalProvider"/>.
		///
		/// The other constraints on this struct will still apply, in addition to the ITraversalProvider.
		///
		/// Note: Either an ITraversalProvider or a callback filter (<see cref="filter"/>) can be set, but not both.
		/// Setting this will clear <see cref="filter"/>, and vice versa.
		///
		/// See: <see cref="ITraversalProvider"/>
		/// See: traversal_provider (view in online documentation for working links)
		/// See: <see cref="filter"/>
		/// </summary>
		public ITraversalProvider traversalProvider {
			get => filterObj as ITraversalProvider;
			set {
				filterObj = value;
				filterType = value != null ? FilterType.TraversalProvider : FilterType.None;
			}
		}

		/// <summary>
		/// Allow traversing only nodes which the given filter function returns true for.
		///
		/// The other constraints on this struct will still apply, in addition to the filter function.
		///
		/// Note: Either an ITraversalProvider (<see cref="traversalProvider)"/> or a filter function can be set, but not both.
		/// Setting this will clear <see cref="traversalProvider"/>, and vice versa.
		///
		/// <code>
		/// var traversalConstraint = TraversalConstraint.None;
		///
		/// // Set a custom filter. This can be an arbitrary function which takes a GraphNode and returns true or false.
		/// traversalConstraint.filter = (GraphNode node) => ((Vector3)node.position).y > 50;
		///
		/// // Search for a path which only traverses nodes above y=50
		/// var path = ABPath.Construct(transform.position, transform.position + Vector3.forward * 10);
		/// path.traversalConstraint = traversalConstraint;
		/// AstarPath.StartPath(path);
		/// </code>
		///
		/// See: <see cref="traversalProvider"/>
		/// </summary>
		public System.Func<GraphNode, bool> filter {
			get => filterObj as System.Func<GraphNode, bool>;
			set {
				filterObj = value;
				filterType = value != null ? FilterType.Func : FilterType.None;
			}
		}

		/// <summary>
		/// Traversal constraint which allows all (walkable) nodes on all graphs to be traversed.
		///
		/// Note: Unwalkable (<see cref="GraphNode.Walkable"/>) nodes can never be traversed, regardless of the settings in this struct.
		///
		/// The returned struct's <see cref="CanTraverse(GraphNode)"/> method will return true for all nodes except those with the <see cref="GraphNode.Walkable"/> property set to false.
		/// </summary>
		public static readonly TraversalConstraint None = new TraversalConstraint {
			tags = ~0,
			graphMask = GraphMask.everything
		};

		/// <summary>
		/// Filter diagonal connections on grid graphs using <see cref="GridGraph.cutCorners"/> for effects applied by any assigned ITraversalProvider.
		/// If a traversal provider is used, returns <see cref="ITraversalProvider.filterDiagonalGridConnections"/> for it.
		/// Otherwise returns true.
		///
		/// See: <see cref="ITraversalProvider.filterDiagonalGridConnections"/>
		/// </summary>
		public bool filterDiagonalGridConnections => filterType != FilterType.TraversalProvider || Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<System.Object, ITraversalProvider>(ref filterObj).filterDiagonalGridConnections;

		/// <summary>
		/// Create a new traversal constraint with the given filter function.
		///
		/// See: <see cref="filter"/>
		/// </summary>
		public TraversalConstraint(System.Func<GraphNode, bool> filter) {
			this = None;
			this.filter = filter;
		}

		/// <summary>
		/// Create a new traversal constraint using the given <see cref="ITraversalProvider"/>.
		///
		/// See: <see cref="traversalProvider"/>
		/// </summary>
		public TraversalConstraint(ITraversalProvider traversalProvider) {
			this = None;
			this.traversalProvider = traversalProvider;
		}

		/// <summary>
		/// True if the node is traversable with respect to the constraints set in this struct.
		///
		/// Note: The <see cref="graphMask"/> is not checked. It is assumed that graph filtering is done before this method is called.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanTraverse (GraphNode node) {
			if (!CanTraverseSkipUserFilter(node)) return false;

			switch (filterType) {
			case FilterType.TraversalProvider:
				// This is a hot method, so we want to avoid all checks that we possibly can.
				// This cast is guaranteed to succeed, so we can skip the check using an unsafe cast.
				return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<System.Object, ITraversalProvider>(ref filterObj).CanTraverse(ref this, node);
			case FilterType.Func:
				return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<System.Object, System.Func<GraphNode, bool> >(ref filterObj)(node);
			default:
				return true;
			}
		}

		/// <summary>
		/// True if the connection between from and to is traversable with respect to the constraints set in this struct.
		///
		/// This is identical to CanTraverse(to), unless an ITraversalProvider is used, which may apply different rules for traversal between two nodes.
		///
		/// Note: It is assumed that the from node is traversable, since the agent came from there. The rules are not checked against it for performance reasons.
		///
		/// Note: The <see cref="graphMask"/> is not checked. It is assumed that graph filtering is done before this method is called.
		///
		/// See: <see cref="ITraversalProvider"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanTraverse (GraphNode from, GraphNode to) {
			if (!CanTraverseSkipUserFilter(to)) return false;

			switch (filterType) {
			case FilterType.TraversalProvider:
				// This is a hot method, so we want to avoid all checks that we possibly can.
				// This cast is guaranteed to succeed, so we can skip the check using an unsafe cast.
				return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<System.Object, ITraversalProvider>(ref filterObj).CanTraverse(ref this, from, to);
			case FilterType.Func:
				return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<System.Object, System.Func<GraphNode, bool> >(ref filterObj)(to);
			default:
				return true;
			}
		}

		/// <summary>
		/// True if the node is traversable with respect to the constraints set in this struct, but without checking the <see cref="filter"/> or the <see cref="traversalProvider"/>.
		/// This method can be used from the <see cref="ITraversalProvider"/> to check additional nodes without causing infinite recursion.
		///
		/// Note: This method does not check the <see cref="filter"/> or the <see cref="traversalProvider"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanTraverseSkipUserFilter(GraphNode node) => node.Walkable && ((tags >> (int)node.Tag & 0x1) != 0);

		/// <summary>
		/// A nearest node constraint which uses the same settings as this traversal constraint.
		///
		/// <code>
		/// var nearestNodeConstraint = traversalConstraint.ToNearestNodeConstraint();
		///
		/// // Use the nearest node constraint to find the closest node
		/// var nearest = AstarPath.active.GetNearest(transform.position, nearestNodeConstraint);
		/// </code>
		///
		/// See: <see cref="AstarPath.GetNearest"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public NearestNodeConstraint ToNearestNodeConstraint () {
			return new NearestNodeConstraint {
					   traversal = this,
					   distanceMetric = DistanceMetric.Euclidean,
					   area = -1,
					   maxDistanceSqr = -1,
					   walkable = NearestNodeConstraint.WalkabilityConstraint.Walkable
			};
		}
	}
}
