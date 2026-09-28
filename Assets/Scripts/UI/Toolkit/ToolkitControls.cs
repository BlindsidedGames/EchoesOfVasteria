using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Shared visual primitives. Callers own gameplay actions and localization lifetimes.</summary>
    public static class ToolkitControls
    {
        public enum TextRole { Caption, Body, Subheading, Heading }

        public static Button Button(string name, Action clicked, Sprite background)
        {
            var button = new Button(clicked) { name = name };
            button.AddToClassList("eov-control");
            button.AddToClassList("button");
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) Audio.AudioManager.Instance?.PlayUIButtonClick(); });
            button.RegisterCallback<NavigationSubmitEvent>(_ => Audio.AudioManager.Instance?.PlayUIButtonClick());
            return button;
        }

        public static Label Text(string value, TextRole role = TextRole.Body)
        {
            var label = new Label(value) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("eov-text");
            SetTextRole(label, role);
            return label;
        }

        public static void SetTextRole(TextElement text, TextRole role)
        {
            foreach (TextRole candidate in Enum.GetValues(typeof(TextRole)))
                text.EnableInClassList("eov-text--" + candidate.ToString().ToLowerInvariant(), candidate == role);
        }

        public static void RepeatWhileHeld(Button button, Action repeat, long delayMilliseconds = 500, long intervalMilliseconds = 100)
        {
            var timer = button.schedule.Execute(() => { if (button.enabledInHierarchy) repeat(); }).Every(intervalMilliseconds);
            timer.Pause();
            // Observe presses before Button's Clickable consumes them at the target.
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && button.enabledInHierarchy) timer.ExecuteLater(delayMilliseconds); }, TrickleDown.TrickleDown);
            button.RegisterCallback<PointerUpEvent>(_ => timer.Pause(), TrickleDown.TrickleDown);
            button.RegisterCallback<PointerLeaveEvent>(_ => timer.Pause(), TrickleDown.TrickleDown);
            button.RegisterCallback<PointerCancelEvent>(_ => timer.Pause(), TrickleDown.TrickleDown);
            button.RegisterCallback<DetachFromPanelEvent>(_ => timer.Pause());
        }

        public static Slider Slider(string name, float minimum, float maximum, Action<float> changed,
            Sprite trackSprite, Sprite fillSprite, Sprite handleSprite, float height = 10)
        {
            var slider = new Slider(minimum, maximum) { name = name };
            slider.AddToClassList("eov-slider"); slider.style.height = height;
            var track = slider.Q("unity-tracker"); ToolkitTheme.Background(track, trackSprite);
            var fill = new VisualElement { name = "value-fill", pickingMode = PickingMode.Ignore };
            fill.AddToClassList("eov-slider-fill"); ToolkitTheme.Background(fill, fillSprite); track.Add(fill);
            var handle = slider.Q("unity-dragger"); ToolkitTheme.Background(handle, handleSprite); handle.style.height = height;
            slider.RegisterValueChangedCallback(e => { SetSliderValue(slider, e.newValue); changed?.Invoke(e.newValue); });
            SetSliderValue(slider, minimum);
            return slider;
        }

        public static void SetSliderValue(Slider slider, float value)
        {
            slider.SetValueWithoutNotify(value);
            var fill = slider.Q("value-fill");
            if (fill != null) fill.style.width = Length.Percent(Mathf.InverseLerp(slider.lowValue, slider.highValue, slider.value) * 100);
        }

        public static Image Icon(Sprite sprite)
        {
            var icon = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("eov-icon");
            return icon;
        }

        public static ScrollView RecessedScroll(VisualElement parent, string name, ToolkitTheme theme, Sprite background = null, bool compact = false)
        {
            var frame = new VisualElement { name = name + "-frame" };
            frame.AddToClassList("eov-recessed-scroll");
            
            parent.Add(frame);
            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                name = name,
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            scroll.AddToClassList(compact ? "eov-scroll" : "eov-buffs-scroll");
            theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll);
            frame.Add(scroll);
            return scroll;
        }
    }
}
