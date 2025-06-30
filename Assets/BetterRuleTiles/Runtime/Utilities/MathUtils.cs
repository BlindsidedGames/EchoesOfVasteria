using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VinTools.Utilities
{
    public static class MathUtils
    {
        //transformation
        public static Vector2 TransformPoint(Vector2 point, Vector2 iH, Vector2 jH) => point.x * iH + point.y * jH;
        public static Vector2Int TransformPoint(Vector2Int point, Vector2Int iH, Vector2Int jH) => point.x * iH + point.y * jH;
    }
}