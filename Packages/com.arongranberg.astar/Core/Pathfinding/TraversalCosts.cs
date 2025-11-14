using System.Runtime.CompilerServices;

namespace Pathfinding {
	/// <summary>
	/// Defines the cost of traversing specific nodes when pathfinding.
	///
	/// To make agents avoid certain nodes, you can set a high penalty for traversing them.
	/// This will make the pathfinding algorithm prefer other nodes instead.
	///
	/// You can set costs based on the node's tag, or you can use a custom traversal provider to set costs based on other criteria with completely custom logic.
	///
	/// <code>
	/// var traversalCosts = new TraversalCosts();
	///
	/// // Make tags 1 and 4 have a higher penalty
	/// var tagCostMultipliers = new float[32];
	/// tagCostMultipliers[1] = 2.5f;
	/// tagCostMultipliers[4] = 5.0f;
	/// traversalCosts.tagCostMultipliers = tagCostMultipliers;
	///
	/// var path = ABPath.Construct(Vector3.zero, Vector3.one);
	/// path.traversalCosts = traversalCosts;
	/// </code>
	///
	/// <b>Traversal cost calculation</b>
	/// Assume we have a path with 3 nodes: A, B, and C.
	/// The path spends 1 meters inside node A, 3 meters inside node B, and 2 meters inside node C.
	///
	/// The total cost of traversing the path is:
	///
	/// <code>
	/// var totalCost = 0f;
	/// totalCost += costs.GetTraversalCostMultiplier(A) * 1000; // 1 meter in A
	/// totalCost += costs.GetConnectionCost(A, B); // cost of moving from A to B
	/// totalCost += costs.GetTraversalCostMultiplier(B) * 3000; // 3 meters in B
	/// totalCost += costs.GetConnectionCost(B, C); // cost of moving from B to C
	/// totalCost += costs.GetTraversalCostMultiplier(C) * 2000; // 2 meters in C
	/// </code>
	///
	/// or, assuming no traversal provider is set:
	///
	/// <code>
	/// var totalCost = 0f;
	/// totalCost += costs.tagCostMultipliers[A.Tag] * 1000; // 1 meter in A
	/// totalCost += costs.tagEntryCosts[B.Tag] + B.Penalty; // cost of moving from A to B
	/// totalCost += costs.tagCostMultipliers[B.Tag] * 3000; // 3 meters in B
	/// totalCost += costs.tagEntryCosts[C.Tag] + C.Penalty; // cost of moving from B to C
	/// totalCost += costs.tagCostMultipliers[C.Tag] * 2000; // 2 meters in C
	/// </code>
	///
	/// Note: On navmesh/recast graphs, the distance the path moves inside each node (triangle) is approximated.
	/// It cannot be exactly calculated for performance reasons. Typically it averages out to around 10-20% higher than the actual distance.
	/// This is usually not a problem in practice, but you should keep this in mind and not rely on exact values.
	///
	/// See: tags (view in online documentation for working links)
	/// See: traversal_provider (view in online documentation for working links)
	/// See: <see cref="ITraversalProvider"/>
	/// See: <see cref="PathfindingTag"/>
	/// See: <see cref="TraversalConstraint"/>
	/// See: <see cref="Path.traversalCosts"/>
	/// </summary>
	[System.Serializable]
	public struct TraversalCosts {
		/// <summary>
		/// Traversal provider to forward all cost calculations to.
		///
		/// If not null, this will override the <see cref="GetTraversalCost"/> method's output completely.
		/// The traversal provider will be called for each node that is explored by a path search.
		/// </summary>
		public ITraversalProvider traversalProvider;
		[UnityEngine.SerializeField]
		uint[] tagEntryCostsInternal;
		[UnityEngine.SerializeField]
		float[] tagCostMultipliersInternal;

		/// <summary>
		/// How much it costs to enter a node with a given tag.
		///
		/// This can be used to make agents avoid, or prefer, nodes with certain tags.
		///
		/// For example, every time a path enters tag 0 (which is the default tag), it will cost an extra tagEntryCosts[0].
		///
		/// For most pathfinding purposes, you can think of this as just the additional cost of traversing a node with the given tag.
		/// The distinction between entering and exiting a node is a very small one, and will not make a difference in most cases.
		///
		/// The index in the array corresponds to the tag.
		/// All entry costs are positive values since the A* algorithm cannot handle negative costs.
		///
		/// If null (the default), all tags will be treated as having an entry cost of zero.
		/// However, the path will still receive cost from the <see cref="tagCostMultipliers"/> field.
		///
		/// This cost will be applied for every node that is entered, regardless of if the previous node had the same tag or not.
		///
		/// Note: This array must be of length 32.
		///
		/// <code>
		/// var traversalCosts = new TraversalCosts();
		///
		/// // Make tags 1 and 4 have a higher penalty
		/// var tagEntryCosts = new uint[32];
		/// tagEntryCosts[1] = 1000;
		/// tagEntryCosts[4] = 20000;
		/// traversalCosts.tagEntryCosts = tagEntryCosts;
		///
		/// var path = ABPath.Construct(Vector3.zero, Vector3.one);
		/// path.traversalCosts = traversalCosts;
		/// </code>
		///
		/// In the <see cref="Seeker"/> and <see cref="FollowerEntity"/> inspectors, this field (called "Cost per node") is hidden if
		/// your scene only contains navmesh/recast graphs, since using per-node costs on those graphs is not recommended
		/// due to the significant differences in node sizes and shapes that they have. Use <see cref="tagCostMultipliers"/> instead.
		///
		/// [Open online documentation to see images]
		///
		/// See: <see cref="TraversalCosts.GetConnectionCost"/>
		/// See: <see cref="TraversalCosts.tagCostMultipliers"/>
		/// See: tags (view in online documentation for working links)
		/// </summary>
		public uint[] tagEntryCosts {
			get => tagEntryCostsInternal;
			set {
				if (value != null && value.Length != 32) throw new System.ArgumentException("Entry costs array must be of length 32");
				tagEntryCostsInternal = value;
			}
		}

