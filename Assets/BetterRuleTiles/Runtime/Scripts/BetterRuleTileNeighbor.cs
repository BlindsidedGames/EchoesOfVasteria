using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Runtime.Utilities;

namespace VinTools.BetterRuleTiles
{
    public class BetterRuleTileNeighbor
    {
        /// <summary>
        /// ID if the rule.
        /// Should be lESS THAN -1.
        /// IDs 0 and up are reserved for tile connectivity, while ID -1 is used for no rule.
        /// While you can change the name and texture of index -1, it will always delete tiles in the editor.
        /// </summary>
        public readonly int Id = -1;
        
        /// <summary>
        /// The name of the rule. It will be displayed on the editor buttons
        /// </summary>
        public readonly string Name = "Tile";
        
        /// <summary>
        /// Description. It'll show up in tooltips
        /// </summary>
        public readonly string Description = "";
        
        /// <summary>
        /// Function to match the rule
        /// </summary>
        private readonly RuleMatchCase _matchCase;
        public delegate bool RuleMatchCase(int neighbor, TileBase other);
        public bool TryInvokeMatchCase(int neighbor, TileBase tile, out bool result)
        {
            bool? check = _matchCase?.Invoke(neighbor, tile);
            if (check != null)
            {
                result = check.Value;
                return true;
            }
            
            result = true;
            return false;
        }

        #region Textures
        
        /// <summary>
        /// A Base64 string for the texture that'll appear on the editor buttons
        /// </summary>
        private readonly string ButtonTextureString;
        
        /// <summary>
        /// A Base64 string for the Texture that'll be displayed in the editor grid
        /// </summary>
        private readonly string TileTextureString;
        
        /// <summary>
        /// A Base64 string for the texture that'll show up in the inspector
        /// </summary>
        private readonly string InspectorTextureString;

        #if UNITY_EDITOR
        
        private Texture2D _tileTexture = null;
        /// <summary>
        /// Texture that will appear on the editor grid
        /// </summary>
        public Texture2D TileTexture
        {
            get 
            {
                if (!_tileTexture) _tileTexture = TextureUtils.Base64ToTexture(TileTextureString);
                return _tileTexture;
            }
        }
        
        private Texture2D _buttonTexture = null;
        /// <summary>
        /// Texture that will appear in the editor tile drawer
        /// </summary>
        public Texture2D ButtonTexture
        {
            get
            {
                if (!_buttonTexture) _buttonTexture = TextureUtils.Base64ToTexture(ButtonTextureString);
                return _buttonTexture;
            }
        }
        
        private Texture2D _inspectorTexture = null;
        /// <summary>
        /// Texture which will appear in the small grid in the inspector
        /// </summary>
        public Texture2D InspectorTexture
        {
            get
            {
                if (!_inspectorTexture) _inspectorTexture = TextureUtils.Base64ToTexture(InspectorTextureString);
                return _inspectorTexture;
            }
        }
        #endif
        #endregion
        
        /// <param name="id">ID if the rule</param>
        /// <param name="name">The name of the rule</param>
        /// <param name="description">Description</param>
        /// <param name="ruleMatchCase">Function to match the rule</param>
        /// <param name="buttonTextureString">A Base64 string for the texture that'll appear on the editor buttons</param>
        /// <param name="tileTextureString">A Base64 string for the Texture that'll be displayed in the editor grid</param>
        /// <param name="inspectorTextureString">A Base64 string for the texture that'll show up in the inspector</param>
        public BetterRuleTileNeighbor(int id, string name, string description, RuleMatchCase ruleMatchCase, string buttonTextureString = "", string tileTextureString = "", string inspectorTextureString = "")
        {
            Id = id;
            Name = name;
            Description = description;
            _matchCase = ruleMatchCase;
            ButtonTextureString = buttonTextureString;
            TileTextureString = tileTextureString;
            InspectorTextureString = inspectorTextureString;
        }
        /// <param name="id">ID if the rule</param>
        /// <param name="name">The name of the rule</param>
        /// <param name="description">Description</param>
        /// <param name="buttonTextureString">A Base64 string for the texture that'll appear on the editor buttons</param>
        /// <param name="tileTextureString">A Base64 string for the Texture that'll be displayed in the editor grid</param>
        /// <param name="inspectorTextureString">A Base64 string for the texture that'll show up in the inspector</param>
        public BetterRuleTileNeighbor(int id, string name, string description, string buttonTextureString = "", string tileTextureString = "", string inspectorTextureString = "")
        {
            Id = id;
            Name = name;
            Description = description;
            ButtonTextureString = buttonTextureString;
            TileTextureString = tileTextureString;
            InspectorTextureString = inspectorTextureString;
        }
        /// <param name="id">ID if the rule</param>
        /// <param name="name">The name of the rule</param>
        /// <param name="ruleMatchCase">Function to match the rule</param>
        /// <param name="buttonTextureString">A Base64 string for the texture that'll appear on the editor buttons</param>
        /// <param name="tileTextureString">A Base64 string for the Texture that'll be displayed in the editor grid</param>
        /// <param name="inspectorTextureString">A Base64 string for the texture that'll show up in the inspector</param>
        public BetterRuleTileNeighbor(int id, string name, RuleMatchCase ruleMatchCase, string buttonTextureString = "", string tileTextureString = "", string inspectorTextureString = "")
        {
            Id = id;
            Name = name;
            _matchCase = ruleMatchCase;
            ButtonTextureString = buttonTextureString;
            TileTextureString = tileTextureString;
            InspectorTextureString = inspectorTextureString;
        }
    }
}