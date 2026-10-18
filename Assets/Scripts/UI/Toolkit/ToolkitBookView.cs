using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Library index and article reader. Credits retains its independent layout.</summary>
    public sealed class ToolkitBookView : IDisposable
    {
        private readonly List<ToolkitTextBinding> bindings = new();
        private readonly List<ToolkitTextBinding> articleBindings = new();
        private readonly Dictionary<ToolkitBookDefinition.Section, Button> topics = new();
        private readonly List<(VisualElement root, ToolkitBookDefinition.Section[] sections)> groups = new();
        private readonly ToolkitBookDefinition definition;
        private VisualElement articleContent;
        private Label articleTitle;
        private Image articleIcon;
        private Button overview, reference, portraitTopics;
        private VisualElement topicIndex, portraitArticle;
        private bool topicsOpen;
        private ToolkitBookDefinition.Section selected;
        private readonly Dictionary<string, int> selectedIllustrations;
        private ToolkitTextBinding illustrationBinding;
        public VisualElement Root { get; }
        public ScrollView Scroll { get; }
        public string SelectedSectionId => selected?.id;
        public bool ReferenceSelected { get; private set; }

        public ToolkitBookView(ToolkitBookDefinition definition, ToolkitTheme theme, VisualTreeAsset template,
            string previousSection = null, bool previousReference = false, Dictionary<string, int> selectedIllustrations = null)
        {
            this.definition = definition;
            this.selectedIllustrations = selectedIllustrations ?? new Dictionary<string, int>();
            Root = template.CloneTree(); Root.AddToClassList("eov-book");
            theme.Apply(Root); ToolkitGameplay.Apply(Root, theme);
            Root.AddToClassList("menu-surface"); Root.AddToClassList("book-reviewed");
            var frame = Root.Q("book-frame");
            var heading = new Label(); heading.AddToClassList("book-heading"); frame.Insert(0, heading);
            Scroll = Root.Q<ScrollView>("book-scroll");
            theme.StyleScroll(Scroll); ToolkitGameplay.StyleScroll(Scroll);
            if (definition.presentation == ToolkitBookDefinition.Presentation.Credits)
            {
                heading.text = "Credits"; Root.AddToClassList("eov-credits");
                foreach (var credit in definition.sections) AddCredit(credit);
                return;
            }
            bindings.Add(new ToolkitTextBinding(heading, LocalText("help.library.title", "Library")));
            Root.AddToClassList("library-reader");
            portraitTopics = ToolkitGameplay.B(frame, "Browse topics", () => { topicsOpen = !topicsOpen; RefreshPortrait(); }, "portrait-topic-picker");
            var layout = new VisualElement(); layout.AddToClassList("library-layout"); frame.Add(layout);
            var index = new ScrollView(ScrollViewMode.Vertical) { name = "library-topics" };
            topicIndex = index; index.AddToClassList("library-topics");
            theme.StyleScroll(index); ToolkitGameplay.StyleScroll(index); layout.Add(index);
            var article = portraitArticle = new VisualElement(); article.AddToClassList("library-article"); layout.Add(article);
            var titleRow = new VisualElement(); titleRow.AddToClassList("library-title-row"); article.Add(titleRow);
            articleIcon = ToolkitGameplay.Icon(titleRow, null, 22);
            articleTitle = new Label { name = "library-article-title" }; articleTitle.AddToClassList("library-article-title"); titleRow.Add(articleTitle);
            var tabs = new VisualElement(); tabs.AddToClassList("library-tabs"); article.Add(tabs);
            overview = Tab(tabs, "library-overview", "help.overview.title", "Overview", false);
            reference = Tab(tabs, "library-reference", "help.reference.title", "Reference", true);
            Scroll.RemoveFromHierarchy(); Scroll.name = "library-article-scroll";
            Scroll.AddToClassList("library-article-scroll"); article.Add(Scroll);
            articleContent = new VisualElement { name = "library-article-content" }; Scroll.Add(articleContent);
            foreach (ToolkitBookDefinition.TopicGroup category in Enum.GetValues(typeof(ToolkitBookDefinition.TopicGroup)))
            {
                var sections = definition.sections.Where(s => s.group == category).ToArray();
                if (sections.Length == 0) continue;
                var group = new VisualElement(); group.AddToClassList("library-topic-group"); index.Add(group);
                var label = new Label(); label.AddToClassList("library-group-title"); group.Add(label);
                bindings.Add(new ToolkitTextBinding(label, LocalText("help.group." + category, category.ToString())));
                foreach (var section in sections)
                {
                    var button = new Button(() => { Select(section, false); Audio.AudioManager.Instance?.PlayUIButtonClick(); }) { name = section.id };
                    button.AddToClassList("library-topic"); ToolkitGameplay.Icon(button, section.icon, 14);
                    var title = new Label(); title.AddToClassList("library-topic-title"); button.Add(title);
                    bindings.Add(new ToolkitTextBinding(title, section.title)); topics.Add(section, button); group.Add(button);
                }
                groups.Add((group, sections));
            }
            var initial = definition.sections.FirstOrDefault(s => s.id == previousSection && ToolkitVisibilityRule.All(s.visibility))
                ?? definition.sections.FirstOrDefault(s => ToolkitVisibilityRule.All(s.visibility));
            if (initial != null) Select(initial, previousReference);
            RefreshVisibility();
        }

        public void RefreshPortrait()
        {
            if (portraitTopics == null) return;
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            var tabs = overview.parent;
            if (portrait && tabs.parent != Root) Root.Add(tabs);
            else if (!portrait && tabs.parent == Root) portraitArticle.Insert(1, tabs);
            tabs.style.display = portrait && topicsOpen ? DisplayStyle.None : DisplayStyle.Flex;
            Root.style.paddingBottom = portrait ? 48 : StyleKeyword.Null;
            portraitTopics.style.display = portrait ? DisplayStyle.Flex : DisplayStyle.None;
            portraitTopics.text = topicsOpen ? "Back to article" : "Browse topics";
            topicIndex.style.display = !portrait || topicsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            portraitArticle.style.display = portrait && topicsOpen ? DisplayStyle.None : DisplayStyle.Flex;
        }
        private static ToolkitBookDefinition.Text LocalText(string key, string fallback) => new()
        { fallback = fallback, localized = new LocalizedString("Wiki", key) };

        private Button Tab(VisualElement parent, string name, string key, string title, bool isReference)
        {
            var button = new Button(() => { Select(selected, isReference); Audio.AudioManager.Instance?.PlayUIButtonClick(); }) { name = name };
            button.AddToClassList("library-tab"); parent.Add(button);
            bindings.Add(new ToolkitTextBinding(button, LocalText(key, title))); return button;
        }

        private void Select(ToolkitBookDefinition.Section section, bool isReference)
        {
            if (section == null || !ToolkitVisibilityRule.All(section.visibility)) return;
            illustrationBinding?.Dispose(); illustrationBinding = null;
            foreach (var binding in articleBindings) binding.Dispose(); articleBindings.Clear();
            selected = section; ReferenceSelected = isReference; topicsOpen = false; RefreshPortrait();
            foreach (var topic in topics) topic.Value.EnableInClassList("active", topic.Key == section);
            articleBindings.Add(new ToolkitTextBinding(articleTitle, section.title)); articleIcon.sprite = section.icon;
            overview.EnableInClassList("active", !isReference); reference.EnableInClassList("active", isReference);
            var hasReference = !string.IsNullOrEmpty(section.details?.fallback) || section.referenceRows.Length > 0;
            reference.style.display = hasReference ? DisplayStyle.Flex : DisplayStyle.None;
            articleContent.Clear(); Scroll.scrollOffset = Vector2.zero;
            if (!isReference && section.illustrations.Length > 0)
            {
                var figures = new VisualElement { name = "library-illustrations" }; figures.AddToClassList("library-illustrations"); articleContent.Add(figures);
                var detail = new Label { name = "library-illustration-description" }; detail.AddToClassList("library-illustration-description");
                var choices = new List<(Button button, int index)>();
                void SelectIllustration(int index)
                {
                    selectedIllustrations[section.id] = index;
                    foreach (var choice in choices) choice.button.EnableInClassList("active", choice.index == index);
                    illustrationBinding?.Dispose();
                    illustrationBinding = new ToolkitTextBinding(detail, section.illustrations[index].description);
                }
                for (var index = 0; index < section.illustrations.Length; index++)
                {
                    var illustration = section.illustrations[index];
                    if (!illustration.sprite) continue;
                    var choiceIndex = index;
                    var figure = new Button(() => { SelectIllustration(choiceIndex); Audio.AudioManager.Instance?.PlayUIButtonClick(); }) { name = "library-illustration-" + index };
                    figure.AddToClassList("library-figure"); figures.Add(figure); choices.Add((figure, index));
                    var image = new Image { sprite = illustration.sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                    image.AddToClassList("library-figure-art"); figure.Add(image);
                    BindText(figure, illustration.caption, "library-figure-caption");
                }
                articleContent.Add(detail);
                if (choices.Count > 0)
                {
                    var initial = selectedIllustrations.TryGetValue(section.id, out var saved) && choices.Any(c => c.index == saved) ? saved : choices[0].index;
                    SelectIllustration(initial);
                }
            }
            BindText(articleContent, isReference ? section.details : section.body, "library-prose").name = "library-article-body";
            if (isReference && section.referenceRows.Length > 0) AddTable(section);
            foreach (var button in Scroll.contentContainer.Query<Button>().ToList()) ToolkitDragScroll.EnableFrom(Scroll, button);
        }

        private Label BindText(VisualElement parent, ToolkitBookDefinition.Text text, string css)
        {
            var label = new Label(); label.AddToClassList(css); parent.Add(label);
            articleBindings.Add(new ToolkitTextBinding(label, text)); return label;
        }

        private void AddTable(ToolkitBookDefinition.Section section)
        {
            var table = new VisualElement { name = "library-reference-table" }; table.AddToClassList("library-table"); articleContent.Add(table);
            if (section.referenceColumns.Length > 0)
            {
                var header = new VisualElement(); header.AddToClassList("library-table-row"); header.AddToClassList("library-table-header"); table.Add(header);
                foreach (var column in section.referenceColumns) BindText(header, column, "library-table-cell");
            }
            foreach (var row in section.referenceRows)
            {
                var line = new VisualElement(); line.AddToClassList("library-table-row"); table.Add(line);
                var identity = new VisualElement(); identity.AddToClassList("library-table-cell"); identity.AddToClassList("library-table-identity"); line.Add(identity);
                if (row.icon) ToolkitGameplay.Icon(identity, row.icon, 15);
                BindText(identity, row.label, "library-table-label"); BindText(line, row.value, "library-table-cell");
                if (section.referenceColumns.Length > 2) BindText(line, row.extra, "library-table-cell");
            }
        }

        public void RefreshVisibility()
        {
            foreach (var topic in topics) topic.Value.style.display = ToolkitVisibilityRule.All(topic.Key.visibility) ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var group in groups) group.root.style.display = group.sections.Any(s => ToolkitVisibilityRule.All(s.visibility)) ? DisplayStyle.Flex : DisplayStyle.None;
            if (selected != null && !ToolkitVisibilityRule.All(selected.visibility))
            {
                var first = definition.sections.FirstOrDefault(s => ToolkitVisibilityRule.All(s.visibility));
                if (first != null) Select(first, false);
                else { selected = null; articleTitle.text = ""; articleIcon.sprite = null; articleContent.Clear(); }
            }
        }

        private void AddCredit(ToolkitBookDefinition.Section credit)
        {
            var row = new VisualElement { name = credit.id }; row.AddToClassList("eov-credit");
            var title = new Label(); title.AddToClassList("eov-chapter-title"); var body = new Label(); body.AddToClassList("eov-chapter-body");
            bindings.Add(new ToolkitTextBinding(title, new ToolkitBookDefinition.Text
                { key = credit.title.key, fallback = credit.title.fallback, localized = credit.title.localized, bold = true }));
            bindings.Add(new ToolkitTextBinding(body, credit.body)); row.Add(title); row.Add(body); Scroll.Add(row);
        }

        public void Dispose()
        {
            illustrationBinding?.Dispose();
            foreach (var binding in bindings) binding.Dispose(); foreach (var binding in articleBindings) binding.Dispose();
            bindings.Clear(); articleBindings.Clear(); Root.RemoveFromHierarchy();
        }
    }
}
