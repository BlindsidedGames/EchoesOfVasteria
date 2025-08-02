#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using TimelessEchoes.Quests;

namespace TimelessEchoes.Editor
{
    public class QuestFlowWindow : OdinEditorWindow
    {
        private readonly Dictionary<QuestData, Rect> nodeRects = new();
        private readonly List<QuestData> quests = new();
        private Vector2 scroll;

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
            Refresh();
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
            Repaint();
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

        [Obsolete]
        protected override void OnGUI()
        {
            SirenixEditorGUI.Title("Quest Flow", null, TextAlignment.Left, true);
            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }
            DrawGraph();
        }

        private void DrawGraph()
        {
            float width = nodeRects.Count > 0 ? nodeRects.Values.Max(r => r.xMax) + 100f : position.width;
            float height = nodeRects.Count > 0 ? nodeRects.Values.Max(r => r.yMax) + 100f : position.height;
            var contentRect = new Rect(0, 0, width, height);

            var area = GUILayoutUtility.GetRect(width, height);
            scroll = GUI.BeginScrollView(area, scroll, contentRect);

            Handles.BeginGUI();
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
            Handles.EndGUI();

            foreach (var pair in nodeRects)
            {
                string label = pair.Key.questName.GetLocalizedString();
                if (string.IsNullOrEmpty(label))
                {
                    label = pair.Key.questId;
                }
                GUI.Box(pair.Value, label, EditorStyles.helpBox);
            }

            GUI.EndScrollView();
        }
    }
}
#endif
