using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;
namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Shared town-window geometry; only writes styles when bounds or the visual tree change.</summary>
    public sealed class ToolkitWindowLayout
    {
        private VisualElement previousRoot;
        private Rect previousBounds;
        public static Rect SafeArea => ToolkitBookScreen.CalculateSafeArea(
            new Vector2Int(Screen.width, Screen.height), Screen.safeArea,
            Application.isMobilePlatform ? 1 : SafeAreaRatio);

        public bool Centered(VisualElement root, ToolkitTheme theme)
        {
            var area = SafeArea;
            if(root!=null && root.ClassListContains("gameplay"))
            {
                var modernWidth=Mathf.Min(640,area.width-24);
                return Apply(root,new Rect(area.center.x-modernWidth/2,area.y+44,modernWidth,Mathf.Max(0,area.height-56)));
            }
            var width = Mathf.Min(theme.windowWidth, area.width);
            return Apply(root, new Rect(area.center.x - width * .5f, area.y + theme.windowTopInset,
                width, Mathf.Max(0, area.height - theme.windowTopInset - theme.windowBottomInset)));
        }

        public bool Fill(VisualElement root, ToolkitTheme theme, float companionWidth = 0)
        {
            var area = SafeArea;
            if(root!=null && root.ClassListContains("gameplay"))
                return Apply(root,new Rect(area.x+12,area.y+44,Mathf.Max(0,area.width-companionWidth-24),Mathf.Max(0,area.height-56)));
            return Apply(root, new Rect(area.x, area.y + theme.windowTopInset,
                Mathf.Max(0, area.width - companionWidth), Mathf.Max(0, area.height - theme.windowTopInset - theme.windowBottomInset)));
        }

        public bool Apply(VisualElement root, Rect bounds)
        {
            if (root == null || (ReferenceEquals(root, previousRoot) && previousBounds == bounds)) return false;
            previousRoot = root; previousBounds = bounds;
            root.style.left = bounds.x; root.style.top = bounds.y;
            root.style.width = bounds.width; root.style.height = bounds.height;
            return true;
        }
    }
}
