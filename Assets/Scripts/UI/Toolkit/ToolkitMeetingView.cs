using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Native encounter presentation. Rewards remain owned by TalkToNpcTask.</summary>
    public sealed class ToolkitMeetingView : IDisposable
    {
        private readonly IReadOnlyList<string> lines;
        private readonly Action finished;
        private readonly Label dialogue;
        private readonly VisualElement dialogueFrame;
        private readonly ToolkitTextBinding meet, next, close;
        private readonly Label meetLabel, nextLabel, closeLabel;
        private int index = -1;
        private bool completed, disposed;
        public VisualElement Root { get; }
        public Button AdvanceButton { get; }

        public ToolkitMeetingView(ToolkitTheme theme, VisualTreeAsset template, Sprite portrait,
            ToolkitMeetingDefinition definition, IReadOnlyList<string> lines, Action finished)
        {
            this.lines = lines ?? Array.Empty<string>();
            this.finished = finished;
            Root = template.CloneTree();
            Root.AddToClassList("eov-meeting");
            theme.Apply(Root);ToolkitGameplay.Apply(Root,theme);
            Root.Q("portrait-frame").AddToClassList("surface");
            Root.Q("portrait-shadow").style.display=DisplayStyle.None;
            Root.Q("button-shadow").style.display=DisplayStyle.None;
            var image = Root.Q<Image>("portrait");
            image.sprite = portrait;
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
            AdvanceButton.AddToClassList("button");
            meetLabel = Root.Q<Label>("meet-label");
            nextLabel = Root.Q<Label>("next-label");
            closeLabel = Root.Q<Label>("close-label");
            // Stable keys are shared by every encounter; dialogue still comes from the task assets.
            meet = new ToolkitTextBinding(meetLabel, definition.meet);
            next = new ToolkitTextBinding(nextLabel, definition.next);
            close = new ToolkitTextBinding(closeLabel, definition.close);
            AdvanceButton.clicked += Advance;
            Refresh();
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
            dialogueFrame.style.display = started ? DisplayStyle.Flex : DisplayStyle.None;
            dialogue.text = started && index < lines.Count ? "<b><smallcaps>" + lines[index] + "</smallcaps></b>" : string.Empty;
            meetLabel.style.display = !started ? DisplayStyle.Flex : DisplayStyle.None;
            nextLabel.style.display = started && index < lines.Count - 1 ? DisplayStyle.Flex : DisplayStyle.None;
            closeLabel.style.display = started && index >= lines.Count - 1 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            AdvanceButton.clicked -= Advance;
            meet.Dispose(); next.Dispose(); close.Dispose();
            Root.RemoveFromHierarchy();
        }
    }
}
