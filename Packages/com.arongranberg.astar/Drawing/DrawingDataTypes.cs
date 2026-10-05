using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Drawing {
	/// <summary>
	/// Used to cache drawing data over multiple frames.
	/// This is useful as a performance optimization when you are drawing the same thing over multiple consecutive frames.
	///
	/// <code>
	/// private RedrawScope redrawScope;
	///
	/// void Start () {
	///     redrawScope = DrawingManager.GetRedrawScope();
	///     using (var builder = DrawingManager.GetBuilder(redrawScope)) {
	///         builder.WireSphere(Vector3.zero, 1.0f, Color.red);
	///     }
	/// }
	///
	/// void OnDestroy () {
	///     redrawScope.Dispose();
	/// }
	/// </code>
	///
	/// See: <see cref="DrawingManager.GetRedrawScope"/>
	/// </summary>
	public struct RedrawScope : System.IDisposable {
		// Stored as a GCHandle to allow storing this struct in an unmanaged ECS component or system
		internal System.Runtime.InteropServices.GCHandle gizmos;
		/// <summary>
		/// ID of the scope.
		/// Zero means no or invalid scope.
		/// </summary>
		internal int id;

		/// <summary>True if the scope has been created</summary>
		public bool isValid => id != 0;

#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
		static int idCounter = 1;

		internal RedrawScope (DrawingData gizmos, int id) {
			this.gizmos = gizmos.gizmosHandle;
			this.id = id;
		}

		internal RedrawScope (DrawingData gizmos) {
			this.gizmos = gizmos.gizmosHandle;
			// Should be enough with 4 billion ids before they wrap around.
			id = idCounter++;
		}
#endif

		/// <summary>
		/// Everything rendered with this scope and which is not older than one frame is drawn again.
		/// This is useful if you for some reason cannot draw some items during a frame (e.g. some asynchronous process is modifying the contents)
		/// but you still want to draw the same thing as the last frame to at least draw *something*.
		///
		/// Note: The items age will be reset. So the next frame you can call
		/// this method again to draw the items yet again.
		/// </summary>
		internal void Draw () {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
			if (gizmos.IsAllocated) {
				if (gizmos.Target is DrawingData gizmosTarget) gizmosTarget.Draw(this);
			}
#endif
		}

		/// <summary>
		/// Stops keeping all previously rendered items alive, and starts a new scope.
		/// Equivalent to first calling Dispose on the old scope and then creating a new one.
		/// </summary>
		public void Rewind () {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
			GameObject associatedGameObject = null;
			if (gizmos.IsAllocated) {
				if (gizmos.Target is DrawingData gizmosTarget) associatedGameObject = gizmosTarget.GetAssociatedGameObject(this);
			}
			Dispose();
			this = DrawingManager.GetRedrawScope(associatedGameObject);
#endif
		}

		internal void DrawUntilDispose (GameObject associatedGameObject) {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
			if (gizmos.Target is DrawingData gizmosTarget) gizmosTarget.DrawUntilDisposed(this, associatedGameObject);
#endif
		}

		/// <summary>
		/// Dispose the redraw scope to stop rendering the items.
		///
		/// You must do this when you are done with the scope, even if it was never used to actually render anything.
		/// The items will stop rendering immediately: the next camera to render will not render the items unless kept alive in some other way.
		/// However, items are always rendered at least once.
		/// </summary>
		public void Dispose () {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
			if (gizmos.IsAllocated) {
				if (gizmos.Target is DrawingData gizmosTarget) gizmosTarget.DisposeRedrawScope(this);
			}
#endif
			gizmos = default;
			id = 0;
		}
	};

	// Members of \reflink{DrawingData} that stay available when ALINE is excluded from builds.
	//
	// \reflink{DrawingData.Hasher} is used by calling code that is not itself stripped, and the
	// material references are here so they can still be overridden.
	public partial class DrawingData {
		/// <summary>Combines hashes into a single hash value</summary>
		public struct Hasher : IEquatable<Hasher> {
			ulong hash;

			public static Hasher NotSupplied => new Hasher { hash = ulong.MaxValue };

			[System.Obsolete("Use the constructor instead")]
			public static Hasher Create<T>(T init) {
				var h = new Hasher();

				h.Add(init);
				return h;
			}

			/// <summary>
			/// Includes the given data in the final hash.
			/// You can call this as many times as you want.
			/// </summary>
			public void Add<T>(T hash) {
				// Just a regular hash function. The + 12289 is to make sure that hashing zeros doesn't just produce a zero (and generally that hashing one X doesn't produce a hash of X)
				// (with a struct we can't provide default initialization)
				this.hash = (1572869UL * this.hash) ^ (ulong)hash.GetHashCode() + 12289;
			}

			public readonly ulong Hash => hash;

			public override int GetHashCode () {
				return (int)hash;
			}

			public bool Equals (Hasher other) {
				return hash == other.hash;
			}
		}

		/// <summary>Material to use for surfaces</summary>
		public Material surfaceMaterial;

		/// <summary>Material to use for lines</summary>
		public Material lineMaterial;

		/// <summary>Material to use for text</summary>
		public Material textMaterial;

		public DrawingSettings.Settings settings;

		public DrawingSettings.Settings settingsRef {
			get {
				if (settings == null) {
					settings = DrawingSettings.DefaultSettings;
				}

				return settings;
			}
		}
	}
}
