#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VinTools.BetterRuleTiles.Editor.EditorWindows;

namespace VinTools.BetterRuleTiles.Editor.Data
{
    public static class TileEditorClipboard
    {
        public static BetterRuleTileContainer.GridShape GridShape = BetterRuleTileContainer.GridShape.Isometric;
        public static List<BetterRuleTileContainer.GridCell> Contents = new List<BetterRuleTileContainer.GridCell>();
        public static List<int> IncludedTileIDs = new List<int>();
        public static Rect Rect;
        public static Vector2Int Origin;

        /// <summary>
        /// Which editor are we pasting in. If this is null than there is no paste in progress.
        /// </summary>
        public static BetterRuleTileEditor CurrentlyPastingInEditor
        {
            get => _currentlyPastingInEditor;
            set
            {
                _currentlyPastingInEditor = value;
                PasteInProgress = value != null;
            }
        }
        private static BetterRuleTileEditor _currentlyPastingInEditor = null;
        /// <summary>
        /// Is paste in progress
        /// </summary>
        public static bool PasteInProgress { get; private set; } = false;
        
        public static void CopySelection(BetterRuleTileContainer file, Rect selectionRect, out bool hasSelection)
        {
            Contents.Clear();
            IncludedTileIDs.Clear();
            
            foreach (var item in file.Grid.Where(t => selectionRect.Contains(t.Position)).ToArray())
            {
                var cell = BetterRuleTileContainer.GridCell.Clone(item);
                cell.Locked = false;
                
                Contents.Add(cell);
                if (!IncludedTileIDs.Contains(item.TileID)) IncludedTileIDs.Add(item.TileID);
            } 
            
            Rect = selectionRect;
            Origin = new Vector2Int((int)Rect.position.x, (int)Rect.position.y);
            hasSelection = false;
            
            GridShape = file.settings._gridShape;
        }
        
        /// <summary>
        /// Move the clipboard contents by the specified amount on the grid
        /// </summary>
        /// <param name="moveBy"></param>
        public static void MoveClipboard(Vector2Int moveBy)
        {
            for (int i = 0; i < Contents.Count; i++) Contents[i].Position += moveBy;
            Rect.position += moveBy;
        }
        
        public static void StartPaste(BetterRuleTileEditor editor)
        {
            CurrentlyPastingInEditor = editor; 
            
            // Check if the grid type is the same
            if (!CheckCorrectGridShape)
            {
                CancelPaste();
                return;
            }
            
            // Get the mouse position
            Vector2Int mouseOffset = editor._grid.GetMouseTilePos() - Vector2Int.RoundToInt(Rect.position);
            for (int i = 0; i < Contents.Count; i++) Contents[i].Position += mouseOffset;
            Rect.position += mouseOffset;
            
            editor._hasSelection = false; 
            editor._selectedTool = BetterRuleTileEditor.ToolbarTool.Move;
        }

        public static void CancelPaste() => CurrentlyPastingInEditor = null;
        
        public static void PasteClipboard(BetterRuleTileContainer file)
        {
            // if no paste selection
            if (!PasteInProgress) return;
            // if file does not correspond with the editor where the paste started
            if (CurrentlyPastingInEditor._file != file) return;
            
            // Disable selection
            CurrentlyPastingInEditor = null;
            
            // Add any missing tiles
            foreach (var tileID in IncludedTileIDs)
            {
                if (tileID > 0 && !file._tiles.Exists(t => t.UniqueID == tileID)) file.CreateTile(tileID);
            }
            
            Vector2Int currentPos = new Vector2Int((int)Rect.position.x, (int)Rect.position.y);
            file.PasteToFile(Contents, currentPos - Origin);
        }

        /// <summary>
        /// Copy the clipboard contents into the file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="clipboard"></param>
        /// <param name="moveBy"></param>
        private static void PasteToFile(this BetterRuleTileContainer file, List<BetterRuleTileContainer.GridCell> clipboard, Vector2Int moveBy)
        {
            file.RecordObject($"Pasted selection ({file.name})");

            //convert to an array we can modify and is not a reference of the original copied object
            BetterRuleTileContainer.GridCell[] tiles = new BetterRuleTileContainer.GridCell[clipboard.Count];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = BetterRuleTileContainer.GridCell.Clone(clipboard[i]);

            //fix clipboard on hexagonal grids
            if (file.settings._gridShape == BetterRuleTileContainer.GridShape.HexagonalPointedTop)
            {
                for (int i = 0; i < tiles.Length; i++) BetterRuleTileContainer.FixMoveOnPointedHexGrid(ref tiles[i], moveBy);
            }
            if (file.settings._gridShape == BetterRuleTileContainer.GridShape.HexagonalFlatTop)
            {
                for (int i = 0; i < tiles.Length; i++) BetterRuleTileContainer.FixMoveOnFlattopHexGrid(ref tiles[i], moveBy);
            }

            //paste the tiles
            for (int i = 0; i < tiles.Length; i++)
            {
                var cell = file._grid.Find(t => t.Position == tiles[i].Position);

                if (cell != null && cell.Locked) break;
                if (cell != null) file._grid[file._grid.IndexOf(cell)] = tiles[i];
                else file._grid.Add(tiles[i]);
            }

            //save
            file.SaveAsset();
        }
        
        /// <summary>
        /// Returns whether the clipboard matches the current editor shape. Shows a warning when false
        /// </summary>
        private static bool CheckCorrectGridShape
        {
            get
            {
                if (CurrentlyPastingInEditor._file.settings._gridShape != GridShape)
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Grid Shape!",
                        $"The contents on the clipboard do not match the shape of the grid you're trying to paste to! \n\n" +
                        $"Clipboard:\t {GridShape}\n" +
                        $"Editor:\t\t {CurrentlyPastingInEditor._file.settings._gridShape}",
                        "OK");
                    return false;
                }
                
                return true;
            }
        }
    }
}
#endif