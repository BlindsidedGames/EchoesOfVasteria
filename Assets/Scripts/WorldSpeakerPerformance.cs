using System.Collections;
using TimelessEchoes.UI;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>Optional visual performance during an encounter; owns no task or gameplay state.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WorldSpeakerPerformance : MonoBehaviour
    {
        [SerializeField] private Sprite[] enterFrames, exitFrames, idleFront, idleSide, idleBack;
        [SerializeField, Min(1)] private float framesPerSecond = 12;
        [SerializeField] private bool sideFacesRight;
        [SerializeField] private Sprite caughtItem;
        [SerializeField] private Vector2 catchOrigin = new(1.8f, -1.3f);
        [SerializeField] private SpriteRenderer entranceTree;
        [SerializeField] private Sprite entranceStump;
        [SerializeField, Min(1)] private int enterLoops = 1;
        [SerializeField, Min(0)] private int chopImpactFrame = 2;
        [SerializeField] private GameObject townArrivalTree, townArrivalStump;
        private System.Action townImpact;
        private Quaternion townTreeRotation;
        private int previousSortingOrder;
        private bool townSortingOverride;
        private Sprite standingTreeSprite;
        private Quaternion treeRotation;
        private SpriteRenderer catchVisual;
        private enum Phase { Dormant, Entering, Talking, Leaving, TownStanding, TownCasting, TownChopping }
        private Phase phase;
        private SpriteRenderer artwork;
        private Animator animator;
        private Transform listener;
        private Sprite previousSprite;
        private bool previousAnimatorEnabled, previousFlip;
        private float elapsed;
        public bool Ready => phase == Phase.Talking;
        public bool Playing => phase != Phase.Dormant;

        public void Begin(Transform target)
        {
            if (Playing || !target) return;
            CaptureVisuals();
            listener = target;
            phase = Phase.Entering; elapsed = 0;
            ApplyFrame();
        }

        private void CaptureVisuals()
        {
            artwork = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            previousSprite = artwork.sprite;
            previousFlip = artwork.flipX;
            previousAnimatorEnabled = animator && animator.enabled;
            if (animator) animator.enabled = false;
        }

        public bool OwnsTownArrivalChange(GameObject obj) => obj && (obj == townArrivalTree || obj == townArrivalStump);

        // Prepared while inactive, before the reveal makes the actor visible.
        public bool PrepareTownArrival(System.Action impact = null)
        {
            var chopping = townArrivalTree && enterFrames != null && enterFrames.Length > 0;
            if (Playing || idleFront == null || idleFront.Length == 0 || !chopping && (exitFrames == null || exitFrames.Length == 0)) return false;
            CaptureVisuals();
            townImpact = chopping ? impact : null;
            if (chopping)
            {
                townTreeRotation = townArrivalTree.transform.localRotation;
                // The existing canopy draws above the ordinary town idle pose.
                // Keep the axe performance visible until that tree is removed.
                var treeArt = townArrivalTree.GetComponent<SpriteRenderer>();
                previousSortingOrder = artwork.sortingOrder;
                if (treeArt && treeArt.sortingLayerID == artwork.sortingLayerID)
                { artwork.sortingOrder = Mathf.Max(artwork.sortingOrder, treeArt.sortingOrder + 1); townSortingOverride = true; }
            }
            phase = Phase.TownStanding; elapsed = 0;
            ApplyFrame();
            return true;
        }

        public IEnumerator PlayTownArrival()
        {
            if (phase != Phase.TownStanding) yield break;
            phase = townArrivalTree ? Phase.TownChopping : Phase.TownCasting; elapsed = 0;
            ApplyFrame();
            while (this && isActiveAndEnabled && (phase is Phase.TownCasting or Phase.TownChopping)) yield return null;
        }

        private void Update()
        {
            if (!Playing) return;
            if ((phase is Phase.Entering or Phase.Talking or Phase.Leaving) && !listener) { Cancel(); return; }
            if (TownWindowManager.Instance && TownWindowManager.Instance.IsWindowOpen(TownWindowManager.Window.Options)) return;
            elapsed += Time.unscaledDeltaTime;
            if (phase == Phase.TownChopping)
            {
                AnimateTownChopping();
                if (elapsed >= Duration(enterFrames) * Mathf.Max(1, enterLoops)) { Cancel(); return; }
            }
            if (phase == Phase.Entering && elapsed >= Mathf.Max(Duration(enterFrames) * Mathf.Max(1, enterLoops), caughtItem ? .63f : 0))
            { AnimateChopping(); ClearCatch(); phase = Phase.Talking; elapsed = 0; }
            else if ((phase is Phase.Leaving or Phase.TownCasting) && elapsed >= Duration(exitFrames))
            { Cancel(); return; }
            ApplyFrame();
            if (phase == Phase.Entering) { AnimateCatch(); AnimateChopping(); }
        }

        private float Duration(Sprite[] frames) => (frames?.Length ?? 0) / Mathf.Max(1, framesPerSecond);

        private void OnEnable()
        {
            if (!entranceTree) return;
            if (!standingTreeSprite) { standingTreeSprite = entranceTree.sprite; treeRotation = entranceTree.transform.localRotation; }
            entranceTree.sprite = standingTreeSprite;
            entranceTree.transform.localRotation = treeRotation;
            entranceTree.enabled = true;
        }

        private void AnimateChopping()
        {
            if (!entranceTree || enterFrames == null || enterFrames.Length == 0) return;
            var frame = elapsed * Mathf.Max(1, framesPerSecond);
            var finalImpact = (Mathf.Max(1, enterLoops) - 1) * enterFrames.Length + chopImpactFrame;
            if (frame >= finalImpact)
            {
                entranceTree.sprite = entranceStump;
                entranceTree.enabled = entranceStump;
                entranceTree.transform.localRotation = treeRotation;
                return;
            }
            var sinceHit = (frame % enterFrames.Length - chopImpactFrame) / Mathf.Max(1, framesPerSecond);
            var shake = sinceHit >= 0 && sinceHit < .18f ? Mathf.Sin(sinceHit / .18f * Mathf.PI * 2) * 2 : 0;
            entranceTree.transform.localRotation = treeRotation * Quaternion.Euler(0, 0, shake);
        }

        private void AnimateTownChopping()
        {
            if (!townArrivalTree || townImpact == null) return;
            var frame = elapsed * Mathf.Max(1, framesPerSecond);
            if (frame >= (Mathf.Max(1, enterLoops) - 1) * enterFrames.Length + chopImpactFrame)
            {
                townArrivalTree.transform.localRotation = townTreeRotation;
                var apply = townImpact; townImpact = null; apply();
                return;
            }
            var sinceHit = (frame % enterFrames.Length - chopImpactFrame) / Mathf.Max(1, framesPerSecond);
            var shake = sinceHit >= 0 && sinceHit < .18f ? Mathf.Sin(sinceHit / .18f * Mathf.PI * 2) * 2 : 0;
            townArrivalTree.transform.localRotation = townTreeRotation * Quaternion.Euler(0, 0, shake);
        }

        private void AnimateCatch()
        {
            // A cosmetic catch only: no item pickup, inventory or reward calls.
            if (!caughtItem || elapsed < .08f) return;
            if (!catchVisual)
            {
                var item = new GameObject("Caught item");
                item.transform.SetParent(transform, false);
                catchVisual = item.AddComponent<SpriteRenderer>();
                catchVisual.sprite = caughtItem;
                catchVisual.sortingLayerID = artwork.sortingLayerID;
                catchVisual.sortingOrder = artwork.sortingOrder + 1;
            }
            var t = Mathf.Clamp01((elapsed - .08f) / .55f);
            var start = catchOrigin;
            if (previousFlip) start.x = -start.x;
            var position = Vector2.Lerp(start, new Vector2(0, .45f), t);
            position.y += Mathf.Sin(t * Mathf.PI) * 1.5f;
            catchVisual.transform.localPosition = position;
            // Match native world pixels, then tuck into Gill's pack at the end.
            var tuck = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.72f, 1, t));
            catchVisual.transform.localScale = Vector3.one * (caughtItem.pixelsPerUnit / 16 * tuck);
            catchVisual.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-25, 70, t));
        }

        private void ClearCatch()
        {
            if (catchVisual) { catchVisual.enabled = false; Destroy(catchVisual.gameObject); catchVisual = null; }
        }

        private void ApplyFrame()
        {
            var frames = phase == Phase.TownStanding ? idleFront : phase is Phase.Entering or Phase.TownChopping ? enterFrames : exitFrames;
            artwork.flipX = previousFlip;
            if (phase == Phase.Talking)
            {
                var delta = listener.position - transform.position;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                { frames = idleSide; artwork.flipX = (delta.x > 0) != sideFacesRight; }
                else frames = delta.y > 0 ? idleBack : idleFront;
            }
            if (frames == null || frames.Length == 0) return;
            var index = Mathf.FloorToInt(elapsed * Mathf.Max(1, framesPerSecond));
            var loop = phase is Phase.Talking or Phase.TownStanding ||
                (phase is Phase.Entering or Phase.TownChopping) && enterLoops > 1;
            artwork.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
        }

        public IEnumerator Finish()
        {
            if (!Playing) yield break;
            ClearCatch();
            phase = Phase.Leaving; elapsed = 0;
            ApplyFrame();
            while (this && isActiveAndEnabled && phase == Phase.Leaving) yield return null;
        }

        public void Cancel()
        {
            if (!Playing) return;
            phase = Phase.Dormant;
            if (townImpact != null && townArrivalTree) townArrivalTree.transform.localRotation = townTreeRotation;
            townImpact = null;
            if (townSortingOverride && artwork) artwork.sortingOrder = previousSortingOrder;
            townSortingOverride = false;
            ClearCatch();
            if (artwork) { artwork.sprite = previousSprite; artwork.flipX = previousFlip; }
            if (animator) animator.enabled = previousAnimatorEnabled;
            listener = null;
        }

        private void OnDisable() => Cancel();
    }
}
