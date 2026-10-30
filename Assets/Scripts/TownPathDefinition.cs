using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TimelessEchoes
{
    [CreateAssetMenu(menuName = "Timeless Echoes/Town Paths")]
    public sealed class TownPathDefinition : ScriptableObject
    {
        public Tile[] grass = new Tile[256];
        public Tile[] dirt = new Tile[256];
        public Tile[] pavingFill = new Tile[4];
        public Tile[] pavingEdge = new Tile[256];
        public Tile[] pavingStairs = new Tile[3];
        public Vector3Int[] baseCells;
        public Vector3Int[] firstField;
        public Vector3Int[] secondField;
        public Vector3Int[] thirdField;
        public Vector3Int[] millApproach;
        public Vector3Int[] pierApproach;
        public Vector3Int stairTop = new(-86, -5, 0);

        public HashSet<Vector3Int> Cells(int gardenCapacity, bool millBuilt)
        {
            var result = new HashSet<Vector3Int>();
            void Add(Vector3Int[] cells) { if (cells != null) result.UnionWith(cells); }
            Add(baseCells);
            if (gardenCapacity >= 1) Add(firstField);
            if (gardenCapacity >= 3) Add(secondField);
            if (gardenCapacity >= 5) Add(thirdField);
            if (millBuilt) Add(millApproach);
            Add(pierApproach);
            return result;
        }

        public static int Neighbours(ISet<Vector3Int> cells, Vector3Int cell)
        {
            var mask = 0;
            for (int i = 0; i < Offsets.Length; i++) if (cells.Contains(cell + Offsets[i])) mask |= 1 << i;
            return mask;
        }

        // Clockwise from north; shared with the authored tile catalogue.
        public static readonly Vector3Int[] Offsets = {
            new(0,1,0), new(1,1,0), new(1,0,0), new(1,-1,0),
            new(0,-1,0), new(-1,-1,0), new(-1,0,0), new(-1,1,0)
        };
    }
}
