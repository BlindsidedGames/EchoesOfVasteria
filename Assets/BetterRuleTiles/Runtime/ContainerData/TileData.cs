using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Internal;

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileContainer
    {
        [System.Serializable]
        public class TileData
        {
            public int UniqueID;

            public string Name;
            public Texture2D Texture;

            public Color Color;
            public TileShape TextureShape;
            public bool MirrorTexX;
            public bool MirrorTexY;

            public Sprite DefaultSprite;
            public Tile.ColliderType ColliderType = Tile.ColliderType.Sprite;
            public GameObject DefaultGameObject;

            public bool uniqueTile = true;
            public int variationOf = -1;

            public List<int> variations = new List<int>();
            public List<CustomTileProperty> customProperties = new List<CustomTileProperty>();

            public TileData(string name, Color color, int id, GridShape shape)
            {
                Name = name;
                Color = color;
                UniqueID = id;

                switch (shape)
                {
                    case GridShape.Isometric: TextureShape = TileShape.Isometric; break;
                    case GridShape.HexagonalPointedTop: TextureShape = TileShape.HexagonPointedTop; break;
                    case GridShape.HexagonalFlatTop: TextureShape = TileShape.HexagonFlatTop; break;
                    default: TextureShape = TileShape.Square; break;
                }
            }
            
            public TileData() { }

            public static void CloneTileData(TileData from, TileData to)
            {
                to.UniqueID = from.UniqueID;
                to.Name = from.Name;
                to.Texture = from.Texture;
                to.Color = from.Color;
                to.TextureShape = from.TextureShape;
                to.MirrorTexX = from.MirrorTexX;
                to.MirrorTexY = from.MirrorTexY;
                to.DefaultSprite = from.DefaultSprite;
                to.ColliderType = from.ColliderType;
                to.DefaultGameObject = from.DefaultGameObject;
                to.uniqueTile = from.uniqueTile;
                to.variationOf = from.variationOf;
                to.variations = from.variations;
                to.customProperties = from.customProperties;
                to.variations = from.variations;
                to.customProperties = from.customProperties;
            }
        }
    }
}