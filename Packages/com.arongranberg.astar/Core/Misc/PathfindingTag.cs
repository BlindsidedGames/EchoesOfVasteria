namespace Pathfinding {
	/// <summary>
	/// Represents a single pathfinding tag.
	///
	/// Values of this type will be automatically displayed in the unity inspector as a dropdown list of all the pathfinding tags.
	///
	/// Note: The tag refers to a pathfinding tag, not a unity tag that is applied to GameObjects, or any other kind of tag.
	///
	/// See: tags (view in online documentation for working links)
	/// </summary>
	[System.Serializable]
	public struct PathfindingTag {
		/// <summary>
		/// Underlaying tag value.
		/// Should always be between 0 and <see cref="GraphNode.MaxTagIndex"/> (inclusive).
		/// </summary>
		public uint value;

		public PathfindingTag(uint value) {
			this.value = value;
		}

		public static implicit operator uint (PathfindingTag tag) {
			return tag.value;
		}

		public static implicit operator PathfindingTag(uint tag) {
			return new PathfindingTag(tag);
		}

		/// <summary>Get the value of the PathfindingTag with the given name</summary>
		public static PathfindingTag FromName (string tagName) {
			AstarPath.FindAstarPath();
			if (AstarPath.active == null) throw new System.InvalidOperationException("There's no AstarPath component in the scene. Cannot get tag names.");

			var tagNames = AstarPath.active.GetTagNames();
			var tag = System.Array.IndexOf(tagNames, tagName);
			if (tag == -1) throw new System.ArgumentException("There's no pathfinding tag with the name '" + tagName + "'");

			return new PathfindingTag((uint)tag);
		}

		/// <summary>
		/// Converts the tag value to a bitmask of tags.
		///
		/// This can, for example, be used to set <see cref="TraversalConstraint.tags"/>.
		/// </summary>
		public int ToMask () {
			return 1 << (int)value;
		}

		public override string ToString () {
			return value.ToString();
		}
	}
}
