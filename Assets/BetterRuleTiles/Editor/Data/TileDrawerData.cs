#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Generation;

namespace VinTools.BetterRuleTiles.Editor.Data
{
    public class TileDrawerData
    {
        public Dictionary<int, Texture2D> ButtonTextureDictionary = new Dictionary<int, Texture2D>();
        public Dictionary<int, Texture2D> TileTextureDictionary = new Dictionary<int, Texture2D>();
        public Dictionary<int, string> TileNameDictionary = new Dictionary<int, string>();
        public Dictionary<int, string> TileDescriptionDictionary = new Dictionary<int, string>();
        public int[] Ids = new int[] {};
        
        public void Initialize(BetterRuleTileContainer container)
        {
            // create an instance of the rule tile to get the data from it
            IBetterRuleTile tileInstance = (IBetterRuleTile)ScriptableObject.CreateInstance(
                BetterRuleTileGenerator.GetCustomTileType(container.settings._selectedExportScriptGUID, container));
            var Data = tileInstance.NeighborRules;
            Object.DestroyImmediate(tileInstance as Object);
            
            // clear the dictionaries
            ButtonTextureDictionary.Clear();
            TileTextureDictionary.Clear();
            TileNameDictionary.Clear();
            TileDescriptionDictionary.Clear();
            List<int> tempIds = new List<int>();
            
            // Initialize the dictionaries with the data
            foreach (var tileNeighbor in Data)
            {
                ButtonTextureDictionary.Add(tileNeighbor.Id, tileNeighbor.ButtonTexture);
                TileTextureDictionary.Add(tileNeighbor.Id, tileNeighbor.TileTexture);
                TileNameDictionary.Add(tileNeighbor.Id, tileNeighbor.Name);
                TileDescriptionDictionary.Add(tileNeighbor.Id, tileNeighbor.Description);
                tempIds.Add(tileNeighbor.Id);
            }
            
            Ids = tempIds.OrderByDescending(id => id).ToArray();
        }
        
        public Texture2D GetButtonTexture(int id)
        {
            if (ButtonTextureDictionary.TryGetValue(id, out var texture)) return texture;
            return null;
        }
        public Texture2D GetTileTexture(int id)
        {
            if (TileTextureDictionary.TryGetValue(id, out var texture)) return texture;
            return null;
        }
        public string GetTileName(int id)
        {
            if (TileNameDictionary.TryGetValue(id, out var str)) return str;
            return null;
        }
        public string GetTileDescription(int id)
        {
            if (TileDescriptionDictionary.TryGetValue(id, out var str)) return str;
            return null;
        }

        public TileDrawerData(BetterRuleTileContainer container)
        {
            Initialize(container);
        }
    }
}
#endif