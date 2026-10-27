using System.Text.RegularExpressions;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    // Resource art is a layout element, not a font glyph with a different baseline.
    internal sealed class ToolkitQuestRequirementRow : VisualElement
    {
        private readonly Resource resource;
        private readonly bool knownIcon;
        private readonly Image icon;
        public readonly Label Text;

        public ToolkitQuestRequirementRow(Resource resource, float fontSize, bool rightAligned = false, bool knownIcon = false)
        {
            this.resource = resource;
            this.knownIcon = knownIcon;
            pickingMode = PickingMode.Ignore;
            AddToClassList("quest-resource-line");
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = rightAligned ? Justify.FlexEnd : Justify.FlexStart;
            style.flexShrink = 0;
            icon = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("quest-resource-icon");
            icon.style.flexShrink = 0;
            icon.style.marginRight = fontSize * .35f;
            Add(icon);
            Text = new Label { pickingMode = PickingMode.Ignore, enableRichText = true };
            ToolkitControls.SetTextSize(Text, fontSize);
            Text.style.marginLeft = Text.style.marginRight = Text.style.paddingLeft = Text.style.paddingRight = 0;
            Text.style.flexShrink = 1;
            Text.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(Text);
        }

        public void SetText(string richText)
        {
            var unlocked = knownIcon || resource && ResourceManager.Instance && ResourceManager.Instance.IsUnlocked(resource);
            icon.sprite = resource ? (unlocked ? resource.icon : resource.UnknownIcon) : null;
            icon.style.display = icon.sprite ? DisplayStyle.Flex : DisplayStyle.None;
            Text.text = ToolkitControls.WithoutInlineTextSize(Regex.Replace(richText, @"<sprite[^>]*>[\s ]*", ""));
        }
    }
}
