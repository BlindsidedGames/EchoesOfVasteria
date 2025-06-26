using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TimelessEchoes.Tilemaps // Ensure this namespace is correct for your project
{
    /// <summary>
    ///     This is a top-level class, not a nested one.
    /// </summary>
    [Serializable]
    public class WeightedTilingRule : RuleTile.TilingRule
    {
        public float[] m_SpriteWeights;
    }

    /// <summary>
    ///     RuleTile variant that lets you assign a weight to each sprite when the rule’s <b>Output</b> is set to <i>Random</i>
    ///     .
    /// </summary>
    [CreateAssetMenu(fileName = "Weighted Rule Tile", menuName = "2D/Tiles/Weighted Rule Tile")]
    public class WeightedRuleTile : RuleTile
    {
#if UNITY_EDITOR
        // This is the reflection-based method from your original script.
        // With the un-nested WeightedTilingRule class, this should now work correctly.
        public Type GetTilingRuleType()
        {
            return typeof(WeightedTilingRule);
        }
#endif

        // --- All the runtime logic for selecting sprites remains the same ---
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            var identity = Matrix4x4.identity;
            tileData.sprite = m_DefaultSprite;
            tileData.gameObject = m_DefaultGameObject;
            tileData.colliderType = m_DefaultColliderType;
            tileData.transform = identity;
            tileData.flags = TileFlags.LockTransform;

            var transform = identity;

            foreach (var rule in m_TilingRules)
            {
                if (!RuleMatches(rule, position, tilemap, ref transform))
                    continue;

                switch (rule.m_Output)
                {
                    case TilingRuleOutput.OutputSprite.Single:
                        if (rule.m_Sprites != null && rule.m_Sprites.Length > 0)
                            tileData.sprite = rule.m_Sprites[0];
                        break;

                    case TilingRuleOutput.OutputSprite.Animation:
                        if (rule.m_Sprites != null && rule.m_Sprites.Length > 0)
                            tileData.sprite = rule.m_Sprites[0];
                        tileData.flags |= TileFlags.LockColor;
                        break;

                    case TilingRuleOutput.OutputSprite.Random:
                    {
                        if (rule is WeightedTilingRule wRule && wRule.m_SpriteWeights != null &&
                            wRule.m_SpriteWeights.Length == rule.m_Sprites.Length && rule.m_Sprites.Length > 0)
                        {
                            var pick = GetWeightedIndex(wRule.m_SpriteWeights, position);
                            tileData.sprite = rule.m_Sprites[pick];
                        }
                        else if (rule.m_Sprites != null && rule.m_Sprites.Length > 0)
                        {
                            var hash = Mathf.Abs(GetRandomHash(position));
                            tileData.sprite = rule.m_Sprites[hash % rule.m_Sprites.Length];
                        }

                        break;
                    }
                }

                tileData.transform = transform;
                tileData.gameObject = rule.m_GameObject;
                tileData.colliderType = rule.m_ColliderType;
                return;
            }
        }

        private static int GetWeightedIndex(float[] weights, Vector3Int position)
        {
            var total = 0f;
            foreach (var w in weights) total += Mathf.Max(0f, w);
            if (total <= 0f) return 0;

            var rawHash = Mathf.Abs(GetRandomHash(position));
            var rnd = rawHash % 10000 / 10000f * total;

            var accum = 0f;
            for (var i = 0; i < weights.Length; ++i)
            {
                accum += Mathf.Max(0f, weights[i]);
                if (rnd < accum) return i;
            }

            return weights.Length - 1;
        }

        private static int GetRandomHash(Vector3Int position)
        {
            unchecked
            {
                var hash = position.x;
                hash = hash + 0x7ed55d16 + (hash << 12);
                hash ^= (int)0xc761c23c ^ (hash >> 19);
                hash += position.y;
                hash += 0x165667b1 + (hash << 5);
                hash = (hash + (int)0xd3a2646c) ^ (hash << 9);
                return hash;
            }
        }
    }
}