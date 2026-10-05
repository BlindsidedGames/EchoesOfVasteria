#pragma warning disable 649 // Field `Drawing.GizmoContext.activeTransform' is never assigned to, and will always have its default value `null'. Not used outside of the unity editor.
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pathfinding.Drawing {
	/// <summary>Info about the current selection in the editor</summary>
	public static class GizmoContext {
#if UNITY_EDITOR
		static Transform activeTransform;
#endif

		static HashSet<Transform> selectedTransforms = new HashSet<Transform>();

		static internal bool drawingGizmos;
		static internal bool dirty;
		private static int selectionSizeInternal;

		/// <summary>Number of top-level transforms that are selected</summary>
		public static int selectionSize {
			get {
				Refresh();
				return selectionSizeInternal;
			}
			private set {
				selectionSizeInternal = value;
			}
		}

		internal static void SetDirty () {
			dirty = true;
		}

		private static void Refresh () {
#if UNITY_EDITOR
			if (!drawingGizmos) throw new System.Exception("Can only be used inside the ALINE library's gizmo drawing functions.");
			if (dirty) {
				dirty = false;
				DrawingManager.MarkerRefreshSelectionCache.Begin();
				activeTransform = Selection.activeTransform;
				selectedTransforms.Clear();
				// Optimization to avoid allocating an empty array when calling Selection.transforms in many cases
				if (Selection.count > 0) {
					var topLevel = Selection.transforms;
					for (int i = 0; i < topLevel.Length; i++) selectedTransforms.Add(topLevel[i]);
					selectionSize = topLevel.Length;
				} else {
					selectionSize = 0;
				}
				DrawingManager.MarkerRefreshSelectionCache.End();
			}
#endif
		}

		/// <summary>
		/// True if the component is selected.
		/// This is a deep selection: even children of selected transforms are considered to be selected.
		/// </summary>
		public static bool InSelection (Component c) {
			return InSelection(c.transform);
		}

		/// <summary>
		/// True if the transform is selected.
		/// This is a deep selection: even children of selected transforms are considered to be selected.
		/// </summary>
		public static bool InSelection (Transform tr) {
			Refresh();
			var leaf = tr;
			while (tr != null) {
				if (selectedTransforms.Contains(tr)) {
					selectedTransforms.Add(leaf);
					return true;
				}
				tr = tr.parent;
			}
			return false;
		}

		/// <summary>
		/// True if the component is shown in the inspector.
		/// The active selection is the GameObject that is currently visible in the inspector.
		/// </summary>
		public static bool InActiveSelection (Component c) {
			return InActiveSelection(c.transform);
		}

		/// <summary>
		/// True if the transform is shown in the inspector.
		/// The active selection is the GameObject that is currently visible in the inspector.
		/// </summary>
		public static bool InActiveSelection (Transform tr) {
#if UNITY_EDITOR
			Refresh();
			return tr.transform == activeTransform;
#else
			return false;
#endif
		}
	}

	/// <summary>
	/// Every object that wants to draw gizmos should implement this interface.
	/// See: <see cref="Drawing.MonoBehaviourGizmos"/>
	/// </summary>
	public interface IDrawGizmos {
		void DrawGizmos();

		/// <summary>
		/// True if the drawer still exists and shouldn't be destroyed.
		/// This is only called for drawers that do not inherit from MonoBehaviour.
		/// MonoBehaviour drawers are automatically checked.
		/// </summary>
		bool Exists => throw new System.NotImplementedException("This method should be overridden in the implementing class, unless it inherits from MonoBehaviour");
	}

	public enum DetectedRenderPipeline {
		BuiltInOrCustom,
		HDRP,
		URP
	}
}
