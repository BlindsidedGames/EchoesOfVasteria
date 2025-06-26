using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TimelessEchoes.Tilemaps
{
    /// <summary>
    /// Extension of RuleTile that supports weighted random sprite output.
    /// </summary>
    [Serializable]
    public class WeightedRuleTile : RuleTile
    {
        /// <summary>
        /// Tiling rule with per-sprite weights.
        /// </summary>
        [Serializable]
        public class WeightedTilingRule : TilingRule
        {
            public float[] m_SpriteWeights = new float[1];

            public new WeightedTilingRule Clone()
            {
                var rule = new WeightedTilingRule
                {
                    m_Neighbors = new List<int>(m_Neighbors),
                    m_NeighborPositions = new List<Vector3Int>(m_NeighborPositions),
                    m_RuleTransform = m_RuleTransform,
                    m_Sprites = (Sprite[])m_Sprites.Clone(),
                    m_GameObject = m_GameObject,
                    m_MinAnimationSpeed = m_MinAnimationSpeed,
                    m_MaxAnimationSpeed = m_MaxAnimationSpeed,
                    m_PerlinScale = m_PerlinScale,
                    m_Output = m_Output,
                    m_ColliderType = m_ColliderType,
                    m_RandomTransform = m_RandomTransform,
                    m_SpriteWeights = (float[])m_SpriteWeights.Clone()
                };
                return rule;
            }
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            var iden = Matrix4x4.identity;

            tileData.sprite = m_DefaultSprite;
            tileData.gameObject = m_DefaultGameObject;
            tileData.colliderType = m_DefaultColliderType;
            tileData.flags = TileFlags.LockTransform;
            tileData.transform = iden;

            Matrix4x4 transform = iden;
            foreach (TilingRule rule in m_TilingRules)
            {
                if (RuleMatches(rule, position, tilemap, ref transform))
                {
                    switch (rule.m_Output)
                    {
                        case TilingRuleOutput.OutputSprite.Single:
                        case TilingRuleOutput.OutputSprite.Animation:
                            tileData.sprite = rule.m_Sprites[0];
                            break;
                        case TilingRuleOutput.OutputSprite.Random:
                            int index;
                            if (rule is WeightedTilingRule wRule &&
                                wRule.m_SpriteWeights != null &&
                                wRule.m_SpriteWeights.Length == rule.m_Sprites.Length)
                            {
                                index = GetWeightedIndex(position, wRule.m_SpriteWeights);
                            }
                            else
                            {
                                index = Mathf.Clamp(
                                    Mathf.FloorToInt(GetPerlinValue(position, rule.m_PerlinScale, 100000f) *
                                                     rule.m_Sprites.Length),
                                    0, rule.m_Sprites.Length - 1);
                            }
                            tileData.sprite = rule.m_Sprites[index];
                            if (rule.m_RandomTransform != TilingRuleOutput.Transform.Fixed)
                                transform = ApplyRandomTransform(rule.m_RandomTransform, transform,
                                    rule.m_PerlinScale, position);
                            break;
                    }

                    tileData.transform = transform;
                    tileData.gameObject = rule.m_GameObject;
                    tileData.colliderType = rule.m_ColliderType;
                    break;
                }
            }
        }

        private static int GetWeightedIndex(Vector3Int position, float[] weights)
        {
            var oldState = Random.state;
            long hash = position.x;
            hash = hash + 0xabcd1234 + (hash << 15);
            hash = hash + 0x0987efab ^ (hash >> 11);
            hash ^= position.y;
            hash = hash + 0x46ac12fd + (hash << 7);
            hash = hash + 0xbe9730af ^ (hash << 11);
            Random.InitState((int)hash);

            float total = 0f;
            foreach (var w in weights)
                total += w;

            float r = Random.Range(0f, total);
            for (int i = 0; i < weights.Length; i++)
            {
                r -= weights[i];
                if (r < 0f)
                {
                    Random.state = oldState;
                    return i;
                }
            }

            Random.state = oldState;
            return Mathf.Max(0, weights.Length - 1);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(WeightedRuleTile))]
        public class WeightedRuleTileEditor : RuleTileEditor
        {
            public override void RuleInspectorOnGUI(Rect rect, RuleTile.TilingRuleOutput tilingRule)
            {
                base.RuleInspectorOnGUI(rect, tilingRule);

                var wRule = tilingRule as WeightedTilingRule;
                if (wRule == null || wRule.m_Output != TilingRuleOutput.OutputSprite.Random)
                    return;

                int count = wRule.m_Sprites != null ? wRule.m_Sprites.Length : 0;
                if (wRule.m_SpriteWeights == null || wRule.m_SpriteWeights.Length != count)
                    Array.Resize(ref wRule.m_SpriteWeights, count);

                for (int i = 0; i < count; i++)
                    wRule.m_SpriteWeights[i] = EditorGUILayout.FloatField($"Weight {i + 1}", wRule.m_SpriteWeights[i]);
            }
        }
#endif
    }
}
