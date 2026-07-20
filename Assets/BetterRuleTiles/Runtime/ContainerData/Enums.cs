using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Internal;
using VinTools.BetterRuleTiles.Runtime.Utilities;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileContainer
    {
       
        public enum TileShape
        {
            Square = 0,
            [InspectorName("1x1 Slope")] Slope_1x1 = 10,
            [InspectorName("2x1 Slope Bottom")] Slope_2x1_Bottom = 11,
            [InspectorName("2x1 Slope Top")] Slope_2x1_Top = 12,
            Diamond = 20,
            Isometric = 22,
            [InspectorName("Hexagon Pointed Top")] HexagonPointedTop = 25,
            [InspectorName("Hexagon Flat Top")] HexagonFlatTop = 26,
            Circle = 30,
        }
        public enum GridShape
        {
            Square = 0,
            Isometric = 1,
            [InspectorName("Hexagonal - Pointed-Top")] HexagonalPointedTop = 2,
            [InspectorName("Hexagonal - Flat-Top")] HexagonalFlatTop = 3,
        }
        public enum Edge
        {
            Up,
            Down,
            Left,
            Right
        }
 
    }
}