using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Farming;
using TimelessEchoes.Quests;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimelessEchoes
{
    public readonly struct TownVisualChange
    {
        public readonly Bounds Bounds;
        public readonly Action Apply;
        public readonly GameObject RevealedObject;
        public readonly GameObject TargetObject;
        public TownVisualChange(Bounds bounds, Action apply, GameObject revealedObject = null, GameObject targetObject = null)
        { Bounds = bounds; Apply = apply; RevealedObject = revealedObject; TargetObject = targetObject; }
    }

    /// <summary>Presents committed town changes. Never owns quest rewards or save data.</summary>
    public sealed class TownQuestReveal : MonoBehaviour
    {
        private static TownQuestReveal active;
        public static bool IsPlaying => active && active.playing;
        private bool playing, cancelled, restoreMenu;
        private bool presentingDialogue, openFields;
        private string completedQuest;
        private TownCameraPan cameraPan;
        private WorldPresentation.RevealSession presentation;
        private QuestObjectStateController[] objects;
        private FieldsWorldView fields;
        private TownPathNetwork paths;
        private Transform town;
        private Action resumeQuests, removeSkip;
        private Coroutine sequence;
        private readonly List<WorldSpeakerPerformance> arrivals = new();
        private readonly List<(SpriteRenderer renderer, Color color)> fading = new();
        private readonly List<(UnityEngine.Tilemaps.Tilemap map, Color color)> fadingTilemaps = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => active = null;

        public sealed class Scope : IDisposable
        {
            private TownQuestReveal owner;
            private bool handedOff;
            internal Scope(TownQuestReveal owner) => this.owner = owner;
            public void Play(string questId = null)
            {
                if (handedOff || !owner) return;
                try { owner.completedQuest = questId; owner.Present(); handedOff = true; }
                catch (Exception exception)
                {
                    // The quest has already committed; presentation failure cannot undo its hand-in.
                    owner.Cleanup(); handedOff = true;
                    Debug.LogException(exception, owner);
                }
            }
            public void Dispose() { if (!handedOff && owner) owner.Cleanup(); owner = null; }
        }

        public static Scope Begin()
        {
            if (IsPlaying || WorldPresentation.IsBusy || !GameManager.Instance || !GameManager.Instance.IsInTown || Characters.CharacterCreation.Required) return null;
            var camera = FindAnyObjectByType<TownCameraPan>();
            var root = GameObject.Find("Hometown")?.transform;
            if (!camera || !camera.CanPresent || !root || !QuestManager.Instance) return null;
            var windows = TownWindowManager.Instance;
            if (windows && windows.HasOpenWindow && !windows.IsWindowOpen(TownWindowManager.Window.Quests)) return null;
            var owner = QuestManager.Instance.GetComponent<TownQuestReveal>();
            if (!owner) owner = QuestManager.Instance.gameObject.AddComponent<TownQuestReveal>();
            owner.cameraPan = camera; owner.town = root;
            owner.objects = FindObjectsByType<QuestObjectStateController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            owner.fields = FindAnyObjectByType<FieldsWorldView>();
            owner.paths = FindAnyObjectByType<TownPathNetwork>();
            foreach (var controller in owner.objects) controller.HoldPresentation();
            owner.fields?.HoldPresentation();
            owner.paths?.HoldPresentation();
            return new Scope(owner);
        }

        public static Bounds? SpriteBounds(GameObject obj)
        {
            Bounds? combined = null;
            foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.enabled || !renderer.sprite || renderer.color.a <= 0) continue;
                var visibleChildren = true;
                for (var parent = renderer.transform; parent != obj.transform; parent = parent.parent)
                    if (!parent.gameObject.activeSelf) { visibleChildren = false; break; }
                if (!visibleChildren) continue;
                var local = renderer.sprite.bounds;
                foreach (var x in new[] { local.min.x, local.max.x })
                    foreach (var y in new[] { local.min.y, local.max.y })
                    {
                        var point = renderer.transform.TransformPoint(new Vector3(renderer.flipX ? -x : x, renderer.flipY ? -y : y, 0));
                        if (combined.HasValue) { var bounds = combined.Value; bounds.Encapsulate(point); combined = bounds; }
                        else combined = new Bounds(point, Vector3.zero);
                    }
            }
            foreach (var tilemap in obj.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true))
            {
                var renderer = tilemap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                if (!renderer || !renderer.enabled || tilemap.GetUsedTilesCount() == 0) continue;
                var visibleChildren = true;
                for (var parent = tilemap.transform; parent != obj.transform; parent = parent.parent)
                    if (!parent.gameObject.activeSelf) { visibleChildren = false; break; }
                if (!visibleChildren) continue;
                var local = tilemap.localBounds;
                foreach (var x in new[] { local.min.x, local.max.x })
                    foreach (var y in new[] { local.min.y, local.max.y })
                    {
                        var point = tilemap.transform.TransformPoint(new Vector3(x, y, 0));
                        if (combined.HasValue) { var bounds = combined.Value; bounds.Encapsulate(point); combined = bounds; }
                        else combined = new Bounds(point, Vector3.zero);
                    }
            }
            return combined;
        }

        private sealed class Group
        {
            public Bounds Bounds;
            public readonly List<TownVisualChange> Changes = new();
        }

        private void Present()
        {
            var changes = objects.SelectMany(controller => controller.PendingPresentationChanges(town)).ToList();
            var expansion = fields ? fields.PendingPresentationChange() : null;
            if (expansion.HasValue) changes.Add(expansion.Value);
            var routeChange = paths ? paths.PendingPresentationChange() : null;
            if (routeChange.HasValue) changes.Add(routeChange.Value);
            if (changes.Count == 0) { FarmStory.Instance?.QueueCompletion(completedQuest); Cleanup(); return; }
            var groups = new List<Group>();
            foreach (var change in changes)
            {
                var group = groups.FirstOrDefault(g => Vector2.Distance(g.Bounds.center, change.Bounds.center) < 14);
                if (group == null) { group = new Group { Bounds = change.Bounds }; groups.Add(group); }
                else group.Bounds.Encapsulate(change.Bounds);
                group.Changes.Add(change);
            }
            presentation = WorldPresentation.BeginReveal(this);
            if (presentation == null) { FarmStory.Instance?.QueueCompletion(completedQuest); Cleanup(); return; }
            cancelled = false; restoreMenu = true; openFields = false; presentingDialogue = false; playing = true; active = this;
            resumeQuests = TownWindowManager.Instance?.SuspendQuestsForReveal();
            var screen = FindAnyObjectByType<ToolkitQuestsScreen>(FindObjectsInactive.Include);
            if (screen && screen.IsConfigured) removeSkip = screen.ShowRevealSkip(() => cancelled = true);
            Blindsided.EventHandler.OnLoadData += Loaded;
            sequence = StartCoroutine(Reveal(groups));
        }

        private void Loaded() { restoreMenu = false; cancelled = true; }
        private bool Interrupted
        {
            get
            {
                if (!cameraPan || !cameraPan.CanPresent || !GameManager.Instance || !GameManager.Instance.IsInTown)
                { restoreMenu = false; return true; }
                if (presentingDialogue) return cancelled;
                if (TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow)
                { restoreMenu = false; return true; }
                return cancelled || presentation == null || presentation.Interrupted;
            }
        }

        private void Update()
        {
            if (playing && !presentingDialogue && !UI.Toolkit.ToolkitDimScreen.BlocksInput && Keyboard.current != null &&
                (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) cancelled = true;
            // Stop an in-flight shared camera move as soon as Skip or another
            // window interrupts it; the coroutine's finally still settles visuals.
            if (playing && Interrupted) presentation?.Dispose();
        }

        private IEnumerator Reveal(List<Group> groups)
        {
            try
            {
                // Leave the hand-in frame before listening for skip input or showing world changes.
                yield return null;
                foreach (var group in groups)
                {
                    if (Interrupted) yield break;
                    yield return presentation.Focus(group.Bounds, .65f);
                    if (Interrupted) yield break;
                    var before = town.GetComponentsInChildren<SpriteRenderer>(true)
                        .Where(r => r.enabled && r.gameObject.activeInHierarchy).ToHashSet();
                    var beforeTilemaps = town.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)
                        .Where(map => map.gameObject.activeInHierarchy).ToHashSet();
                    var deferred = new HashSet<GameObject>();
                    foreach (var change in group.Changes)
                    {
                        if (change.RevealedObject && !change.RevealedObject.activeInHierarchy)
                            foreach (var actor in change.RevealedObject.GetComponentsInChildren<WorldSpeakerPerformance>(true))
                            {
                                var atImpact = group.Changes.Where(c => actor.OwnsTownArrivalChange(c.TargetObject)).ToArray();
                                if (!actor.PrepareTownArrival(() => { foreach (var pending in atImpact) pending.Apply(); })) continue;
                                arrivals.Add(actor);
                                foreach (var pending in atImpact) deferred.Add(pending.TargetObject);
                            }
                    }
                    foreach (var change in group.Changes)
                        if (!change.TargetObject || !deferred.Contains(change.TargetObject)) change.Apply();
                    foreach (var renderer in town.GetComponentsInChildren<SpriteRenderer>(true))
                        if (renderer.enabled && renderer.sprite && renderer.gameObject.activeInHierarchy && !before.Contains(renderer))
                        { fading.Add((renderer, renderer.color)); var color = renderer.color; color.a = 0; renderer.color = color; }
                    foreach (var map in town.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true))
                        if (map.gameObject.activeInHierarchy && !beforeTilemaps.Contains(map))
                        { fadingTilemaps.Add((map, map.color)); var color = map.color; color.a = 0; map.color = color; }
                    for (var elapsed = 0f; elapsed < .35f && !Interrupted; elapsed += Time.unscaledDeltaTime)
                    {
                        foreach (var item in fading) if (item.renderer)
                        { var color = item.color; color.a *= Mathf.Clamp01(elapsed / .35f); item.renderer.color = color; }
                        foreach (var item in fadingTilemaps) if (item.map)
                        { var color = item.color; color.a *= Mathf.Clamp01(elapsed / .35f); item.map.color = color; }
                        yield return null;
                    }
                    RestoreColors();
                    foreach (var actor in arrivals)
                    {
                        if (!actor) continue;
                        if (Interrupted) yield break;
                        var performance = actor.PlayTownArrival();
                        while (!Interrupted && performance.MoveNext()) yield return performance.Current;
                        if (Interrupted) yield break;
                    }
                    arrivals.Clear();
                    for (var elapsed = 0f; elapsed < .6f && !Interrupted; elapsed += Time.unscaledDeltaTime) yield return null;
                }
                if (!Interrupted && FarmStory.Instance && FarmStory.Instance.HasCompletion(completedQuest))
                {
                    removeSkip?.Invoke(); removeSkip = null;
                    presentingDialogue = true;
                    yield return FarmStory.Instance.ShowCompletion(completedQuest, presentation);
                    openFields = !Interrupted && FarmStory.Instance.DialogueCompleted && completedQuest == "Farm.Garden.Build01.v1";
                    presentation = null;
                }
                else if (!Interrupted) yield return presentation.Restore(.55f);
            }
            finally { Cleanup(); }
        }

        private void RestoreColors()
        {
            foreach (var item in fading) if (item.renderer) item.renderer.color = item.color;
            fading.Clear();
            foreach (var item in fadingTilemaps) if (item.map) item.map.color = item.color;
            fadingTilemaps.Clear();
        }

        private void Cleanup()
        {
            RestoreColors();
            foreach (var actor in arrivals) if (actor) actor.Cancel();
            arrivals.Clear();
            if (objects != null) foreach (var controller in objects) if (controller) controller.ReleasePresentation();
            if (fields) fields.ReleasePresentation();
            if (paths) paths.ReleasePresentation();
            objects = null; fields = null; paths = null;
            presentation?.Dispose(); presentation = null;
            playing = false; if (active == this) active = null;
            Blindsided.EventHandler.OnLoadData -= Loaded;
            removeSkip?.Invoke(); removeSkip = null;
            var resume = resumeQuests; resumeQuests = null;
            if (restoreMenu)
            {
                if (openFields) TownWindowManager.Instance?.OpenWindow(TownWindowManager.Window.Farm);
                else resume?.Invoke();
            }
            presentingDialogue = false; openFields = false; completedQuest = null;
            sequence = null;
        }

        private void OnDisable()
        {
            restoreMenu = false;
            if (sequence != null) StopCoroutine(sequence);
            Cleanup();
        }
    }
}
