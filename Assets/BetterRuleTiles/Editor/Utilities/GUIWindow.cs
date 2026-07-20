#if UNITY_EDITOR
using UnityEngine;

namespace VinTools.BetterRuleTiles.Editor.GUIWindows
{
    public class GUIWindow
    {
        #region Variables
        public enum WindowLayout
        {
            Free = 0,
            Fill = 1,
            Center = 2,

            SpanTop = 10,
            SpanBottom = 11,
            SpanLeft = 12,
            SpanRight = 13,

            TopLeftCorner = 20,
            TopRightCorner = 21,
            BottomLeftCorner = 22,
            BottomRightCorner = 23,
        }

        protected WindowLayout layout;
        protected RectOffset padding;
        public Rect rectangle;
        public bool lockPosition;
        protected bool hideOnUnfocus = false;
        protected bool autoLayout = false;
        protected GUIContent windowTitle;
        protected GUIStyle windowStyle;
        protected GUI.WindowFunction windowFunction;
        
        private bool _visible = false;
        #endregion

        #region Properties
        public float width { get => rectangle.width; set => rectangle.width = value; }
        public float height { get => rectangle.height; set => rectangle.height = value; }
        public float x { get => rectangle.x; set => rectangle.x = value; }
        public float y { get => rectangle.y; set => rectangle.y = value; }

        public Vector2 position { get => new Vector2(x, y); set { x = value.x; y = value.y; } }
        public Vector2 size { get => new Vector2(width, height); set { width = value.x; height = value.y; } }

        public bool visible
        {
            get => _visible;
            set
            {
                if (_visible == value) return;
                
                _visible = value;
                if (value) OnBecameVisible();
                else OnBecameHidden();
            }
        }
        #endregion

        #region Constructor
        public GUIWindow(Rect rect, GUI.WindowFunction windowFunction, GUIContent title = null, bool visible = true, bool autoLayout = false, bool hideOnUnfocus = false, GUIStyle style = null)
        {
            if (title == null) title = GUIContent.none;

            this.layout = WindowLayout.Free;
            this.padding = new RectOffset(0, 0, 0, 0);
            this.rectangle = rect;
            this.visible = visible;
            this.hideOnUnfocus = hideOnUnfocus;
            this.windowFunction = windowFunction;
            this.autoLayout = autoLayout;
            this.windowTitle = title;
            this.windowStyle = style;
        }
        public GUIWindow(WindowLayout placement, RectOffset padding, Vector2 size, GUI.WindowFunction windowFunction, bool visible)
        {
            this.layout = placement;
            this.padding = padding;
            this.rectangle = new Rect(Vector2.zero, size);
            this.lockPosition = true;
            this.visible = visible;
            this.hideOnUnfocus = false;
            this.windowFunction = windowFunction;
            this.autoLayout = false;
            this.windowTitle = GUIContent.none;
            this.windowStyle = GUIStyle.none;
        }
        public GUIWindow(WindowLayout placement, RectOffset padding, Vector2 size, GUI.WindowFunction windowFunction, GUIContent title = null, bool visible = true, bool autoLayout = false, bool hideOnUnfocus = false, GUIStyle style = null)
        {
            if (title == null) title = GUIContent.none;

            this.layout = placement;
            this.padding = padding;
            this.rectangle = new Rect(Vector2.zero, size);
            this.lockPosition = true;
            this.visible = visible;
            this.hideOnUnfocus = hideOnUnfocus;
            this.windowFunction = windowFunction;
            this.autoLayout = autoLayout;
            this.windowTitle = title;
            this.windowStyle = style;
        }
        public GUIWindow(Rect rect, GUIContent title = null, bool visible = true, bool autoLayout = false, bool hideOnUnfocus = false, GUIStyle style = null)
        {
            if (title == null) title = GUIContent.none;

            this.layout = WindowLayout.Free;
            this.padding = new RectOffset(0, 0, 0, 0);
            this.rectangle = rect;
            this.visible = visible;
            this.hideOnUnfocus = hideOnUnfocus;
            this.windowFunction = null;
            this.autoLayout = autoLayout;
            this.windowTitle = title;
            this.windowStyle = style;
        }
        public GUIWindow(WindowLayout placement, RectOffset padding, Vector2 size, GUIContent title = null, bool visible = true, bool autoLayout = false, bool hideOnUnfocus = false, GUIStyle style = null)
        {
            if (title == null) title = GUIContent.none;

            this.layout = placement;
            this.padding = padding;
            this.rectangle = new Rect(Vector2.zero, size);
            this.lockPosition = true;
            this.visible = visible;
            this.hideOnUnfocus = hideOnUnfocus;
            this.windowFunction = null;
            this.autoLayout = autoLayout;
            this.windowTitle = title;
            this.windowStyle = style;
        }
        #endregion

