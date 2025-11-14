namespace Pathfinding {
	/// <summary>
	/// Helper proxy for handling compatibility with the old NNConstraint API.
	///
	/// Deprecated: Use <see cref="GraphUpdateObject.graphMask"/> instead. This class is deprecated and will be removed in a future version.
	/// </summary>
	public class NNConstraintGraphUpdateObjectProxy {
		readonly GraphUpdateObject graphUpdateObject;

		public NNConstraintGraphUpdateObjectProxy (GraphUpdateObject graphUpdateObject) {
			this.graphUpdateObject = graphUpdateObject;
		}

		public GraphMask graphMask {
			get => graphUpdateObject.graphMask;
			set => graphUpdateObject.graphMask = value;
		}
	}

	/// <summary>
	/// Helper proxy for handling compatibility with the old NNConstraint API.
	/// This class is used to allow the new NearestNodeConstraint API to be used in the same way as the old NNConstraint API.
	///
	/// Deprecated: Use <see cref="NearestNodeConstraint"/> instead. This class is deprecated and will be removed in a future version.
	/// </summary>
	public class NNConstraintPathProxy {
		readonly Path path;
		readonly int pathID;

		internal NNConstraintPathProxy (Path path) {
			this.path = path;
			this.pathID = path.pathID;
		}

		void Validate () {
			if (path.pathID != pathID) {
				throw new System.InvalidOperationException("Trying to change traversability constraint for a path that no longer exists. Check out the migration guide for version 5.4 for more info.");
			}
		}

		public GraphMask graphMask {
			get => path.traversalConstraint.graphMask;
			set {
				Validate();
				path.traversalConstraint.graphMask = value;
			}
		}

		public int tags {
			get => path.traversalConstraint.tags;
			set {
				Validate();
				path.traversalConstraint.tags = value;
			}
		}

		public DistanceMetric distanceMetric {
			get => path.nearestNodeDistanceMetric;
			set {
				Validate();
				path.nearestNodeDistanceMetric = value;
			}
		}
	}

	/// <summary>
	/// Nearest node constraint. Constrains which nodes will be returned by the <see cref="AstarPath.GetNearest"/> function.
	///
	/// Deprecated: Use <see cref="NearestNodeConstraint"/> instead. This class is deprecated and will be removed in a future version.
	/// </summary>
	[System.Obsolete("Use NearestNodeConstraint struct instead. Check the upgrade guide for version 5.4 for more info.")]
	public class NNConstraint {
		/// <summary>
		/// Graphs treated as valid to search on.
		/// This is a bitmask meaning that bit 0 specifies whether or not the first graph in the graphs list should be able to be included in the search,
		/// bit 1 specifies whether or not the second graph should be included and so on.
		/// <code>
		/// // Enables the first and third graphs to be included, but not the rest
		/// myNNConstraint.graphMask = (1 << 0) | (1 << 2);
		/// </code>
		/// <code>
		/// GraphMask mask1 = GraphMask.FromGraphName("My Grid Graph");
		/// GraphMask mask2 = GraphMask.FromGraphName("My Other Grid Graph");
		///
		/// NearestNodeConstraint nn = NearestNodeConstraint.Walkable;
		///
		/// nn.graphMask = mask1 | mask2;
		///
		/// // Find the node closest to somePoint which is either in 'My Grid Graph' OR in 'My Other Grid Graph'
		/// var info = AstarPath.active.GetNearest(somePoint, nn);
		/// </code>
		///
		/// Note: This does only affect which nodes are returned from a <see cref="AstarPath.GetNearest"/> call, if a valid graph is connected to an invalid graph using a node link then it might be searched anyway.
		///
		/// See: <see cref="AstarPath.GetNearest"/>
		/// See: <see cref="SuitableGraph"/>
		/// See: bitmasks (view in online documentation for working links)
		/// </summary>
		public GraphMask graphMask = GraphMask.everything;

		/// <summary>Only treat nodes in the area <see cref="area"/> as suitable. Does not affect anything if <see cref="area"/> is less than 0 (zero)</summary>
		public bool constrainArea;

		/// <summary>Area ID to constrain to. Will not affect anything if less than 0 (zero) or if <see cref="constrainArea"/> is false</summary>
		public int area = -1;

		/// <summary>
		/// Determines how to measure distances to the navmesh.
		///
		/// The default is a euclidean distance, which works well for most things.
		///
		/// See: <see cref="DistanceMetric"/>
		/// </summary>
		public DistanceMetric distanceMetric;

		/// <summary>Constrain the search to only walkable or unwalkable nodes depending on <see cref="walkable"/>.</summary>
		public bool constrainWalkability = true;

		/// <summary>
		/// Only search for walkable or unwalkable nodes if <see cref="constrainWalkability"/> is enabled.
		/// If true, only walkable nodes will be searched for, otherwise only unwalkable nodes will be searched for.
		/// Does not affect anything if <see cref="constrainWalkability"/> if false.
		/// </summary>
		public bool walkable = true;

		/// <summary>
		/// if available, do an XZ check instead of checking on all axes.
		/// The navmesh/recast graph as well as the grid/layered grid graph supports this.
		///
		/// This can be important on sloped surfaces. See the image below in which the closest point for each blue point is queried for:
		/// [Open online documentation to see images]
		///
		/// Deprecated: Use <see cref="distanceMetric"/> = DistanceMetric.ClosestAsSeenFromAbove() instead
		/// </summary>
		[System.Obsolete("Use distanceMetric = DistanceMetric.ClosestAsSeenFromAbove() instead")]
		public bool distanceXZ {
			get {
				return distanceMetric.isProjectedDistance && distanceMetric.distanceScaleAlongProjectionDirection == 0;
			}
			set {
				if (value) {
					distanceMetric = DistanceMetric.ClosestAsSeenFromAbove();
				} else {
					distanceMetric = DistanceMetric.Euclidean;
				}
			}
		}

		/// <summary>
		/// Sets if tags should be constrained.
		/// See: <see cref="tags"/>
		/// </summary>
		public bool constrainTags = true;

		/// <summary>
		/// Nodes which have any of these tags set are suitable.
		/// This is a bitmask, i.e bit 0 indicates that tag 0 is good, bit 3 indicates tag 3 is good etc.
		/// See: <see cref="constrainTags"/>
		/// See: <see cref="graphMask"/>
		/// See: bitmasks (view in online documentation for working links)
		/// </summary>
		public int tags = -1;

		/// <summary>
		/// Constrain distance to node.
		/// Uses distance from <see cref="AstarPath.maxNearestNodeDistance"/>.
		/// If this is false, it will completely ignore the distance limit.
		///
		/// If there are no suitable nodes within the distance limit then the search will terminate with a null node as a result.
		/// Note: This value is not used in this class, it is used by the AstarPath.GetNearest function.
		/// </summary>
		public bool constrainDistance = true;

		/// <summary>
		/// Returns whether or not the graph conforms to this NNConstraint's rules.
		/// Note that only the first 31 graphs are considered using this function.
		/// If the <see cref="graphMask"/> has bit 31 set (i.e the last graph possible to fit in the mask), all graphs
		/// above index 31 will also be considered suitable.
		/// </summary>
		public virtual bool SuitableGraph (int graphIndex, NavGraph graph) {
			return graphMask.Contains((uint)graphIndex);
		}

		/// <summary>Returns whether or not the node conforms to this NNConstraint's rules</summary>
		public virtual bool Suitable (GraphNode node) {
			if (constrainWalkability && node.Walkable != walkable) return false;

			if (constrainArea && area >= 0 && node.Area != area) return false;

			if (constrainTags && ((tags >> (int)node.Tag) & 0x1) == 0) return false;

			return true;
		}

		public void UseSettings (PathRequestSettings settings) {
			graphMask = settings.graphMask;
			constrainTags = true;
			tags = settings.traversableTags;
			constrainWalkability = true;
			walkable = true;
		}

		/// <summary>
		/// The default NNConstraint.
		/// Equivalent to new NNConstraint ().
		/// This NNConstraint has settings which works for most, it only finds walkable nodes
		/// and it constrains distance set by A* Inspector -> Settings -> Max Nearest Node Distance
		///
		/// Deprecated: Use <see cref="NNConstraint.Walkable"/> instead. It is equivalent, but the name is more descriptive.
		/// </summary>
		[System.Obsolete("Use NNConstraint.Walkable instead. It is equivalent, but the name is more descriptive")]
		public static NNConstraint Default {
			get {
				return new NNConstraint();
			}
		}

		/// <summary>
		/// An NNConstraint which filters out unwalkable nodes.
		/// This is the most commonly used NNConstraint.
		///
		/// It also constrains the nearest node to be within the distance set by A* Inspector -> Settings -> Max Nearest Node Distance
		/// </summary>
		public static NNConstraint Walkable {
			get {
				return new NNConstraint();
			}
		}

		/// <summary>Returns a constraint which does not filter the results</summary>
		public static NNConstraint None {
			get {
				return new NNConstraint {
						   constrainWalkability = false,
						   constrainArea = false,
						   constrainTags = false,
						   constrainDistance = false,
						   graphMask = GraphMask.everything,
				};
			}
		}

		/// <summary>Default constructor. Equals to the property <see cref="Default"/></summary>
		public NNConstraint () {
		}

		public TraversalConstraint ToTraversalConstraint () {
			return new TraversalConstraint {
					   tags = tags,
					   graphMask = graphMask,
			};
		}

		public NearestNodeConstraint ToNearestNodeConstraint () {
			return new NearestNodeConstraint {
					   filter = null,
					   distanceMetric = distanceMetric,
					   tags = constrainTags ? tags : -1,
					   area = constrainArea ? area : -1,
					   graphMask = graphMask,
					   maxDistanceSqr = constrainDistance ? -1 : float.PositiveInfinity,
					   walkable = constrainWalkability ? (walkable ? NearestNodeConstraint.WalkabilityConstraint.Walkable : NearestNodeConstraint.WalkabilityConstraint.Unwalkable) : NearestNodeConstraint.WalkabilityConstraint.DontCare,
			};
		}
	}

	/// <summary>
	/// A special NNConstraint which can use different logic for the start node and end node in a path.
	/// A PathNNConstraint can be assigned to the Path.nnConstraint field, the path will first search for the start node, then it will call SetStart and proceed with searching for the end node (nodes in the case of a MultiTargetPath).
	/// The default PathNNConstraint will constrain the end point to lie inside the same area as the start point.
	/// </summary>
	[System.Obsolete("Use NearestNodeConstraint instead", true)]
	public class PathNNConstraint : NNConstraint {
	}
}
