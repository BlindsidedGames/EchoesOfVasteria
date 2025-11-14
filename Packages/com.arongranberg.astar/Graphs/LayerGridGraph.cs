#if !ASTAR_NO_GRID_GRAPH
using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;
using Pathfinding.Graphs.Grid;

namespace Pathfinding {
	/// <summary>
	/// Grid Graph, supports layered worlds.
	/// [Open online documentation to see images]
	/// The GridGraph is great in many ways, reliable, easily configured and updatable during runtime.
	/// But it lacks support for worlds which have multiple layers, such as a building with multiple floors.
	/// That's where this graph type comes in. It supports basically the same stuff as the grid graph, but also multiple layers.
	/// It uses a bit more memory than a regular grid graph, but is otherwise equivalent.
	///
	/// See: get-started-grid (view in online documentation for working links)
	/// See: graphTypes (view in online documentation for working links)
	///
	/// \section layergridgraph-inspector Inspector
	/// [Open online documentation to see images]
	///
	/// \inspectorField{inspectorGridMode; Shape}
	/// \inspectorField{is2D; 2D}
	/// \inspectorField{AlignToTilemap; Align  to tilemap}
	/// \inspectorField{width; Width}
	/// \inspectorField{depth; Depth}
	/// \inspectorField{nodeSize; Node size}
	/// \inspectorField{aspectRatio; Aspect ratio (isometric/advanced shape)}
	/// \inspectorField{isometricAngle; Isometric angle (isometric/advanced shape)}
	/// \inspectorField{center; Center}
	/// \inspectorField{rotation; Rotation}
	/// \inspectorField{neighbours; Connections}
	/// \inspectorField{cutCorners; Cut corners}
	/// \inspectorField{maxStepHeight; Max step height}
	/// \inspectorField{maxStepUsesSlope; Account for slopes}
	/// \inspectorField{maxSlope; Max slope}
	/// \inspectorField{erodeIterations; Erosion iterations}
	/// \inspectorField{erosionUseTags; Erosion → Erosion Uses Tags}
	/// \inspectorField{collision.use2D; Use 2D physics}
	///
	/// <b>Collision testing</b>
	/// \inspectorField{collision.collisionCheck; Enable Collision Testing}
	/// \inspectorField{collision.type; Collider type}
	/// \inspectorField{collision.diameter; Diameter}
	/// \inspectorField{collision.height; Height/length}
	/// \inspectorField{collision.collisionOffset; Offset}
	/// \inspectorField{collision.mask; Obstacle layer mask}
	/// \inspectorField{GridGraphEditor.collisionPreviewOpen; Preview}
	///
	/// <b>Height testing</b>
	/// \inspectorField{collision.heightCheck; Enable Height Testing}
	/// \inspectorField{collision.fromHeight; Ray length}
	/// \inspectorField{collision.heightMask; Mask}
	/// \inspectorField{collision.thickRaycast; Thick raycast}
	/// \inspectorField{collision.unwalkableWhenNoGround; Unwalkable when no ground}
	///
	/// <b>Rules</b>
	/// Take a look at grid-rules (view in online documentation for working links) for a list of available rules.
	///
	/// <b>Other settings</b>
	/// \inspectorField{showMeshSurface; Show surface}
	/// \inspectorField{showMeshOutline; Show outline}
	/// \inspectorField{showNodeConnections; Show connections}
	/// \inspectorField{NavGraph.initialPenalty; Initial penalty}
	///
	/// Note: The graph supports 16 layers by default, but it can be increased to 256 by enabling the ASTAR_LEVELGRIDNODE_MORE_LAYERS option in the A* Inspector → Settings → Optimizations tab.
	///
	/// See: <see cref="GridGraph"/>
	/// </summary>
	[Pathfinding.Util.Preserve]
	public class LayerGridGraph : GridGraph, IUpdatableGraph {
		// This function will be called when this graph is destroyed
		protected override void DisposeUnmanagedData () {
			base.DisposeUnmanagedData();

			// Clean up a reference in a static variable which otherwise should point to this graph forever and stop the GC from collecting it
			LevelGridNode.ClearGridGraph((int)graphIndex, this);
		}

		public LayerGridGraph () {
			newGridNodeDelegate = () => new LevelGridNode();
		}

		protected override GridNodeBase[] AllocateNodesJob (int size, out Unity.Jobs.JobHandle dependency) {
			var newNodes = new LevelGridNode[size];

			dependency = active.AllocateNodes(newNodes, size, newGridNodeDelegate, 1);
			return newNodes;
		}

		/// <summary>
		/// Number of layers.
		/// Warning: Do not modify this variable
		/// </summary>
		[JsonMember]
		internal int layerCount;

		/// <summary>Nodes with a short distance to the node above it will be set unwalkable</summary>
		[JsonMember]
		public float characterHeight = 0.4F;

		internal int lastScannedWidth;
		internal int lastScannedDepth;

		public override int LayerCount {
			get => layerCount;
			protected set => layerCount = value;
		}

		public override int MaxLayers => LevelGridNode.MaxLayerCount;

		public override int CountNodes () {
			if (nodes == null) return 0;

			int counter = 0;
			for (int i = 0; i < nodes.Length; i++) {
				if (nodes[i] != null) counter++;
			}
			return counter;
		}

