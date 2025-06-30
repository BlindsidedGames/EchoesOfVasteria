using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using VinTools.BetterRuleTiles.Internal;
using System;

namespace VinTools.BetterRuleTiles
{
    [CreateAssetMenu(menuName = "VinTools/Custom Tiles/Better Hexagonal Rule Tile")]
    public class BetterHexagonalRuleTile : HexagonalRuleTile<Neighbor>
    {
        public int UniqueID;
        public BetterHexagonalRuleTile[] otherTiles;
        public List<TileBase> variations = new List<TileBase>();
        public List<CustomTileProperty> customProperties = new List<CustomTileProperty>();

        public List<ExtendedTilingRule> m_ExtendedTilingRules = new List<ExtendedTilingRule>();
        public List<ExtraTilingRule> m_ExtraTilingRules = new List<ExtraTilingRule>();

        //performance improvements
        public bool HasVariations = true;
        public bool HasExtraTilingRules = true;
        public bool TreatSimilarTilesAsSame = false;

        public int UniqueIdentifier { get => UniqueID; set => UniqueID = value; }
        public UnityEngine.Object TileObject { get => this; }

        public override bool RuleMatch(int neighbor, TileBase tile)
        {
            if (tile is RuleOverrideTile ot)
                tile = ot.m_InstanceTile;

            switch (neighbor)
            {
                case Neighbor.This: return tile == this || (HasVariations && variations.Contains(tile));
                case Neighbor.NotThis: return tile != this && (!HasVariations || !variations.Contains(tile));
                case Neighbor.Any: return tile != null;
                case Neighbor.Empty: return tile == null;
                case Neighbor.Ignore: return true;
                default:
                    if (neighbor > 0) return tile == otherTiles[neighbor - 1] || (TreatSimilarTilesAsSame && otherTiles[neighbor - 1].HasVariations && otherTiles[neighbor - 1].variations.Contains(tile));
                    break;
            }
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
                    // check for extra output
                    ExtraTilingRule extraData = m_ExtraTilingRules[i];
                    switch (extraData.m_ExtendedOutputSprite)
                    {
                        case ExtendedOutputSprite.Pattern:
                            Sprite output = BetterRuleTileMethods.GetPatternSprite(position, m_TilingRules[i], extraData);
                            if (output != null) tileData.sprite = output;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the integer with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public int GetInt(string key, int defaultValue = default)
        {
            if (GetProperty(key, out var p)) return p.GetInt();
            else return defaultValue;
        }
        /// <summary>
        /// Gets the float with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public float GetFloat(string key, float defaultValue = default)
        {
            if (GetProperty(key, out var p)) return p.GetFloat();
            else return defaultValue;
        }
        /// <summary>
        /// Gets the double with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public double GetDouble(string key, double defaultValue = default)
        {
            if (GetProperty(key, out var p)) return p.GetDouble();
            else return defaultValue;
        }
        /// <summary>
        /// Gets the character with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public char GetChar(string key, char defaultValue = default)
        {
            if (GetProperty(key, out var p)) return p.GetChar();
            else return defaultValue;
        }
        /// <summary>
        /// Gets the string with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public string GetString(string key, string defaultValue = default)
        {
            if (GetProperty(key, out var p)) return p.GetString();
            else return defaultValue;
        }
        /// <summary>
        /// Gets the boolean with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public bool GetBool(string key, bool defaultValue = default)
        {
            if (GetProperty(key, out var p)) return p.GetBool();
            else return defaultValue;
        }

        private bool GetProperty(string key, out CustomTileProperty prop)
        {
            prop = customProperties.Find(t => t._key == key);
            if (prop == null) return false;
            else return true;
        }
    }
}