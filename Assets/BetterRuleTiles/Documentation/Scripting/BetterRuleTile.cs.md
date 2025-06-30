---
title: BetterRuleTile.cs
---

# BetterRuleTile.cs

### Namespace
```cs 
using VinTools.BetterRuleTiles; 
```

### Methods

```cs
public int GetInt(string key, int defaultValue = default);
public float GetFloat(string key, float defaultValue = default);
public double GetDouble(string key, double defaultValue = default);
public char GetChar(string key, char defaultValue = default);
public string GetString(string key, string defaultValue = default);
public bool GetBool(string key, bool defaultValue = default);
```

### Example script

```cs
//This script finds the tile of the specified tilemap where your mouse cursor is
//If the tile under your mouse cursor is a better rule tile, it will read it's custom properties

using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles;

public class CustomPropertyTest : MonoBehaviour
{
    public Tilemap tilemap;
    public Camera cam;

    private void Awake()
    {
        //if the script is placed on the tilemap itself, this will assign it automatically
        if (tilemap == null && gameObject.TryGetComponent(out Tilemap map)) tilemap = map;
        //set the camera if it wasn't set by the user
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        //check if tilemap and camera were assigned
        if (tilemap == null)
        {
            Debug.LogError("Tilemap was not assigned!", this);
            return;
        }
        if (cam == null)
        {
            Debug.LogError("Camera was not assigned!", this);
            return;
        }

        //get the cell position of the mouse (this could also be the position of the player for example)
        //you have to cast the worldpoint to a vector2 so it doesn't use the camera's z position
        Vector3Int cellPos = tilemap.WorldToCell((Vector2)cam.ScreenToWorldPoint(Input.mousePosition));

        Debug.Log(cellPos);
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

            Debug.Log($"Tile: {tile.name}, Walkable: {walkable}, Walking speed: {walkspeed}");
        }
        //if the tile is not a better tule tile
        else if (tile != null) Debug.Log($"Tile: {tile.name}");
        //if there is no tile
        else Debug.Log("Tile: Empty");
    }
}
```