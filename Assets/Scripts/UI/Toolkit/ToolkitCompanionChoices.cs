using TimelessEchoes.NPC;
using TimelessEchoes.Quests;
using TimelessEchoes.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed class ToolkitCompanionChoices : VisualElement
    {
        private readonly Image[] previews;
        private readonly Button[] choices;
        private readonly ToolkitLockIcon[] locks;
        private readonly Button unlock;
        private readonly ShopService shop;
        private static readonly string[] CatNames = { "Original", "White", "Ginger", "Brown", "Silver" };
        private static readonly string[] DogNames = { "Tan", "Grey", "Slate", "Brown", "Black" };

        internal static bool QuestComplete => QuestUtils.QuestCompleted(GameManager.Instance ? GameManager.Instance.mildredQuestId : "Mildred");

        public ToolkitCompanionChoices(VisualElement header, bool preview = false, System.Action returnToShop = null)
        {
            var colours = MildredAppearance.Colours;
            if (!colours) return;
            void OpenShop()
            {
                if (returnToShop != null) returnToShop();
                else Object.FindAnyObjectByType<ToolkitNavigationScreen>()?.ShowCompanionShop();
            }
            if (!preview)
            {
                shop = ShopService.Ensure();
                unlock = ToolkitGameplay.B(header, "", OpenShop, "companion-unlock");
                unlock.name = "companion-unlock";
            }
            else
            {
                var hint = ToolkitGameplay.L(this, "", "muted companion-preview-hint");
                ToolkitLocalization.Bind(hint, "companion.quest-lock", "Complete Mildred’s quest to choose a companion.");
            }
            previews = new Image[colours.ChoiceCount];
            choices = new Button[colours.ChoiceCount];
            locks = new ToolkitLockIcon[colours.ChoiceCount];
            VisualElement row = null;
            for (var i = 0; i < choices.Length; i++)
            {
                if (i % MildredColours.Count == 0) row = ToolkitGameplay.E(this, "row mildred-choices");
                var choice = i;
                var button = choices[i] = ToolkitGameplay.B(row, "", () =>
                {
                    if (preview) return;
                    if (MildredAppearance.IsUnlocked(choice)) MildredAppearance.Select(choice);
                    else OpenShop();
                    Refresh();
                }, "mildred-choice");
                button.name = "companion-choice-" + i;
                var lockedBackground = ToolkitGameplay.E(button, "companion-locked-background");
                lockedBackground.pickingMode = PickingMode.Ignore;
                previews[i] = ToolkitGameplay.Icon(button, colours.PreviewFrames(i)[0], 48);
                var label = ToolkitGameplay.L(button, "", "mildred-colour-name");
                if (i < MildredColours.Count)
                    ToolkitLocalization.Bind(label, "mildred.colours." + CatNames[i].ToLowerInvariant(), CatNames[i]);
                else
                {
                    var colour = DogNames[i - MildredColours.Count];
                    ToolkitLocalization.Bind(label, "companion.dog." + colour.ToLowerInvariant(), colour);
                }
                locks[i] = new ToolkitLockIcon();
                locks[i].AddToClassList("companion-lock");
                button.Add(locks[i]);
                if (preview) button.SetEnabled(false);
            }
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (!preview && !TimelessEchoes.Platform.StartupDiagnostics.EditorScreenshotSession)
                {
                    shop.Purchases.Connect();
                    shop.GetComponent<LegacyShopOwner>()?.Check();
                }
            });
            if (!preview) schedule.Execute(Refresh).Every(83);
            Refresh();

            void Refresh()
            {
                if (unlock != null)
                {
                    unlock.style.display = shop.CosmeticsOwned ? DisplayStyle.None : DisplayStyle.Flex;
                    unlock.text = ToolkitLocalization.Text("companion.unlock-all", "Unlock all · {0}", shop.Purchases.Price(shop.Settings.cosmeticProductId) ?? "…");
                }
                for (var i = 0; i < choices.Length; i++)
                {
                    var frames = colours.PreviewFrames(i);
                    var count = i < MildredColours.Count ? Mathf.Min(20, frames.Length) : frames.Length;
                    if (count > 0) previews[i].sprite = frames[preview ? 0 : Mathf.FloorToInt(Time.unscaledTime * 12) % count];
                    choices[i].EnableInClassList("active", !preview && i == MildredAppearance.Selected);
                    var locked = preview || !MildredAppearance.IsUnlocked(i);
                    choices[i].EnableInClassList("companion-locked", locked);
                    locks[i].style.display = locked ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }
    }
}
