using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using VinTools.BetterRuleTiles.Internal;
using System;

namespace VinTools.BetterRuleTiles
{
    public interface IBetterRuleTile
    {
        int UniqueIdentifier { get; set; } 
        public TileBase[] otherTiles { get; set; } 
        public List<TileBase> variations { get; set; } 

        /// <summary>
        /// The extended tiling rule is used during tile generation,
        /// it contains both the default 'm_TilingRules' and the 'm_ExtraTilingRules' variables.
        /// At the end of the generation, the values of this are saved to their respective data types
        /// and this variable is cleared.
        /// </summary>
        List<ExtendedTilingRule> TempTilingRules { get; set; } 
        List<ExtraTilingRule> m_ExtraTilingRules { get; set; } 
        List<RuleTile.TilingRule> TilingRules { get; set; }
        List<CustomTileProperty> CustomTileProperties { get; set; }
        Tile.ColliderType DefaultColliderType { get; set; }
        Sprite DefaultSprite { get; set; }
        GameObject DefaultGameObject { get; set; }
        
        bool HasVariations { get; set; } 
        bool HasExtraTilingRules { get; set; } 
        bool TreatSimilarTilesAsSame { get; set; } 
        
        TileBase Tile { get; }

        BetterRuleTileNeighbor[] NeighborRules { get; }
    }
}