using System;
using System.Collections.Generic;
using TimelessEchoes.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitShopScreen : MonoBehaviour
    {
        private ToolkitTheme theme;
        private ThemeStyleSheet runtimeTheme;
        private PanelTextSettings textSettings;
        private PanelSettings settings;
        private ShopService shop;
        private VisualElement root, modal;
        private Label boostStatus, storeStatus;
        private Button claim, permanent;
        private readonly List<(Button button, string id)> buyButtons = new();
        private readonly ToolkitWindowLayout layout = new();
        private Texture2D bannerTexture;
        private int imageRevision;
        private float nextRefresh;
        public bool IsOpen => root != null;
        public bool IsConfigured => theme && runtimeTheme && textSettings;
        public void Configure(ToolkitTheme theme, ThemeStyleSheet runtimeTheme, PanelTextSettings textSettings)
        { this.theme = theme; this.runtimeTheme = runtimeTheme; this.textSettings = textSettings; }
        private static Label Text(VisualElement parent, string value, string classes = "")
        { var label = ToolkitGameplay.L(parent, value, classes); label.enableRichText = false; return label; }
        public bool Show()
        {
            if (IsOpen) return true;
            shop = ShopService.Ensure();
            if (!IsConfigured || !shop.Unlocked) return false;
            shop.Promotions.Initialize(); shop.Promotions.RefreshCatalog(); shop.Purchases.Connect();
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "shop" }; theme.Apply(root); ToolkitGameplay.Apply(root, theme);
            root.AddToClassList("menu-surface"); root.AddToClassList("shop-ui"); root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Shop"));
            document.rootVisualElement.Add(root);
            var header = ToolkitGameplay.E(root, "row shop-header"); Text(header, "Shop", "heading");
            var scroll = ToolkitControls.RecessedScroll(root, "shop-body", theme, compact: true);
            var offers = ToolkitGameplay.E(scroll, "row shop-offers");
            var boost = ToolkitGameplay.E(offers, "shop-card");
            Text(boost, "Gathering bonus", "heading");
            Text(boost, "+50% resources from adventures", "shop-description");
            boostStatus = Text(boost, "", "muted");
            var actions = ToolkitGameplay.E(boost, "row shop-actions");
            claim = ToolkitGameplay.B(actions, "Activate · 30 min", OpenPromotion, "primary"); claim.name = "shop-claim";
            ToolkitGameplay.B(actions, "More games", OpenGames);
            var purchases = ToolkitGameplay.E(offers, "shop-card");
            Text(purchases, "Permanent gathering bonus", "heading");
            Text(purchases, "+50% adventure resources on every save. No top-ups needed.", "shop-description");
            permanent = ToolkitGameplay.B(purchases, "", () => { if (shop.PermanentOwned) shop.TogglePermanent(); else shop.Purchases.Buy(shop.Settings.permanentProductId); });
            permanent.name = "shop-permanent";
            Text(scroll, "Support the developer", "heading shop-section-heading");
            Text(scroll, "Optional support. No extra gameplay bonuses.", "muted shop-description");
            var supporters = ToolkitGameplay.E(scroll, "row shop-supporters");
            var titles = new[] { "Small thanks", "Big thanks", "Huge thanks" };
            for (var i = 0; i < shop.Settings.supporterProductIds.Length; i++)
            {
                var id = shop.Settings.supporterProductIds[i]; var card = ToolkitGameplay.E(supporters, "shop-card shop-supporter");
                Text(card, titles[i], "shop-product-title");
                var button = ToolkitGameplay.B(card, "", () => shop.Purchases.Buy(id)); button.name = "shop-supporter-" + i;
                buyButtons.Add((button, id));
            }
            var footer = ToolkitGameplay.E(scroll, "row shop-actions shop-footer");
            if (shop.Purchases.Supported) ToolkitGameplay.B(footer, "Restore purchases", () => shop.Purchases.Restore());
            storeStatus = Text(scroll, "", "muted shop-status");
            shop.Changed += Refresh; shop.Purchases.Changed += Refresh;
            Refresh(); layout.Fill(root, theme); return true;
        }
        private void Refresh()
        {
            if (root == null) return;
            if (!shop.Unlocked) { Hide(); return; }
            var remaining = TimeSpan.FromSeconds(shop.Remaining);
            boostStatus.text = shop.PermanentOwned ? (shop.PermanentEnabled ? "Permanent bonus active" : "Permanent bonus disabled")
                : shop.Remaining > 0 ? $"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00} remaining · Pauses outside adventures"
                : "30 minutes per claim · Bank up to 2 hours";
            claim.text = shop.Remaining > 0 ? "Add 30 min" : "Activate · 30 min";
            claim.SetEnabled(shop.CanClaim); claim.style.display = shop.PermanentOwned ? DisplayStyle.None : DisplayStyle.Flex;
            permanent.text = shop.PermanentOwned ? shop.OriginalOwner ? "Original owner · " + (shop.PermanentEnabled ? "On" : "Off") : "Owned · " + (shop.PermanentEnabled ? "On" : "Off")
                : "Buy · " + (shop.Purchases.Price(shop.Settings.permanentProductId) ?? "Unavailable");
            permanent.SetEnabled(shop.PermanentOwned || shop.Purchases.Available(shop.Settings.permanentProductId));
            permanent.EnableInClassList("primary", shop.PermanentEnabled);
            foreach (var item in buyButtons)
            {
                item.button.text = shop.Purchases.Price(item.id) ?? "Unavailable";
                item.button.SetEnabled(shop.Purchases.Available(item.id));
            }
            storeStatus.text = shop.Purchases.Supported ? shop.Purchases.Status : "Purchases are available on iOS and Android.";
        }
        private string Locale => UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
        private void OpenPromotion()
        {
            if (!shop.CanClaim) return;
            var game = shop.Promotions.Next();
            if (game == null) { shop.Claim(); return; }
            ShowPromotion(game, true);
        }
        private void NewModal(string title)
        {
            CloseModal(); modal = ToolkitGameplay.E(root, "shop-modal"); modal.name = "shop-modal";
            var header = ToolkitGameplay.E(modal, "row shop-header"); Text(header, title, "heading");
            ToolkitGameplay.B(header, "×", CloseModal, "close-control");
        }
        private void OpenGames()
        {
            shop.Promotions.RefreshCatalog(); NewModal("More games");
            var list = ToolkitControls.RecessedScroll(modal, "shop-games", theme, compact: true);
            foreach (var game in shop.Promotions.Games)
            {
                var selected = game; var copy = game.Copy(Locale);
                ToolkitGameplay.B(list, copy.title, () => ShowPromotion(selected, false), "shop-game-link");
            }
        }
        private void ShowPromotion(PromotionGame game, bool reward)
        {
            var copy = game.Copy(Locale); NewModal(copy.title);
            var body = ToolkitControls.RecessedScroll(modal, "shop-promotion", theme, compact: true);
            var image = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; image.AddToClassList("shop-banner"); body.Add(image);
            Text(body, copy.description, "shop-description");
            Text(body, "Game promotion", "muted");
            var buttons = ToolkitGameplay.E(modal, "row shop-actions");
            ToolkitGameplay.B(buttons, "View game", () => Application.OpenURL(game.links[shop.Promotions.Platform]));
            if (reward)
            {
                var button = ToolkitGameplay.B(buttons, "Claim 30 min", () => { if (shop.Claim()) CloseModal(); }, "primary");
                button.name = "shop-promotion-claim"; button.SetEnabled(shop.CanClaim);
            }
            var revision = imageRevision;
            shop.Promotions.StartCoroutine(shop.Promotions.LoadImage(game, texture =>
            {
                if (!IsOpen || revision != imageRevision) { if (texture) Destroy(texture); return; }
                bannerTexture = texture; image.image = texture;
            }));
        }
        private void CloseModal()
        {
            imageRevision++; modal?.RemoveFromHierarchy(); modal = null;
            if (bannerTexture) Destroy(bannerTexture); bannerTexture = null;
        }
        private void Update()
        {
            if (!IsOpen) return;
            layout.Fill(root, theme);
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .25f; Refresh();
        }
        public void Hide()
        {
            CloseModal();
            if (shop) { shop.Changed -= Refresh; shop.Purchases.Changed -= Refresh; }
            root?.RemoveFromHierarchy(); root = null; buyButtons.Clear();
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
