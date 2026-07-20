#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Utilities;

namespace VinTools.BetterRuleTiles.Editor.Generation
{
    public partial class BetterRuleTileGenerator
    {
        private static Dictionary<string, Type> _tileTypes;
        public static Dictionary<string, Type> TileTypes {
            get
            {
                if (_tileTypes == null) _tileTypes = GetDerivedTileTypes();
                return _tileTypes;
            }
        }
        
        /// <summary>
        /// Get all the class types that derive from BetterRuleTileBase or BetterHexagonalRuleTileBase
        /// </summary>
        /// <returns>List of class types</returns>
        [MenuItem("Tools/Better Rule Tiles/Find Derived Classes")]
        private static Dictionary<string, Type> GetDerivedTileTypes()
        {
            var types = TypeCache.GetTypesDerivedFrom<BetterRuleTileBase>();
            var hextypes = TypeCache.GetTypesDerivedFrom<BetterHexagonalRuleTileBase>();
            List<Type> typesList = new List<Type>();
            
            foreach (var type in types) typesList.Add(type);
            foreach (var type in hextypes) typesList.Add(type);
            var dict = typesList.ToDictionary(TypeUtils.GetTypeGuid);
            dict.Add("Default", null);
            return dict;
        }
        
        /// <summary>
        /// Return the class type that matches the given tileType
        /// </summary>
        /// <param name="tileGuid"></param>
        /// <param name="container">The container asset for fallback</param>
        /// <returns>The script type associated with the key</returns>
        public static Type GetCustomTileType(string tileGuid, BetterRuleTileContainer container)
        {
            // get default 
            if (tileGuid == "Default") return GetDefaultTileType(container.settings._gridShape);

            foreach (var type in TileTypes)
                if (type.Key == tileGuid) return type.Value;
            
            // fallback
            return GetDefaultTileType(container.settings._gridShape);
        }
        
        /// <summary>
        /// Generate the tiles based on the selected tile type in the container file
        /// </summary>
        /// <param name="container"></param>
        /// <returns>Whether the tiles got generated or not</returns>
        private static bool GenerateCustomTiles(BetterRuleTileContainer container)
        {
            string tileGuid = container.settings._selectedExportScriptGUID;
            if (tileGuid == "Default") return false;
            Type tileType = TileTypes[tileGuid];
            
            // Square tiles
            if (tileType.IsSubclassOf(typeof(BetterRuleTileBase)))
            {
                GenerateTiles<BetterRuleTileBase>(container);
                return true;
            }
            // Hey tiles
            if (tileType.IsSubclassOf(typeof(BetterHexagonalRuleTileBase)))
            {
                GenerateTiles<BetterHexagonalRuleTileBase>(container);
                return true;
            }
            
            // failed to create tile
            return false;
        }
    }
}
#endif