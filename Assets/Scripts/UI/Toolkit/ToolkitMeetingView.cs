using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Native encounter presentation. Rewards remain owned by TalkToNpcTask.</summary>
    public sealed class ToolkitMeetingView : IDisposable
    {
        private readonly IReadOnlyList<string> lines, lineKeys;
        private readonly Action finished;
        private readonly Label dialogue;
        private readonly VisualElement dialogueFrame;
        private readonly ToolkitTextBinding meet, next, close;
        private readonly Label meetLabel, nextLabel, closeLabel;
        private readonly VisualElement portraitColumn, portraitFrame, contentColumn;
        private readonly ScrollView dialogueScroll;
        private bool? portraitLayout;
        private int index = -1;
        private bool completed, disposed;
        public VisualElement Root { get; }
        public Button AdvanceButton { get; }
        public bool AwaitingMeet => index < 0;

        public ToolkitMeetingView(ToolkitTheme theme, VisualTreeAsset template, Sprite portrait,
            ToolkitMeetingDefinition definition, IReadOnlyList<string> lines, Action finished, IReadOnlyList<string> localizedLineKeys = null)
        {
            this.lines = lines ?? Array.Empty<string>();
            lineKeys = localizedLineKeys ?? Array.Empty<string>();
            this.finished = finished;
            Root = template.CloneTree();
            Root.AddToClassList("eov-meeting");
            theme.Apply(Root);ToolkitGameplay.Apply(Root,theme);
            Root.Q("portrait-frame").AddToClassList("surface");
            Root.Q("portrait-shadow").style.display=DisplayStyle.None;
            Root.Q("button-shadow").style.display=DisplayStyle.None;
            var image = Root.Q<Image>("portrait");
            image.sprite = portrait;
            Root.EnableInClassList("meeting-no-portrait", !portrait);
            Root.Q("portrait-frame").style.display = portrait ? DisplayStyle.Flex : DisplayStyle.None;
            image.scaleMode = ScaleMode.ScaleToFit;
            // Character frames include animation padding. Keep the authored pixel scale
            // consistent instead of shrinking a padded 64px frame into a 32px square.
            if (portrait)
            {
                image.style.width = portrait.rect.width;
                image.style.height = portrait.rect.height;
                image.style.flexShrink = 0;
            }
            dialogueFrame = Root.Q("dialogue-frame");
            dialogueFrame.AddToClassList("surface");
            
            dialogue = Root.Q<Label>("dialogue");
            AdvanceButton = Root.Q<Button>("advance");
            if (!portrait) AdvanceButton.style.top = 0;
            AdvanceButton.AddToClassList("button");
            meetLabel = Root.Q<Label>("meet-label");
            nextLabel = Root.Q<Label>("next-label");
            closeLabel = Root.Q<Label>("close-label");
            // Stable keys are shared by every encounter; dialogue still comes from the task assets.
            meet = new ToolkitTextBinding(meetLabel, definition.meet);
            next = new ToolkitTextBinding(nextLabel, definition.next);
            close = new ToolkitTextBinding(closeLabel, definition.close);
            AdvanceButton.clicked += Advance;
            portraitFrame = Root.Q("portrait-frame");
            portraitColumn = new VisualElement { name = "meeting-portrait-column" };
            portraitColumn.AddToClassList("meeting-portrait-column");
            contentColumn = new VisualElement { name = "meeting-content-column" };
            contentColumn.AddToClassList("meeting-content-column");
            dialogueScroll = new ScrollView(ScrollViewMode.Vertical) { name = "meeting-dialogue-scroll" };
            dialogueScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            dialogueScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            ToolkitGameplay.StyleScroll(dialogueScroll);
            ToolkitLocalization.Changed += Refresh;
            Refresh();
        }

        public void LayoutPortrait(bool portraitMode)
        {
            if (portraitLayout == portraitMode) return;
            if (!portraitLayout.HasValue && !portraitMode) { portraitLayout = false; return; }
            portraitLayout = portraitMode;
            var portraitImage = portraitFrame.Q<Image>("portrait");
            if (portraitImage.sprite)
            {
                var pixelScale = portraitMode ? 2 : 1;
                portraitImage.style.width = portraitImage.sprite.rect.width * pixelScale;
                portraitImage.style.height = portraitImage.sprite.rect.height * pixelScale;
            }
            if (portraitMode)
            {
                Root.Insert(0, portraitColumn);
                Root.Add(contentColumn);
                portraitColumn.Add(portraitFrame);
                contentColumn.Add(dialogueFrame);
                contentColumn.Add(AdvanceButton);
                dialogueFrame.Add(dialogueScroll);
                dialogueScroll.Add(Root.Q("dialogue-inset"));
            }
            else
            {
                dialogueFrame.Add(dialogueScroll.Q("dialogue-inset"));
                dialogueScroll.RemoveFromHierarchy();
                Root.Add(portraitFrame);
                Root.Add(dialogueFrame);
                Root.Add(AdvanceButton);
                portraitColumn.RemoveFromHierarchy();
                contentColumn.RemoveFromHierarchy();
            }
            Refresh();
        }

        public void FitPortrait(float availableHeight)
        {
            var noPortrait = Root.ClassListContains("meeting-no-portrait");
            var safe = ToolkitResponsiveLayout.SafeScreenArea;
            Root.style.width = safe.width - 16;
            Root.style.left = safe.xMin + 8;
            var height = noPortrait ? 76 : 96;
            if (AwaitingMeet)
            {
                Root.style.height = height;
                Root.style.top = safe.yMax - ToolkitResponsiveLayout.WindowBottom - height - 8;
                return;
            }
            // Measure after layout so localization and the actual available text width
            // determine the card height. Long entries scroll above a fixed action.
            var width = dialogue.resolvedStyle.width;
            if (!float.IsFinite(width) || width <= 0) return;
            var textHeight = dialogue.MeasureTextSize(dialogue.text, width, VisualElement.MeasureMode.Exactly,
                0, VisualElement.MeasureMode.Undefined).y;
            var minimum = noPortrait ? 76 : 96;
            height = Mathf.RoundToInt(Mathf.Clamp(Mathf.Ceil(textHeight) + 64, minimum, availableHeight));
            Root.style.height = height;
            Root.style.top = safe.yMax - ToolkitResponsiveLayout.WindowBottom - height - 8;
        }

        public void Advance()
        {
            if (disposed || completed) return;
            Audio.AudioManager.Instance?.PlayUIButtonClick();
            if (index >= 0 && index >= lines.Count - 1)
            {
                completed = true;
                AdvanceButton.SetEnabled(false);
                finished?.Invoke();
                return;
            }
            index++;
            Refresh();
        }

        private void Refresh()
        {
            var started = index >= 0;
            Root.EnableInClassList("meeting-awaiting", !started);
            if (dialogueScroll != null) dialogueScroll.scrollOffset = Vector2.zero;
            // Reserve the dialogue space before meeting so the action stays at bottom-right.
            dialogueFrame.style.display = started || portraitLayout == true ? DisplayStyle.Flex : DisplayStyle.None;
            var text = started && index < lines.Count ? (index < lineKeys.Count && !string.IsNullOrEmpty(lineKeys[index])
                ? ToolkitLocalization.Text(lineKeys[index], lines[index]) : lines[index]) : string.Empty;
            dialogue.text = ToolkitResponsiveLayout.IsPortrait ? text : "<b><smallcaps>" + text + "</smallcaps></b>";
            meetLabel.style.display = !started ? DisplayStyle.Flex : DisplayStyle.None;
            nextLabel.style.display = started && index < lines.Count - 1 ? DisplayStyle.Flex : DisplayStyle.None;
            closeLabel.style.display = started && index >= lines.Count - 1 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            AdvanceButton.clicked -= Advance;
            ToolkitLocalization.Changed -= Refresh;
            meet.Dispose(); next.Dispose(); close.Dispose();
            Root.RemoveFromHierarchy();
        }
    }
}
