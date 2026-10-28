using System;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Owns the localization subscription for exactly one visible text element.</summary>
    public sealed class ToolkitTextBinding : IDisposable
    {
        private readonly TextElement target;
        private readonly LocalizedString localized;
        private readonly string fallback;
        private readonly bool bold, smallCaps;
        private readonly Func<string, string> format;
        private bool disposed;

        public ToolkitTextBinding(TextElement target, ToolkitBookDefinition.Text text, Func<string, string> format = null)
        {
            this.target = target;
            this.format = format;
            fallback = text?.fallback ?? string.Empty;
            bold = text?.bold ?? false;
            smallCaps = text?.smallCaps ?? false;
            target.enableRichText = true;
            Refresh(fallback);
            localized = text?.localized;
            // Semantic keys in the authored Toolkit definitions belong to the existing
            // TownUI collection. Preserve explicit table references (for example Wiki).
            if ((localized == null || localized.IsEmpty) && !string.IsNullOrEmpty(text?.key))
                localized = new LocalizedString("TownUI", text.key);
            if (localized != null && !localized.IsEmpty) localized.StringChanged += Refresh;
        }

        private void Refresh(string value)
        {
            if (disposed) return;
            var display = string.IsNullOrEmpty(value) ? fallback : value;
            if (format != null) display = format(display);
            if (smallCaps) display = "<smallcaps>" + display + "</smallcaps>";
            if (bold) display = "<b>" + display + "</b>";
            target.text = display;
            if (display.Contains("<link=\"help:")) ToolkitHelpLinks.Attach(target);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (localized != null && !localized.IsEmpty) localized.StringChanged -= Refresh;
        }
    }
}
