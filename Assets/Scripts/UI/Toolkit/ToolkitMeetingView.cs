using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        private readonly Label speaker;
        private string speakerKey, speakerFallback;
        private readonly VisualElement dialogueFrame;
        private readonly ToolkitTextBinding meet, next, close;
        private readonly Label meetLabel, nextLabel, closeLabel;
        private readonly VisualElement portraitColumn, portraitFrame, contentColumn;
        private readonly ScrollView dialogueScroll;
        private bool? portraitLayout;
        private float portraitPixelScale;
        private string finalLabelKey, finalLabelFallback;
        public Action<int> LineChanged { get; set; }
        private float portraitOffsetY;
        private int index = -1;
        private bool completed, disposed;
        private string navigationShortcut;
        private static readonly Regex HelpRoutes = new("<link\\s*=\\s*[\"']?help:([^\"'\\s>]+)", RegexOptions.IgnoreCase);
        private bool hasTextMeasurement;
        private float measuredTextHeight;
        private (string text, float width, float size, Font font, FontDefinition definition,
            FontStyle weight, float letters, float words, float paragraphs, WhiteSpace whitespace,
            bool richText, float scale) textMeasurementKey;
        public VisualElement Root { get; }
        public Button AdvanceButton { get; }
        public Func<bool> BeforeStart { private get; set; }
        public Func<bool> BeforeAdvance { private get; set; }
        private VisualElement guidedActions;
        private Button skipButton;
        private VisualElement ActionElement => guidedActions ?? (VisualElement)AdvanceButton;

        public void AddSkipAction(Action skip)
        {
            if (guidedActions != null) return;
            Root.AddToClassList("meeting-guided");
            guidedActions = new VisualElement(); guidedActions.AddToClassList("meeting-actions");
            var parent = AdvanceButton.parent;
            parent.Insert(parent.IndexOf(AdvanceButton), guidedActions);
            guidedActions.Add(AdvanceButton);
            skipButton = ToolkitGameplay.B(guidedActions, ToolkitLocalization.Text("meeting.skip", "Skip"), skip, "meeting-skip");
            skipButton.name = "meeting-skip";
        }
        public bool AwaitingMeet => index < 0;

        public void SetNavigationShortcut(string route) => navigationShortcut = route;
        public void SetFinalActionLabel(string key, string fallback)
        { finalLabelKey = key; finalLabelFallback = fallback; Refresh(); }
        public void SetPortrait(Sprite sprite, float pixelScale)
        {
            portraitPixelScale = pixelScale;
            Root.Q<Image>("portrait").sprite = sprite;
        }

        public string FindNavigationRoute(Func<string, bool> matches)
        {
            if (disposed || completed || index < 0 || index != lines.Count - 1 ||
                BeforeAdvance != null || AdvanceButton?.enabledInHierarchy != true) return null;
            if (!string.IsNullOrEmpty(navigationShortcut) && matches(navigationShortcut)) return navigationShortcut;
            foreach (Match link in HelpRoutes.Matches(dialogue.text ?? string.Empty))
                if (matches(link.Groups[1].Value)) return link.Groups[1].Value;
            return null;
        }

        public bool FollowNavigation(string route)
        {
            var navigation = UnityEngine.Object.FindAnyObjectByType<ToolkitNavigationScreen>();
            if (!navigation || FindNavigationRoute(candidate => candidate == route) == null) return false;
            Advance();
            navigation.FollowHelpAfterDialogue(route);
            return true;
        }

        public ToolkitMeetingView(ToolkitTheme theme, VisualTreeAsset template, Sprite portrait,
            ToolkitMeetingDefinition definition, IReadOnlyList<string> lines, Action finished, IReadOnlyList<string> localizedLineKeys = null, bool continueAfterDialogue = false, float portraitPixelScale = 1)
        {
            this.portraitPixelScale = portraitPixelScale;
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
                image.style.width = portrait.rect.width * portraitPixelScale;
                image.style.height = portrait.rect.height * portraitPixelScale;
                image.style.flexShrink = 0;
            }
            dialogueFrame = Root.Q("dialogue-frame");
            dialogueFrame.AddToClassList("surface");

            dialogue = Root.Q<Label>("dialogue");
            speaker = new Label { name = "meeting-npc-name", pickingMode = PickingMode.Ignore };
            speaker.AddToClassList("meeting-npc-name");
            dialogue.parent.Insert(0, speaker);
            ToolkitHelpLinks.Attach(dialogue, FollowNavigation);
            AdvanceButton = Root.Q<Button>("advance");
            if (!portrait) AdvanceButton.style.top = 0;
            AdvanceButton.AddToClassList("button");
            meetLabel = Root.Q<Label>("meet-label");
            nextLabel = Root.Q<Label>("next-label");
            closeLabel = Root.Q<Label>("close-label");
            // Stable keys are shared by every encounter; dialogue still comes from the task assets.
            meet = new ToolkitTextBinding(meetLabel, definition.meet);
            next = new ToolkitTextBinding(nextLabel, definition.next);
            close = new ToolkitTextBinding(closeLabel, continueAfterDialogue ? definition.next : definition.close);
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

        public void SetSpeaker(string npcId)
        {
            var identity = npcId switch
            {
                "Barkley1" => "Barkley", "Ivan1" => "Ivan", "Gill1" => "Gill",
                "Farmers1" => "Farmers", "OldTimer1" => "OldTimer",
                "Witch1" or "EvaWateringCanIntroduction" => "Eva",
                "Orin" or "OrinHubIntroduction" => "Orin", "Reaper1" => "Carl", "Flora" or "Tillman" => npcId, _ => null
            };
            speakerKey = identity == null ? null : "npc.name." + identity;
            portraitOffsetY = identity == "Carl" ? 8 : 0;
            speakerFallback = identity switch { "Farmers" => "Flora and Tillman", "OldTimer" => "The Old Timer", _ => identity };
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
                var pixelScale = (portraitMode ? 2 : 1) * portraitPixelScale;
                portraitImage.style.width = portraitImage.sprite.rect.width * pixelScale;
                portraitImage.style.height = portraitImage.sprite.rect.height * pixelScale;
            }
            if (portraitMode)
            {
                Root.Insert(0, portraitColumn);
                Root.Add(contentColumn);
                portraitColumn.Add(portraitFrame);
                contentColumn.Add(dialogueFrame);
                contentColumn.Add(ActionElement);
                dialogueFrame.Add(dialogueScroll);
                dialogueScroll.Add(Root.Q("dialogue-inset"));
            }
            else
            {
                dialogueFrame.Add(dialogueScroll.Q("dialogue-inset"));
                dialogueScroll.RemoveFromHierarchy();
                Root.Add(portraitFrame);
                Root.Add(dialogueFrame);
                Root.Add(ActionElement);
                portraitColumn.RemoveFromHierarchy();
                contentColumn.RemoveFromHierarchy();
            }
            Refresh();
        }

        public void FitPortrait(float availableHeight, float navigationClearance = 0)
        {
            var noPortrait = Root.ClassListContains("meeting-no-portrait");
            var safe = ToolkitResponsiveLayout.SafeScreenArea;
            Root.style.width = safe.width - 16;
            Root.style.left = safe.xMin + 8;
            var height = noPortrait ? 76 : 96;
            if (AwaitingMeet)
            {
                Root.style.height = height;
                // Expanded play puts health and abilities at the bottom of the map.
                // Keep an unstarted encounter above that HUD instead of covering it.
                Root.style.top = ToolkitResponsiveLayout.FocusView ? safe.yMin + 12 :
                    safe.yMax - ToolkitResponsiveLayout.WindowBottom - height - 8 - navigationClearance;
                return;
            }
            // Measure after layout so localization and the actual available text width
            // determine the card height. Long entries scroll above a fixed action.
            height = Mathf.RoundToInt(PreferredHeight(availableHeight));
            Root.style.height = height;
            Root.style.top = safe.yMax - ToolkitResponsiveLayout.WindowBottom - height - 8 - navigationClearance;
        }

        // The screen owns placement. This view only reports how much room its
        // full localized conversation needs, with scrolling for unusually long text.
        public float PreferredHeight(float maximumHeight)
        {
            var minimum = Root.ClassListContains("meeting-no-portrait") ? 76 : 96;
            // Reserve the full conversation before Meet, even while its first line is hidden.
            var width = dialogueScroll.contentViewport.resolvedStyle.width;
            if (!float.IsFinite(width) || width <= 0) return Mathf.Min(144, maximumHeight);
            var nameHeight = speaker.resolvedStyle.display == DisplayStyle.None ? 0 : speaker.resolvedStyle.height + 4;
            var textHeight = MeasureDialogueHeight(width) + (float.IsFinite(nameHeight) ? nameHeight : 16);
            // Include the card borders and text/scroll rounding, so a short second
            // line never needs scrolling just to reveal its final few pixels.
            return Mathf.Clamp(Mathf.Ceil(textHeight) + (guidedActions != null ? 116 : 76), Mathf.Min(minimum, maximumHeight), maximumHeight);
        }

        public void LayoutBottomDialogue(bool portraitMode)
        {
            // Share one flow layout; typography can still respond to orientation.
            LayoutPortrait(true);
            Root.style.flexDirection = FlexDirection.Row;
            Root.style.alignItems = Align.Stretch;
            Root.style.paddingLeft = Root.style.paddingRight = 12;
            Root.style.paddingTop = Root.style.paddingBottom = 12;
            Root.style.borderLeftWidth = Root.style.borderRightWidth = Root.style.borderTopWidth = Root.style.borderBottomWidth = 1;
            Root.style.backgroundColor = ToolkitColorTheme.Surface;
            Root.style.borderLeftColor = Root.style.borderRightColor = Root.style.borderTopColor = Root.style.borderBottomColor = ToolkitColorTheme.HudBorder;
            portraitColumn.style.width = 56;
            portraitColumn.style.marginRight = 12;
            portraitColumn.style.flexShrink = 0;
            portraitColumn.style.display = Root.ClassListContains("meeting-no-portrait") ? DisplayStyle.None : DisplayStyle.Flex;
            contentColumn.style.flexGrow = 1;
            contentColumn.style.flexShrink = 1;
            contentColumn.style.minWidth = contentColumn.style.minHeight = 0;
            portraitFrame.style.position = dialogueFrame.style.position = Position.Relative;
            portraitFrame.style.left = portraitFrame.style.top = dialogueFrame.style.left = dialogueFrame.style.top = StyleKeyword.Auto;
            portraitFrame.style.width = 56;
            portraitFrame.style.height = 96;
            // Animated artwork may extend beyond its column. Only the outer
            // dialogue border clips it, never a smaller invisible portrait box.
            Root.style.overflow = Overflow.Hidden;
            portraitColumn.style.overflow = portraitFrame.style.overflow = Overflow.Visible;
            portraitFrame.style.paddingLeft = portraitFrame.style.paddingRight = 0;
            portraitFrame.style.paddingTop = portraitFrame.style.paddingBottom = 0;
            portraitFrame.style.backgroundColor = dialogueFrame.style.backgroundColor = Color.clear;
            portraitFrame.style.borderLeftWidth = portraitFrame.style.borderRightWidth = portraitFrame.style.borderTopWidth = portraitFrame.style.borderBottomWidth = 0;
            dialogueFrame.style.borderLeftWidth = dialogueFrame.style.borderRightWidth = dialogueFrame.style.borderTopWidth = dialogueFrame.style.borderBottomWidth = 0;
            dialogueFrame.style.width = StyleKeyword.Auto;
            dialogueFrame.style.minWidth = dialogueFrame.style.minHeight = 0;
            dialogueFrame.style.flexGrow = dialogueFrame.style.flexShrink = 1;
            dialogueFrame.style.paddingLeft = dialogueFrame.style.paddingRight = dialogueFrame.style.paddingTop = dialogueFrame.style.paddingBottom = 0;
            var inset = Root.Q("dialogue-inset");
            inset.style.paddingLeft = inset.style.paddingRight = inset.style.paddingTop = inset.style.paddingBottom = 0;
            dialogueScroll.style.flexGrow = 1;
            dialogueScroll.style.minHeight = 0;
            ActionElement.style.position = Position.Relative;
            ActionElement.style.left = ActionElement.style.top = StyleKeyword.Auto;
            ActionElement.style.alignSelf = Align.FlexEnd;
            ActionElement.style.flexShrink = 0;
            ActionElement.style.marginTop = 8;
            ActionElement.style.width = guidedActions != null ? 96 : 76;
            AdvanceButton.style.minHeight = 32;
            AdvanceButton.style.height = guidedActions != null ? StyleKeyword.Auto : 32;
            AdvanceButton.style.position = Position.Relative;
            AdvanceButton.style.left = AdvanceButton.style.top = StyleKeyword.Auto;
            AdvanceButton.style.marginTop = guidedActions != null ? 0 : 8;
            var image = portraitFrame.Q<Image>("portrait");
            if (image.sprite)
            {
                // One dialogue pixel scale across characters and orientations,
                // independent of world PPU. Preserve sheet padding and relative
                // character sizes rather than fitting silhouettes to the column.
                var scale = (5f / 3f) * portraitPixelScale;
                image.style.width = image.sprite.rect.width * scale;
                image.style.height = image.sprite.rect.height * scale;
                image.style.translate = new Translate(0, portraitOffsetY);
            }
        }

        private float MeasureDialogueHeight(float width)
        {
            var style = dialogue.resolvedStyle;
            var scale = ToolkitResponsiveLayout.Scale(new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height));
            var conversation = string.Join("\n", System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0, lines.Count), FormattedLine));
            var key = (conversation, width, style.fontSize, style.unityFont, style.unityFontDefinition,
                style.unityFontStyleAndWeight, style.letterSpacing, style.wordSpacing,
                style.unityParagraphSpacing, style.whiteSpace, dialogue.enableRichText, scale);
            if (!hasTextMeasurement || !key.Equals(textMeasurementKey))
            {
                measuredTextHeight = 0;
                for (var i = 0; i < lines.Count; i++)
                    measuredTextHeight = Mathf.Max(measuredTextHeight, dialogue.MeasureTextSize(FormattedLine(i), width,
                        VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y);
                textMeasurementKey = key;
                hasTextMeasurement = true;
            }
            return measuredTextHeight;
        }

        private string FormattedLine(int line)
        {
            var text = line < lineKeys.Count && !string.IsNullOrEmpty(lineKeys[line])
                ? ToolkitLocalization.Text(lineKeys[line], lines[line]) : lines[line];
            return ToolkitResponsiveLayout.IsPortrait ? text : "<b><smallcaps>" + text + "</smallcaps></b>";
        }

        public void Advance()
        {
            if (disposed || completed) return;
            if (index < 0 && BeforeStart?.Invoke() == false) return;
            if (index >= 0 && BeforeAdvance?.Invoke() == true) return;
            Audio.AudioManager.Instance?.PlayUIButtonClick();
            if (index >= 0 && index >= lines.Count - 1)
            {
                completed = true;
                AdvanceButton.SetEnabled(false);
                finished?.Invoke();
                return;
            }
            index++;
            LineChanged?.Invoke(index);
            Refresh();
        }

        private void Refresh()
        {
            if (finalLabelKey != null) closeLabel.text = ToolkitLocalization.Text(finalLabelKey, finalLabelFallback);
            if (skipButton != null) skipButton.text = ToolkitLocalization.Text("meeting.skip", "Skip");
            hasTextMeasurement = false; // Locale/font fallback refreshes can change metrics without changing the text.
            var started = index >= 0;
            Root.EnableInClassList("meeting-awaiting", !started);
            if (dialogueScroll != null) dialogueScroll.scrollOffset = Vector2.zero;
            // Reserve the dialogue space before meeting so the action stays at bottom-right.
            speaker.text = speakerKey == null ? "" : ToolkitLocalization.Text(speakerKey, speakerFallback);
            speaker.style.display = string.IsNullOrEmpty(speaker.text) ? DisplayStyle.None : DisplayStyle.Flex;
            dialogueFrame.style.display = started || portraitLayout == true || !string.IsNullOrEmpty(speaker.text) ? DisplayStyle.Flex : DisplayStyle.None;
            dialogue.style.display = started ? DisplayStyle.Flex : DisplayStyle.None;
            dialogue.text = started && index < lines.Count ? FormattedLine(index) : string.Empty;
            ToolkitHelpLinks.Attach(dialogue, FollowNavigation);
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
