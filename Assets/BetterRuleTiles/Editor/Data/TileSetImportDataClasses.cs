#if UNITY_EDITOR
using UnityEngine;

namespace VinTools.BetterRuleTiles.Editor.Data
{
    public class TileSetImportDataClasses
    {
        public class TileSetImportAreaOptions
        {
            /// <summary>
            /// The size of the sprite area that are going to be used for the pattern
            /// </summary>
            public Vector2Int FrameSize = Vector2Int.one;
            
            /// <summary>
            /// The position of the sprite area
            /// </summary>
            public Vector2Int Position = Vector2Int.zero;

            /// <summary>
            /// The color used to draw the outline, this will be randomized so the user can distinguish the separate sets of frames
            /// </summary>
            public Color OutlineColor = GetRandomOutlineColor();

            public static Color GetRandomOutlineColor()
            {
                //randomize color
                float rand1 = UnityEngine.Random.Range(0.6f, 1.0f);
                float rand2 = UnityEngine.Random.Range(0.6f, 1.0f);
                float rand3 = UnityEngine.Random.Range(0.6f, 1.0f);
                int exclude = UnityEngine.Random.Range(0, 3);

                return new Color(
                    exclude == 0 ? .5f : rand1,
                    exclude == 1 ? .5f : rand2,
                    exclude == 2 ? .5f : rand3
                );
            }
        }
    
        /// <summary>
        /// Class containing all information for a series of frame sets
        /// </summary>
        public class AnimationImportOptions : TileSetImportAreaOptions
        {
            /// <summary>
            /// The index of the selected wrap option
            /// </summary>
            public int WrapMode = 0;
            /// <summary>
            /// Direction of the frames
            /// </summary>
            public Vector2Int FrameDirection = Vector2Int.right;
            /// <summary>
            /// Space between frames
            /// </summary>
            public int Spacing = 0;
            
            /// <summary>
            /// The amount of frames in the animation, calculated by the preview function
            /// </summary>
            public int _frameCount;

            /// <summary>
            /// Labels of the wrap options
            /// </summary>
            public static readonly GUIContent[] WrapDirectionLabels = new GUIContent[]
            {
                new GUIContent("Right"),
                new GUIContent("Down"),
                new GUIContent("Left"),
                new GUIContent("Up")
            };
            /// <summary>
            /// Corresponding vectors for the wrap option index
            /// </summary>
            public static readonly Vector2Int[] WrapDirectionValues = new Vector2Int[]
            {
                Vector2Int.right,
                Vector2Int.up, //in UI down and up is flipped
                Vector2Int.left,
                Vector2Int.down //in UI down and up is flipped
            };  
        }
    }
}
#endif