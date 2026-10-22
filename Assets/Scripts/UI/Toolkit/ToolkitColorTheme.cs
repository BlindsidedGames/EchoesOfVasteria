using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Device appearance preference, independent of progression and cloud saves.</summary>
    public static class ToolkitColorTheme
    {
        private const string PreferenceKey = "EoV.Interface.LightMode";
        private static readonly List<WeakReference<VisualElement>> Roots = new();
        private static bool? light;
        private static StyleSheet sheet;
        public static event Action Changed;
        public static bool IsLight => light ??= PlayerPrefs.GetInt(PreferenceKey, 0) == 1;
        public static Color Surface => IsLight ? new Color32(213, 171, 135, 255) : new Color32(41, 36, 38, 255);
        public static Color HudSurface => IsLight ? new Color32(213, 171, 135, 255) : new Color32(43, 36, 37, 255);
        public static Color HudBorder => IsLight ? new Color32(141, 86, 63, 255) : new Color32(126, 104, 78, 255);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Roots.Clear(); light = null; sheet = null; Changed = null;
        }

        public static void Bind(VisualElement root)
        {
            if (!sheet) sheet = Resources.Load<StyleSheet>("UI/Light");
            if (sheet && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            var reference = new WeakReference<VisualElement>(root);
            if (root.panel != null) Roots.Add(reference);
            Apply(root);
            // A root may be detached while its menu is rebuilt or moved into the portrait dock.
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (!Roots.Contains(reference)) Roots.Add(reference);
                Apply(root);
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ => Roots.Remove(reference));
        }

        private static void Apply(VisualElement root)
        {
            root.EnableInClassList("light", IsLight);
            root.EnableInClassList("dark", !IsLight);
        }

        public static void SetLight(bool value)
        {
            if (IsLight == value) return;
            light = value;
            PlayerPrefs.SetInt(PreferenceKey, value ? 1 : 0);
            PlayerPrefs.Save();
            for (var i = Roots.Count - 1; i >= 0; i--)
            {
                if (!Roots[i].TryGetTarget(out var root)) Roots.RemoveAt(i);
                else Apply(root);
            }
            Changed?.Invoke();
        }
    }
}
