using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private Label buffEffects;
        private TimelessEchoes.Buffs.BuffRecipe selectedBuff;
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
                ToolkitLocalization.Bind(heading, "credits.title", "Credits"); Root.AddToClassList("eov-credits");
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
            var initial = definition.sections.FirstOrDefault(s => s.id == previousSection && ToolkitVisibilityRule.All(s.visibility));
            if (initial != null) Select(initial, previousReference);
            else ShowTopics();
            RefreshVisibility();
            ToolkitLocalization.Changed += LocaleChanged;
        }

        public void RefreshPortrait()
        {
            if (portraitTopics == null) return;
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            var showTopics = topicsOpen || selected == null;
            var tabs = overview.parent;
            if (portrait && tabs.parent != Root) Root.Add(tabs);
            else if (!portrait && tabs.parent == Root) portraitArticle.Insert(1, tabs);
            tabs.style.display = showTopics ? DisplayStyle.None : DisplayStyle.Flex;
            Root.style.paddingBottom = portrait ? 48 : StyleKeyword.Null;
            portraitTopics.style.display = portrait && selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            portraitTopics.text = topicsOpen ? ToolkitLocalization.Text("library.back-to-article", "Back to article") : ToolkitLocalization.Text("library.browse-topics", "Browse topics");
            topicIndex.style.display = !portrait || showTopics ? DisplayStyle.Flex : DisplayStyle.None;
            portraitArticle.style.display = showTopics ? DisplayStyle.None : DisplayStyle.Flex;
            Root.EnableInClassList("library-topics-home", showTopics);
        }

        private void ShowTopics()
        {
            selected = null; ReferenceSelected = false; topicsOpen = true;
            illustrationBinding?.Dispose(); illustrationBinding = null;
            foreach (var binding in articleBindings) binding.Dispose(); articleBindings.Clear();
            foreach (var topic in topics) topic.Value.RemoveFromClassList("active");
            articleTitle.text = ""; articleIcon.sprite = null; articleContent.Clear();
            selectedBuff = null; buffEffects = null;
            RefreshPortrait();
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
            selectedBuff = null; buffEffects = null;
            if (!isReference && section.illustrations.Length > 0)
            {
                var figures = new VisualElement { name = "library-illustrations" }; figures.AddToClassList("library-illustrations"); articleContent.Add(figures);
                var detail = new Label { name = "library-illustration-description" };
                ToolkitHelpLinks.Attach(detail, OpenArticleLink); detail.AddToClassList("library-illustration-description");
                var choices = new List<(Button button, int index)>();
                void SelectIllustration(int index)
                {
                    selectedIllustrations[section.id] = index;
                    foreach (var choice in choices) choice.button.EnableInClassList("active", choice.index == index);
                    illustrationBinding?.Dispose();
                    // The buff catalogue uses recipe prose and effects below. Keep
                    // the separate role explanations in the Echoes article.
                    var catalogue = section.id == "help-buffs";
                    detail.style.display = catalogue ? DisplayStyle.None : DisplayStyle.Flex;
                    illustrationBinding = catalogue ? null : new ToolkitTextBinding(detail, DefenseText(section.illustrations[index].description));
                    selectedBuff = section.illustrations[index].buff;
                    RefreshSharedEffects();
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
                    if (illustration.buff)
                    {
                        var caption = new Label(); caption.AddToClassList("library-figure-caption"); figure.Add(caption);
                        ToolkitLocalization.Bind(caption, "buff." + illustration.buff.name, illustration.buff.GetDisplayName());
                    }
                    else BindText(figure, illustration.caption, "library-figure-caption");
                }
                articleContent.Add(detail);
                buffEffects = new Label { name = "library-buff-effects", enableRichText = true };
                buffEffects.AddToClassList("library-prose"); articleContent.Add(buffEffects);
                if (choices.Count > 0)
                {
                    var initial = selectedIllustrations.TryGetValue(section.id, out var saved) && choices.Any(c => c.index == saved) ? saved : choices[0].index;
                    SelectIllustration(initial);
                }
            }
            if (section.id == "help-patchnotes" && !isReference)
            {
                BindText(articleContent, section.body, "library-prose", value => PatchNotesPart(value, false)).name = "library-article-body";
                var history = BindText(articleContent, section.body, "library-prose", value => PatchNotesPart(value, true));
                history.name = "library-patch-history";
                history.AddToClassList("library-patch-history");
            }
            else
            {
                var body = BindText(articleContent, isReference ? section.details : section.body, "library-prose");
                body.name = "library-article-body";
                if (section.id == "help-patchnotes") body.AddToClassList("library-patch-history");
            }
            if (isReference && section.referenceRows.Length > 0) AddTable(section);
            foreach (var button in Scroll.contentContainer.Query<Button>().ToList()) ToolkitDragScroll.EnableFrom(Scroll, button);
        }

        private static string PatchNotesPart(string text, bool history)
        {
            var versions = Regex.Matches(text, @"<b>[^<]*\d+\.\d+\.\d+[^<]*</b>");
            if (versions.Count < 2) return history ? string.Empty : text;
            var split = versions[1].Index;
            return history ? text.Substring(split).TrimStart() : text.Substring(0, split).TrimEnd();
        }

        private Label BindText(VisualElement parent, ToolkitBookDefinition.Text text, string css, Func<string, string> format = null)
        {
            var label = new Label(); label.AddToClassList(css); parent.Add(label);
            if (text?.fallback?.Contains("<link=\"help:") == true) ToolkitHelpLinks.Attach(label, OpenArticleLink);
            articleBindings.Add(new ToolkitTextBinding(label, DefenseText(text), format)); return label;
        }

        private bool OpenArticleLink(string route)
        {
            if (route != "reference") return false;
            Select(selected, true); return true;
        }

        // Both Library and Buffs call the recipe formatter: the same translated
        // effect templates, current upgrades and Cauldron timing bonuses apply.
        private void LocaleChanged() { RefreshPortrait(); RefreshSharedEffects(); }

        public void RefreshSharedEffects()
        {
            if (buffEffects == null) return;
            buffEffects.style.display = selectedBuff ? DisplayStyle.Flex : DisplayStyle.None;
            if (selectedBuff) buffEffects.text = string.Join("\n", selectedBuff.GetDescriptionLines());
        }

        private static ToolkitBookDefinition.Text DefenseText(ToolkitBookDefinition.Text text)
        {
            var key = text?.localized?.TableEntryReference.Key;
            if (key != "help.combat.details" && key != "help.combat.art.1.description") return text;
            var scalar = Combat.DefaultArmorScalarN;
            object[] arguments = { scalar, scalar * 3 };
            return new ToolkitBookDefinition.Text
            {
                key = text.key, bold = text.bold, smallCaps = text.smallCaps,
                fallback = string.Format(Blindsided.Utilities.LocaleNumbers.Culture, text.fallback, arguments),
                localized = new LocalizedString(text.localized.TableReference, text.localized.TableEntryReference) { Arguments = arguments }
            };
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
                ShowTopics();
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
            ToolkitLocalization.Changed -= LocaleChanged;
            illustrationBinding?.Dispose();
            foreach (var binding in bindings) binding.Dispose(); foreach (var binding in articleBindings) binding.Dispose();
            bindings.Clear(); articleBindings.Clear(); Root.RemoveFromHierarchy();
        }
    }
}
