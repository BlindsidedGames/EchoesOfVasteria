#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using VinTools.BetterRuleTiles.Editor.Generation;
using VinTools.BetterRuleTiles.Editor.Utilities;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class ExportDropdownWindow : GUIWindow
    {
        
        public ExportDropdownWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base(
            new Rect(0, 0, 250, 50), new GUIContent(""), false, true, true)
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }

        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;

        private void UpdateUI(int windowID)
        {
            // title bar
            GUI.Label(new Rect(0, 0, width, 25), "Export options", Style.CenteredBoldLabel);
            EditorGUILayout.Space();

            // script and grid selection
            TileTypeSelection(windowID);
            GridTypeSelection(windowID);
            EditorGUILayout.Space(); 
            
            //_file.settings._generatePalette = EditorGUILayout.Toggle(new GUIContent("Generate palette"), _file.settings._generatePalette);
            //_file.settings._addMissingRules = EditorGUILayout.Toggle(new GUIContent("Add missing rules", "In tiles which are variations of another tile, should the missing rules be added from the parent tile?"), _file.settings._addMissingRules);
            _file.settings._collapseSimilarRules = EditorGUILayout.Toggle(new GUIContent("Simplify similar rules", "Checks tiles which have the same sprite, finds a common pattern between them and replaces them with one rule that applies for all"), _file.settings._collapseSimilarRules);
            if (!_file.UseTileSet) _file.settings._useUniversalSpriteSettings = EditorGUILayout.Toggle(new GUIContent("Universal sprite settings", "With this setting enabled, all rules which have the same output sprite will use the same output settings, so you don't have to individually set them. A new window will be enabled with this option"), _file.settings._useUniversalSpriteSettings);
            _file.settings._treatSimilarTilesAsSame = EditorGUILayout.Toggle(new GUIContent("Treat similar tiles as same", "With this option enabled, when setting up a tiling rule to a tile that is set up to automatically connect to other tiles, the automatically connected tiles will be treated as if it was that same tile in the tiling rules"), _file.settings._treatSimilarTilesAsSame);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate tiles"))
            {
                BetterRuleTileGenerator.GenerateTiles(_file);
                visible = false;
            }
            EditorGUILayout.Space(); 
        }

        /// <summary>
        /// Determines if the given types implement the same base tile type. If they don't implement either, it returns false.
        /// </summary>
        /// <param name="type1"></param>
        /// <param name="type2"></param>
        /// <returns></returns>
        private bool DerivesFromSameBaseTile(Type type1, Type type2)
        {
            if (typeof(BetterRuleTileBase).IsAssignableFrom(type1)) return typeof(BetterRuleTileBase).IsAssignableFrom(type2);
            if (typeof(BetterHexagonalRuleTileBase).IsAssignableFrom(type1)) return typeof(BetterHexagonalRuleTileBase).IsAssignableFrom(type2);
            return false;
        }

        // old type dropdown based on enum
        /*private void TileTypeSelection(int windowID)
        {
            // Get the new type
            var scriptType = (BetterRuleTileGenerator.CustomTileTypes)EditorGUILayout.EnumPopup("Rule tile script", (BetterRuleTileGenerator.CustomTileTypes)_file.settings._selectedExportScript);
            int intValue = (int)scriptType;
            
            // if the type didn't change, return
            if (intValue == _file.settings._selectedExportScript) return;
            
            // if the type of the already exported tile doesn't match, don't change it
            if (_file._tileObjects.Count > 0 && !DerivesFromSameBaseTile(
                    _file._tileObjects[0].GetType(),
                    BetterRuleTileGenerator.GetCustomTileType(intValue, _file)))
            {
                EditorUtility.DisplayDialog("Not compatible!", "Selected script is not compatible with the already exported tiles! In order to change the type to this script, you need to delete the already existing tiles.", "Cancel");
                return;
            }
            
            // if the type is not compatible with the grid type, give a warning
            if (!DerivesFromSameBaseTile(
                    BetterRuleTileGenerator.GetDefaultTileType(_file.settings._gridShape),
                    BetterRuleTileGenerator.GetCustomTileType(intValue, _file)))
            {
                EditorUtility.DisplayDialog("Note", "Selected script is not compatible with the current grid type!\nMake sure to change grid type before exporting!", "OK");
            }
            
            // set the new value
            _file.settings._selectedExportScript = intValue;
            // update the drawer in the window
            _editor.UpdateDrawerData();
        }*/

        private void TileTypeSelection(int windowID)
        {
            List<string> popupOptions = new List<string>();
            foreach (var key in BetterRuleTileGenerator.TileTypes.Keys)
            {
                var type = TypeUtils.GetTypeFromGuid(key);
                popupOptions.Add(type != null ? type.FullName : "Default");
            }
            int selectedIndex = BetterRuleTileGenerator.TileTypes.Keys.ToList().IndexOf(_file.settings._selectedExportScriptGUID);
            
            // Get the new type
            int scriptTypeIndex = EditorGUILayout.Popup("Rule tile script", selectedIndex, popupOptions.ToArray());
            if (scriptTypeIndex < 0) scriptTypeIndex = popupOptions.Count - 1;
            string selectedScriptGuid = BetterRuleTileGenerator.TileTypes.Keys.ToList()[scriptTypeIndex];
            
            // if the type didn't change, return
            if (selectedScriptGuid == _file.settings._selectedExportScriptGUID) return;
            
            // if the type of the already exported tile doesn't match, don't change it
            if (_file._tileObjects.Count > 0 && _file._tileObjects[0] && !DerivesFromSameBaseTile(
                    _file._tileObjects[0].GetType(),
                    BetterRuleTileGenerator.GetCustomTileType(selectedScriptGuid, _file)))
            {
                EditorUtility.DisplayDialog("Not compatible!", "Selected script is not compatible with the already exported tiles! In order to change the type to this script, you need to delete the already existing tiles.", "Cancel");
                return;
            }
            
            // if the type is not compatible with the grid type, give a warning
            if (!DerivesFromSameBaseTile(
                    BetterRuleTileGenerator.GetDefaultTileType(_file.settings._gridShape),
                    BetterRuleTileGenerator.GetCustomTileType(selectedScriptGuid, _file)))
            {
                EditorUtility.DisplayDialog("Note", "Selected script is not compatible with the current grid type!\nMake sure to change grid type before exporting!", "OK");
            }
            
            // set the new value
            _file.settings._selectedExportScriptGUID = selectedScriptGuid;
            // update the drawer in the window
            _editor.UpdateDrawerData();
        }

        private void GridTypeSelection(int windowID)
        {
            // don't display while using a tileset
            if (_file.UseTileSet) return;

            // get the new value
            var newShape = (BetterRuleTileContainer.GridShape)EditorGUILayout.EnumPopup("Grid type", _file.settings._gridShape);
            if (newShape == _file.settings._gridShape) return;
            
            // check if grid is compatible with already exported tiles
            if (_file._tileObjects.Count > 0 && !DerivesFromSameBaseTile(
                    _file._tileObjects[0].GetType(),
                    BetterRuleTileGenerator.GetDefaultTileType(newShape)))
            {
                EditorUtility.DisplayDialog("Not compatible!", "Selected grid type is not compatible with the already exported tiles! In order to change the grid type, you need to delete the already existing tiles.", "Cancel");
                return;
            }
            
            // apply change
            bool changedShape = _file.ChangeGridShape(newShape);
            if (changedShape) _editor.SetupGrid(true);
        }
    }
}
#endif