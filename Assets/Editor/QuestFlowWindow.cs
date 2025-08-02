#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using TimelessEchoes.Quests;

namespace TimelessEchoes.Editor
{
    public class QuestFlowWindow : OdinEditorWindow
    {
        private readonly Dictionary<QuestData, Rect> nodeRects = new();
        private readonly List<QuestData> quests = new();

        private ScrollView scrollView;
        private VisualElement content;
        private bool isPanning;
        private Vector2 panStart;
        private Vector2 scrollStart;

        [MenuItem("Timeless/Quest Flow")]
        private static void Open()
        {
            var window = GetWindow<QuestFlowWindow>();
            window.titleContent = new GUIContent("Quest Flow");
            window.Refresh();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("Quest Flow");
            SetupUI();
            Refresh();
        }

        private void SetupUI()
        {
            rootVisualElement.Clear();

            var toolbar = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row }
            };
            var refreshButton = new Button(Refresh) { text = "Refresh" };
            toolbar.Add(refreshButton);
            rootVisualElement.Add(toolbar);

            // Use a ScrollView that supports scrolling in both directions.
            // ScrollViewMode.Both has been deprecated; VerticalAndHorizontal
            // provides equivalent functionality in modern Unity versions.
            scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
            {
                style = { flexGrow = 1 }
            };
            rootVisualElement.Add(scrollView);

            content = new VisualElement
            {
                style = { position = Position.Relative }
            };
            scrollView.Add(content);

            scrollView.RegisterCallback<MouseDownEvent>(OnMouseDown);
            scrollView.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            scrollView.RegisterCallback<MouseUpEvent>(OnMouseUp);

            DrawInterface();
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (e.button != 2) return;
            isPanning = true;
            panStart = e.localMousePosition;
            scrollStart = scrollView.scrollOffset;
            e.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            if (!isPanning) return;
            var delta = e.localMousePosition - panStart;
            scrollView.scrollOffset = scrollStart - delta;
            e.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            if (e.button != 2) return;
            isPanning = false;
            e.StopPropagation();
        }

        private void Refresh()
        {
            quests.Clear();
            var guids = AssetDatabase.FindAssets("t:QuestData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
                if (quest != null)
                {
                    quests.Add(quest);
                }
            }

            CalculateLayout();
            DrawInterface();
        }

        private void DrawInterface()
        {
            if (scrollView == null || content == null) return;

            content.Clear();

            float width = nodeRects.Count > 0 ? nodeRects.Values.Max(r => r.xMax) + 100f : position.width;
            float height = nodeRects.Count > 0 ? nodeRects.Values.Max(r => r.yMax) + 100f : position.height;
            content.style.width = width;
            content.style.height = height;

            // IMGUIContainer allows drawing with Handles in the UI Toolkit
            // hierarchy without instantiating the abstract ImmediateModeElement.
            var lines = new IMGUIContainer(() =>
            {
                Handles.color = Color.white;
                foreach (var quest in quests)
                {
                    if (!nodeRects.TryGetValue(quest, out var fromRect)) continue;
                    if (quest.requiredQuests == null) continue;
                    foreach (var req in quest.requiredQuests)
                    {
                        if (req == null || !nodeRects.TryGetValue(req, out var reqRect)) continue;
                        var start = new Vector3(reqRect.xMax, reqRect.center.y);
                        var end = new Vector3(fromRect.xMin, fromRect.center.y);
                        Handles.DrawLine(start, end);
                    }
                }
            });
            lines.StretchToParentSize();
            content.Add(lines);

            foreach (var pair in nodeRects)
            {
                string labelText = pair.Key.questName.GetLocalizedString();
                if (string.IsNullOrEmpty(labelText))
                {
                    labelText = pair.Key.questId;
                }

                var node = new Label(labelText)
                {
                    style =
                    {
                        position = Position.Absolute,
                        left = pair.Value.x,
                        top = pair.Value.y,
                        width = pair.Value.width,
                        height = pair.Value.height
                    }
                };
                node.AddToClassList("quest-node");
                content.Add(node);
            }
        }

        private void CalculateLayout()
        {
            nodeRects.Clear();
            var depth = new Dictionary<QuestData, int>();
            foreach (var quest in quests)
            {
                depth[quest] = GetDepth(quest, new HashSet<QuestData>());
            }

            const float nodeWidth = 180f;
            const float nodeHeight = 40f;
            const float xSpacing = 220f;
            const float ySpacing = 80f;

            var groups = quests.GroupBy(q => depth[q]).OrderBy(g => g.Key);
            foreach (var group in groups)
            {
                int index = 0;
                foreach (var quest in group)
                {
                    float x = 10 + group.Key * xSpacing;
                    float y = 10 + index * ySpacing;
                    nodeRects[quest] = new Rect(x, y, nodeWidth, nodeHeight);
                    index++;
                }
            }
        }

        private int GetDepth(QuestData quest, HashSet<QuestData> visited)
        {
            if (quest == null || !visited.Add(quest) || quest.requiredQuests == null || quest.requiredQuests.Count == 0)
                return 0;

            int max = 0;
            foreach (var req in quest.requiredQuests)
            {
                if (req == null) continue;
                max = Mathf.Max(max, 1 + GetDepth(req, visited));
            }
            visited.Remove(quest);
            return max;
        }
    }
}
#endif

