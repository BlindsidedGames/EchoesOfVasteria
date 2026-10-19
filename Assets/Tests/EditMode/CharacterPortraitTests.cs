#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Characters;
using UnityEngine.UIElements;

namespace Tests.EditMode
{
    public sealed class CharacterPortraitTests
    {
        [Test]
        public void SameFramePortraitAcceptsInPlaceEditsToolVisibilityAndReuseAfterClearing()
        {
            var catalog = CharacterCatalog.Load();
            var profile = catalog.NewProfile();
            profile.Tool = "Tool/Metal0";
            var frame = catalog.Frame("SwordAttack_1_Right", 0);
            var host = new VisualElement();
            var portrait = new CharacterImage(host);
            try
            {
                portrait.Set(profile, frame);
                var head = host.Q<Image>("appearance-Head");
                var tool = host.Q<Image>("appearance-Tool");
                var previousHead = head.sprite;
                Assert.NotNull(previousHead); Assert.NotNull(tool.sprite);
                profile.Head = catalog.parts.First(p => p.slot == CharacterSlot.Head &&
                    p.id.Contains("/Hair_") && p.id != profile.Head).id;
                portrait.Set(profile, frame);
                Assert.AreNotSame(previousHead, head.sprite, "Editing the existing profile must update an unchanged animation frame.");
                Assert.NotNull(head.sprite);
                portrait.Set(profile, frame, false);
                Assert.IsNull(tool.sprite, "Hiding the tool must take effect even when the frame is unchanged.");
                portrait.Set(profile, frame);
                Assert.NotNull(tool.sprite);
                portrait.Set(null, frame);
                Assert.True(host.Query<Image>().ToList().All(image => image.sprite == null), "A cleared portrait must release every layer.");
                portrait.Set(profile, frame);
                Assert.NotNull(head.sprite); Assert.NotNull(tool.sprite);
            }
            finally
            {
                portrait.Set(null, frame);
                catalog.ReleaseUnused(CharacterCreation.Current);
            }
        }
    }
}
#endif
