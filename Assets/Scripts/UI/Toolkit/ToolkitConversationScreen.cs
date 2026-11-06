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
        private VisualElement root, frame;
        private GameData owner;
        private string npcId;
        private float preferredHeight;
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
            ToolkitGameplay.B(root, "", Hide, "mildred-dismiss");
            frame = ToolkitGameplay.E(root, "surface conversation-dialog");
            var header = ToolkitGameplay.E(frame, "row between mildred-header");
            var name = ToolkitGameplay.L(header, "", "heading");
            ToolkitLocalization.Bind(name, "npc.name." + npcId,
                npcId == "Farmers" ? "Flora and Tillman" : npcId == "OldTimer" ? "The Old Timer" : npcId);
            var close = ToolkitGameplay.B(header, "×", Hide, "mildred-close"); close.name = "conversations-close";
            var scroll = ToolkitGameplay.Scroll(frame, "conversation-scroll");
            ToolkitGameplay.StyleScroll(scroll);
            var companionQuest = GameManager.Instance ? GameManager.Instance.mildredQuestId : "Mildred";
            if (npcId == "Eva" && owner.Quests.TryGetValue(companionQuest, out var petQuest) && petQuest.Completed)
            {
                var companionHeader = ToolkitGameplay.E(scroll, "row companion-header conversation-section");
                var label = ToolkitGameplay.L(companionHeader, "", "heading");
                ToolkitLocalization.Bind(label, "companion.title", "Companion");
                scroll.Add(new ToolkitCompanionChoices(companionHeader));
            }
            var heading = ToolkitGameplay.L(scroll, "", "heading conversation-section");
            ToolkitLocalization.Bind(heading, "conversations.title", "Conversations");
            var entries = catalog.entries.Where(e => WorldSpeakerAnchor.Canonical(e.npcId) == npcId).ToArray();
            preferredHeight = 90 + entries.Length * 40 + (scroll.Q<ToolkitCompanionChoices>() != null ? 220 : 0);
            foreach (var entry in entries)
            {
                var known = owner.HeardConversations.ContainsKey(entry.id);
                var button = ToolkitGameplay.B(scroll, "", () => Play(entry), "conversation-choice");
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
            root.RegisterCallback<KeyDownEvent>(evt => { if (evt.keyCode == KeyCode.Escape) { Hide(); evt.StopPropagation(); } });
            Layout();
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
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Hide(); return; }
            Layout();
        }
        private void Layout()
        {
            if (frame == null) return;
            var area = ToolkitResponsiveLayout.SafeScreenArea;
            frame.style.width = Mathf.Min(360, area.width - 20);
            frame.style.height = Mathf.Min(preferredHeight, area.height - 28);
            frame.style.left = area.center.x;
            frame.style.top = area.center.y;
        }

        public void Hide()
        {
            if (!IsOpen && !panel) return;
            blockThroughFrame = Time.frameCount + 1;
            if (replay) Destroy(replay.gameObject);
            replay = null;
            root?.RemoveFromHierarchy(); root = null; frame = null;
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref panel);
            if (active == this) active = null;
        }
        private void OnDisable() => Hide();
    }
}
