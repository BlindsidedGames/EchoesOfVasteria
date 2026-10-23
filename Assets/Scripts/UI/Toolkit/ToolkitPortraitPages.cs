using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Portrait composition of existing live controls, with desktop hierarchy restored on rotation.</summary>
    public sealed class ToolkitPortraitPages
    {
        public sealed class Page
        {
            public readonly string Key, Title;
            public readonly VisualElement[] Content;
            public Page(string key, string title, params VisualElement[] content) { Key = key; Title = title; Content = content; }
        }
        private readonly VisualElement host, shared, tabs, content;
        private readonly VisualElement[] hidden;
        private readonly Page[] pages;
        private readonly ScrollView scroll;
        private readonly List<(VisualElement element, VisualElement parent, int index)> origins = new();
        private readonly List<Button> buttons = new();
        private readonly List<VisualElement> panels = new();
        private bool attached;
        private VisualElement navigationHost;
        private StyleLength originalPadding;
        public int Selected { get; private set; }
        public bool Active => attached;
        public ToolkitPortraitPages(VisualElement host, VisualElement shared, VisualElement[] hidden, ScrollView scroll, params Page[] pages)
        {
            this.host = host; this.shared = shared; this.hidden = hidden; this.scroll = scroll; this.pages = pages;
            tabs = new VisualElement(); tabs.AddToClassList("portrait-page-tabs");
            content = new VisualElement(); content.AddToClassList("portrait-pages-content");
            if (shared != null) Remember(shared);
            foreach (var page in pages) foreach (var element in page.Content) Remember(element);
            for (var i = 0; i < pages.Length; i++)
            {
                var index = i;
                var button = ToolkitGameplay.B(tabs, pages[i].Title, () => Select(index));
                ToolkitLocalization.Bind(button, pages[i].Key, pages[i].Title);
                button.name = "portrait-page-" + i;
                buttons.Add(button);
                var panel = new VisualElement(); panel.AddToClassList("portrait-page"); panels.Add(panel); content.Add(panel);
            }
        }
        private void Remember(VisualElement element) => origins.Add((element, element.parent, element.parent.IndexOf(element)));
        public void Update()
        {
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            if (portrait == attached) return;
            attached = portrait;
            if (portrait)
            {
                if (shared != null) host.Insert(0, shared);
                foreach (var element in hidden) element.style.display = DisplayStyle.None;
                host.Add(content);
                navigationHost = host;
                while (navigationHost.parent != null && !navigationHost.ClassListContains("gameplay")) navigationHost = navigationHost.parent;
                originalPadding = navigationHost.style.paddingBottom;
                navigationHost.style.paddingBottom = 48;
                navigationHost.Add(tabs);
                for (var i = 0; i < pages.Length; i++) foreach (var element in pages[i].Content) panels[i].Add(element);
                Refresh();
            }
            else
            {
                // Siblings were recorded in original order, so insertion restores that order too.
                foreach (var origin in origins) origin.parent.Insert(Mathf.Min(origin.index, origin.parent.childCount), origin.element);
                foreach (var element in hidden) element.style.display = StyleKeyword.Null;
                tabs.RemoveFromHierarchy(); content.RemoveFromHierarchy();
                if (navigationHost != null) navigationHost.style.paddingBottom = originalPadding;
            }
        }
        public void Select(int index)
        {
            if (index < 0 || index >= pages.Length || Selected == index) return;
            Selected = index; Refresh();
            if (scroll != null) scroll.schedule.Execute(() => scroll.scrollOffset = Vector2.zero);
        }
        private void Refresh()
        {
            for (var i = 0; i < panels.Count; i++)
            {
                panels[i].style.display = i == Selected ? DisplayStyle.Flex : DisplayStyle.None;
                buttons[i].EnableInClassList("active", i == Selected);
            }
        }
    }
}
