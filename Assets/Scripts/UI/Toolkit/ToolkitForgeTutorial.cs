using TimelessEchoes.Gear;
using TimelessEchoes.Gear.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitForgeScreen
    {
        private VisualElement lessonPanel, lessonPointer;
        private Label lessonText;
        private Button lessonTarget;
        private int lessonStage = -1;
        private float lessonStarted;
        private float FirstLessonMargin => float.IsNaN(lessonPanel.layout.height) ? 24 : Mathf.Max(24, lessonPanel.layout.height + 8);

        private void BuildFirstLesson()
        {
            lessonStage = -1;
            lessonPanel = new VisualElement { name = "forge-first-lesson" };
            lessonPanel.AddToClassList("surface"); lessonPanel.style.paddingTop = lessonPanel.style.paddingBottom = 4;
            lessonPanel.style.paddingLeft = lessonPanel.style.paddingRight = 4;
            lessonPanel.style.position = Position.Absolute; lessonPanel.style.left = lessonPanel.style.right = 4; lessonPanel.style.top = 4; lessonPanel.style.minHeight = 16;
            root.Add(lessonPanel);
            lessonText = ToolkitControls.Text(""); lessonText.style.whiteSpace = WhiteSpace.Normal; lessonPanel.Add(lessonText);
            lessonPointer = new Image { name = "forge-lesson-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
            lessonPointer.style.position = Position.Absolute; lessonPointer.style.width = lessonPointer.style.height = 24;
            root.Add(lessonPointer);
        }
        private void RefreshFirstLesson()
        {
            bool active = ForgeFirstLesson.Active;
            lessonPanel.style.display = lessonPointer.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            main.style.marginTop = sidebar.style.marginTop = active ? FirstLessonMargin : 0;
            if (!active)
            {
                lessonTarget = null;
                foreach (var entry in definition.gear) root.Q<Button>("gear-" + entry.slot).SetEnabled(true);
                foreach (var binding in definition.catalog.cores) root.Q<Button>("core-" + binding.core.name).SetEnabled(true);
                foreach (var row in conversions.Values) row.amount.SetEnabled(true);
                return;
            }
            var stage = ForgeFirstLesson.Stage;
            var core = definition.catalog.cores[0].core;
            string target, text;
            switch (stage)
            {
                case 0: target = "smelt-Ingot"; text = "Ivan supplied the materials. Click here to smelt your first ingot."; break;
                case 1: target = "core-" + core.name; text = "Your core determines the rarity odds. Later, you can upgrade cores to higher tiers. Select this basic core."; break;
                case 2: target = "gear-Weapon"; text = "Choose Weapon to make your first sword."; break;
                case 3: target = "forge-craft"; text = "Click Craft to use your ingot and core to make a sword."; break;
                default: target = "forge-replace"; text = "Your sword is ready. Click Equip to wield it."; break;
            }
            lessonText.text = ToolkitLocalization.Text("forge.lesson.step-" + stage, text);
            lessonTarget = root.Q<Button>(target);
            foreach (var entry in definition.gear) root.Q<Button>("gear-" + entry.slot).SetEnabled(stage == 2 && entry.slot == "Weapon");
            foreach (var binding in definition.catalog.cores) root.Q<Button>("core-" + binding.core.name).SetEnabled(stage == 1 && binding.core == core);
            autoCraft.SetEnabled(false);
            foreach (var row in conversions.Values) row.amount.SetEnabled(false);
            if (stage == 4) replace.text = ToolkitLocalization.Text("forge.equip", "Equip");
            if (lessonStage != stage)
            {
                lessonStage = stage; lessonStarted = Time.unscaledTime;
                if (lessonTarget != null) ((ScrollView)main).ScrollTo(lessonTarget);
            }
        }
        private void PositionFirstLessonHand()
        {
            if (lessonTarget == null || !ForgeFirstLesson.Active) return;
            main.style.marginTop = sidebar.style.marginTop = FirstLessonMargin;
            if (!((ScrollView)main).contentViewport.worldBound.Contains(lessonTarget.worldBound.center))
                ((ScrollView)main).ScrollTo(lessonTarget);
            var anchor = root.WorldToLocal(new Vector2(lessonTarget.worldBound.center.x, lessonTarget.worldBound.yMax));
            var bob = 4 * (1 + Mathf.Cos((Time.unscaledTime - lessonStarted) * Mathf.PI * 2 / 1.2f));
            var above = anchor.y + 2 + bob + 24 > root.layout.height;
            lessonPointer.style.rotate = new Rotate(Angle.Degrees(above ? 180 : 0));
            lessonPointer.style.left = Mathf.Round(anchor.x - 12);
            lessonPointer.style.top = Mathf.Round(above ? root.WorldToLocal(lessonTarget.worldBound.position).y - 26 - bob : anchor.y + 2 + bob);
        }
    }
}
