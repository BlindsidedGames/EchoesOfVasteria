using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Internal;

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileContainer
    {
        [System.Serializable]
        public class UniversalSpriteData
        {
            public Sprite BaseSprite;

            public GameObject GameObject;
            public Sprite[] Sprites = new Sprite[0];
            public Tile.ColliderType ColliderType = Tile.ColliderType.Sprite;
            public ExtendedOutputSprite OutputSprite = ExtendedOutputSprite.Single;
            //public RuleTile.TilingRuleOutput.Transform Transform = RuleTile.TilingRuleOutput.Transform.Fixed;
            //public bool IncludeSpriteInOutput = false;

            public float NoiseScale = .5f;
            public RuleTile.TilingRuleOutput.Transform RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed;

            public float MinAnimationSpeed = 1f;
            public float MaxAnimationSpeed = 1f;
            public Vector2Int PatternSize = Vector2Int.one;

            public UniversalSpriteData(Sprite sprite)
            {
                BaseSprite = sprite;
                Sprites = new Sprite[] { sprite };
            }
            
            public UniversalSpriteData(UniversalSpriteData other)
            {
                this.BaseSprite = other.BaseSprite;
                this.GameObject = other.GameObject;
                this.Sprites = other.Sprites.ToArray();
                this.ColliderType = other.ColliderType;
                this.OutputSprite = other.OutputSprite;
                this.NoiseScale = other.NoiseScale;
                this.RandomTransform = other.RandomTransform;
                this.MinAnimationSpeed = other.MinAnimationSpeed;
                this.MaxAnimationSpeed = other.MaxAnimationSpeed;
                this.PatternSize = other.PatternSize;
            }

            public void ApplyToCell(GridCell cell)
            {
                cell.Sprite = this.BaseSprite;
                cell.GameObject = this.GameObject;
                cell.Sprites = this.Sprites;
                cell.ColliderType = this.ColliderType;
                cell.OutputSprite = this.OutputSprite;
                cell.NoiseScale = this.NoiseScale;
                cell.RandomTransform = this.RandomTransform;
                cell.MinAnimationSpeed = this.MinAnimationSpeed;
                cell.MaxAnimationSpeed = this.MaxAnimationSpeed;
                cell.PatternSize = this.PatternSize;
            }
        }
    }
}