		/// <summary>
		/// Multiplier for the cost of moving some distance across a node with a given tag.
		///
		/// This will be multiplied by the traversed distance in millimeters (see <see cref="Int3.Precision"/>) to get the final cost.
		/// The default value is 1, which means that the cost will be the same as the distance.
		/// If you set this to 2, the cost will be twice as high as the distance.
		///
		/// For example, moving 1 world unit across a node with tag 0 will cost 1000 * tagCostMultipliers[0] units of movement cost.
		///
		/// If null (the default), all tags will be treated as having a multiplier of 1.
		///
		/// Note: Prefer to make tags more costly (cost multiplier >1), rather than making them cheaper than the default (cost multiplier <1).
		///       If you set any cost multiplier to less than 1, you'll also need to reduce <see cref="AstarPath.heuristicScale"/> to the lowest cost multiplier that you use in the project,
		///       otherwise the pathfinding algorithm may not find the optimal path. Alternatively you could compensate with a higher <see cref="tagEntryCosts"/> value, so that the
		///       agent can never move to the target with a lower cost than the default. See https://en.wikipedia.org/wiki/Admissible_heuristic.
		///
		/// Note: This array must be of length 32.
		///
		/// In the inspector, this value is displayed as "Cost per world unit", and is shown as multiplied by 1000 (<see cref="Int3.Precision"/>), to make it the cost per world unit.
		/// While in code, it is a multiplier of the default cost.
		///
		/// [Open online documentation to see images]
		///
		/// <code>
		/// var traversalCosts = new TraversalCosts();
		///
		/// // Make tags 1 and 4 have a higher penalty
		/// var tagCostMultipliers = new float[32];
		/// tagCostMultipliers[1] = 2.5f;
		/// tagCostMultipliers[4] = 5.0f;
		/// traversalCosts.tagCostMultipliers = tagCostMultipliers;
		///
		/// var path = ABPath.Construct(Vector3.zero, Vector3.one);
		/// path.traversalCosts = traversalCosts;
		/// </code>
		///
		/// See: <see cref="TraversalCosts.GetTraversalCostMultiplier"/>
		/// See: <see cref="TraversalCosts.tagEntryCosts"/>
		/// See: <see cref="Seeker.tagCostMultipliers"/>
		/// See: <see cref="PathRequestSettings.tagCostMultipliers"/>
		/// </summary>
		public float[] tagCostMultipliers {
			get => tagCostMultipliersInternal;
			set {
				if (value != null && value.Length != 32) throw new System.ArgumentException("Tag cost multiplier array must be of length 32");
				tagCostMultipliersInternal = value;
			}
		}

		/// <summary>
		/// True if traversal costs might be non-default, and false if they are guaranteed to be default for all nodes.
		///
		/// If false, then <see cref="GetTraversalCostMultiplier"/> will always return 1, and GetConnectionCost will always return the node's penalty.
		/// </summary>
		public bool hasCosts => tagCostMultipliersInternal != null || tagEntryCostsInternal != null || traversalProvider != null;

		/// <summary>Cost for traversing the given node</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[System.Obsolete("Use GetTraversalCostMultiplier instead")]
		public uint GetTraversalCost (GraphNode node) {
			// return traversalProvider != null ? traversalProvider.GetTraversalCost(ref this, node) : GetDefaultTraversalCost(node);
			return 0;
		}

		/// <summary>
		/// Multiplier for the cost of traversing some distance across the given node.
		///
		/// This will be multiplied by the traversed distance in millimeters (see <see cref="Int3.Precision"/>) to get the final cost.
		///
		/// If <see cref="traversalProvider"/> is set, this call will be forwarded to it. Otherwise, the cost will be taken from <see cref="tagCostMultipliers"/>.
		///
		/// See: <see cref="tagCostMultipliers"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetTraversalCostMultiplier (GraphNode node) {
			return traversalProvider?.GetTraversalCostMultiplier(ref this, node) ?? tagCostMultipliersInternal ? [node.Tag] ?? 1f;
		}

		/// <summary>
		/// Instantaneous cost for moving between from and to.
		/// This cost is not scaled by the distance between the two nodes.
		///
		/// If <see cref="traversalProvider"/> is set, this call will be forwarded to it. Otherwise, the cost will be taken from <see cref="tagEntryCosts"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetConnectionCost (GraphNode from, GraphNode to) {
			return traversalProvider?.GetConnectionCost(ref this, from, to) ?? ((tagEntryCostsInternal ? [to.Tag] ?? 0) + to.Penalty);
		}

		/// <summary>Like GetTraversalCostMultiplier, but ignores any <see cref="traversalProvider"/> that may be set</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetDefaultTraversalCostMultiplier (GraphNode node) {
			return tagCostMultipliersInternal ? [node.Tag] ?? 1f;
		}

		/// <summary>Cost for traversing the given node, ignoring any <see cref="traversalProvider"/> that may be set</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetDefaultConnectionCost (GraphNode from, GraphNode to) {
			return to.Penalty + GetTagEntryCost(to.Tag);
		}

		/// <summary>
		/// Cost for entering a node with the given tag.
		///
		/// See: tags (view in online documentation for working links)
		/// </summary>
		/// <param name="tag">A value between 0 (inclusive) and 32 (exclusive).</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetTagEntryCost (uint tag) {
			return tagEntryCostsInternal ? [tag] ?? 0;
		}
	}
}
