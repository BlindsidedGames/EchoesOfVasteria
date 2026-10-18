using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.MapGeneration
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class BreakablePot : MonoBehaviour
    {
        [SerializeField] private Sprite brokenSprite;
        private SpriteRenderer visual;
        private Sprite intactSprite;
        internal static readonly List<BreakablePot> Active = new();
        internal bool Broken { get; private set; }
        internal SpriteRenderer Visual => visual;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();

        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            intactSprite = visual.sprite;
        }

        private void OnEnable()
        {
            // Decorations are reused by the segment pool. A new spawn is intact.
            Broken = false;
            visual.sprite = intactSprite;
            Active.Add(this);
            PotClickInput.EnsureActive();
        }

        private void OnDisable() => Active.Remove(this);

        internal void Break()
        {
            if (Broken || !brokenSprite) return;
            Broken = true;
            visual.sprite = brokenSprite;
        }
    }
}
