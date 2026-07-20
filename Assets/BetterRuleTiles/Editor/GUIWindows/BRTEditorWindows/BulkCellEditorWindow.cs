#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class BulkCellEditorWindow : GUIWindow
    {
        private const int DefaultWindowWidth = GridCellInfoWindow.DefaultWindowWidth;
        private const int DefaultWindowHeight = 115;
        private const int DefaultBottomPadding = 4;
        
        public BulkCellEditorWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base(
            GUIWindow.WindowLayout.BottomRightCorner, new RectOffset(4, 4, 30, DefaultBottomPadding), new Vector2(DefaultWindowWidth, DefaultWindowHeight), new GUIContent(""), false)
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }

        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;

        private bool _windowCollapsed = false;
        
        // variables to apply to cells
        private int priorityGroup = 0;
        
        private void UpdateUI(int windowID)
        {
            // lock position
            lockPosition = _file.settings._lockWindows;
            padding.bottom = _editor._window_GridCellInfo.visible ? 2 * DefaultBottomPadding + (int)_editor._window_GridCellInfo.height : DefaultBottomPadding;
            height = _windowCollapsed ? 25 : DefaultWindowHeight;
            width = DefaultWindowWidth;
            
            // title
            GUI.Label(new Rect(0, 0, width, 25), $"Bulk cell editor", Style.CenteredBoldLabel);
            
            // collapse
            if (GUI.Button(new Rect(width - 20, 3, 16, 16), _windowCollapsed ? Tex.Get(Tex.T_ID.t_tileInfo_upArrow) : Tex.Get(Tex.T_ID.t_tileInfo_downArrow), Style.IconButton))
                _windowCollapsed = !_windowCollapsed;
            if (_windowCollapsed) return;
            
            EditorGUILayout.Space();
            _editor.guiBuilder.DrawInBackground(new GUIContent("Rule priority"), 1, (r) =>
            {
                priorityGroup = EditorGUILayout.IntField(new GUIContent("Priority group", "Priority of the rule. Higher number means higher priority. Rules within the same priority group will be sorted automatically."), priorityGroup);
            });
            
            EditorGUILayout.Space();
            if (GUILayout.Button($"Apply to all cells inside selection"))
            {
                foreach (var item in _file.Grid.Where(t => _editor._selectionRect.Contains(t.Position)).ToArray())
                    ApplyChangesToCell(item);
                    
                // disable selection (and the window with it) to indicate the changes
                _editor._hasSelection = false;
            }
            
            
            
            // move window
            if (!lockPosition) GUI.DragWindow();
        }

        void ApplyChangesToCell(BetterRuleTileContainer.GridCell cell)
        {
            cell.Priority = priorityGroup;
        }
    }
}
#endif