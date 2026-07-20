using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileContainer
    {
        [System.Serializable]
        public class EditorSettings
        {

            //public int _selectedExportScript;
            //public string _selectedExportScript = "Default";
            public string _selectedExportScriptGUID = "Default";
            public GridShape _gridShape = GridShape.Square;

            public float _zoomAmount = 1;
            public const int GridUnitPixelSize = 32;
            public Vector2 _cellSize = Vector2.one;
            public Vector2 _tileSize = Vector2.one;
            public Vector2 _tileRenderOffset = Vector2.zero;
            public Vector2 _gridOffset = Vector2.zero;
            [FormerlySerializedAs("_tileAnchor")] public Vector2 _spriteAnchor = new Vector2(.5f, .5f);

            public bool _lockWindows = true;
            public bool _hideSprites = false;
            public bool _showRuler = true;
            public bool _showModified = false;
            public bool _renderSmallGrid = false;
            public float _zoomTreshold = .35f;
            public int _drawerSize = 6;
            public bool _displaySpriteDrawer = true;
            public bool _displayUniversalSpriteSettings = false;

            public int _drawerHeight = 50;
            public int _drawerCollumns = 2;
            public int _expandedDrawerCollumns = 10;

            public bool _renderLockedOverlay = false;
            public bool _renderLockedOutline = true;
            public Color _lockedOutlineColor = Color.red;

            //public bool _generatePalette = true;
            //public bool _addMissingRules = true;
            public bool _collapseSimilarRules = false;
            public bool _useUniversalSpriteSettings = false;
            public bool _treatSimilarTilesAsSame = false;

            public Color _presetBlockColor = new Color(255 / 255f, 147 / 255f, 15 / 255f);

            public void SetDisplaySpriteDrawer(bool set)
            {
                if (set && _displayUniversalSpriteSettings) _displayUniversalSpriteSettings = false;
                _displaySpriteDrawer = set;
            }
            public void SetDisplayUniversalSpriteSettings(bool set)
            {
                if (set && _displaySpriteDrawer) _displaySpriteDrawer = false;
                _displayUniversalSpriteSettings = set;
            }
        }
    }
}