using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;
using Pathfinding.Util;
using UnityEngine.Assertions;

namespace Pathfinding {
	using System;
	using Pathfinding.Drawing;
	using Unity.Jobs;

	/// <summary>
	/// Graph for off-mesh links.
	///
	/// This is an internal graph type which is used to store off-mesh links.
	/// An off-mesh link between two nodes A and B is represented as: <code> A <--> N1 <--> N2 <--> B </code>.
	/// where N1 and N2 are two special nodes added to this graph at the exact start and endpoints of the link.
	///
	/// This graph is not persistent. So it will never be saved to disk and a new one will be created each time the game starts.
	///
	/// It is also not possible to query for the nearest node in this graph. The <see cref="GetNearest"/> method will always return an empty result.
	/// This is by design, as all pathfinding should start on the navmesh, not on an off-mesh link.
	///
	/// See: <see cref="OffMeshLinks"/>
	/// See: <see cref="NodeLink2"/>
	/// </summary>
	[JsonOptIn]
	[Pathfinding.Util.Preserve]
	public class LinkGraph : NavGraph {
		LinkNode[] nodes = new LinkNode[0];
		int nodeCount;

		public override bool isScanned => true;

		public override bool persistent => false;

		public override bool showInInspector => false;

		public override int CountNodes() => nodeCount;

		protected override void DestroyAllNodes () {
			base.DestroyAllNodes();
			nodes = new LinkNode[0];
			nodeCount = 0;
		}

		public override void GetNodes<T>(GraphNode.NodeActionWithData<T> action, ref T data) {
			if (nodes == null) return;
			for (int i = 0; i < nodeCount; i++) action(nodes[i], ref data);
		}

		internal LinkNode AddNode () {
			AssertSafeToUpdateGraph();
			if (nodeCount >= nodes.Length) {
				Memory.Realloc(ref nodes, Mathf.Max(16, nodeCount * 2));
			}
			nodeCount++;
			return nodes[nodeCount-1] = new LinkNode(active) {
					   nodeInGraphIndex = nodeCount - 1,
					   GraphIndex = graphIndex,
					   Walkable = true,
			};
		}

		internal void RemoveNode (LinkNode node) {
			if (nodes[node.nodeInGraphIndex] != node) throw new ArgumentException("Node is not in this graph");
			// Remove and swap with the last node
			nodeCount--;
			nodes[node.nodeInGraphIndex] = nodes[nodeCount];
			nodes[node.nodeInGraphIndex].nodeInGraphIndex = node.nodeInGraphIndex;
			nodes[nodeCount] = null;
			node.Destroy();
		}

		public override float NearestNodeDistanceSqrLowerBound(Vector3 position, ref NearestNodeConstraint constraint) => float.PositiveInfinity;

		/// <summary>
		/// It's not possible to query for the nearest node in a link graph.
		/// This method will always return an empty result.
		/// </summary>
		public override NNInfo GetNearest(Vector3 position, ref NearestNodeConstraint constraint) => default;

		public override void OnDrawGizmos (DrawingData gizmos, bool drawNodes, RedrawScope redrawScope, bool renderInGame) {
			// We rely on the link components themselves to draw the links

			// TODO
			base.OnDrawGizmos(gizmos, drawNodes, redrawScope, renderInGame);
		}

		class LinkGraphUpdatePromise : IGraphUpdatePromise {
			public LinkGraph graph;

			public void Apply (IGraphUpdateContext ctx) {
				// Destroy all previous nodes (if any)
				graph.DestroyAllNodes();
			}

			public IEnumerator<JobHandle> Prepare() => null;
		}

		protected override IGraphUpdatePromise ScanInternal () => new LinkGraphUpdatePromise { graph = this };
	}

	public class LinkNode : PointNode {
		public OffMeshLinks.OffMeshLinkSource linkSource;
		public OffMeshLinks.OffMeshLinkConcrete linkConcrete;
		public int nodeInGraphIndex;

		public LinkNode() { }
		public LinkNode(AstarPath active) : base(active) {}

		public override void RemovePartialConnection (GraphNode node) {
			linkConcrete.staleConnections = true;
			// Mark the link as dirty so that it will be recalculated.
			// Ensure that this does not immediately schedule an update.
			// Nodes should only be updated during work items and while graphs are scanned,
			// and in those cases node links will be refreshed anyway.
			// However, this can also trigger when the AstarPath component is being destroyed,
			// or when a graph is removed. In those cases, we don't want to schedule an update.
			AstarPath.active.offMeshLinks.DirtyNoSchedule(linkSource);
			base.RemovePartialConnection(node);
		}

		public override void Open (ref Path.SearchContext ctx, uint pathNodeIndex, uint gScore) {
			// Note: Not calling path.OpenCandidateConnectionsToEndNode here, because link nodes should never be the end node of a path

			if (connections == null) return;

			var pathHandler = ctx.pathHandler;
			var pn = pathHandler.pathNodes[pathNodeIndex];
			// Check if our parent node was also a link node by checking if it is in the same graph as this node.
			// If it is, we are allowed to connect to non-link nodes.
			// Otherwise, we are at the start of the link and we must only connect to other link nodes.
			// This is to avoid the path going to a link node, and then going directly back to a non-link node
			// without actually traversing the link. It would technically be a valid path,
			// but it causes confusion for other scripts that look for off-mesh links in the path.
			// TODO: Store the other link node as a field to be able to do a more robust check here?
			var isEndOfLink = !pathHandler.IsTemporaryNode(pn.parentIndex) && pathHandler.GetNode(pn.parentIndex).GraphIndex == GraphIndex;

			if (isEndOfLink) {
				// Connect to non-link nodes
				for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i].node;

					if (other.GraphIndex != GraphIndex && ctx.traversalConstraint.CanTraverse(this, other)) {
						// When connecting to a non-link node, use a special function to open the connection.
						// The typical case for this is that we are at the end of an off-mesh link and we are connecting to a navmesh node.
						// In that case, this node's position is in the interior of the navmesh node. We let the navmesh node decide how
						// that should be handled.
						var connectionCost = ctx.traversalCosts.GetConnectionCost(this, other);
						other.OpenAtPoint(ref ctx, pathNodeIndex, position, gScore + connectionCost);
					}
				}
			} else {
				// Connect to other link nodes
				var ourTraversalCostFactor = ctx.traversalCosts.GetTraversalCostMultiplier(this);
				for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i].node;

					if (other.GraphIndex == GraphIndex && ctx.traversalConstraint.CanTraverse(this, other)) {
						var connectionCost = ctx.traversalCosts.GetConnectionCost(this, other);
						// Half the traversal is done on this node and half on the other node.
						connectionCost += (uint)System.Math.Round(connections[i].cost * 0.5f * (ourTraversalCostFactor + ctx.traversalCosts.GetTraversalCostMultiplier(other)));
						ctx.OpenCandidateConnection(pathNodeIndex, other.NodeIndex, gScore + connectionCost, 0, other.position);
					}
				}
			}
		}

		public override void OpenAtPoint (ref Path.SearchContext ctx, uint pathNodeIndex, Int3 pos, uint gScore) {
			// Note: Not calling path.OpenCandidateConnectionsToEndNode here, because link nodes should never be the end node of a path

			var connectionCost = (uint)System.Math.Round((pos - this.position).magnitude * ctx.traversalCosts.GetTraversalCostMultiplier(this));
			ctx.OpenCandidateConnection(pathNodeIndex, NodeIndex, gScore + connectionCost, 0, this.position);
		}
	}
}
