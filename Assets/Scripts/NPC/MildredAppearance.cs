using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Shop;
using UnityEngine;

namespace TimelessEchoes.NPC
{
    [DisallowMultipleComponent]
    public sealed class MildredAppearance : MonoBehaviour
    {
        public const string PreferenceKey = "Appearance.MildredColour";
        private static MildredColours colours;
        private static int selected = -1;
        private SpriteRenderer visual;
        private Animator animator;
        private RuntimeAnimatorController originalController;
        private int appliedChoice = -1;
        private readonly WorldTapInput tap = new();

        public static MildredColours Colours => colours ? colours : colours = Resources.Load<MildredColours>("MildredColours/Colours");
        public static int Selected
        {
            get
            {
                if (selected < 0) selected = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, (Colours ? Colours.ChoiceCount : MildredColours.Count) - 1);
                // Keep the requested appearance while store restoration is pending.
                return IsUnlocked(selected) ? selected : selected >= MildredColours.Count ? MildredColours.Count : 0;
            }
        }
        public static bool IsUnlocked(int choice) => choice is 0 or 1 or 5 or 6 || ShopService.Instance && ShopService.Instance.CosmeticsOwned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { colours = null; selected = -1; }

        public static void Select(int colour)
        {
            colour = Mathf.Clamp(colour, 0, (Colours ? Colours.ChoiceCount : MildredColours.Count) - 1);
            if (!IsUnlocked(colour)) return;
            selected = colour;
            PlayerPrefs.SetInt(PreferenceKey, selected);
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            visual = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
            if (animator) originalController = animator.runtimeAnimatorController;
        }

        private void LateUpdate()
        {
            if (animator && Colours && appliedChoice != Selected)
            {
                animator.runtimeAnimatorController = Colours.Controller(Selected) ?? originalController;
                appliedChoice = Selected;
            }
            // Animator owns frame timing; only replace its chosen frame after animation evaluation.
            if (visual && Colours) visual.sprite = Colours.Remap(visual.sprite, Selected);
        }

        private bool Hit(Camera camera, Vector2 position)
        {
            if (!visual || !visual.enabled || !visual.sprite || !camera.pixelRect.Contains(position)) return false;
            var bounds = visual.bounds;
            var a = camera.WorldToScreenPoint(bounds.min); var b = camera.WorldToScreenPoint(bounds.max);
            // Keep the small cat tappable on phones without adding a physics obstacle.
            var size = new Vector2(Mathf.Max(44, b.x - a.x), Mathf.Max(44, b.y - a.y));
            return new Rect((Vector2)(a + b) * .5f - size * .5f, size).Contains(position);
        }

        private void Update()
        {
            var camera = Camera.main;
            if (camera && tap.Released(out var position) && Hit(camera, position))
                FindAnyObjectByType<ToolkitNavigationScreen>()?.ShowMildredColours();
        }
    }
}
