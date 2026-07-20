#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using VinTools.BetterRuleTiles.Internal;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class TileInfoWindow : GUIWindow
    {
        public TileInfoWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base( 
            GUIWindow.WindowLayout.BottomRightCorner, new RectOffset(4, 4, 30, 4), new Vector2(300, 400), new GUIContent(""), false)
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }

        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;

        public BetterRuleTileContainer.TileData TempTileData = new BetterRuleTileContainer.TileData(); 
        
        Vector2 _scrollPos;
        private int _tile_selectedVariationTile;
        private bool _inspector_collapsed = false;
        
        
        void UpdateSelectedTile()
        {
            int index = _selectedDrawerTile - 1;
            
            _file.RecordObject($"Modified tile ({_file.Tiles[index].Name})");

            BetterRuleTileContainer.TileData.CloneTileData(TempTileData, _file.Tiles[index]);
            _file._tempTex = null;
            _file.SaveAsset();
        }
        
        
        private int p_selectedDrawerTile = int.MinValue;

        public int _selectedDrawerTile
        {
            get => p_selectedDrawerTile;
            set
            {
                if (p_selectedDrawerTile == value) return;
                if (p_selectedDrawerTile > 0) UpdateSelectedTile();

                if (value > 0) BetterRuleTileContainer.TileData.CloneTileData(_file.Tiles[value - 1], TempTileData);
                
                p_selectedDrawerTile = value;
            }
        }
        
        private void UpdateUI(int windowID)
        {
            //title
            GUI.Label(new Rect(0, 0, width, 25), $"Tile info: \"{_file.GetTileName(_selectedDrawerTile - 1)}\"", Style.CenteredBoldLabel);

            //collapse
            if (GUI.Button(new Rect(width - 20, 3, 16, 16), _inspector_collapsed ? Tex.Get(Tex.T_ID.t_tileInfo_upArrow) : Tex.Get(Tex.T_ID.t_tileInfo_downArrow), Style.IconButton))
            {
                _inspector_collapsed = !_inspector_collapsed;
                height = _inspector_collapsed ? 25 : 400;
                width = 300;
            }
            if (_inspector_collapsed) return;

            //set up scrollbar
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            //draw editor options
            _editor.guiBuilder.DrawInBackground(new GUIContent("Editor options", "Change how the tile appears in the editor window"), 2, (r) =>
            {
                TempTileData.Name = EditorGUILayout.TextField("Tile name", TempTileData.Name);
                TempTileData.Texture = EditorGUILayout.ObjectField("Tile image", TempTileData.Texture, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Texture2D;
            });
            EditorGUILayout.Space();

            //if (_tileInfo_Texture != null && !_tileInfo_Texture.isReadable) EditorGUILayout.HelpBox("This texture is not readable, therefore some functionality may not be available for this image", MessageType.Warning);
            if (TempTileData.Texture == null)
            {
                _editor.guiBuilder.DrawInBackground(new GUIContent("Auto texture", "If you didn't specify a tile image in the \"Editor options\" section, you can change the values of the automatically generated image here"), 4, (r) =>
                {
                    TempTileData.Color = EditorGUILayout.ColorField("Tile color", TempTileData.Color);
                    TempTileData.TextureShape = (BetterRuleTileContainer.TileShape)EditorGUILayout.EnumPopup("Tile shape", TempTileData.TextureShape);
                    TempTileData.MirrorTexX = EditorGUILayout.Toggle("Mirror X", TempTileData.MirrorTexX);
                    TempTileData.MirrorTexY = EditorGUILayout.Toggle("Mirror Y", TempTileData.MirrorTexY);
                }); 
                EditorGUILayout.Space();
            }

            //rule tile options
            _editor.guiBuilder.DrawInBackground(new GUIContent("Rule tile options", "Specify the default options of the rule tile"), 3, (r) =>
            {
                TempTileData.DefaultSprite = EditorGUILayout.ObjectField("Default sprite", TempTileData.DefaultSprite, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Sprite;
                TempTileData.ColliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup("Default collider", TempTileData.ColliderType);
                TempTileData.DefaultGameObject = EditorGUILayout.ObjectField("Default gameobject", TempTileData.DefaultGameObject, typeof(GameObject), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as GameObject;
            });
            EditorGUILayout.Space();

            //variations
            _editor.guiBuilder.DrawInBackground(new GUIContent("Tile variation", "Change whether this tile is unique or a variation of another one. If a tile is a variation of another tile, it will autofill all missing rules from the tile specified"), TempTileData.uniqueTile ? 1 : 2, (r) =>
            {
                TempTileData.uniqueTile = EditorGUILayout.Toggle(new GUIContent("Unique tile", "Is this tile unique, or a variation of another tile?"), TempTileData.uniqueTile);
                if (!TempTileData.uniqueTile)
                {
                    int varIndex = _file.Tiles.FindIndex(t => t.UniqueID == TempTileData.variationOf);
                    varIndex = EditorGUILayout.Popup(new GUIContent("Variation of",
                        //"If a tile is a variation of another, that other tile will treat this if it was the same. But this tile can have separate rules for interacting with itself, and separate rules when interacting with it's parent. This is a good option if you want to make sloped variations of existing tiles."
                        "If a tile is a variation of another tile, it will autofill all missing rules from the tile specified here"
                        ), varIndex, _file.GetTileNames());
                    if (varIndex >= 0) TempTileData.variationOf = _file.Tiles[varIndex].UniqueID;
                }
            });
            EditorGUILayout.Space();

            //connection selection
            _editor.guiBuilder.DrawInBackground(new GUIContent("Connect to", "The tile will connect these tiles as it was the same tile"), TempTileData.variations.Count, (r) =>
            {
                int varIndex2 = _file.Tiles.FindIndex(t => t.UniqueID == _tile_selectedVariationTile);
                varIndex2 = EditorGUI.Popup(new Rect(r.x + r.width - 135, r.y + 1, 80, 18), varIndex2, _file.GetTileNamesSorted());
                bool addButton = GUI.Button(new Rect(r.x + r.width - 51, r.y + 1, 50, 18), "Add");

                if (varIndex2 >= 0) _tile_selectedVariationTile = _file.Tiles[varIndex2].UniqueID;
                if (addButton && !TempTileData.variations.Contains(varIndex2 + 1)) TempTileData.variations.Add(varIndex2 + 1);

                foreach (var item in TempTileData.variations)
                {
                    EditorGUILayout.BeginHorizontal();
                    var tile = _file.Tiles.Find(t => t.UniqueID == item);
                    GUILayout.Label(tile != null ? tile.Name : "Deleted");
                    if (GUILayout.Button("✕", GUILayout.Width(20)))
                    {
                        TempTileData.variations.Remove(item);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }, 21f, 3, -6);
            EditorGUILayout.Space();

            //custom properties
            _editor.guiBuilder.DrawInBackground(new GUIContent("Custom properties", "Custom properties can be accessed by scripts to load custom data from the tiles"), TempTileData.customProperties.Count, (r) =>
            {
                bool addButton = GUI.Button(new Rect(r.x + r.width - 51, r.y + 1, 50, 18), "Add");
                if (addButton) TempTileData.customProperties.Add(new CustomTileProperty());

                for (int i = 0; i < TempTileData.customProperties.Count; i++)
                {
                    var item = TempTileData.customProperties[i];

                    EditorGUILayout.BeginHorizontal();
                    item._key = EditorGUILayout.TextField(new GUIContent("", "Enter the key which you'll use to access this variable in script"), item._key, GUILayout.Width(80));
                    item._type = (CustomTileProperty.Type)EditorGUILayout.EnumPopup(item._type, GUILayout.Width(80));

                    switch (item._type)
                    {
                        case CustomTileProperty.Type.Int:
                            item._val_int = EditorGUILayout.IntField(item._val_int);
                            break;
                        case CustomTileProperty.Type.Float:
                            item._val_float = EditorGUILayout.FloatField(item._val_float);
                            break;
                        case CustomTileProperty.Type.Double:
                            item._val_double = EditorGUILayout.DoubleField(item._val_double);
                            break;
                        case CustomTileProperty.Type.Char:
                            item._val_char = EditorGUILayout.TextField($"{item._val_char}")[0];
                            break;
                        case CustomTileProperty.Type.String:
                            item._val_string = EditorGUILayout.TextField(item._val_string);
                            break;
                        case CustomTileProperty.Type.Bool:
                            item._val_bool = EditorGUILayout.Toggle(item._val_bool);
                            break;
                        default:
                            break;
                    }

                    if (GUILayout.Button("✕", GUILayout.Width(20))) TempTileData.customProperties.Remove(item);
                    EditorGUILayout.EndHorizontal();
                }
            }, lineHeight: 21f, adjustHeight: -4);
            EditorGUILayout.Space();


            EditorGUILayout.EndScrollView();
            GUILayout.Space(30);

            //buttons
            if (GUI.Button(new Rect(5, height - 25, width - 35, 19), "Apply Changes")) UpdateSelectedTile();
            if (GUI.Button(new Rect(width - 25, height - 25, 20, 19), new GUIContent("✕", "Delete Tile")) &&
                EditorUtility.DisplayDialog("Deleting tile.", "Are you sure you want to delete this tile?", "Yes", "Cancel"))
            {
                int index = _selectedDrawerTile - 1;
                _selectedDrawerTile = int.MinValue;
                _file.DeleteTile(_file.Tiles[index]);
            }

            //lock position
            lockPosition = _file.settings._lockWindows;
        }
    }
}
#endif