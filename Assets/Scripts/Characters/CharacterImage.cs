using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.Characters
{
    // Creator previews and HUD portraits use the same baked frames as world actors.
    public sealed class CharacterImage
    {
        private readonly Image[] images = new Image[8];
        private readonly VisualElement root;
        private bool hasFrame;
        private (CharacterCatalog catalog, string head, string chest, string legs, string feet,
            string accessory, string tool, int frame, bool showTool) lastFrame;
        public CharacterImage(VisualElement parent, float zoom = 1)
        {
            root = new VisualElement { name = "character-image", pickingMode = PickingMode.Ignore };
            root.style.position = Position.Absolute;
            // Modular art has 64px frames; a 2x HUD zoom preserves the original 32px portrait's pixel scale.
            root.style.left = root.style.right = root.style.top = root.style.bottom = Length.Percent(-50 * (zoom - 1));
            parent.Add(root);
            for (var i = 0; i < images.Length; i++)
            {
                var image = new Image { name = "appearance-" + (CharacterSlot)i, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                image.style.position = Position.Absolute; image.style.left = image.style.right = image.style.top = image.style.bottom = 0;
                root.Add(image); images[i] = image;
            }
            root.Insert(1, images[(int)CharacterSlot.Head]);
        }
        public void Set(CharacterProfile profile, int frame, bool showTool = true)
        {
            var catalog = CharacterCatalog.Load(); if (!catalog) return;
            root.style.display = profile == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (profile == null) {
                foreach (var image in images) image.sprite = null;
                hasFrame = false; lastFrame = default;
                return;
            }
            // Creator profiles are edited in place, so compare the appearance values,
            // not just the profile reference. HUD calls between animation frames can
            // then avoid redundant layer writes, reordering and wardrobe-cache scans.
            var key = (catalog, profile.Head, profile.Chest, profile.Legs, profile.Feet,
                profile.Accessory, profile.Tool, frame, showTool);
            if (hasFrame && key.Equals(lastFrame)) return;
            hasFrame = true; lastFrame = key;
            for (var i = 0; i < images.Length; i++)
                images[i].sprite = !showTool && i == (int)CharacterSlot.Tool ? null : catalog.SpriteFor(profile, (CharacterSlot)i, frame);
            var tool = images[(int)CharacterSlot.Tool];
            if (frame < catalog.toolBehind.Length && catalog.toolBehind[frame]) tool.SendToBack(); else tool.BringToFront();
            catalog.TrimAppearanceCache(profile, CharacterEquipmentAppearance.Resolve(CharacterCreation.Current));
        }
    }
}
