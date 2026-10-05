using System;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding.Drawing {
	/// <summary>
	/// Specifies text alignment relative to an anchor point.
	///
	/// <code>
	/// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter);
	/// </code>
	/// <code>
	/// // Draw the label 20 pixels below the object
	/// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter.withPixelOffset(0, -20));
	/// </code>
	///
	/// See: <see cref="Draw.Label2D"/>
	/// See: <see cref="Draw.Label3D"/>
	/// </summary>
	public struct LabelAlignment {
		/// <summary>
		/// Where on the text's bounding box to anchor the text.
		///
		/// The pivot is specified in relative coordinates, where (0,0) is the bottom left corner and (1,1) is the top right corner.
		/// </summary>
		public float2 relativePivot;
		/// <summary>How much to move the text in screen-space</summary>
		public float2 pixelOffset;

		public static readonly LabelAlignment TopLeft = new LabelAlignment { relativePivot = new float2(0.0f, 1.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment MiddleLeft = new LabelAlignment { relativePivot = new float2(0.0f, 0.5f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment BottomLeft = new LabelAlignment { relativePivot = new float2(0.0f, 0.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment BottomCenter = new LabelAlignment { relativePivot = new float2(0.5f, 0.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment BottomRight = new LabelAlignment { relativePivot = new float2(1.0f, 0.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment MiddleRight = new LabelAlignment { relativePivot = new float2(1.0f, 0.5f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment TopRight = new LabelAlignment { relativePivot = new float2(1.0f, 1.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment TopCenter = new LabelAlignment { relativePivot = new float2(0.5f, 1.0f), pixelOffset = new float2(0, 0) };
		public static readonly LabelAlignment Center = new LabelAlignment { relativePivot = new float2(0.5f, 0.5f), pixelOffset = new float2(0, 0) };

		/// <summary>
		/// Moves the text by the specified amount of pixels in screen-space.
		///
		/// <code>
		/// // Draw the label 20 pixels below the object
		/// Draw.Label2D(transform.position, "Hello World", 14, LabelAlignment.TopCenter.withPixelOffset(0, -20));
		/// </code>
		/// </summary>
		public LabelAlignment withPixelOffset (float x, float y) {
			return new LabelAlignment {
					   relativePivot = this.relativePivot,
					   pixelOffset = new float2(x, y),
			};
		}
	}

	/// <summary>Maximum allowed delay for a job that is drawing to a command buffer</summary>
	public enum AllowedDelay {
		/// <summary>
		/// If the job is not complete at the end of the frame, drawing will block until it is completed.
		/// This is recommended for most jobs that are expected to complete within a single frame.
		/// </summary>
		EndOfFrame,
		/// <summary>
		/// Wait indefinitely for the job to complete, and only submit the results for rendering once it is done.
		/// This is recommended for long running jobs that may take many frames to complete.
		/// </summary>
		Infinite,
	}

	/// <summary>
	/// Types that are part of the <see cref="CommandBuilder"/> public API.
	///
	/// These are always compiled, even when ALINE is excluded from builds via ALINE_EXCLUDED_IN_BUILD,
	/// so that code referring to them keeps compiling. Only the method bodies are stripped.
	/// </summary>
	public partial struct CommandBuilder {
		public struct ScopeMatrix : IDisposable {
			internal CommandBuilder builder;
			public void Dispose () {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (!builder.gizmos.IsAllocated || !(builder.gizmos.Target is DrawingData data) || !data.data.StillExists(builder.uniqueID)) throw new System.InvalidOperationException("The drawing instance this matrix scope belongs to no longer exists. Matrix scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a matrix scope inside a coroutine?");
#endif
				unsafe {
					builder.PopMatrix();
					builder.buffer = null;
				}
#endif
			}
		}

		public struct ScopeColor : IDisposable {
			internal CommandBuilder builder;
			public void Dispose () {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (!builder.gizmos.IsAllocated || !(builder.gizmos.Target is DrawingData data) || !data.data.StillExists(builder.uniqueID)) throw new System.InvalidOperationException("The drawing instance this color scope belongs to no longer exists. Color scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a color scope inside a coroutine?");
#endif
				unsafe {
					builder.PopColor();
					builder.buffer = null;
				}
#endif
			}
		}

		public struct ScopePersist : IDisposable {
			internal CommandBuilder builder;
			public void Dispose () {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (!builder.gizmos.IsAllocated || !(builder.gizmos.Target is DrawingData data) || !data.data.StillExists(builder.uniqueID)) throw new System.InvalidOperationException("The drawing instance this persist scope belongs to no longer exists. Persist scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a persist scope inside a coroutine?");
#endif
				unsafe {
					builder.PopDuration();
					builder.buffer = null;
				}
#endif
			}
		}

		/// <summary>
		/// Scope that does nothing.
		/// Used for optimization in standalone builds.
		/// </summary>
		public struct ScopeEmpty : IDisposable {
			public void Dispose () {
			}
		}

		public struct ScopeLineWidth : IDisposable {
			internal CommandBuilder builder;
			public void Dispose () {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (!builder.gizmos.IsAllocated || !(builder.gizmos.Target is DrawingData data) || !data.data.StillExists(builder.uniqueID)) throw new System.InvalidOperationException("The drawing instance this line width scope belongs to no longer exists. Line width scopes cannot survive for longer than a frame unless you have a custom drawing instance. Are you using a line width scope inside a coroutine?");
#endif
				unsafe {
					builder.PopLineWidth();
					builder.buffer = null;
				}
#endif
			}
		}

		/// <summary>Determines the symbol to use for <see cref="PolylineWithSymbol"/></summary>
		public enum SymbolDecoration : byte {
			/// <summary>
			/// No symbol.
			///
			/// Space will still be reserved, but no symbol will be drawn.
			/// Can be used to draw dashed lines.
			///
			/// [Open online documentation to see images]
			/// </summary>
			None,
			/// <summary>
			/// An arrowhead symbol.
			///
			/// [Open online documentation to see images]
			/// </summary>
			ArrowHead,
			/// <summary>
			/// A circle symbol.
			///
			/// [Open online documentation to see images]
			/// </summary>
			Circle,
		}

		/// <summary>
		/// Helper for drawing a polyline with symbols at regular intervals.
		///
		/// <code>
		/// var generator = new CommandBuilder.PolylineWithSymbol(CommandBuilder.SymbolDecoration.Circle, 0.2f, 0.0f, 0.47f);
		/// generator.MoveTo(ref Draw.editor, new float3(-0.5f, 0, -0.5f));
		/// generator.MoveTo(ref Draw.editor, new float3(0.5f, 0, 0.5f));
		/// </code>
		///
		/// [Open online documentation to see images]
		///
		/// [Open online documentation to see images]
		///
		/// You can also draw a dashed line using this struct, but for common cases you can use the <see cref="DashedPolyline"/> helper function instead.
		///
		/// <code>
		/// using (Draw.WithColor(color)) {
		///     var dash = 0.1f;
		///     var gap = 0.1f;
		///     var p = new CommandBuilder.PolylineWithSymbol(CommandBuilder.SymbolDecoration.None, gap, 0, dash + gap);
		///     for (int i = 0; i < points.Count; i++) {
		///         p.MoveTo(ref Draw.editor, points[i]);
		///     }
		/// }
		/// </code>
		///
		/// [Open online documentation to see images]
		/// </summary>
		public struct PolylineWithSymbol {
			/// <summary>
			/// The up direction of the symbols.
			///
			/// This is used to determine the orientation of the symbols.
			/// By default this is set to (0,1,0).
			/// </summary>
			public float3 up;

#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
			float3 prev;
			float offset;
			readonly float symbolSize;
			readonly float connectingSegmentLength;
			readonly float symbolPadding;
			readonly float symbolOffset;

			readonly SymbolDecoration symbol;
			State state;
			readonly bool reverseSymbols;

			enum State : byte {
				NotStarted,
				ConnectingSegment,
				PreSymbolPadding,
				Symbol,
				PostSymbolPadding,
			}
#endif

			/// <summary>
			/// Create a new polyline with symbol generator.
			///
			/// Note: If symbolSize + 2*symbolPadding > symbolSpacing, the symbolSpacing parameter will be increased to accommodate the symbol and its padding.
			/// There will be no connecting lines between the symbols in this case, as there's no space for them.
			/// </summary>
			/// <param name="symbol">The symbol to use</param>
			/// <param name="symbolSize">The size of the symbol. In case of a circle, this is the diameter.</param>
			/// <param name="symbolPadding">The padding on both sides of the symbol between the symbol and the line.</param>
			/// <param name="symbolSpacing">The spacing between symbols. This is the distance between the centers of the symbols.</param>
			/// <param name="reverseSymbols">If true, the symbols will be reversed. For cicles this has no effect, but arrowhead symbols will be reversed.</param>
			/// <param name="offset">Distance to shift all symbols forward along the line. Useful for animations. If offset=0, the first symbol's center is at symbolSpacing/2.</param>
			public PolylineWithSymbol(SymbolDecoration symbol, float symbolSize, float symbolPadding, float symbolSpacing, bool reverseSymbols = false, float offset = 0) {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
				if (symbolSpacing <= math.FLT_MIN_NORMAL) throw new System.ArgumentOutOfRangeException(nameof(symbolSpacing), "Symbol spacing must be greater than zero");
				if (symbolSize <= math.FLT_MIN_NORMAL) throw new System.ArgumentOutOfRangeException(nameof(symbolSize), "Symbol size must be greater than zero");
				if (symbolPadding < 0) throw new System.ArgumentOutOfRangeException(nameof(symbolPadding), "Symbol padding must non-negative");

				this.prev = float3.zero;
				this.symbol = symbol;
				this.symbolSize = symbolSize;
				this.symbolPadding = symbolPadding;
				this.connectingSegmentLength = math.max(0, symbolSpacing - symbolPadding * 2f - symbolSize);
				// Calculate actual value, after clamping to a valid range
				symbolSpacing = symbolPadding * 2 + symbolSize + connectingSegmentLength;
				this.reverseSymbols = reverseSymbols;
				symbolOffset = symbol == SymbolDecoration.ArrowHead ? -0.25f * symbolSize : 0;
				if (reverseSymbols) {
					symbolOffset = -symbolOffset;
				}
				symbolOffset += 0.5f * symbolSize;
				this.offset = (this.connectingSegmentLength * 0.5f + offset) % symbolSpacing;
				// Ensure the initial offset is always negative. This makes the state machine start in the correct state when the offset turns positive.
				if (this.offset > 0) this.offset -= symbolSpacing;
				this.state = State.NotStarted;
#endif
				this.up = new float3(0, 1, 0);
			}

			/// <summary>
			/// Move to a new point.
			///
			/// This will draw the symbols and line segments between the previous point and the new point.
			/// </summary>
			/// <param name="draw">The command builder to draw to. You can use a built-in builder like \reflink{Draw.editor} or \reflink{Draw.ingame}, or use a custom one.</param>
			/// <param name="next">The next point in the polyline to move to.</param>
			public void MoveTo (ref CommandBuilder draw, float3 next) {
#if !ALINE_EXCLUDED_IN_BUILD || UNITY_EDITOR
				if (state == State.NotStarted) {
					prev = next;
					state = State.ConnectingSegment;
					return;
				}

				var len = math.length(next - prev);
				var invLen = math.rcp(len);
				var dir = next - prev;
				float3 up = default;
				if (symbol != SymbolDecoration.None) {
					up = math.normalizesafe(math.cross(dir, math.cross(dir, this.up)));
					if (math.all(up == 0f)) {
						up = new float3(0, 0, 1);
					}
					if (reverseSymbols) dir = -dir;
				}

				var currentPositionOnSegment = 0f;
				while (true) {
					if (state == State.ConnectingSegment) {
						if (offset >= 0 && offset != currentPositionOnSegment) {
							currentPositionOnSegment = math.max(0, currentPositionOnSegment);
							var pLast = math.lerp(prev, next, currentPositionOnSegment * invLen);
							var p = math.lerp(prev, next, math.min(offset * invLen, 1));
							draw.Line(pLast, p);
						}

						if (offset < len) {
							state = State.PreSymbolPadding;
							currentPositionOnSegment = offset;
							offset += symbolPadding;
						} else {
							break;
						}
					} else if (state == State.PreSymbolPadding) {
						if (offset >= len) break;

						state = State.Symbol;
						currentPositionOnSegment = offset;
						offset += symbolOffset;
					} else if (state == State.Symbol) {
						if (offset >= len) break;

						if (offset >= 0) {
							var p = math.lerp(prev, next, offset * invLen);
							switch (symbol) {
							case SymbolDecoration.None:
								break;
							case SymbolDecoration.ArrowHead:
								draw.Arrowhead(p, dir, up, symbolSize);
								break;
							case SymbolDecoration.Circle:
							default:
								draw.Circle(p, up, symbolSize * 0.5f);
								break;
							}
						}

						state = State.PostSymbolPadding;
						currentPositionOnSegment = offset;
						offset += -symbolOffset + symbolSize + symbolPadding;
					} else if (state == State.PostSymbolPadding) {
						if (offset >= len) break;

						state = State.ConnectingSegment;
						currentPositionOnSegment = offset;
						offset += connectingSegmentLength;
					} else {
						throw new System.Exception("Invalid state");
					}
				}
				offset -= len;
				prev = next;
#endif
			}
		}
	}
}
