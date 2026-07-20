using System;
using System.Collections.Generic;
using UnityEngine;

namespace VinTools.BetterRuleTiles.Runtime.Utilities
{
    public static class RectTools
    {
        public static bool GetOverlappingArea(this Rect rect, Rect other, out Rect overlappingArea)
        {
            // return original if no overlap (Rect cannot be null)
            if (!rect.Overlaps(other))
            {
                overlappingArea = rect;
                return false;
            }
            
            // get edges of overlapping area
            float top = rect.yMin > other.yMin ? rect.yMin : other.yMin;
            float bottom = rect.yMax < other.yMax ? rect.yMax : other.yMax;
            float left = rect.xMin > other.xMin ? rect.xMin : other.xMin;
            float right = rect.xMax < other.xMax ? rect.xMax : other.xMax;
            
            // save the overlapping area and return true
            overlappingArea = new Rect(left, top, right - left, bottom - top);
            return true;
        }

        public static List<Rect> SubtractOverlappingArea(this Rect rect, Rect other)
        {
            // if there is no overlap, return the original rect
            if (!rect.GetOverlappingArea(other, out Rect overlappingArea)) return new List<Rect> {rect};
            
            // create a list where the different squares will be added
            var newAreaList = new List<Rect>();
            
            // add left side (including top and bottom corners)
            if (rect.xMin < overlappingArea.xMin)
                newAreaList.Add(new Rect(rect.xMin, rect.yMin, overlappingArea.xMin - rect.xMin, rect.height));
            
            // add right side (including top and bottom corners)
            if (rect.xMax > overlappingArea.xMax)
                newAreaList.Add(new Rect(overlappingArea.xMax, rect.yMin, rect.xMax - overlappingArea.xMax, rect.height));
            
            // get the top and bottom width
            float left = rect.xMin > overlappingArea.xMin ? rect.xMin : overlappingArea.xMin;
            float right = rect.xMax < overlappingArea.xMax ? rect.xMax : overlappingArea.xMax;
            float width = right - left;
            
            // add top (only area above overlap)
            if (rect.yMin < overlappingArea.yMin)
                newAreaList.Add(new Rect(left, rect.yMin, width, overlappingArea.yMin - rect.yMin));
            
            // add bottom (only area below overlap)
            if (rect.yMax > overlappingArea.yMax)
                newAreaList.Add(new Rect(left, overlappingArea.yMax, width, rect.yMax - overlappingArea.yMax));
            
            // return list
            return newAreaList;
        }

        public static bool Touches(this Rect rect, Rect other)
        {
            if (rect.Overlaps(other)) return true;
         
            static bool AreClose(float a, float b) => Math.Abs(a - b) < 0.001f;
            
            // left side (of rect)
            if (AreClose(rect.xMin, other.xMax) && rect.yMax >= other.yMin && rect.yMin <= other.yMax) return true;
            // right side (of rect)
            if (AreClose(rect.xMax, other.xMin) && rect.yMax >= other.yMin && rect.yMin <= other.yMax) return true;
            // top side (of rect)
            if (AreClose(rect.yMin, other.yMax) && rect.xMax >= other.xMin && rect.xMin <= other.xMax) return true;
            // bottom side (of rect)
            if (AreClose(rect.yMax, other.yMin) && rect.xMax >= other.xMin && rect.xMin <= other.xMax) return true;
            
            // no touches
            return false;
        }
        
        public static Rect SquareifyRect(Rect rect)
        {
            // get the size of the rect
            float edgeSize = Mathf.Max(rect.width, rect.height);
            Vector2 size = new Vector2(edgeSize, edgeSize);

            // offset the position by half of the size difference
            var newPos = rect.position - ((size - rect.size) / 2f);
            
            return new Rect(newPos, size);
        }
    }
}