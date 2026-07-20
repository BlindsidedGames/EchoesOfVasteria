using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using VinTools.BetterRuleTiles.Internal;
using System;
using UnityEngine.Serialization;

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileBase : RuleTile, IBetterRuleTile
    {
        public int UniqueID;
        public int UniqueIdentifier { get => UniqueID; set => UniqueID = value; }
        
        public TileBase[] otherTiles { get; set; }
        public List<TileBase> variations { get; set; } = new List<TileBase>();

        // Extended rule data
        public List<ExtendedTilingRule> TempTilingRules { get; set; } = new List<ExtendedTilingRule>();
        public List<ExtraTilingRule> m_ExtraTilingRules { get; set; } = new List<ExtraTilingRule>();
        public List<RuleTile.TilingRule> TilingRules { get => m_TilingRules; set  => m_TilingRules = value; }
        public List<CustomTileProperty> CustomTileProperties { get => customProperties; set => customProperties = value; }
        public Tile.ColliderType DefaultColliderType { get => m_DefaultColliderType; set  => m_DefaultColliderType = value; }
        public Sprite DefaultSprite { get => m_DefaultSprite; set => m_DefaultSprite = value; }
        public GameObject DefaultGameObject { get => m_DefaultGameObject; set => m_DefaultGameObject = value; }
        
        //performance improvements
        public bool HasVariations { get; set; } = true;
        public bool HasExtraTilingRules { get; set; } = true;
        public bool TreatSimilarTilesAsSame { get; set; } = false;

        // Extra getters
        public TileBase Tile => this;
        
        /// <summary>
        /// The neighbor rules used in the editor and in the rule tile. Override this to add extra rules to the tile.
        /// </summary>
        protected virtual BetterRuleTileNeighbor[] InitializeCustomNeighborRules => new BetterRuleTileNeighbor[] {};
        
        private BetterRuleTileNeighbor[] _neighborRules;
        public BetterRuleTileNeighbor[] NeighborRules
        {
            get
            {
                // initialize rues if it's not initialized
                if (_neighborRules == null || _neighborRules.Length == 0) 
                    _neighborRules = InitializeCustomNeighborRules;
                
                // return if already initialized
                return _neighborRules;
            }
        }
        
        
        public override bool RuleMatch(int neighbor, TileBase tile)
        {
            if (tile is RuleOverrideTile ot)
                tile = ot.m_InstanceTile;

            // rule connectivity
            if (neighbor > 0 && neighbor <= otherTiles.Length) return tile == ((IBetterRuleTile)otherTiles[neighbor - 1]).Tile || (TreatSimilarTilesAsSame && ((IBetterRuleTile)otherTiles[neighbor - 1]).HasVariations && ((IBetterRuleTile)otherTiles[neighbor - 1]).variations.Contains(tile));
            if (neighbor == 0) return tile == this || (HasVariations && variations.Contains(tile));
            
            // Check all custom neighbor rules
            foreach (var rules in NeighborRules)
            {
                if (neighbor == rules.Id && rules.TryInvokeMatchCase(neighbor, tile, out bool result)) return result;
            }
            
            // if no rule matched
            return true;
        }
        
        
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);

            if (!HasExtraTilingRules || m_TilingRules.Count != m_ExtraTilingRules.Count) return;

            for (int i = 0; i < m_TilingRules.Count; i++)
            {
                Matrix4x4 transform = tileData.transform;
                if (RuleMatches(m_TilingRules[i], position, tilemap, ref transform))
                {
                    // Check for extra output
                    ExtraTilingRule extraData = m_ExtraTilingRules[i];
                    switch (extraData.m_ExtendedOutputSprite)
                    {
                        case ExtendedOutputSprite.Pattern:
                            Sprite output = BetterRuleTileMethods.GetPatternSprite(position, m_TilingRules[i], extraData);
                            if (output != null) tileData.sprite = output;
                            break;
                    }
                    
                    // This should be here... if we don't want the last pattern to always get prioritized 
                    break;
                }
            }
        }
    }
}