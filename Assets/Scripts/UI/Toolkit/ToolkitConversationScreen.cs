using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Conversations;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitConversationScreen : MonoBehaviour
    {
        private static ToolkitConversationScreen active;
        private static int blockThroughFrame;
        private ToolkitTheme theme;
        private ThemeStyleSheet runtimeTheme;
        private PanelTextSettings textSettings;
        private PanelSettings panel;
        private VisualElement root, stack;
        private GameData owner;
        private string npcId;
        private bool evaLesson;
        private ToolkitMeetingScreen replay;
        public static bool BlocksInput => active && (active.root != null || active.replay) || Time.frameCount <= blockThroughFrame;
        public bool IsOpen => root != null || replay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { active = null; blockThroughFrame = -1; }

        public void Configure(ToolkitTheme theme, ThemeStyleSheet runtimeTheme, PanelTextSettings textSettings)
        { this.theme = theme; this.runtimeTheme = runtimeTheme; this.textSettings = textSettings; }

        public void Show(string speaker)
        {
            var catalog = ConversationCatalog.Load();
            if (!catalog || !theme || Oracle.oracle?.saveData == null) return;
            if (active && active != this) active.Hide();
            npcId = WorldSpeakerAnchor.Canonical(speaker);
            owner = Oracle.oracle.saveData;
            evaLesson = npcId == "Eva" && owner.General.EvaConversationPending;
            ConversationCatalog.EnsureHistory(owner);
            active = this;
            if (root != null) root.RemoveFromHierarchy();
            if (!panel) panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings);
            panel.sortingOrder = 300;
            var document = GetComponent<UIDocument>(); document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "npc-conversations" };
            theme.Apply(root); ToolkitGameplay.Apply(root, theme);
            root.styleSheets.Add(Resources.Load<StyleSheet>("UI/MildredColours"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Conversations"));
            root.AddToClassList("conversations");
            document.rootVisualElement.Add(root);
            ToolkitGameplay.B(root, "", () => { if (!evaLesson) Hide(); }, "mildred-dismiss");
            stack = ToolkitGameplay.E(root, "conversation-stack");
            var isEva = npcId == "Eva";
            var preview = isEva && !ToolkitCompanionChoices.QuestComplete;
            var entries = catalog.entries.Where(e => WorldSpeakerAnchor.Canonical(e.npcId) == npcId).ToArray();
            if (evaLesson) AddLesson(entries.FirstOrDefault(entry => entry.introduction)?.introduction);
            var frame = ToolkitGameplay.E(stack, "surface conversation-dialog");
            var header = ToolkitGameplay.E(frame, "row between mildred-header");
            var name = ToolkitGameplay.L(header, "", "heading");
            ToolkitLocalization.Bind(name, "npc.name." + npcId,
                npcId == "Farmers" ? "Flora and Tillman" : npcId == "OldTimer" ? "The Old Timer" : npcId);
            var close = ToolkitGameplay.B(header, "×", Close, evaLesson ? "primary conversation-tutorial-close" : "mildred-close");
            close.name = "conversations-close";
            if (evaLesson) ToolkitLocalization.Bind(close, "common.close", "Close");
            var scroll = ToolkitGameplay.Scroll(frame, "conversation-scroll");
            ToolkitGameplay.StyleScroll(scroll);
            scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            var columns = ToolkitGameplay.E(scroll, "conversation-columns");
            var history = ToolkitGameplay.E(columns, "conversation-history");
            var heading = ToolkitGameplay.L(history, "", "heading conversation-section");
            ToolkitLocalization.Bind(heading, "conversations.title", "Conversations");
            var historyEntries = preview ? entries.OrderByDescending(entry => owner.HeardConversations.ContainsKey(entry.id))
                : entries.AsEnumerable();
            foreach (var entry in historyEntries) AddEntry(entry);
            if (isEva)
            {
                columns.AddToClassList("with-companions");
                var companions = ToolkitGameplay.E(columns, "conversation-companions");
                var companionHeader = ToolkitGameplay.E(companions, "row companion-header conversation-section");
                var label = ToolkitGameplay.L(companionHeader, "", "heading");
                ToolkitLocalization.Bind(label, "companion.title", "Companion");
                companions.Add(new ToolkitCompanionChoices(companionHeader, preview));
            }
            root.RegisterCallback<KeyDownEvent>(evt => { if (evt.keyCode == KeyCode.Escape) { Close(); evt.StopPropagation(); } });
            Layout();

            void AddEntry(ConversationCatalog.Entry entry)
            {
                var known = owner.HeardConversations.ContainsKey(entry.id);
                var button = ToolkitGameplay.B(history, "", () => Play(entry), "conversation-choice");
                button.name = "conversation-" + entry.id;
                button.SetEnabled(known);
                var label = ToolkitGameplay.L(button, "");
                ToolkitLocalization.BindValue(label, () => label.text = known ? entry.Title : ToolkitLocalization.Text("conversations.locked", "Locked"));
                if (!known)
                {
                    button.AddToClassList("conversation-locked");
                    button.Insert(0, new ToolkitLockIcon());
                }
            }
        }

        private void AddLesson(ToolkitNpcIntroduction introduction)
        {
            var card = ToolkitGameplay.E(stack, "surface conversation-lesson-card");
            if (introduction && introduction.portrait)
                ToolkitGameplay.Icon(card, introduction.portrait, 32).AddToClassList("conversation-lesson-portrait");
            var dialogue = ToolkitGameplay.E(card, "conversation-lesson-dialogue");
            var speaker = ToolkitGameplay.L(dialogue, "", "strong");
            ToolkitLocalization.Bind(speaker, "npc.name.Eva", "Eva");
            var lesson = ToolkitGameplay.L(dialogue, "", "conversation-lesson");
            ToolkitLocalization.Bind(lesson, "intro.Eva.conversations",
                "Tap people in town to revisit past conversations. Close this panel to continue.");
        }

        private void Play(ConversationCatalog.Entry entry)
        {
            if (owner != Oracle.oracle?.saveData || !owner.HeardConversations.TryGetValue(entry.id, out var orchard)) return;
            root.RemoveFromHierarchy(); root = null;
            replay = entry.Replay(orchard, WorldSpeakerAnchor.Find(npcId), () =>
            {
                replay = null;
                if (owner == Oracle.oracle?.saveData) Show(npcId);
            });
        }

        private void Update()
        {
            if (owner != Oracle.oracle?.saveData || GameManager.Instance && !GameManager.Instance.IsInTown ||
                Characters.CharacterCreation.Required || TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow)
            { Hide(); return; }
            if (root == null) return;
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Close(); return; }
            Layout();
        }
        private void Layout()
        {
            if (stack == null) return;
            var area = ToolkitResponsiveLayout.SafeScreenArea;
            var desktop = !ToolkitResponsiveLayout.IsPortrait && area.width >= 410;
            root.EnableInClassList("conversation-desktop", desktop);
            root.EnableInClassList("conversation-compact-stack", !desktop && area.height < 540);
            var width = desktop ? (npcId == "Eva" ? 440 : 260) : 360;
            stack.style.width = Mathf.Min(width, area.width - 20);
            stack.style.maxHeight = area.height - 28;
            stack.style.left = area.center.x;
            stack.style.top = area.center.y;
        }

        private void Close()
        {
            if (evaLesson && owner == Oracle.oracle?.saveData)
            {
                owner.General.EvaConversationPending = false;
                Oracle.oracle.RequestCheckpointSave();
            }
            Hide();
        }

        public void Hide()
        {
            if (!IsOpen && !panel) return;
            blockThroughFrame = Time.frameCount + 1;
            if (replay) Destroy(replay.gameObject);
            replay = null;
            evaLesson = false;
            root?.RemoveFromHierarchy(); root = null; stack = null;
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref panel);
            if (active == this) active = null;
        }
        private void OnDisable() => Hide();
    }
}
