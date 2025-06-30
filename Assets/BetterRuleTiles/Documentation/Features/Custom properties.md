---
title: Custom Properties
---
# Custom properties

Custom properties are a way to store information in your [[Tile]]s, that you can later access at runtime using a script.

To add properties to your tiles, select a tile, and in the [[Tile inspector]], scroll down to the `Custom properties` category. Here you can store different types of data in your tile. These datatypes are the following:
- **Int**
- **Float**
- **Double**
- **Char**
- **String**
- **Bool**

![[Features/images/tile-custom-properties.png]]

You can set a name for the property and a value. You'll use this `property name` to access the value of that property.
# Scripting

To read the properties of the tile, you'll need a reference to the tilemap that the tiles are placed on. If you have this reference to the tilemap, you can use the `tilemap.WorldToCell(Vector2 worldPosition)` method to get the tile coordinate at your desired world position. 

After you got the tile coordinate, you can use the `tilemap.GetTile(Vector3Int position)` function to get the tile at that position. This function returns a `TileBase` class, so we first have to check if it's a `BetterRuleTile`, than cast it to such. 

After we've converted the `TileBase` into a `BetterRuleTile` object, we can use the various `Get` functions to get the type of property we want. Each type of property has it's own `Get` function, so use the one you need. You can find all methods on the [[BetterRuleTile.cs]] documentation page.

```cs
//This script finds the tile of the specified tilemap where your mouse cursor is
//If the tile under your mouse cursor is a better rule tile, it will read it's custom properties

using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles;

namespace VinTools.BetterRuleTiles.Sample
{
    public class CustomPropertyTest : MonoBehaviour
    {
        public Tilemap tilemap;
        public Camera cam;

        public event Action<string> onReadValue;

        private void Update()
        {
            //get the cell position of the mouse (this could also be the position of the player for example)
            //you have to cast the worldpoint to a vector2 so it doesn't use the camera's z position
            Vector3Int cellPos = tilemap.WorldToCell((Vector2)cam.ScreenToWorldPoint(Input.mousePosition));

            //get the tile on that cell position as a better rule tile
            TileBase tile = tilemap.GetTile(cellPos);

            //check if the tile is not null. The tile can be null if there was no tile placed there
            //check if the tile is a better rule tile
            if (tile != null && tile is BetterRuleTile)
            {
                //convert it to a better rule tile
                BetterRuleTile betterTile = tile as BetterRuleTile;

                //get the values
                bool walkable = betterTile.GetBool("Walkable");
                float walkspeed = betterTile.GetFloat("WalkingSpeed");

                onReadValue?.Invoke($"Tile: {tile.name}\nWalkable: {walkable}\nWalking speed: {walkspeed}");
            }
            //if the tile is not a better tule tile
            else if (tile != null) onReadValue?.Invoke($"Tile: {tile.name}");
            //if there is no tile
            else onReadValue?.Invoke("Tile: Empty");
        }
    }
}
```