#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.Data;
using VinTools.BetterRuleTiles.Editor.Grid;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Cache = VinTools.BetterRuleTiles.Editor.Data.SpriteCache;
using Clipboard = VinTools.BetterRuleTiles.Editor.Data.TileEditorClipboard;

namespace VinTools.BetterRuleTiles.Editor.EditorWindows
{
    public partial class BetterRuleTileEditor
    {
        private void HandleTools()
        {
            //keyboard shortcuts
            switch (Event.current.keyCode)
            {
                case KeyCode.B: _selectedTool = ToolbarTool.Brush; break;
                case KeyCode.I: _selectedTool = ToolbarTool.Picker; break;
                case KeyCode.D: _selectedTool = ToolbarTool.Eraser; break;
                case KeyCode.M: _selectedTool = ToolbarTool.Move; break;
                case KeyCode.S: _selectedTool = ToolbarTool.Select; break;
                case KeyCode.E: _selectedTool = ToolbarTool.Inspect; break;
                case KeyCode.C:
                    if (HoldingCtrl && Event.current.type == EventType.KeyDown)
                        if (_hasSelection)
                            TileEditorClipboard.CopySelection(_file, _selectionRect, out _hasSelection);
                    break;
                case KeyCode.V:
                    if (HoldingCtrl && Event.current.type == EventType.KeyDown) TileEditorClipboard.StartPaste(this);
                    break;
                case KeyCode.Delete: DeleteSelection(); break;
                case KeyCode.Escape:
                    _hasSelection = false;
                    _inspectingCell = null;
                    TileEditorClipboard.PasteClipboard(_file);
                    break;
                case KeyCode.L: LockSelection(); break;
                case KeyCode.U: UnlockSelection(); break;
                case KeyCode.P: CreatePresetBlock(); break;
            }

            // remove selection if tools are brush or picker
            if (_selectedTool == ToolbarTool.Brush || _selectedTool == ToolbarTool.Picker) _hasSelection = false;
            // stop inspecting cells with selected tools: brush, picker, eraser
            if (_selectedTool == ToolbarTool.Brush || _selectedTool == ToolbarTool.Picker ||
                _selectedTool == ToolbarTool.Eraser) _inspectingCell = null;
            // Confirm paste when selecting any other tool than move
            if (TileEditorClipboard.PasteInProgress && _selectedTool != ToolbarTool.Move)
                TileEditorClipboard.PasteClipboard(_file);

            // turn off tools if mouse is in a window
            if (DidMouseClickWindow()) return;

            //tools
            switch (_selectedTool)
            {
                case ToolbarTool.Brush: HandleTool_Brush(); break;
                case ToolbarTool.Picker: HandleTool_Picker(); break;
                case ToolbarTool.Eraser: HandleTool_Eraser(); break;
                case ToolbarTool.Move: HandleTool_Move(); break;
                case ToolbarTool.Select: HandleTool_Select(); break;
                case ToolbarTool.Inspect: HandleTool_Inspect(); break;
            }
        }

        private void HandleTool_Brush()
        {
            // activate color picker mode when holding CTRL
            if (HoldingCtrl)
            {
                // Activate color picker
                HandleTool_Picker();
                return;
            }

            //only continue when dragging the left mouse 
            if (Event.current.button != 0) return;
            if (Event.current.type != EventType.MouseDrag && Event.current.type != EventType.MouseDown) return;

            //painting sprites
            if (HaveSelectedSprite)
            {
                _file.SetSprite(_grid.GetMouseTilePos(), _selectedSprite);
            }

            //painting tiles
            if (HaveSelectedUserTiles)
            {
                _file.SetTile(_grid.GetMouseTilePos(), _file.Tiles[_selectedDrawerTile - 1].UniqueID);
            }

            //painting default tiles
            if (HaveSelectedDefaultTiles)
            {
                switch (_selectedDrawerTile)
                {
                    case -1: _file.RemoveTile(_grid.GetMouseTilePos()); break;
                    default: _file.SetTile(_grid.GetMouseTilePos(), _selectedDrawerTile); break;
                }
            }

            _needsRepaint = true;
        }

        private void HandleTool_Picker()
        {
            // set cursor
            EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.FPS);

            // only continue code when releasing button 0
            if (Event.current.button != 0 || Event.current.type != EventType.MouseUp) return;

            //find the tile we selected
            var tile = _file.Grid.Find(t => t.Position == _grid.GetMouseTilePos());
            if (tile == null) return;

            //get sprite
            if (tile.Sprite != null)
            {
                _selectedDrawerTile = 0;
                _selectedSprite = tile.Sprite;
                _selectedTool = ToolbarTool.Brush;
                _needsRepaint = true;
                return;
            }

            // if no sprite get tile
            if (_file._tiles.Exists(t => t.UniqueID == tile.TileID) || _drawerData.Ids.Contains(tile.TileID))
            {
                if (tile.TileID < 0) _selectedDrawerTile = tile.TileID;
                //if the order does not match up with the tile id this makes sure to get the correct index
                else _selectedDrawerTile = _file.Tiles.IndexOf(_file.Tiles.Find(t => t.UniqueID == tile.TileID)) + 1;

                _selectedTool = ToolbarTool.Brush;
                _needsRepaint = true;
            }
        }

