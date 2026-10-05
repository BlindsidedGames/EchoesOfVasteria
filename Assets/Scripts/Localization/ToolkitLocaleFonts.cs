using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    // Keep Latin typography, with locale-specific CJK forms and live refresh on attached roots.
    public static class ToolkitLocaleFonts
    {
        private static readonly ConditionalWeakTable<VisualElement, Binding> Bindings = new();
        private static FontAsset chinese, japanese;
        private static TMPro.TMP_FontAsset chineseTmp, japaneseTmp;
        public static void Bind(VisualElement root, Font latin)
        {
            var binding = Bindings.GetValue(root, element => new Binding(element));
            binding.Latin = latin;
            binding.Refresh();
        }
        private sealed class Binding
        {
            private readonly VisualElement root;
            public Font Latin;
            public Binding(VisualElement element)
            {
                root = element;
                root.RegisterCallback<AttachToPanelEvent>(_ => { ToolkitLocalization.Changed += Refresh; Refresh(); });
                root.RegisterCallback<DetachFromPanelEvent>(_ => ToolkitLocalization.Changed -= Refresh);
                if (root.panel != null) ToolkitLocalization.Changed += Refresh;
            }
            public void Refresh()
            {
                var selected = LocalizationSettings.SelectedLocaleAsync;
                var code = selected.IsDone ? selected.Result?.Identifier.Code : null;
                FontAsset preferred = null;
                if (code == "zh-CN") preferred = chinese ? chinese : chinese = Resources.Load<FontAsset>("Fonts/Localization/NotoCJK-sc");
                if (code == "ja") preferred = japanese ? japanese : japanese = Resources.Load<FontAsset>("Fonts/Localization/NotoCJK-jp");
                if (preferred)
                {
                    if (!chineseTmp) chineseTmp = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Localization/NotoCJK-sc TMP");
                    if (!japaneseTmp) japaneseTmp = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Localization/NotoCJK-jp TMP");
                    var fallbacks = TMPro.TMP_Settings.fallbackFontAssets;
                    var first = code == "ja" ? japaneseTmp : chineseTmp;
                    var second = code == "ja" ? chineseTmp : japaneseTmp;
                    if (first && second && fallbacks.IndexOf(first) > fallbacks.IndexOf(second))
                    {
                        fallbacks.Remove(first); fallbacks.Remove(second);
                        fallbacks.Add(first); fallbacks.Add(second);
                    }
                }
                root.style.unityFontDefinition = preferred ? FontDefinition.FromSDFFont(preferred) : FontDefinition.FromFont(Latin);
            }
        }
    }
}
