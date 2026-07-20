#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VinTools.BetterRuleTiles.Editor.EditorWindows;
using Tex = VinTools.BetterRuleTiles.Editor.Data.EditorTextures;
using Cache = VinTools.BetterRuleTiles.Editor.Data.SpriteCache;
using Style = VinTools.BetterRuleTiles.Editor.Data.CustomStyles;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows.BRTEditorWindows
{
    public class SpriteDrawerWindow : GUIWindow
    {
        public SpriteDrawerWindow(BetterRuleTileContainer container, BetterRuleTileEditor editor) : base(
            GUIWindow.WindowLayout.SpanLeft, new RectOffset(4, 4, 30, 125), new Vector2(180, 400), new GUIContent(""))
        {
            this.windowFunction = UpdateUI;
            this._editor = editor;
            this._file = container;
        }

        private BetterRuleTileContainer _file;
        private BetterRuleTileEditor _editor;

        Vector2 _scrollPos;
        
        bool _spriteDrawerCollapsed = true;
        private string _spriteFilter = "";

        
        private void UpdateUI(int windowID)
        {
            // title bar
            GUI.Label(new Rect(0, 0, width - 12, 25), "Sprites", Style.CenteredBoldLabel);
            EditorGUILayout.Space();

            // collapse
            if (GUI.Button(new Rect(width - 18, 4, 16, 16), _spriteDrawerCollapsed ? Tex.Get(Tex.T_ID.t_tileInfo_rightArrow) : Tex.Get(Tex.T_ID.t_tileInfo_leftArrow), Style.IconButton))
            {
                _spriteDrawerCollapsed = !_spriteDrawerCollapsed;
            }

            // Search bar
            var searchRect = new Rect(4, 30, width - 8, 18);
            _spriteFilter = GUI.TextField(searchRect, _spriteFilter);
            GUI.DrawTexture(new Rect(searchRect.x + searchRect.width - 15, searchRect.y + 3, 12, 12), Tex.Get(Tex.T_ID.t_searchIcon));

            // variables
            int size = _file.settings._drawerHeight;
            int spritesCount = Cache.SpriteCount;
            int collumns = _spriteDrawerCollapsed ? _file.settings._drawerCollumns : _file.settings._expandedDrawerCollumns;
            Vector2 scrollSize = new Vector2((spritesCount % collumns) * (size + 5) - 5, Mathf.CeilToInt((float)spritesCount / collumns) * (size + 5));

            // grid
            Vector2 pos = new Vector2(4, 50);
            _scrollPos = GUI.BeginScrollView(new Rect(pos, this.size - new Vector2(pos.x, pos.y + 2)), _scrollPos, new Rect(pos, scrollSize));

            int s = 0;
            for (int i = 0; i < spritesCount; i++)
            {
                if (_spriteFilter == "" || Cache.SortedSprites[i].name.ToLower().Contains(_spriteFilter.ToLower()))
                {
                    int row = s / collumns;
                    int collumn = s % collumns;

                    Rect rect = new Rect(pos + new Vector2(collumn * (size + 5), row * (size + 5)), Vector2.one * size);
                    DrawNextSpriteCell(rect, i);

                    s++;
                }
            }

            GUI.EndScrollView();

            // window properties
            int scrollBarWidth = scrollSize.y < height - pos.y - 1 ? 3 : 16;
            lockPosition = _file.settings._lockWindows;
            width = collumns * (size + 5) + scrollBarWidth;
            padding.top = (int)_editor._window_Toolbar.height + 4; 
        }
        
        void DrawNextSpriteCell(Rect rect, int spriteCellIndex)
        {
            var tex = Cache.TryGetSorted(spriteCellIndex);
            if (tex == null) return;

            Vector2 aspect = tex.width > tex.height ? new Vector2(1, (float)tex.height / tex.width) : (tex.width < tex.height ? new Vector2((float)tex.width / tex.height, 1) : Vector2.one);

            if (GUI.Button(rect, new GUIContent("", Cache.SortedSprites[spriteCellIndex].name)))
                SelectSprite(Cache.SortedSprites[spriteCellIndex]);

            Rect imgRect = new Rect(rect.position + rect.size * .1f + rect.size * (Vector2.one - aspect) * .4f, rect.size * .8f * aspect);
            GUI.DrawTexture(imgRect, tex, ScaleMode.ScaleToFit);
        }

        void SelectSprite(Sprite sprite, bool selectBrush = true, bool collapseSpriteDrawer = true)
        {
            _editor._selectedDrawerTile = 0;
            if (selectBrush) _editor._selectedTool = 0;
            if (collapseSpriteDrawer) _spriteDrawerCollapsed = true;
            _editor._selectedSprite = sprite;
        }
    }
}
#endif