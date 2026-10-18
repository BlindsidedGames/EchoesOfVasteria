using TMPro;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TimelessEchoes
{
    /// <summary>An invisible character tap target and speech frame anchored to Dadocado's mouth.</summary>
    public sealed class DadJokeWorldView : MonoBehaviour
    {
        private DadJokeManager manager;
        private Canvas canvas;
        private GameObject content;
        private RectTransform bubble;
        private TMP_Text bubbleText;
        private RectTransform tapTarget;
        private SpriteRenderer actor;
        private ToolkitNavigationScreen navigation;
        private ToolkitRunScreen runScreen;
        private float closesAt;

        public void Initialize(DadJokeManager source, GameObject bubbleTemplate)
        {
            manager = source;
            actor = source.WorldAnchor.GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one / 16f;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = actor ? actor.sortingLayerID : 0;
            canvas.sortingOrder = short.MaxValue;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.referencePixelsPerUnit = 16;
            gameObject.AddComponent<GraphicRaycaster>();
            content = new GameObject("Controls", typeof(RectTransform));
            content.transform.SetParent(transform, false);
            Place((RectTransform)transform, Vector2.zero, Vector2.zero);
            transform.localScale = Vector3.one / 16f;
            Place((RectTransform)content.transform, Vector2.zero, Vector2.zero);
            var hit = new GameObject("Dadocado tap target", typeof(RectTransform), typeof(Image), typeof(Button));
            hit.transform.SetParent(content.transform, false);
            tapTarget = (RectTransform)hit.transform;
            Place(tapTarget, new Vector2(16, 27), Vector2.zero);
            tapTarget.pivot = new Vector2(.5f, .5f);
            var hitImage = hit.GetComponent<Image>();
            hitImage.color = Color.clear;
            hitImage.canvasRenderer.cullTransparentMesh = false;
            hitImage.raycastTarget = true;
            var button = hit.GetComponent<Button>();
            button.targetGraphic = hitImage;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(ShowJoke);
            bubble = (RectTransform)Instantiate(bubbleTemplate, content.transform).transform;
            bubble.name = "Dadocado speech bubble";
            // The retired overlay contains a second background and a close-button
            // image. Only the authored speech frame belongs in the world bubble.
            foreach (var graphic in bubble.GetComponentsInChildren<Graphic>(true)) graphic.enabled = false;
            foreach (var oldButton in bubble.GetComponentsInChildren<Button>(true)) oldButton.enabled = false;
            var speechFrame = bubble.GetComponent<Image>();
            if (speechFrame)
            {
                speechFrame.enabled = true;
                // The original sprite border contains the entire pointer, so
                // nine-slicing expands the text panel without stretching its point.
                speechFrame.type = Image.Type.Sliced;
            }
            Place(bubble, new Vector2(84, 40), new Vector2(0, -12.5f));
            // Keep the original pointer facing right toward Dadocado.
            bubble.pivot = Vector2.zero;
            bubble.localScale = new Vector3(-1, 1, 1);
            bubbleText = bubble.GetComponentInChildren<TMP_Text>(true);
            if (bubbleText)
            {
                bubbleText.enabled = true;
                bubbleText.raycastTarget = false;
                bubbleText.transform.localScale = new Vector3(-1, 1, 1);
            }
            var dismiss = bubble.GetComponent<Button>() ?? bubble.gameObject.AddComponent<Button>();
            dismiss.enabled = true;
            dismiss.targetGraphic = bubble.GetComponent<Image>() ?? bubble.GetComponentInChildren<Image>(true);
            if (dismiss.targetGraphic) dismiss.targetGraphic.raycastTarget = true;
            dismiss.onClick.RemoveAllListeners();
            dismiss.onClick.AddListener(() => bubble.gameObject.SetActive(false));
            bubble.gameObject.SetActive(false);
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = size;
            rect.anchoredPosition3D = new Vector3(position.x, position.y, 0);
        }

        private void ShowJoke()
        {
            if (!bubbleText) return;
            bubbleText.text = manager.NextJoke();
            var textRect = bubbleText.rectTransform;
            textRect.pivot = new Vector2(.5f, .5f);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 20);
            textRect.offsetMax = new Vector2(-6, -5);
            bubbleText.enableAutoSizing = false;
            bubbleText.fontSize = 6;
            bubbleText.textWrappingMode = TextWrappingModes.Normal;
            bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(40, bubbleText.GetPreferredValues(bubbleText.text, 66, 0).y + 25));
            bubble.gameObject.SetActive(true);
            closesAt = Time.unscaledTime + 3;
        }

        private void LateUpdate()
        {
            if (!manager || !content) return;
            if (!navigation) navigation = FindAnyObjectByType<ToolkitNavigationScreen>();
            if (!runScreen) runScreen = FindAnyObjectByType<ToolkitRunScreen>();
            var visible = manager.isActiveAndEnabled && GameManager.Instance && GameManager.Instance.IsInTown &&
                !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow) &&
                !(navigation && navigation.HasOpenMenu) && !(runScreen && runScreen.HasOpenDetails) &&
                !ToolkitCharacterCreator.BlocksWorldInput;
            content.SetActive(visible);
            if (!visible) { bubble.gameObject.SetActive(false); return; }
            canvas.worldCamera = Camera.main;
            // The mouth is centred at native pixel (9.5, 12.5); the sprite pivot
            // is (8, -1). Use the character transform so camera motion never shifts it.
            transform.position = manager.WorldAnchor.TransformPoint(new Vector3(1.5f / 16f, 13.5f / 16f, 0));
            if (actor)
            {
                tapTarget.position = actor.bounds.center;
                tapTarget.sizeDelta = (Vector2)actor.sprite.bounds.size * 16;
                var leftEdge = transform.InverseTransformPoint(new Vector3(actor.bounds.min.x, transform.position.y, transform.position.z)).x;
                // Measure the three-pixel gap from the pointer tip, not the panel body.
                var pointerRight = leftEdge - 3;
                bubble.anchoredPosition = new Vector2(pointerRight, -12.5f);
            }
            if (bubble.gameObject.activeSelf && (Time.unscaledTime >= closesAt || Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
                bubble.gameObject.SetActive(false);
        }

    }
}