        private void HandleTool_Eraser()
        {
            // only continue when dragging mouse 0
            if (Event.current.button != 0) return;
            if (Event.current.type != EventType.MouseDrag && Event.current.type != EventType.MouseDown) return;

            //draw outline
            _grid.DrawGridTileOutline(_grid.GetMouseTilePos(),
                EditorTextures.Get(EditorTextures.T_ID.t_highlightTexture));
            _needsRepaint = true;

            //find the tile we selected
            var tile = _file.Grid.Find(t => t.Position == _grid.GetMouseTilePos());
            if (tile == null) return;

            //remove
            _file.RemoveSprite(tile.Position);
            _needsRepaint = true;
        }

        private void HandleTool_Move()
        {
            // return if left mouse is not pressed
            if (Event.current.button != 0) return;

            //move selection
            if (_hasSelection)
            {
                // set cursor
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.MoveArrow);

                if (Event.current.type == EventType.MouseDown)
                {
                    //set drag start postition
                    _moveFromPos = _grid.GetMouseTilePos();
                    _moveToPos = _moveFromPos;
                    _movingSelection = true;
                }

                if (Event.current.type == EventType.MouseDrag)
                {
                    _moveToPos = _grid.GetMouseTilePos();
                    _movingSelection = true;
                }

                if (Event.current.type == EventType.MouseUp)
                {
                    Vector2Int movedBy = _moveToPos - _moveFromPos;

                    if (_file.MoveArea(_selectionRect, movedBy))
                    {
                        _selectionStart += movedBy;
                        _selectionEnd += movedBy;
                    }
                    _movingSelection = false;
                }
            }

            //move clipboard
            if (TileEditorClipboard.PasteInProgress)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    //set drag start postition
                    _moveFromPos = _grid.GetMouseTilePos();
                    _moveToPos = _moveFromPos;
                    _movingPasteSelection = true;
                }

                if (Event.current.type == EventType.MouseDrag)
                {
                    _moveToPos = _grid.GetMouseTilePos();
                    _movingPasteSelection = true;
                }

                if (Event.current.type == EventType.MouseUp)
                {
                    Vector2Int movedBy = _moveToPos - _moveFromPos;

                    TileEditorClipboard.MoveClipboard(movedBy);
                    _movingPasteSelection = false;
                }
            }
        }

        private void HandleTool_Select()
        {
            // return if left mouse is not pressed
            if (Event.current.button != 0) return;

            if (Event.current.type == EventType.MouseDown)
            {
                TileEditorClipboard.CancelPaste();
                _hasSelection = true;
                _selectionStart = _grid.GetMouseTilePos();
                _selectionEnd = _selectionStart;
            }

            if (Event.current.type == EventType.MouseDrag) _selectionEnd = _grid.GetMouseTilePos();
        }

        private void HandleTool_Inspect()
        {
            // find cell under cursor
            var cell = _file.Grid.Find(t => t.Position == _grid.GetMouseTilePos());
            // set cursor
            if (cell != null)
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.Zoom);

            if (Event.current.button != 0) return;
            
            // enable creating selection
            if (Event.current.type == EventType.MouseDown)
            {
                TileEditorClipboard.CancelPaste();
                _inspectTool_selectionStart = _grid.GetMouseTilePos();
                _inspectTool_selectionEnd = _inspectTool_selectionStart;
            }

            if (Event.current.type == EventType.MouseDrag) _inspectTool_selectionEnd = _grid.GetMouseTilePos();

            // select inspecting cell
            if (Event.current.type == EventType.MouseUp &&
                _inspectTool_selectionStart == _inspectTool_selectionEnd)
            {
                _inspectingCell = cell;
                GUI.FocusControl(null);
            }

            // apply selection area
            if (Event.current.type == EventType.MouseUp && _inspectTool_selectionEnd != _inspectTool_selectionStart)
            {
                _hasSelection = true;
                _selectionStart = _inspectTool_selectionStart;
                _selectionEnd = _inspectTool_selectionEnd;
                _inspectTool_selectionStart = Vector2Int.zero;
                _inspectTool_selectionEnd = Vector2Int.zero;
            }
        }
        
        #region Selection and clipboard functions

        public void DeleteSelection()
        {
            Clipboard.CancelPaste();
            if (_hasSelection) 
            { 
                _file.DeleteArea(_selectionRect); 
                _hasSelection = false; 
            }
        }

        public void LockSelection()
        {
            Clipboard.CancelPaste();
            if (_hasSelection)
            {
                _file.LockArea(_selectionRect);
            }
        }

        public void UnlockSelection()
        {
            Clipboard.CancelPaste();
            if (_hasSelection)
            {
                _file.UnlockArea(_selectionRect);
            }
        }

        public void CreatePresetBlock()
        {
            if (_hasSelection)
            {
                _file.CreatePresetBlock(_selectionRect);
                _hasSelection = false;
            }
        }
        #endregion
        
        void HighlightCells()
        {
            // highlight selected sprite/tile
            if (_selectedTool == ToolbarTool.Brush && !HoldingCtrl)
            {
                var translucentCol = new Color(1, 1, 1, 0.4f);
                // sprites
                if (HaveSelectedSprite) GridSpriteRenderer.DrawSpriteOnGrid(_grid, _selectedSprite, _grid.GetMouseTilePos(), translucentCol);
                // tiles
                else GridSpriteRenderer.DrawTileOnGrid(_grid, _selectedDrawerTile, _grid.GetMouseTilePos(), _drawerData, translucentCol);
            }
            // outline cell when not in draw or selection mode
            else if (_selectedTool != ToolbarTool.Move && !(_hasSelection && _selectedTool == ToolbarTool.Select && _selectionRect.Contains(_grid.GetMouseTilePos()))) 
                _grid.DrawGridTileOutline(_grid.GetMouseTilePos(), Tex.GetColor(EditorTextures.C_ID.c_highlightColor));
        }
    }
}
#endif