        #region Methods
        public void Show(int windowID, Rect mainWindowPosition, bool forceShow = false)
        {
            //if not visible ignore
            if (!visible && !forceShow) return;

            //if auto layout is enabled
            if (lockPosition) ApplyLayout(mainWindowPosition);

            //show window
            if (autoLayout && windowStyle != null) rectangle = GUILayout.Window(windowID, rectangle, WindowGUI, windowTitle, windowStyle);
            if (autoLayout && windowStyle == null) rectangle = GUILayout.Window(windowID, rectangle, WindowGUI, windowTitle);
            if (!autoLayout && windowStyle != null) rectangle = GUI.Window(windowID, rectangle, WindowGUI, windowTitle, windowStyle);
            if (!autoLayout && windowStyle == null) rectangle = GUI.Window(windowID, rectangle, WindowGUI, windowTitle);

            //return
            if (!hideOnUnfocus) return;

            //hide window if clicked outside
            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && !rectangle.Contains(Event.current.mousePosition)) visible = false;
        }

        public virtual void WindowGUI(int windowID)
        {
            //execute window function if not null
            windowFunction?.Invoke(windowID);

            //drag window if not locked
            if (!lockPosition) GUI.DragWindow();
        }

        public void ApplyLayout(Rect mainWindowPosition)
        {
            switch (layout)
            {
                case WindowLayout.Fill:
                    width = mainWindowPosition.width - padding.left - padding.right;
                    height = mainWindowPosition.height - padding.top - padding.bottom;
                    x = padding.left;
                    y = padding.top;
                    break;
                case WindowLayout.Center:
                    position = (mainWindowPosition.size - size) / 2;
                    break;
                case WindowLayout.SpanTop:
                    width = mainWindowPosition.width - padding.left - padding.right;
                    x = padding.left;
                    y = padding.top;
                    break;
                case WindowLayout.SpanBottom:
                    width = mainWindowPosition.width - padding.left - padding.right;
                    x = padding.left;
                    y = mainWindowPosition.height - height - padding.bottom;
                    break;
                case WindowLayout.SpanLeft:
                    height = mainWindowPosition.height - padding.top - padding.bottom;
                    x = padding.left;
                    y = padding.top;
                    break;
                case WindowLayout.SpanRight:
                    height = mainWindowPosition.height - padding.top - padding.bottom;
                    x = mainWindowPosition.width - width - padding.right;
                    y = padding.top;
                    break;
                case WindowLayout.TopLeftCorner:
                    x = padding.left;
                    y = padding.top;
                    break;
                case WindowLayout.TopRightCorner:
                    x = mainWindowPosition.width - width - padding.right;
                    y = padding.top;
                    break;
                case WindowLayout.BottomLeftCorner:
                    x = padding.left;
                    y = mainWindowPosition.height - height - padding.bottom;
                    break;
                case WindowLayout.BottomRightCorner:
                    x = mainWindowPosition.width - width - padding.right;
                    y = mainWindowPosition.height - height - padding.bottom;
                    break;
            }
        }

        public virtual void OnBecameVisible()
        {
            
        }

        public virtual void OnBecameHidden()
        {
            
        }
        #endregion
    }
}
#endif