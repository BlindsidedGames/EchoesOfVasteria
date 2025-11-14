using System.Runtime.CompilerServices;
using UnityEngine;
namespace Pathfinding {
	/// <summary>
	/// Provides additional traversal information to a path request.
	///
	/// The ITraversalProvider interface is something you can implement to be able to control exactly which nodes are traversable, how costly it is to traverse them, and how costly it is to traverse connections between nodes.
	/// This enables a lot of flexibility in how you can control pathfinding, and is useful for many different scenarios.
	///
	/// For example, you can use it to:
	/// - Make some agents prefer to walk on roads, while others prefer grass.
	/// - Create restricted doors, which only some agents can pass through.
	/// - In a turn based game, add additional cost for moving across a river between two tiles.
	/// - Etc.
	///
	/// Example implementation:
	/// <code>
	/// public class MyCustomTraversalProvider : ITraversalProvider {
	///     /** [CanTraverseImpl] */
	///     public bool CanTraverse (ref TraversalConstraint traversalConstraint, GraphNode node) {
	///         // If, for example, your agents are afraid of heights, prevent your agents from going above 50 world units
	///         return ((Vector3)node.position).y < 50;
	///     }
	///     /** [CanTraverseImpl] */
	///
	///     /** [CanTraverseFromToImpl] */
	///     public bool CanTraverse (ref TraversalConstraint traversalConstraint, GraphNode from, GraphNode to) {
	///         // Connections between nodes can be filtered too.
	///         // If you don't have any special rules for this, just forward the call to checking if the 'to' node is traversable.
	///         // (or just skip this method, as the default implementation will do the same)
	///
	///         // We can, for example, prevent the agent from going directly from a node with tag 2 to a node with tag 3
	///         if (from.Tag == 2 && to.Tag == 3) return false;
	///
	///         return CanTraverse(ref traversalConstraint, to);
	///     }
	///     /** [CanTraverseFromToImpl] */
	///
	///     /** [GetTraversalCostMultiplierImpl] */
	///     public float GetTraversalCostMultiplier (ref TraversalCosts traversalCosts, GraphNode node) {
	///         // For example, if your agent is afraid of heights, you can make nodes high up be 10 times more expensive to traverse
	///         if (((Vector3)node.position).y > 30) return 10.0f;
	///
	///         return DefaultITraversalProvider.GetTraversalCostMultiplier(ref traversalCosts, node);
	///     }
	///     /** [GetTraversalCostMultiplierImpl] */
	///
	///     /** [GetConnectionCostImpl] */
	///     public uint GetConnectionCost (ref TraversalCosts traversalCosts, GraphNode from, GraphNode to) {
	///         // The traversal cost is, by default, the sum of the penalty of the node's tag and the node's penalty
	///         return traversalCosts.GetTagEntryCost(to.Tag) + to.Penalty;
	///         // alternatively:
	///         // return DefaultITraversalProvider.GetConnectionCost(ref traversalCosts, from, to);
	///     }
	///     /** [GetConnectionCostImpl] */
	/// }
	/// </code>
	///
	/// You can use it with a <see cref="Seeker"/>:
	/// <code>
	/// seeker.traversalProvider = new MyCustomTraversalProvider();
	/// </code>
	///
	/// Or with a <see cref="FollowerEntity"/>
	/// <code>
	/// followerEntity.pathfindingSettings.traversalProvider = new MyCustomTraversalProvider();
	/// </code>
	///
	/// Or with a path instance:
	/// <code>
	/// var path = ABPath.Construct(Vector3.zero, Vector3.one);
	/// var traversalProvider = new MyCustomTraversalProvider();
	///
	/// // Use the same provider for both traversability and costs
	/// path.traversalConstraint.traversalProvider = traversalProvider;
	/// path.traversalCosts.traversalProvider = traversalProvider;
	/// </code>
	///
	/// Warning: Your implementation of this interface may be called from separate threads if pathfinding multithreading is enabled, or if you are using the <see cref="FollowerEntity"/> component.
	/// You must ensure that your implementation is thread-safe. It is recommended to make each ITraversalProvider instance immutable, so that it can be shared between threads without any issues.
	///
	/// Note: The ITraversalProvider may continue to be used by the movement script after the path has been calculated, in order to repair or simplify its path.
	///
	/// See: traversal_provider (view in online documentation for working links)
	/// See: tags (view in online documentation for working links)
	/// </summary>
	public interface ITraversalProvider {
		/// <summary>
		/// Filter diagonal connections using <see cref="GridGraph.cutCorners"/> for effects applied by this ITraversalProvider.
		/// This includes tags and other effects that this ITraversalProvider controls.
		///
		/// This only has an effect if <see cref="GridGraph.cutCorners"/> is set to false and your grid has <see cref="GridGraph.neighbours"/> set to Eight.
		///
		/// Take this example, the grid is completely walkable, but an ITraversalProvider is used to make the nodes marked with '#'
		/// as unwalkable. The agent 'S' is in the middle.
		///
		/// <code>
		/// ..........
		/// ....#.....
		/// ...<see cref="S"/>#....
		/// ....#.....
		/// ..........
		/// </code>
		///
		/// If filterDiagonalGridConnections is false the agent will be free to use the diagonal connections to move away from that spot.
		/// However, if filterDiagonalGridConnections is true (the default) then the diagonal connections will be disabled and the agent will be stuck.
		///
		/// Typically, there are a few common use cases:
		/// - If your ITraversalProvider makes walls and obstacles and you want it to behave identically to obstacles included in the original grid graph scan, then this should be true.
		/// - If your ITraversalProvider is used for agent to agent avoidance and you want them to be able to move around each other more freely, then this should be false.
		///
		/// See: <see cref="GridNode"/>
		/// </summary>
		bool filterDiagonalGridConnections => true;

