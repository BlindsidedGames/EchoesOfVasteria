// Stands in for DrawingManager.cs when ALINE is excluded from standalone builds.
#if ALINE_EXCLUDED_IN_BUILD && !UNITY_EDITOR
using UnityEngine;

namespace Pathfinding.Drawing {
	// Stub used when ALINE has been excluded from this build using the ALINE_EXCLUDED_IN_BUILD compiler directive.
	[AddComponentMenu("")]
	public class DrawingManager : MonoBehaviour {
		// Always null, because no drawing data is allocated in this build
		[System.NonSerialized]
		public DrawingData gizmos;

		public static bool allowRenderToRenderTextures = false;
		public static bool drawToAllCameras = false;
		public static float lineWidthMultiplier = 1.0f;

		// Always null, because no manager is created in this build.
		public static DrawingManager instance => null;

		public static void Init () {}

		public static bool ShouldDrawGizmos(System.Type type) => false;

		public static void Register (IDrawGizmos item) {}

		public static void Register (IDrawGizmos item, System.Type overrideType) {}

		public static CommandBuilder GetBuilder(bool renderInGame = false) => default;

		public static CommandBuilder GetBuilder(RedrawScope redrawScope, bool renderInGame = false) => default;

		public static CommandBuilder GetBuilder(DrawingData.Hasher hasher, RedrawScope redrawScope = default, bool renderInGame = false) => default;

		public static bool TryDrawHasher(DrawingData.Hasher hasher, RedrawScope redrawScope = default) => false;

		public static RedrawScope GetRedrawScope(GameObject associatedGameObject = null) => default;
	}
}
#endif
