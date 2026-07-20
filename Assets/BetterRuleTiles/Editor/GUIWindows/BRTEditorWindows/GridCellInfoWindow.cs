#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Editor.CustomGUI;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using VinTools.BetterRuleTiles.Editor.Generation;
using VinTools.BetterRuleTiles.Internal;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class GridCellInfoWindow : GUIWindow
    {
        public const int DefaultWindowWidth = 300;
        private const int DefaultWindowHeight = 350;
        
        public GridCellInfoWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base(
            GUIWindow.WindowLayout.BottomRightCorner, new RectOffset(4, 4, 30, 4), new Vector2(DefaultWindowWidth, DefaultWindowHeight), new GUIContent(""), false)
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }


        
        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;
        private BetterRuleTileContainer.GridCell CurrentCell => _editor._inspectingCell;
        
        private Vector2 _gridCellViewScroll;
        private bool _windowCollapsed = false;
        private bool _showSpritesArray = false;
        private Rect _dragDropRect1;
        private Rect _dragDropRect2;
        
        private void UpdateUI(int windowID)
        {
            // lock position
            lockPosition = _file.settings._lockWindows;
            height = _windowCollapsed ? 25 : DefaultWindowHeight;
            width = DefaultWindowWidth;
            
            if (CurrentCell == null) return;

            // title
            GUI.Label(
                new Rect(0, 0, width, 25), 
                $"Cell info: {CurrentCell.Position} - {(CurrentCell.TileID < 1 ? _editor._drawerData.GetTileName(CurrentCell.TileID) : _file.Tiles.Find(t => t.UniqueID == CurrentCell.TileID).Name)}", 
                Style.CenteredBoldLabel
                );

            // collapse
            if (GUI.Button(new Rect(width - 20, 3, 16, 16), _windowCollapsed ? Tex.Get(Tex.T_ID.t_tileInfo_upArrow) : Tex.Get(Tex.T_ID.t_tileInfo_downArrow), Style.IconButton))
                _windowCollapsed = !_windowCollapsed;
            if (_windowCollapsed) return;

            // scroll rect setup
            _gridCellViewScroll = EditorGUILayout.BeginScrollView(_gridCellViewScroll);

            // default settings
            EditorGUILayout.Space();

            int spriteOptionsLines = _file.settings._useUniversalSpriteSettings ? 5 : (CurrentCell.UseDefaultSettings ? 3 : 5);
            _editor.guiBuilder.DrawInBackground(new GUIContent("Sprite options"), spriteOptionsLines, (r) =>
            {
                // sprite reference
                CurrentCell.Sprite = (Sprite)EditorGUILayout.ObjectField("Display sprite", CurrentCell.Sprite, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                //check universal sprite drawer usage
                if (_file.settings._useUniversalSpriteSettings)
                {
                    GUILayout.Space(12);
                    EditorGUILayout.HelpBox("Universal sprite settings is enabled, change the sprite settings in the \"Sprite Override Settings\" window", MessageType.Info);
                    if (!_file._overrideSprites.Exists(t => t.BaseSprite == CurrentCell.Sprite))
                    {
                        if (GUILayout.Button("Add sprite to overrides"))
                        {
                            _file._overrideSprites.Add(new BetterRuleTileContainer.UniversalSpriteData(CurrentCell.Sprite));
                            _file.settings.SetDisplayUniversalSpriteSettings(true);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Open sprite override settings"))
                        {
                            _editor.CurrentlySepectedOverrideSpriteToModify = _file._overrideSprites.FindIndex(t => t.BaseSprite == CurrentCell.Sprite);
                            _editor.AutoScrollToSelectedOverrideSprite = true;
                            _file.settings.SetDisplayUniversalSpriteSettings(true);
                        }
                    }
                }
                //if not using the universal settings display the default options
                else DrawCellSpriteOptions();
            });
            
            EditorGUILayout.Space();

            //if cell is in a preset block
            var presetblock = _file._presetBlocks.Find(b => b.InBounds(CurrentCell.Position));
            if (presetblock != null)
            {
                _editor.guiBuilder.DrawInBackground(new GUIContent("Neighbor positions"), 5, (r) =>
                {
                    GUILayout.Space(3);
                    presetblock.BlockTransform = (RuleTile.TilingRuleOutput.Transform)EditorGUILayout.EnumPopup("Transform", presetblock.BlockTransform);
                    GUILayout.Space(10);

                    EditorGUILayout.HelpBox("This tile is part of a preset block. To change which neighbors to check for, remove the preset block.", MessageType.Info);

                    if (GUILayout.Button("Delete preset block")) _file.DeletePresetBlock(CurrentCell.Position);
                });
            }
            else
            {
                var guiSize = _editor._grid.DrawInteractiveMiniGrid(EditorGUILayout.GetControlRect(), CurrentCell);
                GUILayout.Space(guiSize.height);
            }

            EditorGUILayout.Space();
            // extra space is needed for the isometric type
            if (_file.settings._gridShape == BetterRuleTileContainer.GridShape.Isometric) GUILayout.Space(7);
            
            _editor.guiBuilder.DrawInBackground(new GUIContent("Rule priority"), 4, (r) =>
            {
                CurrentCell.Priority = EditorGUILayout.IntField(new GUIContent("Priority group", "Priority of the rule. Higher number means higher priority. Rules within the same priority group will be sorted automatically based on rule complexity. This automatic sorting within groups can be altered using the \"Rule order modifier\" option."), CurrentCell.Priority);
                EditorGUILayout.Space();
                
                if (CurrentCell.Sprite == null) GUILayout.Label($"This cell will not be affected by the rule priority.\n", EditorStyles.helpBox);
                else if (CurrentCell.info_ExportedRuleOrder >= 0) GUILayout.Label(new GUIContent($"Initial tiling rule index:\t{CurrentCell.info_InitialRuleOrder}\nIndex after modifying order:\t{CurrentCell.info_ExportedRuleOrder}", "Lower \"tiling rule index\" value means higher priority."), EditorStyles.helpBox);
                else GUILayout.Label($"Export the tiles to display the rule order\n", EditorStyles.helpBox);
                
                // Priority modifier
                EditorGUILayout.BeginHorizontal();
                CurrentCell.PriorityModifier = EditorGUILayout.IntField(new GUIContent("Rule order modifier", "When generating the tiles, this number will specify how much will the rule order change inside its priority group. In most cases, this means that the \"tiling rule index\" will be adjusted by this amount."), CurrentCell.PriorityModifier);
                if (GUILayout.Button(new GUIContent("Apply", "Shortcut for the \"Generate tiles\" button in the export options."))) BetterRuleTileGenerator.GenerateTiles(_file);
                EditorGUILayout.EndHorizontal();
            });
            
            //end scroll
            EditorGUILayout.EndScrollView();

            //check if it was modified
            CurrentCell.AreNeighborPositionsModified = CurrentCell.CheckModifiedNeighborPositions();
            CurrentCell.AreOutputSpritesModified = CurrentCell.CheckModifiedOutput();
            
            // move window
            if (!lockPosition) GUI.DragWindow();
        }
        
        
        void DrawCellSpriteOptions()
        {
            CurrentCell.UseDefaultSettings = EditorGUILayout.Toggle("Use default settings", CurrentCell.UseDefaultSettings);
            if (!CurrentCell.UseDefaultSettings)
            {
                CurrentCell.GameObject = (GameObject)EditorGUILayout.ObjectField("Gameobject", CurrentCell.GameObject, typeof(GameObject), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                CurrentCell.ColliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup("Collider type", CurrentCell.ColliderType);
            }

            //output
            CurrentCell.OutputSprite = (ExtendedOutputSprite)EditorGUILayout.EnumPopup("Output", CurrentCell.OutputSprite);
            if (CurrentCell.OutputSprite != ExtendedOutputSprite.Single)
            {
                bool hexGrid = _file.settings._gridShape == BetterRuleTileContainer.GridShape.HexagonalPointedTop || _file.settings._gridShape == BetterRuleTileContainer.GridShape.HexagonalFlatTop;
                bool hexPattern = hexGrid && CurrentCell.OutputSprite == ExtendedOutputSprite.Pattern;

                GUILayout.Space(12);
                
                if (CurrentCell.OutputSprite == ExtendedOutputSprite.Animation)
                {
                    GUILayout.Label("Animation settings", EditorStyles.boldLabel);
                    CurrentCell.MinAnimationSpeed = EditorGUILayout.FloatField("Min speed", CurrentCell.MinAnimationSpeed);
                    CurrentCell.MaxAnimationSpeed = EditorGUILayout.FloatField("Max speed", CurrentCell.MaxAnimationSpeed);
                }
                if (CurrentCell.OutputSprite == ExtendedOutputSprite.Random)
                {
                    GUILayout.Label("Random tile settings", EditorStyles.boldLabel);
                    CurrentCell.NoiseScale = EditorGUILayout.Slider("Noise", CurrentCell.NoiseScale, 0.001f, 0.999f);
                    CurrentCell.RandomTransform = (RuleTile.TilingRuleOutput.Transform)EditorGUILayout.EnumPopup("Shuffle", CurrentCell.RandomTransform);
                }
                if (CurrentCell.OutputSprite == ExtendedOutputSprite.Pattern && !hexGrid)
                {
                    GUILayout.Label("Pattern settings", EditorStyles.boldLabel);

                    //
                    CurrentCell.PatternSize = EditorGUILayout.Vector2IntField("Pattern size", CurrentCell.PatternSize);
                    if (CurrentCell.PatternSize.x < 1) CurrentCell.PatternSize.x = 1;
                    if (CurrentCell.PatternSize.y < 1) CurrentCell.PatternSize.y = 1;

                    int size = CurrentCell.PatternSize.x * CurrentCell.PatternSize.y;
                    if (size != CurrentCell.Sprites.Length) Array.Resize(ref CurrentCell.Sprites, size);
                }
                else if (hexPattern)
                {
                    GUILayout.Label("Pattern settings", EditorStyles.boldLabel);
                    GUILayout.Label("Hexagonal tiles are not compatible with patterns.");

                    _dragDropRect1 = new Rect();
                    _dragDropRect2 = new Rect();
                    return;
                }

                //space
                var tempRect = EditorGUILayout.GetControlRect(false, 9);
                GUILayout.Label("Output sprites", EditorStyles.boldLabel);

                //display a list using a custom-made editor field
                CurrentCell.Sprites = EditorGUIField<Sprite>.ArrayField("Sprites", ref _showSpritesArray, CurrentCell.Sprites, typeof(Sprite));
                
                // calculate drag & drop rect
                if (tempRect.x > 0 && tempRect.y > 0)
                {
                    _dragDropRect1 = new Rect(
                        tempRect.x + position.x,
                        tempRect.y + 30 - _gridCellViewScroll.y + position.y,
                        tempRect.width,
                        40
                    );
                    _dragDropRect2 = new Rect(
                        tempRect.x + position.x,
                        tempRect.y + 70 - _gridCellViewScroll.y + position.y,
                        tempRect.width * 0.6f,
                        _showSpritesArray ? (CurrentCell.Sprites.Length + 1) * 20 : 0
                    );
                }

                //display pattern
                if (CurrentCell.OutputSprite == ExtendedOutputSprite.Pattern) 
                    GUILayout.Space(SpriteOptionWindowFunctions.DisplayPatternGrid(EditorGUILayout.GetControlRect(), CurrentCell.PatternSize, CurrentCell.Sprites, ref _editor.ConfigWindowWidth));

                if (CurrentCell.OutputSprite != ExtendedOutputSprite.Pattern) 
                    CurrentCell.IncludeSpriteInOutput = EditorGUILayout.Toggle(new GUIContent("Include default sprite", "Add default sprite to the start of the array when generating the tile?"), CurrentCell.IncludeSpriteInOutput);
                else CurrentCell.IncludeSpriteInOutput = false;
            }
            else
            {
                _dragDropRect1 = new Rect();
                _dragDropRect2 = new Rect();
            }
        }

        public bool CheckDragDropSprites()
        {
            // Debug.Log($"Mouse {Event.current.mousePosition} - Drag Drop Rect: {dragDropRect}");
            // EditorGUI.DrawRect(_dragDropRect1, Color.red);
            // EditorGUI.DrawRect(_dragDropRect2, Color.red);
            // EditorGUI.DrawRect(new Rect(Event.current.mousePosition - Vector2.one * 5, Vector2.one * 10), Color.green);

            return _dragDropRect1.Contains(Event.current.mousePosition) || _dragDropRect2.Contains(Event.current.mousePosition);
        }
    }
}
#endif