using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles;
using VinTools.Utilities;
using VinToolsEditor.BetterRuleTiles;
using VinToolsEditor.Utilities;

namespace VinToolsEditor.BetterRuleTiles
{
    public class BRTSpriteReplaceWindow : GUIWindow
    {

        public BRTSpriteReplaceWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base(
            new Rect(0, 0, 250, 50), new GUIContent("Replace"), false, true, false)
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }
        

        private BetterRuleTileEditor _editor;
        private BetterRuleTileContainer _file;

        private int _recolorFrom;
        private int _recolorTo;
        private string _replaceFrom;
        private string _replaceTo;

        private bool _replaceSubSprites = false;
        private int _selectedTab = 0;

        private Rect _lastSearchedRect;
        private int _lastSearchedOverrideIndex;
        private string _commonSubstring;
        
        private int _selectedOverrideIndex => _editor.CurrentlySepectedOverrideSpriteToModify;
        private BetterRuleTileContainer.UniversalSpriteData _selectedSpriteData =>
            _file._overrideSprites[_selectedOverrideIndex];
        
        #region UI

        private void UpdateUI(int windowID)
        {
            // add space for the toolbar
            GUILayout.Space(25);
            
            // variables for the toolbar
            var toolbarRect = new Rect(1, 24, width - 2, 20);
            var overrideEnabledToolbarOptions = new GUIContent[] { new GUIContent("Tiles"), new GUIContent("Sprites"), new GUIContent("Overrides") };
            var overrideDisabledToolbarOptions = new GUIContent[] { new GUIContent("Tiles"), new GUIContent("Sprites")};
            
            // draw toolbar to select tab
            if (_file.settings._useUniversalSpriteSettings) _selectedTab = GUI.Toolbar(toolbarRect, _selectedTab, overrideEnabledToolbarOptions);
            else _selectedTab = GUI.Toolbar(toolbarRect, _selectedTab, overrideDisabledToolbarOptions);
            
            // display based on which tab is selected
            switch (_selectedTab)
            {
                case 0: UpdateTileTabUI(windowID); break;
                case 1: UpdateSpriteTabUI(windowID); break;
                case 2: UpdateOverrideTabUI(windowID); break;
                default: UpdateTileTabUI(windowID); break;
            }
        }

        private void UpdateTileTabUI(int windowID)
        {
            // Tooltip used for every button in this tab
            const string tileTooltip = 
                "Replaces all tiles in the selection, which are the same as the tile you specify in \"Replace from\", to the tile you specify in \"Replace to\"";
            
            // show from and to popups
            EditorGUILayout.Space();
            _recolorFrom = EditorGUILayout.Popup(new GUIContent("Replace from", tileTooltip), _recolorFrom,
                _file.GetTileNames());
            _recolorTo = EditorGUILayout.Popup(new GUIContent("Replace to", tileTooltip), _recolorTo,
                _file.GetTileNames());

            // show prompt if not selected, show button if it has selection
            EditorGUILayout.Space();
            if (_editor._hasSelection)
            {
                if (GUILayout.Button("Replace tiles")) ReplaceTiles(_file, _editor._selectionRect, _recolorFrom, _recolorTo);
            }
            else GUILayout.Label("Select an area to replace tiles in!", EditorStyles.helpBox);

        }

        private void UpdateSpriteTabUI(int windowID)
        {
            // Tooltip used for every button in this tab
            const string spriteTooltip = 
                "Find all sprites in the selection, whose filename includes \"Replace from\", and replaces them with sprites where the \"Replace from\" text is replaced to \"Replace to\"";

            // find common substring
            if (_lastSearchedRect != _editor._selectionRect)
            {
                _commonSubstring = FindCommonSpriteNameInSelection(_file, _editor._selectionRect, _replaceSubSprites);
                _lastSearchedRect = _editor._selectionRect;
            }

            // display common substring
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Common substring: \"{_commonSubstring}\"", EditorStyles.helpBox);
            if (GUILayout.Button("Use"))
            {
                _replaceFrom = _commonSubstring;
                _replaceTo = _commonSubstring;
            }
            GUILayout.EndHorizontal();

            // text fields
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 90;
            _replaceFrom = EditorGUILayout.TextField(new GUIContent("Replace from", spriteTooltip), _replaceFrom);
            _replaceTo = EditorGUILayout.TextField(new GUIContent("Replace to", spriteTooltip), _replaceTo);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();
            
