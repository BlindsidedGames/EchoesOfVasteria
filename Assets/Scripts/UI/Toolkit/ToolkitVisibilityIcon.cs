using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    // Point-filtered 16px sprites use the same palette and pixel scale as the UI atlas.
    public sealed class ToolkitVisibilityIcon : Image
    {
        private static Sprite shownSprite, hiddenSprite;
        public ToolkitVisibilityIcon()
        {
            pickingMode = PickingMode.Ignore;
            scaleMode = ScaleMode.ScaleToFit;
            AddToClassList("equipment-eye-icon");
        }

        public void SetShown(bool value)
        {
            if (!shownSprite) shownSprite = Resources.Load<Sprite>("UI/Icons/VisibilityShown");
            if (!hiddenSprite) hiddenSprite = Resources.Load<Sprite>("UI/Icons/VisibilityHidden");
            sprite = value ? shownSprite : hiddenSprite;
        }
    }
}