		/// <summary>
		/// Get all nodes in a rectangle.
		/// Returns: The number of nodes written to the buffer.
		/// </summary>
		/// <param name="rect">Region in which to return nodes. It will be clamped to the grid.</param>
		/// <param name="buffer">Buffer in which the nodes will be stored. Should be at least as large as the number of nodes that can exist in that region.</param>
		public override int GetNodesInRegion (IntRect rect, GridNodeBase[] buffer) {
			// Clamp the rect to the grid
			// Rect which covers the whole grid
			var gridRect = new IntRect(0, 0, width-1, depth-1);

			rect = IntRect.Intersection(rect, gridRect);

			if (nodes == null || !rect.IsValid() || nodes.Length != width*depth*layerCount) return 0;

			int counter = 0;
			try {
				for (int l = 0; l < layerCount; l++) {
					var lwd = l * Width * Depth;
					for (int z = rect.ymin; z <= rect.ymax; z++) {
						var offset = lwd + z*Width;
						for (int x = rect.xmin; x <= rect.xmax; x++) {
							var node = nodes[offset + x];
							if (node != null) {
								buffer[counter] = node;
								counter++;
							}
						}
					}
				}
			} catch (System.IndexOutOfRangeException) {
				// Catch the exception which 'buffer[counter] = node' would throw if the buffer was too small
				throw new System.ArgumentException("Buffer is too small");
			}

			return counter;
		}

		/// <summary>
		/// Node in the specified cell.
		/// Returns null if the coordinate is outside the grid.
		///
		/// If you know the coordinate is inside the grid and you are looking to maximize performance then you
		/// can look up the node in the internal array directly which is slightly faster.
		/// See: <see cref="nodes"/>
		/// </summary>
		public GridNodeBase GetNode (int x, int z, int layer) {
			if (x < 0 || z < 0 || x >= width || z >= depth || layer < 0 || layer >= layerCount) return null;
			return nodes[x + z*width + layer*width*depth];
		}

		protected override IGraphUpdatePromise ScanInternal (bool async) {
			LevelGridNode.SetGridGraph((int)graphIndex, this);
			layerCount = 0;
			lastScannedWidth = width;
			lastScannedDepth = depth;
			return base.ScanInternal(async);
		}

		protected override GridNodeBase GetNearestFromGraphSpace (Vector3 positionGraphSpace) {
			if (nodes == null || depth*width*layerCount != nodes.Length) {
				return null;
			}

			float xf = positionGraphSpace.x;
			float zf = positionGraphSpace.z;
			int x = Mathf.Clamp((int)xf, 0, width-1);
			int z = Mathf.Clamp((int)zf, 0, depth-1);
			var worldPos = transform.Transform(positionGraphSpace);
			return GetNearestNode(worldPos, x, z);
		}

		private GridNodeBase GetNearestNode (Vector3 position, int x, int z) {
			int index = width*z+x;
			float minDist = float.PositiveInfinity;
			GridNodeBase minNode = null;

			for (int i = 0; i < layerCount; i++) {
				var node = nodes[index + width*depth*i];
				if (node != null) {
					float dist =  ((Vector3)node.position - position).sqrMagnitude;
					if (dist < minDist) {
						minDist = dist;
						minNode = node;
					}
				}
			}
			return minNode;
		}

		protected override void SerializeExtraInfo (GraphSerializationContext ctx) {
			if (nodes == null) {
				ctx.writer.Write(-1);
				return;
			}

			ctx.writer.Write(nodes.Length);

			for (int i = 0; i < nodes.Length; i++) {
				if (nodes[i] == null) {
					ctx.writer.Write(-1);
				} else {
					ctx.writer.Write(0);
					nodes[i].SerializeNode(ctx);
				}
			}

			SerializeNodeSurfaceNormals(ctx);
		}

		protected override void DeserializeExtraInfo (GraphSerializationContext ctx) {
			int count = ctx.reader.ReadInt32();

			if (count == -1) {
				nodes = null;
				return;
			}

			nodes = new LevelGridNode[count];
			for (int i = 0; i < nodes.Length; i++) {
				if (ctx.reader.ReadInt32() != -1) {
					nodes[i] = newGridNodeDelegate();
					active.InitializeNode(nodes[i]);
					nodes[i].DeserializeNode(ctx);
				} else {
					nodes[i] = null;
				}
			}
			DeserializeNativeData(ctx, ctx.meta.version >= AstarSerializer.V4_3_37);
		}

		protected override void PostDeserialization (GraphSerializationContext ctx) {
			UpdateTransform();
			lastScannedWidth = width;
			lastScannedDepth = depth;

			SetUpOffsetsAndCosts();
			LevelGridNode.SetGridGraph((int)graphIndex, this);

			if (nodes == null || nodes.Length == 0) return;

			if (width*depth*layerCount != nodes.Length) {
				Debug.LogError("Node data did not match with bounds data. Probably a change to the bounds/width/depth data was made after scanning the graph, just prior to saving it. Nodes will be discarded");
				nodes = new GridNodeBase[0];
				return;
			}

			for (int i = 0; i < layerCount; i++) {
				for (int z = 0; z < depth; z++) {
					for (int x = 0; x < width; x++) {
						LevelGridNode node = nodes[z*width+x + width*depth*i] as LevelGridNode;

						if (node == null) {
							continue;
						}

						node.NodeInGridIndex = z*width+x;
						node.LayerCoordinateInGrid = i;
					}
				}
			}
		}
	}
}
#endif
