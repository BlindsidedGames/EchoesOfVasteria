using System.Collections;
using TimelessEchoes.Hero;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>A friendly introduction visual, with no combat or animation events.</summary>
    public sealed class WorldIntroductionActor : MonoBehaviour
    {
        private ToolkitNpcIntroduction profile;
        private HeroController hero;
        private Transform map, visual;
        private SpriteRenderer sprite;
        private float elapsed;
        private float animationStartedAt;
        private static WorldIntroductionActor active;
        private ToolkitNavigationScreen flightNavigation;
        public float AnimationStartedAt => animationStartedAt;
        public static bool ReaperHandoffPending => active && active.profile.npcId == "Reaper1" && active.phase != Phase.Gone;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => active = null;
        private enum Phase { Arriving, Idle, Departing, HudHandoff, Gone }
        private Phase phase;

        public static WorldIntroductionActor Create(ToolkitNpcIntroduction profile, Transform owner, HeroController hero)
        {
            if (!profile || !profile.HasWorldArrival || !owner || !hero || !GameManager.Instance || !GameManager.Instance.CurrentMap) return null;
            var root = new GameObject(profile.npcId + " introduction actor");
            root.transform.SetParent(owner, false);
            var actor = root.AddComponent<WorldIntroductionActor>();
            actor.profile = profile; actor.hero = hero; actor.map = GameManager.Instance.CurrentMap.transform;
            actor.animationStartedAt = Time.unscaledTime; active = actor;
            root.transform.position = hero.transform.position + (Vector3)profile.worldArrival.standingOffset;
            root.AddComponent<WorldSpeakerAnchor>().speakerId = profile.npcId;
            var child = new GameObject("Native artwork");
            child.transform.SetParent(root.transform, false);
            actor.visual = child.transform;
            actor.sprite = child.AddComponent<SpriteRenderer>();
            actor.sprite.flipX = profile.worldArrival.standingOffset.x > 0;
            var heroSprite = hero.GetComponentInChildren<SpriteRenderer>();
            if (heroSprite)
            {
                actor.sprite.sortingLayerID = heroSprite.sortingLayerID;
                actor.sprite.sortingOrder = heroSprite.sortingOrder + 1;
            }
            // Native 16 PPU artwork; the actual reaper prefab, Animator and
            // KillTarget events are deliberately absent from this friendly actor.
            actor.ApplyFrame();
            return actor;
        }

        private bool ContextValid => map && hero && GameManager.Instance &&
            GameManager.Instance.CurrentMap && GameManager.Instance.CurrentMap.transform == map && !GameManager.Instance.HeroIsDead;

        private void Update()
        {
            if (!ContextValid) { Destroy(gameObject); return; }
            if (TownWindowManager.Instance && TownWindowManager.Instance.IsWindowOpen(TownWindowManager.Window.Options)) return;
            if (phase == Phase.HudHandoff) return;
            elapsed += Time.unscaledDeltaTime;
            transform.position = hero.transform.position + (Vector3)profile.worldArrival.standingOffset;
            if (phase == Phase.Arriving && elapsed >= profile.worldArrival.approachSeconds)
            { phase = Phase.Idle; elapsed = 0; }
            else if (phase == Phase.Departing && elapsed >= .4f) phase = Phase.Gone;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            var arrival = profile.worldArrival;
            var side = arrival.standingOffset.x < 0 ? -1 : 1;
            if (phase == Phase.Arriving)
            {
                var t = Mathf.Clamp01(elapsed / Mathf.Max(.1f, arrival.approachSeconds));
                var ease = 1 - Mathf.Pow(1 - t, 3);
                // Fly down in an arc, then settle alongside the player's feet.
                visual.localPosition = Vector3.Lerp(new Vector3(side * 3.5f, 3.5f, 0), Vector3.zero, ease) +
                    Vector3.up * Mathf.Sin(t * Mathf.PI) * .45f;
                sprite.sprite = Frame(arrival.entranceFrames, elapsed, arrival.framesPerSecond, false);
            }
            else if (phase == Phase.Idle)
            {
                var animationTime = Time.unscaledTime - animationStartedAt;
                var rate = Mathf.Max(1, profile.portraitFramesPerSecond);
                var cycle = animationTime * rate / Mathf.Max(1, profile.portraitFrames?.Length ?? 0);
                visual.localPosition = Vector3.up * (Mathf.Sin(cycle * Mathf.PI * 2) * .06f);
                // World-facing artwork is independent of the front-facing
                // dialogue portrait. Keep looking at the person beside Carl.
                sprite.flipX = (hero.transform.position.x > transform.position.x) != arrival.standingFacesRight;
                sprite.sprite = Frame(arrival.standingFrames, animationTime, rate, true) ?? profile.portrait;
            }
            else
            {
                var t = Mathf.SmoothStep(0, 1, elapsed / .4f);
                visual.localPosition = new Vector3(side * 2.5f * t, 3 * t, 0);
                sprite.sprite = Frame(arrival.departureFrames, elapsed, arrival.framesPerSecond, false);
                sprite.enabled = phase != Phase.Gone;
            }
        }

        private static Sprite Frame(Sprite[] frames, float time, float rate, bool loop)
        {
            if (frames == null || frames.Length == 0) return null;
            var index = Mathf.FloorToInt(time * Mathf.Max(1, rate));
            return frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
        }

        private void OnDestroy()
        {
            if (flightNavigation) flightNavigation.CancelReaperFlight();
            if (active == this) active = null;
        }

        public IEnumerator WaitForArrival()
        {
            while (this && ContextValid && phase == Phase.Arriving) yield return null;
        }

        public IEnumerator Depart()
        {
            if (!this || !ContextValid) yield break;
            flightNavigation = FindAnyObjectByType<ToolkitNavigationScreen>();
            if (profile.npcId == "Reaper1" && flightNavigation && flightNavigation.CanReceiveReaper)
            {
                phase = Phase.HudHandoff;
                yield return flightNavigation.FlyReaperToProgress(sprite, profile, animationStartedAt);
                if (this) phase = Phase.Gone;
            }
            else
            {
                phase = Phase.Departing; elapsed = 0;
                while (this && ContextValid && phase == Phase.Departing) yield return null;
            }
        }
    }
}
