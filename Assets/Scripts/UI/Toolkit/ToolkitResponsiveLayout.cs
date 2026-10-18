using Screen = UnityEngine.Device.Screen;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>One coordinate system for the camera, dock, and every runtime panel.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class ToolkitResponsiveLayout : MonoBehaviour
    {
        private static ToolkitResponsiveLayout instance;
        private readonly List<PanelSettings> panels = new();
        private readonly List<VisualElement> roots = new();
        private VisualElement dock;
        private PanelSettings dockSettings;
        public static bool IsPortrait => Portrait(new Vector2Int(Screen.width, Screen.height));
        public static bool TownView => Application.isPlaying && GameManager.Instance && !GameManager.Instance.CurrentMap;
        public static bool Portrait(Vector2Int size) => size.x > 0 && size.y >= size.x * 1.25f;
        public static float Scale(Vector2Int size) => Portrait(size) ? 360f / size.x : 432f / Mathf.Max(1, size.y);
        public static Vector2 Size => new Vector2(Screen.width, Screen.height) * Scale(new Vector2Int(Screen.width, Screen.height));
        public static Rect SafeScreenArea
        {
            get
            {
                var size = new Vector2Int(Screen.width, Screen.height);
                var safe = ValidSafeArea(size, Screen.safeArea); var scale = Scale(size);
                return new Rect(safe.x * scale, (size.y - safe.yMax) * scale, safe.width * scale, safe.height * scale);
            }
        }
        public static float WindowTop => IsPortrait ? (TownView ? 12 : 52) : 44;
        public static float MenuTop => WindowTop + (IsPortrait && !TownView ? 28 : 0);
        public static float WindowBottom => IsPortrait ? 44 : 12;

        public static Rect WorldArea
        {
            get
            {
                var size = new Vector2Int(Screen.width, Screen.height);
                var pixels = WorldPixels(size, Screen.safeArea);
                var scale = Scale(size);
                return new Rect(pixels.x * scale, (size.y - pixels.yMax) * scale,
                    pixels.width * scale, pixels.height * scale);
            }
        }

        public static Rect WorldPixels(Vector2Int size, Rect safe)
        {
            if (!Portrait(size)) return new Rect(0, 0, size.x, size.y);
            safe = ValidSafeArea(size, safe);
            if (TownView) return new Rect(0, 0, size.x, size.y);
            var height = safe.width * 9f / 16f;
            // The world can extend behind the Dynamic Island; only interactive UI
            // needs the top safe-area inset. Keep the run viewport at 16:9.
            return new Rect(safe.x, size.y - height, safe.width, height);
        }

        // UI coordinates start at the top; camera pixels start at the bottom.
        public static Rect DockArea(Vector2Int size, Rect safe)
        {
            safe = ValidSafeArea(size, safe);
            var scale = Scale(size);
            if (TownView)
                return new Rect(safe.xMin * scale, (size.y - safe.yMax) * scale,
                    safe.width * scale, safe.height * scale);
            var world = WorldPixels(size, safe);
            var top = (size.y - world.yMin) * scale;
            return new Rect(safe.xMin * scale, top,
                safe.width * scale,
                Mathf.Max(0, (world.yMin - safe.yMin) * scale));
        }

        private static Rect ValidSafeArea(Vector2Int size, Rect safe)
        {
            if (safe.width <= 0 || safe.height <= 0 || safe.xMin < 0 || safe.yMin < 0 || safe.xMax > size.x || safe.yMax > size.y)
                return new Rect(0, 0, size.x, size.y);
            return safe;
        }

        private void OnEnable()
        {
            instance = this;
            var document = GetComponent<UIDocument>();
            if (document && document.panelSettings)
            {
                dockSettings = document.panelSettings;
                CreateDock(document);
            }
        }

        private void CreateDock(UIDocument document)
        {
            if (document.rootVisualElement == null) return;
            document.rootVisualElement.Clear();
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            dock = new VisualElement { name = "portrait-dock", pickingMode = PickingMode.Ignore };
            dock.style.position = Position.Absolute;
            dock.style.backgroundColor = (Color)new Color32(39, 33, 33, 255);
            document.rootVisualElement.Add(dock);
        }

        public static void Register(PanelSettings panel)
        {
            Configure(panel);
            if (!Application.isPlaying) return;
            if (!instance)
            {
                var host = new GameObject("Responsive screen layout");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<ToolkitResponsiveLayout>();
                instance.dockSettings = Instantiate(panel);
                instance.dockSettings.sortingOrder = 1;
                var document = host.AddComponent<UIDocument>();
                document.panelSettings = instance.dockSettings;
                instance.CreateDock(document);
            }
            instance.panels.Add(panel);
        }

        public static void Bind(VisualElement root)
        {
            root.EnableInClassList("portrait", IsPortrait);
            if (instance && !instance.roots.Contains(root)) instance.roots.Add(root);
        }

        private static void Configure(PanelSettings panel)
        {
            // ScaleWithScreenSize reads the editor Game View in Device Simulator.
            // Explicit device scaling keeps UI, camera and safe-area coordinates equal.
            var mode = PanelScaleMode.ConstantPixelSize;
            if (panel.scaleMode != mode) panel.scaleMode = mode;
            var scale = 1f / Scale(new Vector2Int(Screen.width, Screen.height));
            if (!Mathf.Approximately(panel.scale, scale)) panel.scale = scale;
            var resolution = IsPortrait ? new Vector2Int(360, 432) : new Vector2Int(768, 432);
            var match = IsPortrait ? 0 : 1;
            if (panel.referenceResolution != resolution) panel.referenceResolution = resolution;
            if (!Mathf.Approximately(panel.match, match)) panel.match = match;
        }

        private void Update()
        {
            if (dock == null || dock.panel == null)
            {
                var document = GetComponent<UIDocument>();
                if (document && document.panelSettings) { dockSettings = document.panelSettings; CreateDock(document); }
            }
            if (dock == null || !dockSettings) return;
            for (var i = panels.Count - 1; i >= 0; i--)
                if (!panels[i]) panels.RemoveAt(i); else Configure(panels[i]);
            for (var i = roots.Count - 1; i >= 0; i--)
                if (roots[i].panel == null) roots.RemoveAt(i); else roots[i].EnableInClassList("portrait", IsPortrait);
            Configure(dockSettings);
            dock.style.display = IsPortrait && !TownView ? DisplayStyle.Flex : DisplayStyle.None;
            var world = WorldPixels(new Vector2Int(Screen.width, Screen.height), Screen.safeArea);
            var top = (Screen.height - world.yMin) * Scale(new Vector2Int(Screen.width, Screen.height));
            dock.style.left = 0; dock.style.top = top; dock.style.width = Size.x; dock.style.height = Size.y - top;
        }

        private void OnDestroy()
        {
            if (dockSettings) Destroy(dockSettings);
            if (instance == this) instance = null;
        }
    }
}

