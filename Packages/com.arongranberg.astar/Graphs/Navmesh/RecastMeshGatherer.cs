using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Navmesh {
	using System;
	using Pathfinding;
	using Voxelization.Burst;
	using Pathfinding.Util;
	using Pathfinding.Jobs;
	using Pathfinding.Collections;
	using Pathfinding.Pooling;
	using UnityEngine.Profiling;
	using Unity.Collections.LowLevel.Unsafe;
	using Unity.Profiling;
	using Unity.Jobs;

	[BurstCompile]
	public class RecastMeshGatherer {
		readonly int terrainDownsamplingFactor;
		public readonly LayerMask mask;
		public readonly List<string> tagMask;
		readonly float maxColliderApproximationError;
		public readonly Bounds bounds;
		public readonly PhysicsScene physicsScene;
		public readonly PhysicsScene2D physicsScene2D;
		Dictionary<MeshCacheItem, int> cachedMeshes = new Dictionary<MeshCacheItem, int>();
		UnsafeList<UnsafeSpan<Vector3> > vertexBuffers;
		UnsafeList<UnsafeSpan<int> > triangleBuffers;
		UnsafeList<UnsafeSpan<int> > tagsBuffers;
		List<Mesh> meshData;
		readonly RecastGraph.PerLayerModification[] modificationsByLayer;
		readonly RecastGraph.PerLayerModification[] modificationsByLayer2D;
		readonly List<RecastGraph.PerTerrainLayerModification> perTerrainLayerModifications;
#if UNITY_EDITOR
		readonly List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime = new List<(UnityEngine.Object, Mesh)>();
#else
		bool anyNonReadableMesh = false;
#endif

		static class Markers {
			public static readonly ProfilerMarker MarkerCalculateBounds = new ProfilerMarker("CalculateBounds");
			public static readonly ProfilerMarker MarkerGetMissingMeshDataAndBounds = new ProfilerMarker("GetMissingMeshDataAndBounds");
			public static readonly ProfilerMarker MarkerPatchMissingMeshDataAndBounds = new ProfilerMarker("PatchMissingMeshDataAndBounds");
			public static readonly ProfilerMarker MarkerCreateRasterizationMeshes = new ProfilerMarker("CreateRasterizationMeshes");
		}

		UnsafeList<GatheredMesh> meshes;
		List<Material> dummyMaterials = new List<Material>();

		public RecastMeshGatherer (PhysicsScene physicsScene, PhysicsScene2D physicsScene2D, Bounds bounds, int terrainDownsamplingFactor, LayerMask mask, List<string> tagMask, List<RecastGraph.PerLayerModification> perLayerModifications, List<RecastGraph.PerTerrainLayerModification> perTerrainLayerModifications, float maxColliderApproximationError) {
			// Clamp to at least 1 since that's the resolution of the heightmap
			terrainDownsamplingFactor = Math.Max(terrainDownsamplingFactor, 1);

			this.bounds = bounds;
			this.terrainDownsamplingFactor = terrainDownsamplingFactor;
			this.mask = mask;
			this.tagMask = tagMask ?? new List<string>();
			this.maxColliderApproximationError = maxColliderApproximationError;
			this.physicsScene = physicsScene;
			this.physicsScene2D = physicsScene2D;
			this.perTerrainLayerModifications = perTerrainLayerModifications;
			meshes = new UnsafeList<GatheredMesh>(16, Allocator.Persistent);
			vertexBuffers = new UnsafeList<UnsafeSpan<Vector3> >(16, Allocator.Persistent);
			triangleBuffers = new UnsafeList<UnsafeSpan<int> >(16, Allocator.Persistent);
			tagsBuffers = new UnsafeList<UnsafeSpan<int> >(16, Allocator.Persistent);
			cachedMeshes = ObjectPoolSimple<Dictionary<MeshCacheItem, int> >.Claim();
			meshData = ListPool<Mesh>.Claim();
			modificationsByLayer = RecastGraph.PerLayerModification.ToLayerLookup(perLayerModifications, RecastGraph.PerLayerModification.Default);
			// 2D colliders default to being unwalkable
			var default2D = RecastGraph.PerLayerModification.Default;
			default2D.mode = RecastNavmeshModifier.Mode.UnwalkableSurface;
			modificationsByLayer2D = RecastGraph.PerLayerModification.ToLayerLookup(perLayerModifications, default2D);
		}

		struct TreeInfo {
			public UnsafeList<int> submeshIndices;
			public Vector3 localScale;
			public bool supportsRotation;
		}

		public struct MeshCollection : IArenaDisposable {
			UnsafeList<UnsafeSpan<Vector3> > vertexBuffers;
			UnsafeList<UnsafeSpan<int> > triangleBuffers;
			UnsafeList<UnsafeSpan<int> > tagsBuffers;
			public NativeArray<RasterizationMesh> meshes;
#if UNITY_EDITOR
			public List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime;
#endif

			public MeshCollection (UnsafeList<UnsafeSpan<Vector3> > vertexBuffers, UnsafeList<UnsafeSpan<int> > triangleBuffers, UnsafeList<UnsafeSpan<int> > tagsBuffers, NativeArray<RasterizationMesh> meshes
#if UNITY_EDITOR
								   , List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime
#endif
								   ) {
				this.vertexBuffers = vertexBuffers;
				this.triangleBuffers = triangleBuffers;
				this.tagsBuffers = tagsBuffers;
				this.meshes = meshes;
#if UNITY_EDITOR
				this.meshesUnreadableAtRuntime = meshesUnreadableAtRuntime;
#endif
			}

			void IArenaDisposable.DisposeWith (DisposeArena arena) {
				for (int i = 0; i < vertexBuffers.Length; i++) {
					arena.Add(vertexBuffers[i]);
					arena.Add(triangleBuffers[i]);
				}
				for (int i = 0; i < tagsBuffers.Length; i++) {
					arena.Add(tagsBuffers[i]);
				}

				arena.Add(meshes);
				arena.Add(vertexBuffers);
				arena.Add(triangleBuffers);
				arena.Add(tagsBuffers);
			}
		}

		[BurstCompile]
		static void CalculateBounds (ref UnsafeSpan<float3> vertices, ref float4x4 localToWorldMatrix, out Bounds bounds) {
			if (vertices.Length == 0) {
				bounds = new Bounds();
			} else {
				float3 max = float.NegativeInfinity;
				float3 min = float.PositiveInfinity;
				for (uint i = 0; i < vertices.Length; i++) {
					var v = math.transform(localToWorldMatrix, vertices[i]);
					max = math.max(max, v);
					min = math.min(min, v);
				}
				bounds = new Bounds((min+max)*0.5f, max-min);
			}
		}

		static void GetMissingMeshDataAndBounds (List<Mesh> meshData, ref UnsafeList<GatheredMesh> gatheredMeshes, ref UnsafeList<UnsafeSpan<Vector3> > vertexBuffers, ref UnsafeList<UnsafeSpan<int> > triangleBuffers) {
			Markers.MarkerGetMissingMeshDataAndBounds.Begin();
#if UNITY_EDITOR
			// This skips the Mesh.isReadable check
			Mesh.MeshDataArray data = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(meshData);
#else
			Mesh.MeshDataArray data = Mesh.AcquireReadOnlyMeshData(meshData);
#endif
			int meshBufferOffset = vertexBuffers.Length;
			meshData.Clear();

			Profiler.BeginSample("Copying vertices");
			// TODO: We should be able to hold the `data` for the whole scan and not have to copy all vertices/triangles
			for (int i = 0; i < data.Length; i++) {
				MeshUtility.GetMeshData(data, i, out var verts, out var tris);
				vertexBuffers.Add(verts.MoveToUnsafeSpan());
				triangleBuffers.Add(tris.MoveToUnsafeSpan());
			}
			Profiler.EndSample();

			data.Dispose();
			Markers.MarkerPatchMissingMeshDataAndBounds.Begin();
			PatchMissingMeshDataAndBounds(ref gatheredMeshes, ref vertexBuffers, meshBufferOffset);
			Markers.MarkerPatchMissingMeshDataAndBounds.End();
			Markers.MarkerGetMissingMeshDataAndBounds.End();
		}

		[BurstCompile]
		static void PatchMissingMeshDataAndBounds (ref UnsafeList<GatheredMesh> gatheredMeshes, ref UnsafeList<UnsafeSpan<Vector3> > vertexBuffers, int meshBufferOffset) {
			var gatheredMeshesSpan = gatheredMeshes.AsUnsafeSpan();
			for (int i = 0; i < gatheredMeshes.Length; i++) {
				ref var gatheredMesh = ref gatheredMeshesSpan[i];

				if (gatheredMesh.meshDataIndex >= 0) {
					var newBufferIndex = meshBufferOffset + gatheredMesh.meshDataIndex;;
					gatheredMesh.meshDataIndex = -newBufferIndex-1;
				}

				if (gatheredMesh.bounds == new Bounds()) {
					int bufferIndex = -(gatheredMesh.meshDataIndex+1);
					var vertexSpan = vertexBuffers[bufferIndex].Reinterpret<float3>();
					// Recalculate bounding box
					float4x4 m = gatheredMesh.matrix;
					Markers.MarkerCalculateBounds.Begin();
					CalculateBounds(ref vertexSpan, ref m, out gatheredMesh.bounds);
					// Pathfinding.Drawing.Draw.WireBox(gatheredMesh.bounds, Color.yellow);
					Markers.MarkerCalculateBounds.End();
				}
			}
		}

		public MeshCollection Finalize () {
			GetMissingMeshDataAndBounds(this.meshData, ref this.meshes, ref this.vertexBuffers, ref this.triangleBuffers);
			var rasterizationMeshes = new NativeArray<RasterizationMesh>(this.meshes.Length, Allocator.Persistent);
			var rasterizationMeshesSpan = rasterizationMeshes.AsUnsafeSpan();

			Markers.MarkerCreateRasterizationMeshes.Begin();
			CreateRasterizationMeshes(ref this.meshes, ref this.vertexBuffers, ref this.triangleBuffers, ref this.tagsBuffers, ref rasterizationMeshesSpan);
			Markers.MarkerCreateRasterizationMeshes.End();

			cachedMeshes.Clear();
			ObjectPoolSimple<Dictionary<MeshCacheItem, int> >.Release(ref cachedMeshes);
			this.meshes.Dispose();

			return new MeshCollection(
				vertexBuffers,
				triangleBuffers,
				tagsBuffers,
				rasterizationMeshes
#if UNITY_EDITOR
				, this.meshesUnreadableAtRuntime
#endif
				);
		}

		[BurstCompile]
		static void CreateRasterizationMeshes (ref UnsafeList<GatheredMesh> meshes, ref UnsafeList<UnsafeSpan<Vector3> > vertexBuffers, ref UnsafeList<UnsafeSpan<int> > triangleBuffers, ref UnsafeList<UnsafeSpan<int> > tagsBuffers, ref UnsafeSpan<RasterizationMesh> rasterizationMeshesOutput) {
			for (int i = 0; i < meshes.Length; i++) {
				var gatheredMesh = meshes[i];
				Assert.IsTrue(gatheredMesh.meshDataIndex < 0);
				int bufferIndex = -(gatheredMesh.meshDataIndex+1);

				var bounds = gatheredMesh.bounds;

				var triangles = triangleBuffers[bufferIndex];
				rasterizationMeshesOutput[i] = new RasterizationMesh {
					vertices = vertexBuffers[bufferIndex].Reinterpret<float3>(),
					triangles = triangles.Slice(gatheredMesh.indexStart, (gatheredMesh.indexEnd != -1 ? gatheredMesh.indexEnd : triangles.Length) - gatheredMesh.indexStart),
					area = gatheredMesh.area | (gatheredMesh.areaIsTag ? VoxelUtilityBurst.TagReg : 0),
					areas = gatheredMesh.tagDataIndex != -1 ? tagsBuffers[gatheredMesh.tagDataIndex] : default,
					bounds = bounds,
					matrix = gatheredMesh.matrix,
					solid = gatheredMesh.solid,
					doubleSided = gatheredMesh.doubleSided,
					flatten = gatheredMesh.flatten,
				};
			}
		}

		/// <summary>
		/// Add vertex and triangle buffers that can later be used to create a <see cref="GatheredMesh"/>.
		///
		/// The returned index can be used in the <see cref="GatheredMesh.meshDataIndex"/> field of the <see cref="GatheredMesh"/> struct.
		///
		/// You can use the returned index multiple times with different matrices, to create instances of the same object in multiple locations.
		/// </summary>
		public int AddMeshBuffers (Vector3[] vertices, int[] triangles) {
			return AddMeshBuffers(new UnsafeSpan<Vector3>(vertices, Allocator.Persistent), new UnsafeSpan<int>(triangles, Allocator.Persistent));
		}

		/// <summary>
		/// Add vertex and triangle buffers that can later be used to create a <see cref="GatheredMesh"/>.
		///
		/// The returned index can be used in the <see cref="GatheredMesh.meshDataIndex"/> field of the <see cref="GatheredMesh"/> struct.
		///
		/// You can use the returned index multiple times with different matrices, to create instances of the same object in multiple locations.
		/// </summary>
		public int AddMeshBuffers (UnsafeSpan<Vector3> vertices, UnsafeSpan<int> triangles) {
			var meshDataIndex = -vertexBuffers.Length-1;

			vertexBuffers.Add(vertices);
			triangleBuffers.Add(triangles);
			return meshDataIndex;
		}

		/// <summary>Add a mesh to the list of meshes to rasterize</summary>
		public void AddMesh (Renderer renderer, Mesh gatheredMesh) {
			if (ConvertMeshToGatheredMesh(renderer, gatheredMesh, out var gm)) {
				meshes.Add(gm);
			}
		}

		/// <summary>Add a mesh to the list of meshes to rasterize</summary>
		public void AddMesh (GatheredMesh gatheredMesh) {
			meshes.Add(gatheredMesh);
		}

		/// <summary>Holds info about a mesh to be rasterized</summary>
		public struct GatheredMesh {
			/// <summary>
			/// Index in the meshData array.
			/// Can be retrieved from the <see cref="RecastMeshGatherer.AddMeshBuffers"/> method.
			/// </summary>
			public int meshDataIndex;
			/// <summary>
			/// Area ID of the mesh. 0 means walkable, and -1 indicates that the mesh should be treated as unwalkable.
			/// Other positive values indicate a custom area ID which will create a seam in the navmesh.
			/// </summary>
			public int area;
			/// <summary>
			/// If not -1, this is the index in the tags array, containing one tag per triangle.
			/// Otherwise, the tag is set by <see cref="area"/>.
			///
			/// <see cref="areaIsTag"/> will be ignored if set.
			/// </summary>
			public int tagDataIndex;
			/// <summary>Start index in the triangle array</summary>
			public int indexStart;
			/// <summary>End index in the triangle array. -1 indicates the end of the array.</summary>
			public int indexEnd;


			/// <summary>World bounds of the mesh. Assumed to already be multiplied with the <see cref="matrix"/>.</summary>
			public Bounds bounds;

			/// <summary>Matrix to transform the vertices by</summary>
			public Matrix4x4 matrix;

			/// <summary>
			/// If true then the mesh will be treated as solid and its interior will be unwalkable.
			/// The unwalkable region will be the minimum to maximum y coordinate in each cell.
			/// </summary>
			public bool solid;
			/// <summary>See <see cref="RasterizationMesh.doubleSided"/></summary>
			public bool doubleSided;
			/// <summary>See <see cref="RasterizationMesh.flatten"/></summary>
			public bool flatten;
			/// <summary>See <see cref="RasterizationMesh.area"/></summary>
			public bool areaIsTag;

			/// <summary>
			/// Recalculate the <see cref="bounds"/> from the vertices.
			///
			/// The bounds will not be recalculated immediately.
			/// </summary>
			public void RecalculateBounds () {
				// This will cause the bounds to be recalculated later
				bounds = new Bounds();
			}

			public void ApplyNavmeshModifier (RecastNavmeshModifier navmeshModifier) {
				area = AreaFromSurfaceMode(navmeshModifier.mode, navmeshModifier.surfaceID);
				areaIsTag = navmeshModifier.mode == RecastNavmeshModifier.Mode.WalkableSurfaceWithTag;
				solid |= navmeshModifier.solid;
			}

			public void ApplyLayerModification (RecastGraph.PerLayerModification modification) {
				area = AreaFromSurfaceMode(modification.mode, modification.surfaceID);
				areaIsTag = modification.mode == RecastNavmeshModifier.Mode.WalkableSurfaceWithTag;
			}
		}

		enum MeshType {
			Mesh,
			Box,
			Capsule,
		}

		struct MeshCacheItem : IEquatable<MeshCacheItem> {
			public MeshType type;
			public Mesh mesh;
			public int rows;
			public int quantizedHeight;

			public MeshCacheItem (Mesh mesh) {
				type = MeshType.Mesh;
				this.mesh = mesh;
				rows = 0;
				quantizedHeight = 0;
			}

			public static readonly MeshCacheItem Box = new MeshCacheItem {
				type = MeshType.Box,
				mesh = null,
				rows = 0,
				quantizedHeight = 0,
			};

			public bool Equals (MeshCacheItem other) {
				return type == other.type && mesh == other.mesh && rows == other.rows && quantizedHeight == other.quantizedHeight;
			}

			public override int GetHashCode () {
				return (((int)type * 31 ^ (mesh != null ? mesh.GetHashCode() : -1)) * 31 ^ rows) * 31 ^ quantizedHeight;
			}
		}

		bool MeshFilterShouldBeIncluded (MeshFilter filter) {
			if (filter.TryGetComponent<Renderer>(out var rend)) {
				if (filter.sharedMesh != null && rend.enabled && (((1 << filter.gameObject.layer) & mask) != 0 || (tagMask.Count > 0 && tagMask.Contains(filter.tag)))) {
					if (!(filter.TryGetComponent<RecastNavmeshModifier>(out var rmo) && rmo.enabled)) {
						return true;
					}
				}
			}
			return false;
		}

		bool ConvertMeshToGatheredMesh (Renderer renderer, Mesh mesh, out GatheredMesh gatheredMesh) {
			// Ignore meshes that do not have a Position vertex attribute.
			// This can happen for meshes that are empty, i.e. have no vertices at all.
			if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Position)) {
				gatheredMesh = default;
				return false;
			}

#if !UNITY_EDITOR
			if (!mesh.isReadable) {
				// Cannot scan this
				if (!anyNonReadableMesh) {
					Debug.LogError("Some meshes could not be included when scanning the graph because they are marked as not readable. This includes the mesh '" + mesh.name + "'. You need to mark the mesh with read/write enabled in the mesh importer. Alternatively you can only rasterize colliders and not meshes. Mesh Collider meshes still need to be readable.", mesh);
				}
				anyNonReadableMesh = true;
				gatheredMesh = default;
				return false;
			}
#endif

			renderer.GetSharedMaterials(dummyMaterials);
			var submeshStart = renderer is MeshRenderer mrend ? mrend.subMeshStartIndex : 0;
			var submeshCount = dummyMaterials.Count;

			int indexStart = 0;
			int indexEnd = -1;
			if (submeshStart > 0 || submeshCount < mesh.subMeshCount) {
				var a = mesh.GetSubMesh(submeshStart);
				var b = mesh.GetSubMesh(submeshStart + submeshCount - 1);
				indexStart = a.indexStart;
				indexEnd = b.indexStart + b.indexCount;
			}

			// Check the cache to avoid allocating
			// a new array unless necessary
			if (!cachedMeshes.TryGetValue(new MeshCacheItem(mesh), out int meshBufferIndex)) {
#if UNITY_EDITOR
				if (!mesh.isReadable) meshesUnreadableAtRuntime.Add((renderer, mesh));
#endif
				meshBufferIndex = meshData.Count;
				meshData.Add(mesh);
				cachedMeshes[new MeshCacheItem(mesh)] = meshBufferIndex;
			}

			gatheredMesh = new GatheredMesh {
				meshDataIndex = meshBufferIndex,
				bounds = renderer.bounds,
				indexStart = indexStart,
				indexEnd = indexEnd,
				matrix = renderer.localToWorldMatrix,
				doubleSided = false,
				flatten = false,
				tagDataIndex = -1,
			};
			return true;
		}

		GatheredMesh? GetColliderMesh (MeshCollider collider, Matrix4x4 localToWorldMatrix) {
			if (collider.sharedMesh != null) {
				Mesh mesh = collider.sharedMesh;

				// Ignore meshes that do not have a Position vertex attribute.
				// This can happen for meshes that are empty, i.e. have no vertices at all.
				if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Position)) {
					return null;
				}

