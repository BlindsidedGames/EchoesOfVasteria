using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.RuleTile;


namespace VinTools.BetterRuleTiles.Internal
{
    [Serializable]
    public class ExtendedTilingRule : TilingRule
    {
        public ExtendedOutputSprite m_ExtendedOutputSprite;
        public Vector2Int m_PatternSize = Vector2Int.one;

        public RuleTile.TilingRule.OutputSprite ConvertOutputSprite() => ConvertOutputSprite(m_ExtendedOutputSprite);
        public static RuleTile.TilingRule.OutputSprite ConvertOutputSprite(ExtendedOutputSprite outputSprite)
        {
            switch (outputSprite)
            {
                case ExtendedOutputSprite.Single: return TilingRuleOutput.OutputSprite.Single;
                case ExtendedOutputSprite.Random: return TilingRuleOutput.OutputSprite.Random;
                case ExtendedOutputSprite.Animation: return TilingRuleOutput.OutputSprite.Animation;
                case ExtendedOutputSprite.Pattern: return default;
                default: return default;
            }
        }

        public bool IsCustomOutputSprite
        {
            get
            {
                switch (m_ExtendedOutputSprite)
                {
                    case ExtendedOutputSprite.Single: return false;
                    case ExtendedOutputSprite.Random: return false;
                    case ExtendedOutputSprite.Animation: return false;
                    case ExtendedOutputSprite.Pattern: return true;
                    default: return true;
                }
            }
        }

        public TilingRule ExportTilingRule() => ExportTilingRule(this);
        public static TilingRule ExportTilingRule(ExtendedTilingRule rule)
        {
            TilingRule newRule = new TilingRule();

            newRule.m_ColliderType = rule.m_ColliderType;
            newRule.m_GameObject = rule.m_GameObject;
            newRule.m_MaxAnimationSpeed = rule.m_MaxAnimationSpeed;
            newRule.m_MinAnimationSpeed = rule.m_MinAnimationSpeed;
            newRule.m_NeighborPositions = rule.m_NeighborPositions;
            newRule.m_Neighbors = rule.m_Neighbors;
            newRule.m_Output = rule.ConvertOutputSprite();
            newRule.m_PerlinScale = rule.m_PerlinScale;
            newRule.m_RandomTransform = rule.m_RandomTransform;
            newRule.m_RuleTransform = rule.m_RuleTransform;
            newRule.m_Sprites = new Sprite[rule.m_Sprites.Length];
            Array.Copy(rule.m_Sprites, newRule.m_Sprites, rule.m_Sprites.Length);

            return newRule;
        }


        public ExtraTilingRule ExportExtras() => ExportExtras(this);
        public static ExtraTilingRule ExportExtras(ExtendedTilingRule rule)
        {
            ExtraTilingRule newRule = new ExtraTilingRule();

            newRule.m_ExtendedOutputSprite = rule.m_ExtendedOutputSprite;
            newRule.m_Patternsize = rule.m_PatternSize;

            return newRule;
        }
    }

    [Serializable]
    public class ExtraTilingRule
    {
        public ExtendedOutputSprite m_ExtendedOutputSprite = ExtendedOutputSprite.Single;
        public Vector2Int m_Patternsize = Vector2Int.one;
    }

    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        new public const int This = 0;
        public const int Ignore = -1;
        public const int Empty = -2;
        new public const int NotThis = -3;
        public const int Any = -4;
    }

    public static class BetterRuleTileMethods
    {
        /// <summary>
        /// Get a sprite from a tiling rule data for a pattern based on the coordinate
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="data"></param>
        /// <param name="extraData"></param>
        /// <returns></returns>
        public static Sprite GetPatternSprite(Vector3Int pos, TilingRule data, ExtraTilingRule extraData)
        {
            //check if array lenght matches the dimensions
            if (data.m_Sprites.Length != extraData.m_Patternsize.x * extraData.m_Patternsize.y) return null;

            //prevents the values to be negative
            while (pos.x < extraData.m_Patternsize.x) { pos.x += extraData.m_Patternsize.x; }
            while (pos.y < extraData.m_Patternsize.y) { pos.y += extraData.m_Patternsize.y; }

            //get the index on each axis
            int x = pos.x % extraData.m_Patternsize.x;
            int y = pos.y % extraData.m_Patternsize.y;

            //get the index in the array
            int index = x + (((extraData.m_Patternsize.y - 1) * extraData.m_Patternsize.x) - y * extraData.m_Patternsize.x);

            //returns the correct sprite
            return data.m_Sprites[index];
        }
    }

    public enum ExtendedOutputSprite
    {
        Single,
        Random,
        Animation,
        Pattern
    }
}