using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Internal;

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileContainer
    {
        [System.Serializable]
        public class GridCell
        {
            public Vector2Int Position;
            public Sprite Sprite;
            public int TileID;
            public bool UseDefaultSettings = true;
            public bool AreNeighborPositionsModified;
            public bool AreOutputSpritesModified;
            public bool Locked = false;
            public int Priority = 0;
            public int PriorityModifier = 0;
            public int info_InitialRuleOrder = -1;
            public int info_ExportedRuleOrder = -1;

            public bool _overrideUniversalSpriteSettings = false;
            public GameObject GameObject;
            public Sprite[] Sprites = new Sprite[0];
            public Tile.ColliderType ColliderType = Tile.ColliderType.Sprite;
            public ExtendedOutputSprite OutputSprite = ExtendedOutputSprite.Single;
            public RuleTile.TilingRuleOutput.Transform Transform = RuleTile.TilingRuleOutput.Transform.Fixed;
            public bool IncludeSpriteInOutput = false;

            public float NoiseScale = .5f;
            public RuleTile.TilingRuleOutput.Transform RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed;

            public float MinAnimationSpeed = 1f;
            public float MaxAnimationSpeed = 1f;
            public Vector2Int PatternSize = Vector2Int.one;

            public List<Vector3Int> NeighborPositions;

            public GridCell(Vector2Int pos, GridShape shape)
            {
                Position = pos;
                NeighborPositions = DefaultNeighborPositions(shape, pos);
            }
            public GridCell() { }

            public static GridCell Clone(GridCell copy)
            {
                //create a copy of the neighbor positions
                var positionsCopy = new List<Vector3Int>();
                foreach (var item in copy.NeighborPositions) positionsCopy.Add(new Vector3Int(item.x ,item.y, item.z));

                return new GridCell
                {
                    Position = copy.Position,
                    Sprite = copy.Sprite,
                    TileID = copy.TileID,
                    UseDefaultSettings = copy.UseDefaultSettings,
                    AreNeighborPositionsModified = copy.AreNeighborPositionsModified,
                    AreOutputSpritesModified = copy.AreOutputSpritesModified,
                    Locked = copy.Locked,

                    Sprites = copy.Sprites,
                    OutputSprite = copy.OutputSprite,
                    Transform = copy.Transform,
                    GameObject = copy.GameObject,
                    ColliderType = copy.ColliderType,
                    IncludeSpriteInOutput = copy.IncludeSpriteInOutput,

                    NoiseScale = copy.NoiseScale,
                    RandomTransform = copy.RandomTransform,

                    MinAnimationSpeed = copy.MinAnimationSpeed,
                    MaxAnimationSpeed = copy.MaxAnimationSpeed,

                    NeighborPositions = positionsCopy,
                    PatternSize = copy.PatternSize
                };
            }

            public bool CheckModifiedNeighborPositions()
            {
                if (Transform != RuleTile.TilingRuleOutput.Transform.Fixed) return true;

                //TODO Maybe count for hexagonal tiles
                if (NeighborPositions.Count != 8 && NeighborPositions.Count != 6) return true;
                if (NeighborPositions.Min(t => t.x) < -1) return true;
                if (NeighborPositions.Max(t => t.x) < -1) return true;
                if (NeighborPositions.Min(t => t.y) > 1) return true;
                if (NeighborPositions.Max(t => t.y) > 1) return true;

                return false;
            }

            public bool CheckModifiedOutput()
            {
                if (!UseDefaultSettings || ColliderType != Tile.ColliderType.Sprite) return true;
                if (OutputSprite != ExtendedOutputSprite.Single) return true;
                if (Sprites.Length > 1) return true;

                return false;
            }
        }
    }
}