#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using Blindsided.Utilities.Pooling;
using NUnit.Framework;
using TimelessEchoes.Characters;
using TimelessEchoes.MapGeneration;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        // Real mouse input and mine prefabs protect the complete click-to-sprite path,
        // including UI interception, drag rejection and pooled decoration reuse.
        [UnityTest]
        public IEnumerator MinePotsBreakOnClickButNotThroughMenusOrDragsAndResetWhenReused()
        {
            var owner = new GameObject("Protected mine pot interaction");
            var mouse = InputSystem.AddDevice<Mouse>();
            GameObject pot = null;
            try
            {
                oracle.saveData.Character = CharacterCatalog.Load().NewProfile();
                oracle.saveData.Character.Created = true;
                var cameraObject = new GameObject("Pot test camera", typeof(Camera));
                cameraObject.transform.SetParent(owner.transform); cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 2; camera.depth = 20;
                camera.transform.position = new Vector3(0, 0, -10);
                var events = new GameObject("Pot test events", typeof(EventSystem)); events.transform.SetParent(owner.transform);
                var overlay = new GameObject("Blocking menu", typeof(Canvas), typeof(GraphicRaycaster));
                overlay.transform.SetParent(owner.transform); overlay.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var blocker = new GameObject("Menu surface", typeof(RectTransform), typeof(Image));
                blocker.transform.SetParent(overlay.transform, false);
                var rect = blocker.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                overlay.SetActive(false);

                IEnumerator Pointer(Vector2 position, bool pressed)
                {
                    var state = new MouseState { position = position };
                    InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left, pressed));
                    yield return null; yield return null;
                }

                for (var variant = 1; variant <= 2; variant++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Decor/Random Dungeon/Jar {variant}.prefab");
                    var broken = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Environment/Mines/Pots/Jar_{variant}_Broken.png");
                    pot = PoolManager.Get(prefab); pot.transform.position = Vector3.zero;
                    var visual = pot.GetComponent<SpriteRenderer>(); var intact = visual.sprite;
                    var point = (Vector2)camera.WorldToScreenPoint(visual.bounds.center);
                    overlay.SetActive(true); yield return null;
                    yield return Pointer(point, true); yield return Pointer(point, false);
                    Assert.AreEqual(intact, visual.sprite, "Menus must intercept clicks above a pot.");
                    overlay.SetActive(false); yield return null;
                    yield return Pointer(point, true); yield return Pointer(point + Vector2.right * 30, true);
                    yield return Pointer(point, false);
                    Assert.AreEqual(intact, visual.sprite, "Dragging away and back must not smash a pot.");
                    yield return Pointer(point, true); yield return Pointer(point, false);
                    Assert.AreEqual(broken, visual.sprite, "A world click must smash the matching mine pot.");
                    Assert.AreEqual(Vector3.zero, pot.transform.position, "Breaking must not shift the pot's ground anchor.");
                    yield return Pointer(point, true); yield return Pointer(point, false);
                    Assert.AreEqual(broken, visual.sprite, "Repeated clicks leave the pot broken.");
                    PoolManager.Release(pot); pot = PoolManager.Get(prefab);
                    Assert.AreEqual(intact, pot.GetComponent<SpriteRenderer>().sprite, "A recycled decoration starts intact.");
                    PoolManager.Release(pot); pot = null;
                }
            }
            finally
            {
                if (pot) PoolManager.Release(pot);
                Object.DestroyImmediate(owner);
                foreach (var reader in Object.FindObjectsByType<PotClickInput>(FindObjectsSortMode.None)) Object.DestroyImmediate(reader.gameObject);
                InputSystem.RemoveDevice(mouse);
            }
        }
    }
}
#endif