		/// <summary>
		/// True if node should be able to be traversed by the path.
		///
		/// If you are implementing a custom ITraversalProvider, you should override this method and do whatever checks you need to do to determine if the node is traversable.
		///
		/// When used, the ITraversalProvider will pretty much always be wrapped in a <see cref="TraversalConstraint"/> struct.
		/// The <see cref="TraversalConstraint.CanTraverse(GraphNode)"/> method will then do its own checks first, and call this method to do additional filtering.
		/// This means that the <see cref="ITraversalProvider"/> is always at least as strict as the <see cref="TraversalConstraint"/>.
		///
		/// Note: This also means that returning true from this method does not necessarily mean that the node is traversable by a path.
		///      The TraversalConstraint may have additional filters, and in particular it always checks that the node is <see cref="GraphNode.Walkable;walkable"/>.
		///
		/// <code>
		/// public bool CanTraverse (ref TraversalConstraint traversalConstraint, GraphNode node) {
		///     // If, for example, your agents are afraid of heights, prevent your agents from going above 50 world units
		///     return ((Vector3)node.position).y < 50;
		/// }
		/// </code>
		///
		/// See: <see cref="TraversalConstraint.CanTraverse(GraphNode)"/>
		/// </summary>
		/// <param name="traversalConstraint">Full constraint that this traversal provider is part of. You can use this to check additional nodes using \reflink{TraversalConstraint.CanTraverseSkipUserFilter(GraphNode)}.
		///                            It is passed by reference to avoid the performance cost of copying it, since this method is quite hot.</param>
		/// <param name="node">The node to check. Should not be null.</param>
		bool CanTraverse(ref TraversalConstraint traversalConstraint, GraphNode node) => DefaultITraversalProvider.CanTraverse(ref traversalConstraint, node);

		/// <summary>
		/// True if the path can traverse a connection between from and to, and if to can be traversed itself.
		///
		/// This can be used to block movement between specific nodes.
		///
		/// If this method returns true then a call to CanTraverse(traversalConstraint,to) must also return true.
		/// Thus this method is a more flexible version of <see cref="CanTraverse(TraversalConstraint,GraphNode)"/>.
		///
		/// The default implementation will just call <see cref="CanTraverse(TraversalConstraint,GraphNode);CanTraverse(traversalConstraint,to)"/>
		///
		/// <code>
		/// public bool CanTraverse (ref TraversalConstraint traversalConstraint, GraphNode from, GraphNode to) {
		///     // Connections between nodes can be filtered too.
		///     // If you don't have any special rules for this, just forward the call to checking if the 'to' node is traversable.
		///     // (or just skip this method, as the default implementation will do the same)
		///
		///     // We can, for example, prevent the agent from going directly from a node with tag 2 to a node with tag 3
		///     if (from.Tag == 2 && to.Tag == 3) return false;
		///
		///     return CanTraverse(ref traversalConstraint, to);
		/// }
		/// </code>
		///
		/// See: <see cref="TraversalConstraint.CanTraverse(GraphNode,GraphNode)"/>
		/// </summary>
		bool CanTraverse(ref TraversalConstraint traversalConstraint, GraphNode from, GraphNode to) => CanTraverse(ref traversalConstraint, to);