#if !UNITY_EDITOR
				if (!mesh.isReadable) {
					// Cannot scan this
					if (!anyNonReadableMesh) {
						Debug.LogError("Some mesh collider meshes could not be included when scanning the graph because they are marked as not readable. This includes the mesh '" + mesh.name + "'. You need to mark the mesh with read/write enabled in the mesh importer.", mesh);
					}
					anyNonReadableMesh = true;
					return null;
				}
#endif

				// Check the cache to avoid allocating
				// a new array unless necessary
				if (!cachedMeshes.TryGetValue(new MeshCacheItem(mesh), out int meshDataIndex)) {
#if UNITY_EDITOR
					if (!mesh.isReadable) meshesUnreadableAtRuntime.Add((collider, mesh));
#endif
					meshDataIndex = meshData.Count;
					meshData.Add(mesh);
					cachedMeshes[new MeshCacheItem(mesh)] = meshDataIndex;
				}

				return new GatheredMesh {
						   meshDataIndex = meshDataIndex,
						   bounds = collider.bounds,
						   areaIsTag = false,
						   area = 0,
						   tagDataIndex = -1,
						   indexStart = 0,
						   indexEnd = -1,
						   // Treat the collider as solid iff the collider is convex
						   solid = collider.convex,
						   matrix = localToWorldMatrix,
						   doubleSided = false,
						   flatten = false,
				};
			}

			return null;
		}

		public void CollectSceneMeshes () {
			if (tagMask.Count > 0 || mask != 0) {
				// This is unfortunately the fastest way to find all mesh filters.. and it is not particularly fast.
				// Note: We have to sort these because the recast graph is not completely deterministic in terms of ordering of meshes.
				// Different ordering can in rare cases lead to different spans being merged which can lead to different navmeshes.
				var meshFilters = UnityCompatibility.FindObjectsByTypeSorted<MeshFilter>();
				bool containedStatic = false;

				for (int i = 0; i < meshFilters.Length; i++) {
					MeshFilter filter = meshFilters[i];

					if (!MeshFilterShouldBeIncluded(filter)) continue;

					// Note, guaranteed to have a renderer as MeshFilterShouldBeIncluded checks for it.
					// but it can be either a MeshRenderer or a SkinnedMeshRenderer
					filter.TryGetComponent<Renderer>(out var rend);

					if (rend.isPartOfStaticBatch) {
						// Statically batched meshes cannot be used due to Unity limitations
						// log a warning about this
						containedStatic = true;
					} else {
						// Only include it if it intersects with the graph
						if (rend.bounds.Intersects(bounds)) {
							if (ConvertMeshToGatheredMesh(rend, filter.sharedMesh, out var gatheredMesh)) {
								gatheredMesh.ApplyLayerModification(modificationsByLayer[filter.gameObject.layer]);
								meshes.Add(gatheredMesh);
							}
						}
					}
				}

				if (containedStatic) {
					Debug.LogWarning("Some meshes were statically batched. These meshes can not be used for navmesh calculation" +
						" due to technical constraints.\nDuring runtime scripts cannot access the data of meshes which have been statically batched.\n" +
						"One way to solve this problem is to use cached startup (Save & Load tab in the inspector) to only calculate the graph when the game is not playing.");
				}
			}
		}

		static int AreaFromSurfaceMode (RecastNavmeshModifier.Mode mode, int surfaceID) {
			switch (mode) {
			default:
			case RecastNavmeshModifier.Mode.UnwalkableSurface:
				return -1;
			case RecastNavmeshModifier.Mode.WalkableSurface:
				return 0;
			case RecastNavmeshModifier.Mode.WalkableSurfaceWithSeam:
			case RecastNavmeshModifier.Mode.WalkableSurfaceWithTag:
				return surfaceID;
			}
		}

		/// <summary>Find all relevant RecastNavmeshModifier components and create ExtraMeshes for them</summary>
		public void CollectRecastNavmeshModifiers () {
			var buffer = ListPool<RecastNavmeshModifier>.Claim();

			// Get all recast navmesh modifiers inside the bounds
			RecastNavmeshModifier.GetAllInBounds(buffer, bounds);

			// Create an RasterizationMesh object
			// for each RecastNavmeshModifier
			for (int i = 0; i < buffer.Count; i++) {
				AddNavmeshModifier(buffer[i]);
			}

			ListPool<RecastNavmeshModifier>.Release(ref buffer);
		}

		void AddNavmeshModifier (RecastNavmeshModifier navmeshModifier) {
			if (navmeshModifier.includeInScan == RecastNavmeshModifier.ScanInclusion.AlwaysExclude) return;
			if (navmeshModifier.includeInScan == RecastNavmeshModifier.ScanInclusion.Auto && (((mask >> navmeshModifier.gameObject.layer) & 1) == 0 && !tagMask.Contains(navmeshModifier.tag))) return;

			navmeshModifier.ResolveMeshSource(out var filter, out var collider, out var collider2D);

			if (filter != null) {
				// Add based on mesh filter
				Mesh mesh = filter.sharedMesh;
				if (filter.TryGetComponent<MeshRenderer>(out var rend) && mesh != null) {
					if (ConvertMeshToGatheredMesh(rend, filter.sharedMesh, out var gatheredMesh)) {
						gatheredMesh.ApplyNavmeshModifier(navmeshModifier);
						meshes.Add(gatheredMesh);
					}
				}
			} else if (collider != null) {
				// Add based on collider

				if (ConvertColliderToGatheredMesh(collider) is GatheredMesh rmesh) {
					rmesh.ApplyNavmeshModifier(navmeshModifier);
					meshes.Add(rmesh);
				}
			} else if (collider2D != null) {
				// 2D colliders are handled separately
			} else {
				if (navmeshModifier.geometrySource == RecastNavmeshModifier.GeometrySource.Auto) {
					Debug.LogError("Couldn't get geometry source for RecastNavmeshModifier ("+navmeshModifier.gameObject.name +"). It didn't have a collider or MeshFilter+Renderer attached", navmeshModifier.gameObject);
				} else {
					Debug.LogError("Couldn't get geometry source for RecastNavmeshModifier ("+navmeshModifier.gameObject.name +"). It didn't have a " + navmeshModifier.geometrySource + " attached", navmeshModifier.gameObject);
				}
			}
		}

		public void CollectTerrainMeshes (bool rasterizeTrees, float desiredChunkSize) {
			// Find all terrains in the scene
			var terrains = Terrain.activeTerrains;

			if (terrains.Length > 0) {
				// Loop through all terrains in the scene
				for (int j = 0; j < terrains.Length; j++) {
					if (terrains[j].terrainData == null) continue;

					Profiler.BeginSample("Generate terrain chunks");
					bool anyTerrainChunks = GenerateTerrainChunks(terrains[j], bounds, desiredChunkSize);
					Profiler.EndSample();

					// Don't rasterize trees if the terrain did not intersect the graph bounds
					if (rasterizeTrees && anyTerrainChunks) {
						Profiler.BeginSample("Find tree meshes");
						// Rasterize all tree colliders on this terrain object
						CollectTreeMeshes(terrains[j]);
						Profiler.EndSample();
					}
				}
			}
		}

		static int NonNegativeModulus (int x, int m) {
			int r = x%m;
			return r < 0 ? r+m : r;
		}

		/// <summary>Returns ceil(lhs/rhs), i.e lhs/rhs rounded up</summary>
		static int CeilDivision (int lhs, int rhs) {
			return (lhs + rhs - 1)/rhs;
		}

		static void GetAlphamaps (List<RecastGraph.PerTerrainLayerModification> perTerrainLayerModifications, out UnsafeSpan<UnsafeSpan<byte> > alphamaps, out UnsafeSpan<int> areaMapping, out UnsafeSpan<float> areaMappingThresholds, out float alphamapScale, TerrainData terrainData, int terrainDownsamplingFactor) {
			var heightmapResolution = terrainData.heightmapResolution;
			if (perTerrainLayerModifications.Count > 0) {
				alphamaps = new UnsafeSpan<UnsafeSpan<byte> >(Allocator.TempJob, terrainData.alphamapTextureCount);
				var alphamapResolution = terrainData.alphamapResolution;
				var optimalMipmap = math.max(0, math.min((int)math.log2(alphamapResolution), (int)math.floor(math.log2((float)alphamapResolution / ((heightmapResolution-1) / terrainDownsamplingFactor)) + 0.001f)));
				for (int i = 0; i < terrainData.alphamapTextureCount; i++) {
					var tex = terrainData.GetAlphamapTexture(i);
					Assert.IsTrue(tex.isReadable, "Terrain alphamap texture is not readable.");
					Assert.IsTrue(tex.dimension == UnityEngine.Rendering.TextureDimension.Tex2D, "Terrain alphamap texture must be a 2D texture.");
					Assert.IsTrue(tex.width == alphamapResolution && tex.height == alphamapResolution, "Terrain alphamap texture size does not match terrain data.");
					Assert.IsTrue(tex.format == TextureFormat.RGBA32, "Terrain alphamap texture must be in RGBA32 format.");
					Assert.IsTrue(tex.mipmapCount > 1, "Terrain alphamap texture must have mipmaps enabled.");
					alphamaps[i] = terrainData.GetAlphamapTexture(i).GetPixelData<byte>(optimalMipmap).AsUnsafeReadOnlySpan();
					Assert.IsTrue(alphamaps[i].Length == ((alphamapResolution * alphamapResolution) >> (2*optimalMipmap)) * 4, "Terrain alphamap texture data length does not match expected size.");
				}
				areaMapping = new UnsafeSpan<int>(Allocator.TempJob, terrainData.alphamapTextureCount * 4);
				areaMappingThresholds = new UnsafeSpan<float>(Allocator.TempJob, terrainData.alphamapTextureCount * 4);
				areaMapping.FillZeros();
				areaMappingThresholds.Fill(0.5f);

				var alphamapMipmapResolution = alphamapResolution >> optimalMipmap;
				// If the alphamap control textures have a texture space of (0,0) in the lower left corner to (W,H) in the upper right corner,
				// then this is mapped to the terrain from (0.5,0.5) to (W-0.5,H-0.5). Presumably to allow smoother transitions between adjacent terrains.
				// This means the alphamapScale needs to be adjusted to account for this.
				// (0,0) terrain sample space => (0,0) alphamap space
				// (heightmapResolution-1, heightmapResolution-1) => (alphamapMipmapResolution-1, alphamapMipmapResolution-1)
				alphamapScale = (alphamapMipmapResolution - 1) / (float)(heightmapResolution-1);
			} else {
				alphamaps = default;
				areaMapping = default;
				areaMappingThresholds = default;
				alphamapScale = default;
			}
		}

		static void CalculateTerrainChunkLayout (float desiredChunkSize, Vector3 sampleSize, int terrainDownsamplingFactor, int heightmapResolution, Bounds bounds, Vector3 offset, out IntRect sampleRect, out Vector2Int chunks, out Vector2Int chunkSize) {
			// Make chunks at least 12 quads wide
			// since too small chunks just decreases performance due
			// to the overhead of checking for bounds and similar things
			const int MinChunkSize = 12;

			// Find the number of samples along each edge that corresponds to a world size of desiredChunkSize
			// Then round up to the nearest multiple of terrainSampleSize
			chunkSize = new Vector2Int(
				Mathf.CeilToInt(Mathf.Max(desiredChunkSize / (sampleSize.x * terrainDownsamplingFactor), MinChunkSize)) * terrainDownsamplingFactor,
				Mathf.CeilToInt(Mathf.Max(desiredChunkSize / (sampleSize.z * terrainDownsamplingFactor), MinChunkSize)) * terrainDownsamplingFactor
				);
			chunkSize.x = Mathf.Min(chunkSize.x, heightmapResolution);
			chunkSize.y = Mathf.Min(chunkSize.y, heightmapResolution);

			Vector2Int startSample;
			if (float.IsFinite(bounds.size.x)) {
				startSample = new Vector2Int(
					Mathf.FloorToInt((bounds.min.x - offset.x) / sampleSize.x),
					Mathf.FloorToInt((bounds.min.z - offset.z) / sampleSize.z)
					);

				// Ensure we always start at a multiple of the terrainDownsamplingFactor.
				// Otherwise, the generated meshes may not look the same, depending on the original bounds.
				// Not rounding would not technically be wrong, but this makes it more predictable.
				startSample.x -= NonNegativeModulus(startSample.x, terrainDownsamplingFactor);
				startSample.y -= NonNegativeModulus(startSample.y, terrainDownsamplingFactor);

				// Figure out which chunks might intersect the bounding box
				var worldChunkSizeAlongX = chunkSize.x * sampleSize.x;
				var worldChunkSizeAlongZ = chunkSize.y * sampleSize.z;
				chunks = new Vector2Int(
					Mathf.CeilToInt((bounds.max.x - offset.x - startSample.x * sampleSize.x) / worldChunkSizeAlongX),
					Mathf.CeilToInt((bounds.max.z - offset.z - startSample.y * sampleSize.z) / worldChunkSizeAlongZ)
					);
			} else {
				startSample = new Vector2Int(0, 0);
				chunks = new Vector2Int(CeilDivision(heightmapResolution, chunkSize.x), CeilDivision(heightmapResolution, chunkSize.y));
			}

			// Figure out which samples we need from the terrain heightmap
			sampleRect = new IntRect(0, 0, chunks.x * chunkSize.x, chunks.y * chunkSize.y).Offset(startSample);
			var allSamples = new IntRect(0, 0, heightmapResolution - 1, heightmapResolution - 1);
			// Clamp the samples to the heightmap bounds
			sampleRect = IntRect.Intersection(sampleRect, allSamples);

			chunks = new Vector2Int(CeilDivision(sampleRect.Width, chunkSize.x), CeilDivision(sampleRect.Height, chunkSize.y));
		}

		bool GenerateTerrainChunks (Terrain terrain, Bounds bounds, float desiredChunkSize) {
			var terrainData = terrain.terrainData;

			if (terrainData == null)
				throw new ArgumentException("Terrain contains no terrain data");

			Vector3 offset = terrain.GetPosition();
			var terrainSize = terrainData.size;
			Vector3 center = offset + terrainSize * 0.5F;

			// Figure out the bounds of the terrain in world space
			var terrainBounds = new Bounds(center, terrainSize);

			// Only include terrains which intersects the graph
			if (!terrainBounds.Intersects(bounds))
				return false;

			// Size of a single sample
			Vector3 sampleSize = terrainData.heightmapScale;
			sampleSize.y = terrainSize.y;

			CalculateTerrainChunkLayout(
				desiredChunkSize,
				sampleSize,
				terrainDownsamplingFactor,
				terrainData.heightmapResolution, // Original heightmap size
				bounds,
				offset,
				out var sampleRect,
				out var chunks,
				out var chunkSize
				);
			if (!sampleRect.IsValid()) return false;

			Profiler.BeginSample("Get heightmap data");
			float[, ] heights = terrainData.GetHeights(
				sampleRect.xmin,
				sampleRect.ymin,
				sampleRect.Width,
				sampleRect.Height
				);
			bool[, ] holes = terrainData.GetHoles(
				sampleRect.xmin,
				sampleRect.ymin,
				sampleRect.Width - 1,
				sampleRect.Height - 1
				);
			Profiler.EndSample();

			Profiler.BeginSample("Get alphamap textures");
			GetAlphamaps(perTerrainLayerModifications, out var alphamaps, out var areaMapping, out var areaMappingThresholds, out var alphamapScale, terrainData, terrainDownsamplingFactor);
			Profiler.EndSample();

			for (int i = 0; i < perTerrainLayerModifications.Count; i++) {
				var mod = perTerrainLayerModifications[i];
				if (mod.layer >= 0 && mod.layer < areaMapping.Length) {
					areaMapping[mod.layer] = AreaFromSurfaceMode(mod.mode, mod.surfaceID) | (mod.mode == RecastNavmeshModifier.Mode.WalkableSurfaceWithTag ? VoxelUtilityBurst.TagReg : 0);
					areaMappingThresholds[mod.layer] = mod.threshold;
				}
			}

			unsafe {
				var heightsSpan = new UnsafeSpan<float>(heights, out var gcHandle1);
				var holesSpan = new UnsafeSpan<bool>(holes, out var gcHandle2);

				var chunksOffset = offset + new Vector3(sampleRect.xmin * sampleSize.x, 0, sampleRect.ymin * sampleSize.z);
				var chunksMatrix = Matrix4x4.TRS(chunksOffset, Quaternion.identity, sampleSize);
				var output = new NativeArray<JobGenerateHeightmapChunk.TerrainChunk>(chunks.x * chunks.y, Allocator.TempJob);

				Profiler.BeginSample("Generate chunks");
				var job = new JobGenerateHeightmapChunk {
					heights = heightsSpan,
					holes = holesSpan,
					sampleRect = sampleRect,
					chunkSize = chunkSize,
					chunks = chunks,
					stride = terrainDownsamplingFactor,
					alphamapScale = alphamapScale,
					alphaMaps = alphamaps,
					areaMapping = areaMapping,
					areaMappingThresholds = areaMappingThresholds,
					output = output,
				};
				job.ScheduleParallel(chunks.x * chunks.y, 1, default).Complete();
				Profiler.EndSample();

				for (int z = 0; z < chunks.y; z++) {
					for (int x = 0; x < chunks.x; x++) {
						var chunk = output[z * chunks.x + x];
						var meshDataIndex = AddMeshBuffers(chunk.verts, chunk.tris);

						int tagDataIndex = -1;
						if (chunk.tags.Length > 0) {
							tagDataIndex = tagsBuffers.Length;
							tagsBuffers.Add(chunk.tags);
						}

						// Calculate the bounding box of the terrain chunk (axis-aligned, since chunksMatrix is axis-aligned)
						// Conservative estimate: minY = 0, maxY = terrainSize.y
						// Calculate world-space min/max
						var min = chunksMatrix.MultiplyPoint3x4(new Vector3(x * chunkSize.x, 0, z * chunkSize.y));
						var max = chunksMatrix.MultiplyPoint3x4(new Vector3(Mathf.Min((x + 1) * chunkSize.x, sampleRect.xmax), 1, Mathf.Min((z + 1) * chunkSize.y, sampleRect.ymax)));

						// Clamp the bounds to the graph's bounds. This is mostly to prevent the chunk bounds from being VERY tall, since the max height of the terrain can oftne be very high (even if it is unused).
						// Typically this affects nothing, though. But for a rotated graph, this could make PutMeshesIntoTileBuckets more efficient. Seems strange to ever have a rotated graph together with a terrain, though.
						min = Vector3.Max(min, bounds.min);
						max = Vector3.Min(max, bounds.max);
						var chunkBounds = new Bounds();
						chunkBounds.SetMinMax(min, max);

						var chunkMesh = new GatheredMesh {
							meshDataIndex = meshDataIndex,
							// An empty bounding box indicates that it should be calculated from the vertices later.
							bounds = chunkBounds,
							indexStart = 0,
							indexEnd = -1,
							areaIsTag = false,
							area = 0,
							tagDataIndex = tagDataIndex,
							solid = false,
							matrix = chunksMatrix,
							doubleSided = false,
							flatten = false,
						};
						chunkMesh.ApplyLayerModification(modificationsByLayer[terrain.gameObject.layer]);

						meshes.Add(chunkMesh);
					}
				}

				// Release the temporary arrays
				output.Dispose();
				alphamaps.Free(Allocator.TempJob);
				areaMapping.Free(Allocator.TempJob);
				areaMappingThresholds.Free(Allocator.TempJob);
				UnsafeUtility.ReleaseGCObject(gcHandle1);
				UnsafeUtility.ReleaseGCObject(gcHandle2);
			}
			return true;
		}

		/// <summary>Generates a terrain chunk mesh</summary>
		[BurstCompile]
		struct JobGenerateHeightmapChunk : IJobFor {
			public UnsafeSpan<float> heights;
			public UnsafeSpan<bool> holes;
			public IntRect sampleRect;
			public Vector2Int chunkSize;
			public Vector2Int chunks;
			public int stride;
			public float alphamapScale;
			public UnsafeSpan<UnsafeSpan<byte> > alphaMaps;
			public UnsafeSpan<int> areaMapping;
			public UnsafeSpan<float> areaMappingThresholds;
			public NativeArray<TerrainChunk> output;

			public struct TerrainChunk {
				public UnsafeSpan<Vector3> verts;
				public UnsafeSpan<int> tris;
				public UnsafeSpan<int> tags;
			}

			public void Execute (int index) {
				var width = chunkSize.x;
				var depth = chunkSize.y;
				int x0 = (index % chunks.x) * width;
				int z0 = (index / chunks.x) * depth;
				// Downsample to a smaller mesh (full resolution will take a long time to rasterize)
				// Round up the width to the nearest multiple of terrainSampleSize and then add 1
				// (off by one because there are vertices at the edge of the mesh)
				int heightmapWidth = sampleRect.Width;
				int heightmapDepth = sampleRect.Height;
				int resultWidth = CeilDivision(Mathf.Min(width, heightmapWidth - x0), stride) + 1;
				int resultDepth = CeilDivision(Mathf.Min(depth, heightmapDepth - z0), stride) + 1;

				// Create a mesh from the heightmap
				var numVerts = resultWidth * resultDepth;
				var numQuads = (resultWidth-1)*(resultDepth-1);
				var verts = new UnsafeSpan<Vector3>(Allocator.Persistent, numVerts);
				var tris = new UnsafeSpan<int>(Allocator.Persistent, numQuads*2*3);

				// Create lots of vertices
				for (int z = 0; z < resultDepth; z++) {
					int sampleZ = Math.Min(z0 + z*stride, heightmapDepth-1);
					for (int x = 0; x < resultWidth; x++) {
						int sampleX = Math.Min(x0 + x*stride, heightmapWidth-1);
						verts[z*resultWidth + x] = new Vector3(sampleX, heights[sampleZ*heightmapWidth + sampleX], sampleZ);
					}
				}

				var alphamapResolution = 0;
				UnsafeSpan<int> tags = default;
				if (alphaMaps.Length > 0) {
					Assert.AreEqual(areaMappingThresholds.Length, areaMapping.Length);
					Assert.AreEqual(alphaMaps.Length*4, areaMappingThresholds.Length);
					alphamapResolution = (int)math.sqrt(alphaMaps[0].Length/4);
					for (int i = 0; i < alphaMaps.Length; i++) {
						Assert.AreEqual(alphaMaps[i].Length, alphamapResolution*alphamapResolution*4);
					}
					tags = new UnsafeSpan<int>(Allocator.Persistent, numQuads*2);
				} else {
					// No tags, so we can skip allocating the tags array
				}

				// Create the mesh by creating triangles in a grid like pattern
				int triangleIndex = 0;
				int tagIndex = 0;

				for (int z = 0; z < resultDepth-1; z++) {
					for (int x = 0; x < resultWidth-1; x++) {
						// Try to check if the center of the cell is a hole or not.
						// Note that the holes array has a size which is 1 less than the heightmap size
						int sampleX = Math.Min(x0 + stride/2 + x*stride, heightmapWidth-2);
						int sampleZ = Math.Min(z0 + stride/2 + z*stride, heightmapDepth-2);

						// Skip holes
						if (!holes[sampleZ*(heightmapWidth-1) + sampleX]) continue;

						if (alphaMaps.Length > 0) {
							var c0 = (new float2(x0 + sampleRect.xmin + x*stride, z0 + sampleRect.ymin + z*stride) + 0.5f*stride) * alphamapScale;
							var c = math.clamp((int2)math.round(c0), 0, alphamapResolution-1);
							var alphamapIndex = (uint)(c.x + c.y * alphamapResolution) * 4;
							var bestArea = 0;
							var bestWeight = -1f;
							for (uint i = 0; i < areaMappingThresholds.Length; i++) {
								var s = alphaMaps[i/4][alphamapIndex + (i % 4)]*(1/255f) - areaMappingThresholds[i];
								if (s > bestWeight) {
									bestWeight = s;
									bestArea = areaMapping[i];
								}
							}

							tags[tagIndex] = bestArea;
							tags[tagIndex+1] = bestArea;
							tagIndex += 2;
						}

						// Generate two triangles here
						tris[triangleIndex]   = z*resultWidth + x;
						tris[triangleIndex+1] = (z+1)*resultWidth + x+1;
						tris[triangleIndex+2] = z*resultWidth + x+1;
						triangleIndex += 3;
						tris[triangleIndex]   = z*resultWidth + x;
						tris[triangleIndex+1] = (z+1)*resultWidth + x;
						tris[triangleIndex+2] = (z+1)*resultWidth + x+1;
						triangleIndex += 3;
					}
				}

				tris = tris.Slice(0, triangleIndex);
				if (tags.Length > 0) tags = tags.Slice(0, tagIndex);

				output[index] = new TerrainChunk {
					verts = verts,
					tris = tris,
					tags = tags
				};
			}
		}

		void CollectTreeMeshes (Terrain terrain) {
			Profiler.BeginSample("Get tree data from terrain");
			TerrainData data = terrain.terrainData;
			var treeInstances = data.treeInstances;
			var treePrototypes = data.treePrototypes;
			Profiler.EndSample();

			Profiler.BeginSample("Process tree prototypes");
			var treeInfos = new UnsafeSpan<TreeInfo>(Allocator.Temp, treePrototypes.Length);
			var colliders = ListPool<Collider>.Claim();
			var allSubmeshes = new UnsafeList<GatheredMesh>(4, Allocator.Temp);

			var prevMeshData = this.meshData;
			var prevCachedMeshes = this.cachedMeshes;
			// Temporarily swap with a new list, to be able to process tree prototypes before the rest of the meshes
			this.meshData = new List<Mesh>();
			this.cachedMeshes = new Dictionary<MeshCacheItem, int>();
			var cachedTreePrefabs = new Dictionary<GameObject, TreeInfo>();

			for (int i = 0; i < treePrototypes.Length; i++) {
				TreePrototype prot = treePrototypes[i];
				// Make sure that the tree prefab exists
				if (prot.prefab == null) {
					treeInfos[i] = new TreeInfo { submeshIndices = new UnsafeList<int>(0, Allocator.Temp) };
					continue;
				}

				if (!cachedTreePrefabs.TryGetValue(prot.prefab, out TreeInfo treeInfo)) {
					treeInfo.submeshIndices = new UnsafeList<int>(4, Allocator.Temp);

					// The unity terrain system only supports rotation for trees with a LODGroup on the root object.
					// Unity still sets the instance.rotation field to values even they are not used, so we need to explicitly check for this.
					treeInfo.supportsRotation = prot.prefab.TryGetComponent<LODGroup>(out var dummy);
					treeInfo.localScale = prot.prefab.transform.localScale;

					var rootMatrixInv = prot.prefab.transform.localToWorldMatrix.inverse;
					colliders.Clear();
					prot.prefab.GetComponentsInChildren(false, colliders);
					for (int j = 0; j < colliders.Count; j++) {
						// The prefab has a collider, use that instead
						var collider = colliders[j];

						// Generate a mesh from the collider
						if (ConvertColliderToGatheredMesh(collider, rootMatrixInv * collider.transform.localToWorldMatrix) is GatheredMesh mesh) {
							// For trees, we only suppport generating a mesh from a collider. So we ignore the navmeshModifier.geometrySource field.
							if (collider.gameObject.TryGetComponent<RecastNavmeshModifier>(out var navmeshModifier) && navmeshModifier.enabled) {
								if (navmeshModifier.includeInScan == RecastNavmeshModifier.ScanInclusion.AlwaysExclude) continue;

								mesh.ApplyNavmeshModifier(navmeshModifier);
							} else {
								mesh.ApplyLayerModification(modificationsByLayer[collider.gameObject.layer]);
							}

							// The bounds are incorrectly based on collider.bounds, and may not even be initialized by the physics system.
							// It is incorrect because the collider is on the prefab, not on the tree instance
							// so we need to recalculate the bounds based on the actual vertex positions
							mesh.RecalculateBounds();

							treeInfo.submeshIndices.Add(allSubmeshes.Length);
							allSubmeshes.Add(mesh);
						}
					}

					cachedTreePrefabs[prot.prefab] = treeInfo;
				}
				treeInfos[i] = treeInfo;
			}
			ListPool<Collider>.Release(ref colliders);
			Profiler.EndSample();

			// We process the meshes here in order to calculate their correct bounds.
			// This is used to be able to set all tree instances' bounds correctly (as not doing so will make it fall back to calculating every single instance's bounds individually).
			GetMissingMeshDataAndBounds(this.meshData, ref allSubmeshes, ref vertexBuffers, ref triangleBuffers);

			this.meshData = prevMeshData;
			this.cachedMeshes = prevCachedMeshes;

			Profiler.BeginSample("Convert trees to meshes");
			var treeInstancesNative = new UnsafeSpan<TreeInstance>(treeInstances, out var gcHandle);
			var bounds = this.bounds;
			var terrainPos = (float3)terrain.transform.position;
			var terrainSize = (float3)data.size;
			ConvertTreesToMeshes(
				ref treeInstancesNative,
				ref terrainPos,
				ref terrainSize,
				ref treeInfos,
				ref allSubmeshes,
				ref bounds,
				ref meshes
				);
			treeInfos.Free(Allocator.Temp);
			UnsafeUtility.ReleaseGCObject(gcHandle);
			Profiler.EndSample();
		}

		[BurstCompile]
		static void ConvertTreesToMeshes (
			ref UnsafeSpan<TreeInstance> treeInstances,
			ref float3 terrainPos,
			ref float3 terrainSize,
			ref UnsafeSpan<TreeInfo> treeInfos,
			ref UnsafeList<GatheredMesh> allSubmeshes,
			ref Bounds graphBounds,
			ref UnsafeList<GatheredMesh> meshes) {
			for (int i = 0; i < treeInstances.Length; i++) {
				TreeInstance instance = treeInstances[i];
				var treeInfo = treeInfos[instance.prototypeIndex];
				if (treeInfo.submeshIndices.IsEmpty) continue;

				var treePosition = terrainPos +  (float3)instance.position * terrainSize;
				var instanceSize = new float3(instance.widthScale, instance.heightScale, instance.widthScale);
				var prefabScale = instanceSize * (float3)treeInfo.localScale;
				var rotation = treeInfo.supportsRotation ? Quaternion.AngleAxis(instance.rotation * Mathf.Rad2Deg, Vector3.up) : Quaternion.identity;
				var matrix = Matrix4x4.TRS(treePosition, rotation, prefabScale);

				for (int j = 0; j < treeInfo.submeshIndices.Length; j++) {
					var item = allSubmeshes[treeInfo.submeshIndices[j]];
					var m = matrix * item.matrix;
					item.matrix = m;
					item.bounds = MathExtensions.BoundsOfTransformedBounds(m, item.bounds);
					if (graphBounds.Intersects(item.bounds)) {
						meshes.Add(item);
					}
				}
			}
		}

		bool ShouldIncludeCollider (Collider collider) {
			if (!collider.enabled || collider.isTrigger || !collider.bounds.Intersects(bounds) || (collider.TryGetComponent<RecastNavmeshModifier>(out var rmo) && rmo.enabled)) return false;

			var go = collider.gameObject;
			if (((mask >> go.layer) & 1) != 0) return true;

			// Iterate over the tag mask and use CompareTag instead of tagMask.Includes(collider.tag), as this will not allocate.
			for (int i = 0; i < tagMask.Count; i++) {
				if (go.CompareTag(tagMask[i])) return true;
			}
			return false;
		}

		public void CollectColliderMeshes () {
			if (tagMask.Count == 0 && mask == 0) return;

			// Find all colliders that could possibly be inside the bounds
			// TODO: Benchmark?
			// Repeatedly do a OverlapBox check and make the buffer larger if it's too small.
			Profiler.BeginSample("Find colliders in bounds");
			int numColliders = 256;
			Collider[] colliderBuffer = null;
			bool finiteBounds = math.all(math.isfinite(bounds.extents));
			if (!finiteBounds) {
				colliderBuffer = UnityCompatibility.FindObjectsByTypeSorted<Collider>();
				numColliders = colliderBuffer.Length;
			} else {
				do {
					if (colliderBuffer != null) ArrayPool<Collider>.Release(ref colliderBuffer);
					colliderBuffer = ArrayPool<Collider>.Claim(numColliders * 4);
					numColliders = physicsScene.OverlapBox(bounds.center, bounds.extents, colliderBuffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
				} while (numColliders == colliderBuffer.Length);
			}
			Profiler.EndSample();


			for (int i = 0; i < numColliders; i++) {
				Collider collider = colliderBuffer[i];

				if (ShouldIncludeCollider(collider)) {
					if (ConvertColliderToGatheredMesh(collider) is GatheredMesh mesh) {
						mesh.ApplyLayerModification(modificationsByLayer[collider.gameObject.layer]);
						meshes.Add(mesh);
					}
				}
			}

			if (finiteBounds) ArrayPool<Collider>.Release(ref colliderBuffer);
		}

		/// <summary>
		/// Box Collider triangle indices can be reused for multiple instances.
		/// Warning: This array should never be changed
		/// </summary>
		private readonly static int[] BoxColliderTris = {
			0, 1, 2,
			0, 2, 3,

			6, 5, 4,
			7, 6, 4,

			0, 5, 1,
			0, 4, 5,

			1, 6, 2,
			1, 5, 6,

			2, 7, 3,
			2, 6, 7,

			3, 4, 0,
			3, 7, 4
		};

		/// <summary>
		/// Box Collider vertices can be reused for multiple instances.
		/// Warning: This array should never be changed
		/// </summary>
		private readonly static Vector3[] BoxColliderVerts = {
			new Vector3(-1, -1, -1),
			new Vector3(1, -1, -1),
			new Vector3(1, -1, 1),
			new Vector3(-1, -1, 1),

			new Vector3(-1, 1, -1),
			new Vector3(1, 1, -1),
			new Vector3(1, 1, 1),
			new Vector3(-1, 1, 1),
		};

		/// <summary>
		/// Rasterizes a collider to a mesh.
		/// This will pass the col.transform.localToWorldMatrix to the other overload of this function.
		/// </summary>
		GatheredMesh? ConvertColliderToGatheredMesh (Collider col) {
			return ConvertColliderToGatheredMesh(col, col.transform.localToWorldMatrix);
		}

		/// <summary>
		/// Rasterizes a collider to a mesh assuming it's vertices should be multiplied with the matrix.
		/// Note that the bounds of the returned RasterizationMesh is based on collider.bounds. So you might want to
		/// call myExtraMesh.RecalculateBounds on the returned mesh to recalculate it if the collider.bounds would
		/// not give the correct value.
		/// </summary>
		public GatheredMesh? ConvertColliderToGatheredMesh (Collider col, Matrix4x4 localToWorldMatrix) {
			if (col is BoxCollider box) {
				return RasterizeBoxCollider(box, localToWorldMatrix);
			} else if (col is SphereCollider || col is CapsuleCollider) {
				var scollider = col as SphereCollider;
				var ccollider = col as CapsuleCollider;

				float radius = scollider != null ? scollider.radius : ccollider.radius;
				float height = scollider != null ? 0 : (ccollider.height*0.5f/radius) - 1;
				Quaternion rot = Quaternion.identity;
				// Capsule colliders can be aligned along the X, Y or Z axis
				if (ccollider != null) rot = Quaternion.Euler(ccollider.direction == 2 ? 90 : 0, 0, ccollider.direction == 0 ? 90 : 0);
				Matrix4x4 matrix = Matrix4x4.TRS(scollider != null ? scollider.center : ccollider.center, rot, Vector3.one*radius);

				matrix = localToWorldMatrix * matrix;

				return RasterizeCapsuleCollider(radius, height, col.bounds, matrix);
			} else if (col is MeshCollider collider) {
				return GetColliderMesh(collider, localToWorldMatrix);
			}

			return null;
		}

		GatheredMesh RasterizeBoxCollider (BoxCollider collider, Matrix4x4 localToWorldMatrix) {
			Matrix4x4 matrix = Matrix4x4.TRS(collider.center, Quaternion.identity, collider.size*0.5f);

			matrix = localToWorldMatrix * matrix;

			if (!cachedMeshes.TryGetValue(MeshCacheItem.Box, out int meshDataIndex)) {
				meshDataIndex = AddMeshBuffers(BoxColliderVerts, BoxColliderTris);
				cachedMeshes[MeshCacheItem.Box] = meshDataIndex;
			}

			return new GatheredMesh {
					   meshDataIndex = meshDataIndex,
					   bounds = collider.bounds,
					   indexStart = 0,
					   indexEnd = -1,
					   areaIsTag = false,
					   area = 0,
					   tagDataIndex = -1,
					   solid = true,
					   matrix = matrix,
					   doubleSided = false,
					   flatten = false,
			};
		}

		static int CircleSteps (Matrix4x4 matrix, float radius, float maxError) {
			// Take the maximum scale factor among the 3 axes.
			// If the current matrix has a uniform scale then they are all the same.
			var maxScaleFactor = math.sqrt(math.max(math.max(math.lengthsq((Vector3)matrix.GetColumn(0)), math.lengthsq((Vector3)matrix.GetColumn(1))), math.lengthsq((Vector3)matrix.GetColumn(2))));
			var realWorldRadius = radius * maxScaleFactor;

			var cosAngle = 1 - maxError / realWorldRadius;
			int steps = cosAngle < 0 ? 3 : (int)math.ceil(math.PI / math.acos(cosAngle));
			return steps;
		}

		/// <summary>
		/// If a circle is approximated by fewer segments, it will be slightly smaller than the original circle.
		/// This factor is used to adjust the radius of the circle so that the resulting circle will have roughly the same area as the original circle.
		/// </summary>
		static float CircleRadiusAdjustmentFactor (int steps) {
			return 0.5f * (1 - math.cos(2 * math.PI / steps));
		}

		GatheredMesh RasterizeCapsuleCollider (float radius, float height, Bounds bounds, Matrix4x4 localToWorldMatrix) {
			// Calculate the number of rows to use
			int rows = CircleSteps(localToWorldMatrix, radius, maxColliderApproximationError);

			int cols = rows;

			var cacheItem = new MeshCacheItem {
				type = MeshType.Capsule,
				mesh = null,
				rows = rows,
				// Capsules that differ by a very small amount in height will be rasterized in the same way
				quantizedHeight = Mathf.RoundToInt(height/maxColliderApproximationError),
			};

			if (!cachedMeshes.TryGetValue(cacheItem, out var meshDataIndex)) {
				// Generate a sphere/capsule mesh

				var verts = new UnsafeSpan<Vector3>(Allocator.Persistent, rows*cols + 2);

				var tris = new UnsafeSpan<int>(Allocator.Persistent, rows*cols*2*3);

				for (int r = 0; r < rows; r++) {
					for (int c = 0; c < cols; c++) {
						verts[c + r*cols] = new Vector3(Mathf.Cos(c*Mathf.PI*2/cols)*Mathf.Sin((r*Mathf.PI/(rows-1))), Mathf.Cos((r*Mathf.PI/(rows-1))) + (r < rows/2 ? height : -height), Mathf.Sin(c*Mathf.PI*2/cols)*Mathf.Sin((r*Mathf.PI/(rows-1))));
					}
				}

				verts[verts.Length-1] = Vector3.up;
				verts[verts.Length-2] = Vector3.down;

				int triIndex = 0;

				for (int i = 0, j = cols-1; i < cols; j = i++) {
					tris[triIndex + 0] = (verts.Length-1);
					tris[triIndex + 1] = (0*cols + j);
					tris[triIndex + 2] = (0*cols + i);
					triIndex += 3;
				}

				for (int r = 1; r < rows; r++) {
					for (int i = 0, j = cols-1; i < cols; j = i++) {
						tris[triIndex + 0] = (r*cols + i);
						tris[triIndex + 1] = (r*cols + j);
						tris[triIndex + 2] = ((r-1)*cols + i);
						triIndex += 3;

						tris[triIndex + 0] = ((r-1)*cols + j);
						tris[triIndex + 1] = ((r-1)*cols + i);
						tris[triIndex + 2] = (r*cols + j);
						triIndex += 3;
					}
				}

				for (int i = 0, j = cols-1; i < cols; j = i++) {
					tris[triIndex + 0] = (verts.Length-2);
					tris[triIndex + 1] = ((rows-1)*cols + j);
					tris[triIndex + 2] = ((rows-1)*cols + i);
					triIndex += 3;
				}

				UnityEngine.Assertions.Assert.AreEqual(triIndex, tris.Length);

				meshDataIndex = AddMeshBuffers(verts, tris);
				cachedMeshes[cacheItem] = meshDataIndex;
			}

			return new GatheredMesh {
					   meshDataIndex = meshDataIndex,
					   bounds = bounds,
					   areaIsTag = false,
					   area = 0,
					   tagDataIndex = -1,
					   indexStart = 0,
					   indexEnd = -1,
					   solid = true,
					   matrix = localToWorldMatrix,
					   doubleSided = false,
					   flatten = false,
			};
		}

		bool ShouldIncludeCollider2D (Collider2D collider) {
			// Note: Some things are already checked, namely that:
			// - collider.enabled is true
			// - that the bounds intersect (at least approxmately)
			// - that the collider is not a trigger

			// This is not completely analogous to ShouldIncludeCollider, as this one will
			// always include the collider if it has an attached RecastNavmeshModifier, while
			// 3D colliders handle RecastNavmeshModifier components separately.
			if (((mask >> collider.gameObject.layer) & 1) != 0) return true;
			if ((collider.attachedRigidbody as Component ?? collider).TryGetComponent<RecastNavmeshModifier>(out var rmo) && rmo.enabled && rmo.includeInScan == RecastNavmeshModifier.ScanInclusion.AlwaysInclude) return true;

			for (int i = 0; i < tagMask.Count; i++) {
				if (collider.CompareTag(tagMask[i])) return true;
			}
			return false;
		}

		public void Collect2DColliderMeshes () {
			if (tagMask.Count == 0 && mask == 0) return;

			// Find all colliders that could possibly be inside the bounds
			// TODO: Benchmark?
			int numColliders = 256;
			Collider2D[] colliderBuffer = null;
			bool finiteBounds = math.isfinite(bounds.extents.x) && math.isfinite(bounds.extents.y);

			if (!finiteBounds) {
				colliderBuffer = UnityCompatibility.FindObjectsByTypeSorted<Collider2D>();
				numColliders = colliderBuffer.Length;
			} else {
				// Repeatedly do a OverlapArea check and make the buffer larger if it's too small.
				var min2D = (Vector2)bounds.min;
				var max2D = (Vector2)bounds.max;
#if UNITY_6000_2_OR_NEWER
				var filter = ContactFilter2D.noFilter;
#else
				var filter = new ContactFilter2D().NoFilter();
#endif
				// It would be nice to add the layer mask filter here as well,
				// but we cannot since a collider may have a RecastNavmeshModifier component
				// attached, and in that case we want to include it even if it is on an excluded layer.
				// The user may also want to include objects based on tags.
				// But we can at least exclude all triggers.
				filter.useTriggers = false;

				do {
					if (colliderBuffer != null) ArrayPool<Collider2D>.Release(ref colliderBuffer);
					colliderBuffer = ArrayPool<Collider2D>.Claim(numColliders * 4);
					numColliders = physicsScene2D.OverlapArea(min2D, max2D, filter, colliderBuffer);
				} while (numColliders == colliderBuffer.Length);
			}

			// Filter out colliders that should not be included
			for (int i = 0; i < numColliders; i++) {
				if (!ShouldIncludeCollider2D(colliderBuffer[i])) colliderBuffer[i] = null;
			}

			int shapeMeshCount = ColliderMeshBuilder2D.GenerateMeshesFromColliders(colliderBuffer, numColliders, maxColliderApproximationError, out var vertices, out var indices, out var shapeMeshes);
			var bufferIndex = AddMeshBuffers(vertices.Reinterpret<Vector3>(), indices);

			for (int i = 0; i < shapeMeshCount; i++) {
				var shape = shapeMeshes[i];

				// Skip if the shape is not inside the bounds.
				// This is a more granular check than the one done by the OverlapArea call above,
				// since each collider may generate multiple shapes with different bounds.
				// This is particularly important for TilemapColliders which may generate a lot of shapes.
				if (!bounds.Intersects(shape.bounds)) continue;

				var coll = colliderBuffer[shape.tag];
				(coll.attachedRigidbody as Component ?? coll).TryGetComponent<RecastNavmeshModifier>(out var navmeshModifier);

				var rmesh = new GatheredMesh {
					meshDataIndex = bufferIndex,
					bounds = shape.bounds,
					indexStart = shape.startIndex,
					indexEnd = shape.endIndex,
					tagDataIndex = -1,
					areaIsTag = false,
					// Colliders default to being unwalkable
					area = -1,
					solid = false,
					matrix = shape.matrix,
					doubleSided = true,
					flatten = true,
				};

				if (navmeshModifier != null) {
					if (navmeshModifier.includeInScan == RecastNavmeshModifier.ScanInclusion.AlwaysExclude) continue;
					rmesh.ApplyNavmeshModifier(navmeshModifier);
				} else {
					rmesh.ApplyLayerModification(modificationsByLayer2D[coll.gameObject.layer]);
				}

				// 2D colliders are never solid
				rmesh.solid = false;

				meshes.Add(rmesh);
			}

			if (finiteBounds) ArrayPool<Collider2D>.Release(ref colliderBuffer);
			shapeMeshes.Free();
		}
	}
}
