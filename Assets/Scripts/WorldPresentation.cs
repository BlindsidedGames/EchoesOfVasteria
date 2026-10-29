using System;
using System.Collections;
using TimelessEchoes.Hero;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using Unity.Cinemachine;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Exclusive, temporary world framing. Dialogue and quest results stay with their
    /// callers; normal cameras and gameplay tracking never inherit this camera's pose.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class WorldPresentation : MonoBehaviour
    {
        private static WorldPresentation instance;
        private Session active;
        private CinemachineCamera shot;
        private Camera output;
        private TownCameraPan town;
        private float approach;
        private Pose start;
        private bool returning;
        private float returnElapsed;
        private Vector3 dialogueVelocity;
        private float dialogueZoomVelocity;
        private CinemachineCore.GetBlendOverrideDelegate previousBlend, blendOverride;

        private struct Pose
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float Size;
            public Pose(Vector3 position, Quaternion rotation, float size)
            { Position = position; Rotation = rotation; Size = size; }
        }

        public static bool IsBusy => instance && instance.active != null;
        public static bool HasDialogue => instance && instance.active is DialogueSession { Disposed: false };
        public static bool SuppressesFocusReset => HasDialogue;
        public static bool ForcesTallPortrait => instance && instance.active is DialogueSession d &&
            !d.Disposed && !d.Suspended && !d.Guided && d.Map;

        public static bool ResolveExpanded(bool requested)
        {
            if (instance && instance.active is DialogueSession d && !d.Disposed)
            {
                if (d.Suspended || d.Guided) return false;
                if (d.Map) return true;
            }
            return requested;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => instance = null;

        private static WorldPresentation Get()
        {
            if (!instance) instance = new GameObject("World presentation").AddComponent<WorldPresentation>();
            return instance;
        }

        public static DialogueSession BeginDialogue(MonoBehaviour owner, string npcId, Transform subject = null, bool townOverview = false)
        {
            if (!owner || IsBusy) return null;
            var director = Get();
            var session = new DialogueSession(director, owner, npcId, subject ? subject : WorldSpeakerAnchor.Find(npcId), townOverview);
            director.Acquire(session);
            return session;
        }

        public static RevealSession BeginReveal(MonoBehaviour owner)
        {
            if (!owner || IsBusy || !GameManager.Instance || !GameManager.Instance.IsInTown) return null;
            var director = Get();
            var session = new RevealSession(director, owner);
            director.Acquire(session);
            return session;
        }

        public abstract class Session : IDisposable
        {
            internal readonly WorldPresentation Director;
            internal readonly MonoBehaviour Owner;
            internal readonly GameObject Map;
            internal readonly GameManager Game;
            internal readonly bool StartedDead;
            private readonly bool hadGame;
            private readonly bool hadMap;
            internal bool Disposed;
            public bool IsDisposed => Disposed;
            internal Session(WorldPresentation director, MonoBehaviour owner)
            {
                Director = director; Owner = owner; Game = GameManager.Instance;
                Map = Game ? Game.CurrentMap : null;
                hadGame = Game != null;
                hadMap = Map != null;
                StartedDead = Game && Game.HeroIsDead;
            }
            internal bool ContextValid => !hadGame || Game && GameManager.Instance == Game && (!hadMap || Map) && Game.CurrentMap == Map &&
                (StartedDead || !Game.HeroIsDead);
            public bool Interrupted => Disposed || !Director || !ContextValid || !Owner;
            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                if (Director) Director.Release(this);
            }
        }

        public sealed class DialogueSession : Session
        {
            internal readonly Transform Subject;
            internal readonly bool Orin, TownOverview;
            internal bool Suspended, Guided;
            internal Rect Dialogue;
            internal DialogueSession(WorldPresentation director, MonoBehaviour owner, string npcId, Transform subject, bool townOverview)
                : base(director, owner)
            { Subject = subject; Orin = npcId is "Orin" or "OrinHubIntroduction"; TownOverview = Orin && !Map && townOverview; }
            public bool SubjectFramed => !Subject || !Director.output || !Director.output.GetComponent<CinemachineBrain>() ||
                !Suspended && !Guided && Director.shot && Director.shot.gameObject.activeSelf && Director.approach >= .65f;
            public void SetDialogueRect(Rect rect) => Dialogue = rect;
            public void SetSuspended(bool suspended) => Suspended = suspended;
            public void SetGuidance(bool guided) => Guided = guided;
        }

        public sealed class RevealSession : Session
        {
            internal RevealSession(WorldPresentation director, MonoBehaviour owner) : base(director, owner) { }
            public IEnumerator Focus(Bounds bounds, float duration)
            {
                if (!Interrupted) yield return Director.MoveTo(this, bounds, duration, false);
            }
            public IEnumerator Restore(float duration)
            {
                if (!Interrupted) yield return Director.MoveTo(this, default, duration, true);
            }
        }

        public static Rect DialogueRect(float measuredHeight)
        {
            var safe = ToolkitResponsiveLayout.SafeScreenArea;
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            var inRun = GameManager.Instance && GameManager.Instance.CurrentMap;
            var hudClearance = inRun && (!portrait || ToolkitResponsiveLayout.FocusView) ? 104 : 52;
            var bottom = safe.yMax - hudClearance;
            // Leave the entire portrait progress strip, including Carl and the
            // player portraits above its track, visible below the dialogue.
            if (portrait && inRun) bottom = Mathf.Min(bottom, ToolkitResponsiveLayout.WorldArea.yMax - 48);
            var width = portrait ? safe.width - 24 : Mathf.Min(520, safe.width - 48);
            var maxHeight = Mathf.Max(80, Mathf.Min(portrait ? 220 : 146, bottom - safe.yMin - 64));
            var height = Mathf.Clamp(measuredHeight, 76, maxHeight);
            return new Rect(safe.center.x - width * .5f, bottom - height, width, height);
        }

        private void Acquire(Session session)
        {
            active = session; returning = false; approach = 0;
            output = Camera.main;
            town = !session.Map ? FindAnyObjectByType<TownCameraPan>() : null;
            // Block gestures without changing the normal town pan target or zoom.
            if (town && town.CanPresent) town.BeginPresentation();
        }

        private void Awake()
        {
            Blindsided.EventHandler.OnLoadData += Cancel;
            previousBlend = CinemachineCore.GetBlendOverride;
            blendOverride = (from, to, definition, owner) =>
                shot && (ReferenceEquals(from, shot) || ReferenceEquals(to, shot)) ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0) :
                previousBlend?.Invoke(from, to, definition, owner) ?? definition;
            CinemachineCore.GetBlendOverride = blendOverride;
        }

        private void EnsureShot()
        {
            if (shot || !output || !output.GetComponent<CinemachineBrain>()) return;
            var host = new GameObject("Presentation camera");
            host.transform.SetParent(transform, false);
            host.SetActive(false);
            shot = host.AddComponent<CinemachineCamera>();
            shot.Priority = 1000;
            shot.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            shot.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;
        }

        private Pose RenderedPose() => output
            ? new Pose(output.transform.position, output.transform.rotation, output.orthographicSize)
            : new Pose(new Vector3(0, 0, -10), Quaternion.identity, 9);

        private Pose NormalPose()
        {
            if (active?.Map && RunCameraFrame.TryGetCurrent(out var frame) && frame.HasState)
            {
                var state = frame.State;
                return new Pose(state.GetFinalPosition(), state.GetFinalOrientation(), state.Lens.OrthographicSize);
            }
            if (town && town.Camera)
            {
                var state = town.Camera.State;
                return new Pose(state.GetFinalPosition(), state.GetFinalOrientation(), town.Camera.Lens.OrthographicSize);
            }
            return RenderedPose();
        }

        private void SetPose(Pose pose)
        {
            if (!shot) return;
            shot.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            shot.Lens.OrthographicSize = pose.Size;
        }

        private void ActivateShot()
        {
            EnsureShot();
            if (!shot || shot.gameObject.activeSelf) return;
            start = RenderedPose(); approach = 0;
            dialogueVelocity = Vector3.zero; dialogueZoomVelocity = 0;
            SetPose(start);
            shot.gameObject.SetActive(true);
        }

        private static Pose Interpolate(Pose from, Pose to, float fraction)
        {
            var t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(fraction));
            return new Pose(Vector3.Lerp(from.Position, to.Position, t), Quaternion.Slerp(from.Rotation, to.Rotation, t), Mathf.Lerp(from.Size, to.Size, t));
        }

        private void Update()
        {
            if (active == null) return;
            if (!active.ContextValid || !returning && !active.Owner) { Cancel(); return; }
            if (returning)
            {
                returnElapsed += Time.unscaledDeltaTime;
                SetPose(Interpolate(start, NormalPose(), returnElapsed / .45f));
                if (returnElapsed >= .45f) CompleteRelease();
                return;
            }
            if (active is not DialogueSession dialogue) return;
            // Run conversations frame the hero and speaker above the dialogue.
            // Orin uses the hero and his gusts as the shared world subject.
            var hero = HeroController.Instance;
            var orinRun = dialogue.Orin && dialogue.Map;
            var hasSubject = dialogue.Orin ? orinRun ? hero != null : dialogue.TownOverview && town != null : dialogue.Subject != null;
            var focus = !dialogue.Suspended && !dialogue.Guided && hasSubject;
            if (!focus)
            {
                if (shot) shot.gameObject.SetActive(false);
                approach = 0;
                return;
            }
            ActivateShot();
            if (!shot) return;
            Bounds bounds;
            if (orinRun) bounds = new Bounds(hero.transform.position + Vector3.up * .9f, new Vector3(3, 2.5f, 0));
            else if (dialogue.Orin) bounds = town.LandBounds;
            else
            {
                bounds = dialogue.Subject.TryGetComponent<WorldSpeakerAnchor>(out var anchor) ? anchor.FramingBounds :
                    TownQuestReveal.SpriteBounds(dialogue.Subject.gameObject) ?? new Bounds(dialogue.Subject.position, Vector3.one);
                if (dialogue.Map && hero && Vector2.Distance(hero.transform.position, bounds.center) < 8)
                    // Include the standing character's head, rather than just
                    // the movement origin at their feet. Appearance sheets have
                    // transparent padding, so use the authored body envelope.
                    bounds.Encapsulate(new Bounds(hero.transform.position + Vector3.up * .6f, new Vector3(1.5f, 2.2f, 0)));
            }
            var target = Frame(bounds, dialogue.Dialogue, dialogue.Orin && !orinRun);
            approach += Time.unscaledDeltaTime;
            // Text wrapping, line changes and portrait expansion can all move
            // the desired frame. Ease continuously from the camera's current
            // pose instead of snapping to each new target after the entrance.
            var dt = Time.unscaledDeltaTime;
            SetPose(new Pose(Vector3.SmoothDamp(shot.transform.position, target.Position, ref dialogueVelocity, .2f, Mathf.Infinity, dt),
                Quaternion.Slerp(shot.transform.rotation, target.Rotation, 1 - Mathf.Exp(-dt / .12f)),
                Mathf.SmoothDamp(shot.Lens.OrthographicSize, target.Size, ref dialogueZoomVelocity, .2f, Mathf.Infinity, dt)));
        }

        private Pose Frame(Bounds subject, Rect dialogue, bool overview)
        {
            var normal = NormalPose();
            var world = ToolkitResponsiveLayout.WorldArea;
            var aspect = world.width / Mathf.Max(1, world.height);
            var free = world;
            var top = world.yMin + (ToolkitResponsiveLayout.IsPortrait ? 12 : 48);
            var bottom = world.yMax - 12;
            if (dialogue.width > 0) bottom = Mathf.Min(bottom, dialogue.yMin - 12);
            free.yMin = top; free.yMax = Mathf.Max(top + 40, bottom);
            var heightFraction = Mathf.Clamp(free.height / Mathf.Max(1, world.height), .15f, 1);
            var padding = overview ? 2 : dialogue.width > 0 ? .6f : 1.5f;
            var fit = Mathf.Max((subject.extents.y + padding) / heightFraction,
                (subject.extents.x + padding) / Mathf.Max(.1f, aspect * .9f));
            var size = overview ? fit : Mathf.Max(Mathf.Min(normal.Size * .55f, 7), fit);
            if (active?.Map)
            {
                var config = GameManager.CurrentGenerationConfig;
                var height = config ? config.segmentedMapSettings.segmentSize.y : 18;
                size = Mathf.Min(size, height * .5f);
                if (dialogue.width > 0)
                {
                    // A low shoreline speaker must remain in the unobstructed part
                    // of the view. Zoom closer rather than reveal ungenerated ground.
                    var verticalOffset = -(free.center.y - world.center.y) / Mathf.Max(1, world.height);
                    size = Mathf.Min(size, subject.center.y / Mathf.Max(.1f, 1 + 2 * verticalOffset),
                        (height - subject.center.y) / Mathf.Max(.1f, 1 - 2 * verticalOffset));
                }
            }
            // Keep town framing on authored land, even on narrow portrait displays.
            if (town && town.CanPresent)
                size = Mathf.Min(size, town.LandBounds.extents.y - .1f, (town.LandBounds.extents.x - .1f) / Mathf.Max(.1f, aspect));
            size = Mathf.Max(2, size);
            var offset = new Vector3((free.center.x - world.center.x) / Mathf.Max(1, world.width) * 2 * size * aspect,
                -(free.center.y - world.center.y) / Mathf.Max(1, world.height) * 2 * size, 0);
            var position = subject.center - offset; position.z = normal.Position.z;
            if (town && town.CanPresent)
            {
                var land = town.LandBounds;
                position.x = Mathf.Clamp(position.x, land.min.x + size * aspect, land.max.x - size * aspect);
                position.y = Mathf.Clamp(position.y, land.min.y + size, land.max.y - size);
            }
            else if (active?.Map)
            {
                var config = GameManager.CurrentGenerationConfig;
                var height = config ? config.segmentedMapSettings.segmentSize.y : 18;
                var left = RunCameraFrame.TryGetCurrent(out var runFrame) && runFrame.TryGetComponent<CameraClampExtension>(out var boundary)
                    ? boundary.MinimumX : 0;
                position.x = Mathf.Max(left + size * aspect, position.x);
                position.y = Mathf.Clamp(position.y, size, Mathf.Max(size, height - size));
            }
            return new Pose(position, normal.Rotation, size);
        }

        private IEnumerator MoveTo(Session session, Bounds bounds, float duration, bool restore)
        {
            ActivateShot();
            var origin = RenderedPose();
            for (var elapsed = 0f; elapsed < duration && !session.Interrupted; elapsed += Time.unscaledDeltaTime)
            {
                SetPose(Interpolate(origin, restore ? NormalPose() : Frame(bounds, default, false), elapsed / Mathf.Max(.01f, duration)));
                yield return null;
            }
            if (!session.Interrupted) SetPose(restore ? NormalPose() : Frame(bounds, default, false));
        }

        private void Release(Session session)
        {
            if (active != session) return;
            if (session is RevealSession || !session.ContextValid || !shot || !shot.gameObject.activeSelf)
            { CompleteRelease(); return; }
            returning = true; returnElapsed = 0; start = RenderedPose();
        }

        private void CompleteRelease()
        {
            if (shot) shot.gameObject.SetActive(false);
            if (town) town.EndPresentation();
            active = null; town = null; returning = false;
        }

        private void Cancel()
        {
            if (active != null) active.Disposed = true;
            CompleteRelease();
        }

        private void OnDisable() => Cancel();
        private void OnDestroy()
        {
            Cancel();
            Blindsided.EventHandler.OnLoadData -= Cancel;
            if (CinemachineCore.GetBlendOverride == blendOverride) CinemachineCore.GetBlendOverride = previousBlend;
            if (instance == this) instance = null;
        }
    }
}