		/// <summary>
		/// Cost of entering a given node from another node.
		/// Should return the additional cost for moving between from and to. By default, if no tags or penalties
		/// are used, then the connection cost is zero. A cost of 1000 corresponds roughly to the cost of moving 1 world unit.
		///
		/// This cost is not scaled by the distance between the two nodes.
		/// This cost is also not affected by the <see cref="GetTraversalCostMultiplier;traversal cost multiplier"/>.
		///
		/// The default implementation returns 0, and all cost comes from the <see cref="GetTraversalCostMultiplier"/> method instead.
		///
		/// <code>
		/// public uint GetConnectionCost (ref TraversalCosts traversalCosts, GraphNode from, GraphNode to) {
		///     // The traversal cost is, by default, the sum of the penalty of the node's tag and the node's penalty
		///     return traversalCosts.GetTagEntryCost(to.Tag) + to.Penalty;
		///     // alternatively:
		///     // return DefaultITraversalProvider.GetConnectionCost(ref traversalCosts, from, to);
		/// }
		/// </code>
		///
		/// See: <see cref="TraversalCosts.GetConnectionCost(GraphNode,GraphNode)"/>
		/// </summary>
		uint GetConnectionCost(ref TraversalCosts traversalCosts, GraphNode from, GraphNode to) => DefaultITraversalProvider.GetConnectionCost(ref traversalCosts, from, to);

		/// <summary>
		/// Multiplier for the cost of traversing some distance across the given node.
		///
		/// Returns a float representing how costly it is to move across the node, with 1.0 being the default, and, for example, 2.0 being twice as costly.
		///
		/// The value will be multiplied by the traversed distance in millimeters (see <see cref="Int3.Precision"/>) to get the final cost.
		/// For the default multiplier of 1.0, this means that the cost will be the same as the distance in millimeters.
		///
		/// Note: Prefer to make traversal more costly (cost multiplier >1), rather than making it cheaper than the default (cost multiplier <1).
		///       If you return any cost multiplier less than 1, you'll also need to reduce <see cref="AstarPath.heuristicScale"/> to the lowest cost multiplier that you use in the project,
		///       otherwise the pathfinding algorithm may not find the optimal path. Alternatively you could compensate with a higher connection cost value, so that the
		///       agent can never move to the target with a lower cost than the default. See https://en.wikipedia.org/wiki/Admissible_heuristic.
		///
		/// <code>
		/// public float GetTraversalCostMultiplier (ref TraversalCosts traversalCosts, GraphNode node) {
		///     // For example, if your agent is afraid of heights, you can make nodes high up be 10 times more expensive to traverse
		///     if (((Vector3)node.position).y > 30) return 10.0f;
		///
		///     return DefaultITraversalProvider.GetTraversalCostMultiplier(ref traversalCosts, node);
		/// }
		/// </code>
		///
		/// See: <see cref="TraversalCosts.GetTraversalCostMultiplier(GraphNode)"/>
		/// </summary>
		float GetTraversalCostMultiplier(ref TraversalCosts traversalCosts, GraphNode node) => DefaultITraversalProvider.GetTraversalCostMultiplier(ref traversalCosts, node);

		/// <summary>
		/// Can the agent traverse the connection between two nodes.
		///
		/// Deprecated: Use <see cref="CanTraverse(TraversalConstraint,GraphNode,GraphNode)"/> instead.
		/// </summary>
		[System.Obsolete("Use CanTraverse(ref TraversalConstraint, GraphNode, GraphNode) instead")]
		bool CanTraverse(Path path, GraphNode from, GraphNode to) => throw new System.NotImplementedException();

		/// <summary>
		/// Can the agent traverse the given node.
		///
		/// Deprecated: Use <see cref="CanTraverse(TraversalConstraint,GraphNode)"/> instead.
		/// </summary>
		[System.Obsolete("Use CanTraverse(ref TraversalConstraint, GraphNode) instead")]
		bool CanTraverse(Pathfinding.Path path, GraphNode node) => throw new System.NotImplementedException();

		/// <summary>
		/// Cost of traversing the given node.
		///
		/// Deprecated: Use <see cref="GetTraversalCostMultiplier(TraversalCosts,GraphNode)"/> instead.
		/// </summary>
		[System.Obsolete("Use GetTraversalCostMultiplier(ref TraversalCosts, GraphNode) instead")]
		uint GetTraversalCost(Path path, GraphNode node) => throw new System.NotImplementedException();
	}

	/// <summary>Convenience class to access the default implementation of the ITraversalProvider</summary>
	public static class DefaultITraversalProvider {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanTraverse (ref TraversalConstraint traversalConstraint, GraphNode node) {
			// The traversalConstraint will have done its filtering already, so we can just return true
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetTraversalCostMultiplier (ref TraversalCosts traversalCosts, GraphNode node) {
			return traversalCosts.GetDefaultTraversalCostMultiplier(node);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint GetConnectionCost (ref TraversalCosts traversalCosts, GraphNode from, GraphNode to) {
			return traversalCosts.GetDefaultConnectionCost(from, to);
		}
	}
}