            // toggle to replace sub-sprites
            if (!_file.settings._useUniversalSpriteSettings)
            { 
                _replaceSubSprites = EditorGUILayout.Toggle( new GUIContent("Replace sub sprites", "Also replace sprites that are used for animations, patterns, etc."), _replaceSubSprites);
                EditorGUILayout.Space();
            }
            else _replaceSubSprites = false;
            
            // show prompt if not selected, show button if it has selection
            if (_editor._hasSelection)
            {
                if (GUILayout.Button("Replace sprites"))
                    ReplaceSprites(_file, _editor._selectionRect, _replaceFrom, _replaceTo, _replaceSubSprites);
            }
            else GUILayout.Label("Select an area to replace sprites in!", EditorStyles.helpBox);

        }
        void UpdateOverrideTabUI(int windowID)
        {
            // Tooltip used for every button in this tab
            const string spriteTooltip = 
                "Find all sprites in the selected sprite data, whose filename includes \"Replace from\", and replaces them with sprites where the \"Replace from\" text is replaced to \"Replace to\"";

            // find common substring
            if (_lastSearchedOverrideIndex != _selectedOverrideIndex && _selectedOverrideIndex >= 0)
            {
                _commonSubstring = FindCommonSpriteNameInOverride(_selectedSpriteData);
                _lastSearchedOverrideIndex = _selectedOverrideIndex;
            }

            // display common substring
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Common substring: \"{_commonSubstring}\"", EditorStyles.helpBox);
            if (GUILayout.Button("Use"))
            {
                _replaceFrom = _commonSubstring;
                _replaceTo = _commonSubstring;
            }
            GUILayout.EndHorizontal();
            
            // text fields
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 90;
            _replaceFrom = EditorGUILayout.TextField(new GUIContent("Replace from", spriteTooltip), _replaceFrom);
            _replaceTo = EditorGUILayout.TextField(new GUIContent("Replace to", spriteTooltip), _replaceTo);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();

            // show prompt if not selected, show button if it has selection
            if (_selectedOverrideIndex >= 0)
            {
                if (GUILayout.Button("Modify selected override"))
                    ModifySpriteData(_selectedSpriteData, _file, _replaceFrom, _replaceTo);
                if (GUILayout.Button("Create override with new sprites"))
                    CreateNewOverride(_selectedSpriteData, _file, _replaceFrom, _replaceTo);
            }
            else GUILayout.Label("Select a sprite to modify in the \"Sprite Override Settings\" window!", EditorStyles.helpBox);
        }

        #endregion


        private static string FindCommonSpriteNameInSelection(BetterRuleTileContainer file, Rect selection, bool includeSubSprites)
        {
            // create a list for sprite names
            var listOfNames = new List<String>();

            // add all sprite names to list
            foreach (var item in file._grid.Where(t => selection.Contains(t.Position) && t.Sprite))
            {
                // add main sprite name
                var spriteName = AssetDatabase.GetAssetPath(item.Sprite);
                if (!listOfNames.Contains(spriteName)) listOfNames.Add(spriteName);
                if (!listOfNames.Contains(item.Sprite.name)) listOfNames.Add(item.Sprite.name);
                
                // add sub sprites
                if (includeSubSprites)
                {
                    foreach (var sprite in item.Sprites)
                    {
                        var name = AssetDatabase.GetAssetPath(sprite);
                        if (!listOfNames.Contains(name)) listOfNames.Add(name);
                    }
                }
            }

            // return substring
            return TextUtils.FindLongestCommonSubstring(listOfNames);
        }
        private static string FindCommonSpriteNameInOverride(BetterRuleTileContainer.UniversalSpriteData spriteData)
        {
            // create a list for sprite names
            var listOfNames = new List<String>();

            // add main sprite name
            var spriteName = AssetDatabase.GetAssetPath(spriteData.BaseSprite);
            if (!listOfNames.Contains(spriteName)) listOfNames.Add(spriteName);
            if (!listOfNames.Contains(spriteData.BaseSprite.name)) listOfNames.Add(spriteData.BaseSprite.name);
            
            // add sub sprites
            foreach (var sprite in spriteData.Sprites)
            {
                var name = AssetDatabase.GetAssetPath(sprite);
                if (!listOfNames.Contains(name)) listOfNames.Add(name);
            }
            
            

            // return substring
            return TextUtils.FindLongestCommonSubstring(listOfNames);
        }
        
        private static void ReplaceTiles(BetterRuleTileContainer file, Rect selection, int from, int to)
        {
            // record object for undo
            file.RecordObject(
                $"Replaced tiles \"{file._tiles[from].Name}\" to \"{file._tiles[to].Name}\" in selection ({file.name})");

            // replace
            foreach (var item in file._grid.Where(t =>
                         selection.Contains(t.Position) && t.TileID == file._tiles[from].UniqueID))
                item.TileID = file._tiles[to].UniqueID;

            // save
            file.SaveAsset();
        }

        private static void ReplaceSprites(BetterRuleTileContainer file, Rect selection, string from, string to, bool replaceSubSprites = false)
        {
            // record object for undo
            file.RecordObject($"Replaced sprites \"{from}\" to \"{to}\" in selection ({file.name})");

            // replace
            foreach (var item in file._grid.Where(t => selection.Contains(t.Position) && t.Sprite))
            {
                // replace main sprite
                item.Sprite = GetNewSprite(item.Sprite, from, to);

                // replace other sprites
                if (replaceSubSprites)
                {
                    for (int i = 0; i < item.Sprites.Length; i++)
                    {
                        item.Sprites[i] = GetNewSprite(item.Sprites[i], from, to);
                    }
                }
            }

            // save
            file.SaveAsset();
        }

        private static void ModifySpriteData(BetterRuleTileContainer.UniversalSpriteData spriteData, BetterRuleTileContainer file, string from, string to)
        {
            // record object for undo
            file.RecordObject($"Replaced sprites \"{from}\" to \"{to}\" in sprite data ({spriteData.BaseSprite.name})");

            // check if sprite does not have an override
            var newSprite = GetNewSprite(spriteData.BaseSprite, from, to);
            if (file._overrideSprites.Exists(d => d.BaseSprite == newSprite))
            {
                EditorUtility.DisplayDialog("Replace failed!",
                    $"The sprite \"{newSprite.name}\" already has an override in the universal overrides window!", "ok");
                return;
            }
            
            // replace sprites
            ReplaceSpriteInOverride(spriteData, from, to);
        }

        private static void CreateNewOverride(BetterRuleTileContainer.UniversalSpriteData spriteData, BetterRuleTileContainer file, string from, string to)
        {
            // get new sprite
            var newSprite = GetNewSprite(spriteData.BaseSprite, from, to);
            
            // record object for undo
            file.RecordObject($"Created a new override for the sprite \"{newSprite.name}\"");
            
            // check if sprite does not have an override
            if (file._overrideSprites.Exists(d => d.BaseSprite == newSprite))
            {
                EditorUtility.DisplayDialog("Replace failed!",
                    $"The sprite \"{newSprite.name}\" already has an override in the universal overrides window!", "ok");
                return;
            }
            
            // create a copy of the override
            var newData = new BetterRuleTileContainer.UniversalSpriteData(spriteData);
            ReplaceSpriteInOverride(newData, from, to);
            
            // add it to the list of overrides
            file._overrideSprites.Add(newData);
        }

        private static void ReplaceSpriteInOverride(BetterRuleTileContainer.UniversalSpriteData spriteData, string from, string to)
        {
            // replace main sprite
            spriteData.BaseSprite = GetNewSprite(spriteData.BaseSprite, from, to);

            // replace other sprites
            for (int i = 0; i < spriteData.Sprites.Length; i++)
            {
                spriteData.Sprites[i] = GetNewSprite(spriteData.Sprites[i], from, to);
            }
        }
        
        private static Sprite GetNewSprite(Sprite oldSprite, string from, string to)
        {
            // get path of original sprite
            string path = AssetDatabase.GetAssetPath(oldSprite);
            var splitPath = path.Split('/');
            splitPath[splitPath.Length - 1] =
                TextUtils.ReplaceFirstOccurrence(splitPath[splitPath.Length - 1], from, to);

            // create altered path
            string newPath = "";
            for (int i = 0; i < splitPath.Length; i++)
            {
                newPath += splitPath[i];
                if (i + 1 < splitPath.Length) newPath += "/";
            }

            // replace sprite name 
            string spriteName = TextUtils.ReplaceFirstOccurrence(oldSprite.name, from, to);

            // find new sprite
            if (AssetDatabase.LoadMainAssetAtPath(newPath) != null)
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(newPath))
                {
                    if (asset.name.Equals(spriteName) && asset is Sprite sprite) return sprite;
                }
            }

            // default
            return oldSprite;
        }
    }
}