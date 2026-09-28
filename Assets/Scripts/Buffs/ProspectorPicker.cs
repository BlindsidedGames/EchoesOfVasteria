using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TimelessEchoes.Buffs
{
    public sealed class ProspectorPicker : MonoBehaviour
    {
        public RectTransform safeArea, panel, content;
        public ScrollRect scroll;
        public Button rowPrefab, confirm, cancel, previousSkill, nextSkill;
        public TMP_Text selectedText, skillText;
        public Color selectedRowTint = new Color(.7843137f, .7843137f, .7843137f, 1f);
        private readonly List<Button> rows = new();
        private readonly List<TaskData> rowTasks = new();
        private TaskData[] tasks;
        private string[] skills;
        private int skillIndex;
        private TaskData selected;
        private GameObject owner, previousFocus;
        private System.Action confirmed;
        private Vector2 screenSize;
        private Rect lastSafeArea;
        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOpenState() => IsOpen = false;

        private void Awake()
        {
            if (!enabled) return; // Retired presentation must not subscribe or build hidden UI.
            confirm.onClick.AddListener(Confirm);
            cancel.onClick.AddListener(Close);
            previousSkill.onClick.AddListener(() => Filter(-1));
            nextSkill.onClick.AddListener(() => Filter(1));
        }

        public void Open(GameObject owningWindow, System.Action onConfirmed)
        {
            owner = owningWindow;
            confirmed = onConfirmed;
            previousFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            selected = BuffManager.Instance.ProspectorTarget;
            tasks = Resources.LoadAll<TaskData>("Tasks").Where(t => t != null && t.weight > 0 &&
                TaskWeightService.IsTaskUnlocked(t)).OrderBy(t => t.taskName).ToArray();
            skills = new[] { "All skills" }.Concat(tasks.Select(SkillName).Distinct().OrderBy(s => s)).ToArray();
            skillIndex = 0;
            gameObject.SetActive(true);
            IsOpen = true;
            Resize();
            Refresh();
            EventSystem.current?.SetSelectedGameObject(cancel.gameObject);
        }

        private static string SkillName(TaskData task) => task.associatedSkill != null ? task.associatedSkill.name : "Other";
        private void Filter(int delta) { skillIndex = (skillIndex + delta + skills.Length) % skills.Length; Refresh(); }
        private void Refresh()
        {
            skillText.text = skills[skillIndex];
            var visible = tasks.Where(t => skillIndex == 0 || SkillName(t) == skills[skillIndex]).ToArray();
            while (rows.Count < visible.Length)
            {
                var row = Instantiate(rowPrefab, content);
                int index = rows.Count;
                rows.Add(row);
                rowTasks.Add(null);
                row.onClick.AddListener(() => { selected = rowTasks[index]; RefreshSelection(); });
                row.gameObject.SetActive(true);
            }
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                row.gameObject.SetActive(i < visible.Length);
                if (i >= visible.Length) continue;
                var task = visible[i];
                rowTasks[i] = task;
                var rect = (RectTransform)row.transform;
                rect.anchoredPosition = new Vector2(0, -i * 68f);
                var icon = row.transform.Find("IconFrame/Icon").GetComponent<Image>();
                icon.sprite = task.taskIcon;
                // Match the resource/task views: native sprite proportions, clipped by
                // the icon frame instead of compressing tall sprites into a square.
                if (icon.sprite != null) icon.SetNativeSize();
                // Uniform magnification in the popup's touch-sized control units. Keep
                // this independent of canvas scale so high-DPI phones do not get tiny icons.
                icon.rectTransform.localScale = Vector3.one * 2.5f;
                icon.enabled = icon.sprite != null;
                row.GetComponentInChildren<TMP_Text>().text = task.taskName;

            }
            content.sizeDelta = new Vector2(0, visible.Length * 68f);
            scroll.verticalNormalizedPosition = 1;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            selectedText.text = selected != null ? "Target: " + selected.taskName : "Choose a task";
            confirm.interactable = selected != null && TaskWeightService.IsTaskUnlocked(selected);
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].gameObject.activeSelf)
                    rows[i].image.color = selected != null && rowTasks[i] == selected
                        ? rowPrefab.image.color * selectedRowTint : rowPrefab.image.color;
        }

        private void Confirm()
        {
            if (BuffManager.Instance != null && BuffManager.Instance.SetProspectorTarget(selected))
            { confirmed?.Invoke(); Close(); }
        }

        public void Close() { EndInteraction(); gameObject.SetActive(false); }
        private void OnDisable() => EndInteraction();
        private void EndInteraction()
        {
            IsOpen = false;
            confirmed = null;
            if (previousFocus != null && previousFocus.activeInHierarchy)
                EventSystem.current?.SetSelectedGameObject(previousFocus);
            previousFocus = null;
        }

        private void Update()
        {
            if (owner == null || !owner.activeInHierarchy || TimelessEchoes.Stats.GameplayStatTracker.Instance?.RunInProgress == true)
            { Close(); return; }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Close(); return; }
            if (screenSize != new Vector2(Screen.width, Screen.height) || lastSafeArea != Screen.safeArea) Resize();
        }

        private void Resize()
        {
            screenSize = new Vector2(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            // Parent canvases use very small reference heights (288/432). Work in device-independent
            // control sizes, converting through their scale rather than squeezing the list away.
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            float pixelDensity = Application.isMobilePlatform && Screen.dpi > 0
                ? Mathf.Clamp(Screen.dpi / 160f, 1f, 5f) : Mathf.Max(1f, Screen.height / 1080f);
            ApplyLayout(safeArea.rect.size, pixelDensity / Mathf.Max(.01f, canvas.scaleFactor));
        }

        public void ApplyLayout(Vector2 available, float controlScale)
        {
            controlScale = Mathf.Max(.01f, controlScale);
            panel.localScale = Vector3.one * controlScale;
            // The existing UI draws one sprite pixel per canvas unit (16 PPU). Layout uses
            // device-independent control sizes, but its extra scale must not shrink the
            // sliced borders. Counter it here so borders match neighbouring native panels.
            // New pooled rows inherit this setting from rowPrefab after layout.
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
            {
                if (image.type == Image.Type.Sliced)
                    image.pixelsPerUnitMultiplier = controlScale;
            }
            // Match Buffs_SV inside ScrollViews: left/right/top 2, bottom 1 native
            // canvas units. Its hidden Image + Mask viewport fills the inner scroll area.
            var scrollArea = (RectTransform)scroll.transform;
            scrollArea.offsetMin = new Vector2(2, 1) / controlScale;
            scrollArea.offsetMax = new Vector2(-2, -2) / controlScale;
            panel.sizeDelta = new Vector2(Mathf.Min(640, available.x / controlScale - 24),
                Mathf.Min(720, available.y / controlScale - 24));
            bool compact = panel.rect.height < 500 && panel.rect.width > 520;
            SetBox((RectTransform)previousSkill.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(16, -72), new Vector2(80, -16));
            SetBox((RectTransform)nextSkill.transform, Vector2.one, Vector2.one,
                new Vector2(-80, -72), new Vector2(-16, -16));
            SetBox(skillText.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(84, -72), new Vector2(-84, -16));
            var area = (RectTransform)scroll.transform.parent;
            area.offsetMin = new Vector2(16, compact ? 112 : 130);
            area.offsetMax = new Vector2(-16, -84);
            SetBox(selectedText.rectTransform, Vector2.zero, new Vector2(1, 0),
                new Vector2(16, 78), new Vector2(-16, compact ? 110 : 126));
        }

        private static void SetBox(RectTransform rect, Vector2 min, Vector2 max, Vector2 lower, Vector2 upper)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = lower; rect.offsetMax = upper;
        }
    }
}
