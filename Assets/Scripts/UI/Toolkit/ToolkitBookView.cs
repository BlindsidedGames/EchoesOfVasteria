using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Native Toolkit book view. No Canvas, TMP or legacy view is required at runtime.</summary>
    public sealed class ToolkitBookView : IDisposable
    {
        private readonly List<ToolkitTextBinding> bindings = new();
        private readonly Dictionary<string, Action<bool>> expanders = new();
        private readonly Dictionary<string, bool> expandedState = new();
        private readonly List<(VisualElement element, ToolkitVisibilityRule[] rules)> visibility = new();
        public VisualElement Root { get; }
        public ScrollView Scroll { get; }

        public ToolkitBookView(ToolkitBookDefinition definition, ToolkitTheme theme, VisualTreeAsset template,
            IReadOnlyDictionary<string, bool> previousExpanded = null)
        {
            Root = template.CloneTree();
            Root.AddToClassList("eov-book");
            theme.Apply(Root);ToolkitGameplay.Apply(Root,theme);Root.AddToClassList("menu-surface");Root.AddToClassList("book-reviewed");
            
            Scroll = Root.Q<ScrollView>("book-scroll");
            theme.StyleScroll(Scroll);ToolkitGameplay.StyleScroll(Scroll);
            var heading=new Label(definition.presentation==ToolkitBookDefinition.Presentation.Credits?"Credits":"Library");heading.AddToClassList("book-heading");Root.Q("book-frame").Insert(0,heading);
            if (definition.presentation == ToolkitBookDefinition.Presentation.Credits)
            {
                Root.AddToClassList("eov-credits");
                foreach (var credit in definition.sections) AddCredit(credit, theme);
                return;
            }
            foreach (var section in definition.sections)
            {
                var group = new VisualElement();
                group.AddToClassList("eov-chapter");
                var header = new Button { name = section.id };
                header.AddToClassList("eov-chapter-header");
                header.AddToClassList("button");
                var title = new Label();
                title.AddToClassList("eov-chapter-title");
                header.Add(title);
                var icon = new Label { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("book-disclosure");
                header.Add(icon);
                var content = new VisualElement();
                content.AddToClassList("eov-chapter-content");
                
                var body = new Label { enableRichText = true };
                body.AddToClassList("eov-chapter-body");
                content.Add(body);
                bindings.Add(new ToolkitTextBinding(title, Heading(section.title)));
                bindings.Add(new ToolkitTextBinding(body, section.body));
                bool expanded = section.expanded;
                void SetExpanded(bool value)
                {
                    expanded = value;
                    expandedState[section.id] = value;
                    content.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                    icon.text = value ? "−" : "+";
                    header.EnableInClassList("expanded", value);
                }
                header.clicked += () => SetExpanded(!expanded);
                header.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0) Audio.AudioManager.Instance?.PlayUIButtonClick();
                });
                header.RegisterCallback<NavigationSubmitEvent>(_ => Audio.AudioManager.Instance?.PlayUIButtonClick());
                expanders.Add(section.id, SetExpanded);
                SetExpanded(previousExpanded != null && previousExpanded.TryGetValue(section.id, out var saved) ? saved : expanded);
                group.Add(header);
                group.Add(content);
                Scroll.Add(group);
                visibility.Add((group, section.visibility));
            }
            RefreshVisibility();
        }

        public void RefreshVisibility()
        {
            foreach (var entry in visibility)
                entry.element.style.display = ToolkitVisibilityRule.All(entry.rules) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void AddCredit(ToolkitBookDefinition.Section credit, ToolkitTheme theme)
        {
            var row = new VisualElement { name=credit.id };
            row.AddToClassList("eov-credit");
            
            var title = new Label { enableRichText = true };
            title.AddToClassList("eov-chapter-title");
            var body = new Label { enableRichText = true };
            body.AddToClassList("eov-chapter-body");
            bindings.Add(new ToolkitTextBinding(title, Heading(credit.title)));
            bindings.Add(new ToolkitTextBinding(body, credit.body));
            row.Add(title);
            row.Add(body);
            Scroll.Add(row);
        }

        private static ToolkitBookDefinition.Text Heading(ToolkitBookDefinition.Text source) => new()
        { key=source.key, fallback=source.fallback, localized=source.localized, bold=true, smallCaps=false };

        public void SetExpanded(string id, bool expanded) => expanders[id](expanded);

        public Dictionary<string, bool> CaptureExpanded() => new(expandedState);

        public void Dispose()
        {
            foreach (var binding in bindings) binding.Dispose();
            bindings.Clear();
            expanders.Clear();
            visibility.Clear();
            Root.RemoveFromHierarchy();
        }
    }
}
