using UnityEngine;

namespace Pathfinding.Util {
	/// <summary>Compatibility class for Unity APIs that are not available in all Unity versions</summary>
	public static class UnityCompatibility {
		public static T[] FindObjectsByTypeSorted<T>() where T : Object {
#if UNITY_6000_6_OR_NEWER
			// Unity 6000.6 removed sorted searches. Sorting by entity id restores the property callers
			// rely on: repeated searches within a session visit the objects in the same order, so
			// recast scans and graph modifier ordering stay reproducible.
			var objs = Object.FindObjectsByType<T>();
			System.Array.Sort(objs, (a, b) => a.GetEntityId().CompareTo(b.GetEntityId()));
			return objs;
#elif UNITY_2021_3_OR_NEWER && !(UNITY_2022_1_OR_NEWER && !UNITY_2022_2_OR_NEWER)
			return Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID);
#else
			return Object.FindObjectsOfType<T>();
#endif
		}

		public static T[] FindObjectsByTypeUnsorted<T>() where T : Object {
#if UNITY_6000_6_OR_NEWER
			return Object.FindObjectsByType<T>();
#elif UNITY_2021_3_OR_NEWER && !(UNITY_2022_1_OR_NEWER && !UNITY_2022_2_OR_NEWER)
			return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
			return Object.FindObjectsOfType<T>();
#endif
		}

		public static T[] FindObjectsByTypeUnsortedWithInactive<T>() where T : Object {
#if UNITY_6000_6_OR_NEWER
			return Object.FindObjectsByType<T>(FindObjectsInactive.Include);
#elif UNITY_2021_3_OR_NEWER && !(UNITY_2022_1_OR_NEWER && !UNITY_2022_2_OR_NEWER)
			return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
			return Object.FindObjectsOfType<T>(true);
#endif
		}

		public static T FindAnyObjectByType<T>() where T : Object {
#if UNITY_2021_3_OR_NEWER && !(UNITY_2022_1_OR_NEWER && !UNITY_2022_2_OR_NEWER)
			return Object.FindAnyObjectByType<T>();
#else
			return Object.FindObjectOfType<T>();
#endif
		}
	}
}

#if !UNITY_2022_3_OR_NEWER
namespace Pathfinding {
	public class IgnoredByDeepProfilerAttribute : System.Attribute {
	}
}
#endif
