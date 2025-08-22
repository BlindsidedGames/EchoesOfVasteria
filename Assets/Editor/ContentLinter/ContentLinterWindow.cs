#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TimelessEchoes.Buffs;
using TimelessEchoes.Quests;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

namespace TimelessEchoes.EditorTools
{
    /// <summary>
    /// Content Linter + ID Registry
    /// - Scans QuestData, TaskData, Resource, and BuffRecipe assets for common content issues
    /// - Provides Fix All for duplicate/empty/invalid IDs and obvious data errors
    /// </summary>
    public class ContentLinterWindow : EditorWindow
    {
        private const string WindowTitle = "Content Linter";

        [MenuItem("Tools/Content Linter")] private static void Open()
        {
            var wnd = GetWindow<ContentLinterWindow>(utility: false, title: WindowTitle, focus: true);
            wnd.minSize = new Vector2(700, 400);
            wnd.Show();
        }

        private readonly List<Issue> issues = new List<Issue>();
        private Vector2 scroll;
        private bool groupQuests = true, groupTasks = true, groupResources = true, groupBuffs = true;

        private enum Severity { Info, Warning, Error }

        private class Issue
        {
            public string Category;
            public Severity Level;
            public string Message;
            public UnityEngine.Object Context;
            public Action Fix;
            public bool CanFix => Fix != null;
        }

        private void OnEnable()
        {
            ScanAll();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Rescan", EditorStyles.toolbarButton))
                {
                    ScanAll();
                }

                if (GUILayout.Button("Apply All Auto-Fixes", EditorStyles.toolbarButton))
                {
                    ApplyAllFixes();
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Issues: {issues.Count}", EditorStyles.miniLabel, GUILayout.Width(90));
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawSectionHeader("Quests", ref groupQuests);
            if (groupQuests) DrawIssues("Quest");

            GUILayout.Space(8);
            DrawSectionHeader("Tasks", ref groupTasks);
            if (groupTasks) DrawIssues("Task");

            GUILayout.Space(8);
            DrawSectionHeader("Resources", ref groupResources);
            if (groupResources) DrawIssues("Resource");

            GUILayout.Space(8);
            DrawSectionHeader("Buffs", ref groupBuffs);
            if (groupBuffs) DrawIssues("Buff");

            EditorGUILayout.EndScrollView();
        }

        private void DrawSectionHeader(string title, ref bool expanded)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                expanded = EditorGUILayout.Foldout(expanded, title, true);
                GUILayout.FlexibleSpace();
                var count = issues.Count(i => i.Category.StartsWith(title, StringComparison.OrdinalIgnoreCase));
                EditorGUILayout.LabelField(count.ToString(), GUILayout.Width(40));
            }
        }

        private void DrawIssues(string categoryContains)
        {
            var filtered = issues.Where(i => i.Category.IndexOf(categoryContains, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            foreach (var issue in filtered)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var color = GUI.color;
                    GUI.color = issue.Level switch
                    {
                        Severity.Error => new Color(1f, 0.5f, 0.5f),
                        Severity.Warning => new Color(1f, 1f, 0.6f),
                        _ => Color.white
                    };
                    EditorGUILayout.LabelField($"[{issue.Level}] {issue.Message}", GUILayout.ExpandWidth(true));
                    GUI.color = color;

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeObject = issue.Context;

                    using (new EditorGUI.DisabledScope(!issue.CanFix))
                    {
                        if (GUILayout.Button("Fix", GUILayout.Width(50)))
                        {
                            try
                            {
                                issue.Fix?.Invoke();
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Fix failed: {e}");
                            }
                        }
                    }
                }
            }
        }

        private void ApplyAllFixes()
        {
            var applied = 0;
            foreach (var issue in issues.ToList())
            {
                if (!issue.CanFix) continue;
                try
                {
                    issue.Fix?.Invoke();
                    applied++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Fix failed: {e}");
                }
            }

            if (applied > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ScanAll();
            }
        }

        private void ScanAll()
        {
            issues.Clear();
            ScanQuests();
            ScanTasks();
            ScanResources();
            ScanBuffs();
            Repaint();
        }

        #region Quest Scans

        private void ScanQuests()
        {
            var guids = AssetDatabase.FindAssets("t:QuestData");
            var quests = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<QuestData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();

            // ID uniqueness
            var idToAssets = new Dictionary<string, List<QuestData>>(StringComparer.OrdinalIgnoreCase);
            foreach (var q in quests)
            {
                var id = q.questId ?? string.Empty;
                if (!idToAssets.TryGetValue(id, out var list))
                {
                    list = new List<QuestData>();
                    idToAssets[id] = list;
                }
                list.Add(q);
            }

            foreach (var pair in idToAssets)
            {
                var id = pair.Key;
                var list = pair.Value;
                if (string.IsNullOrWhiteSpace(id))
                {
                    foreach (var q in list)
                    {
                        AddIssue(
                            category: "Quests",
                            Severity.Error,
                            $"Quest '{q.name}' has empty questId",
                            q,
                            fix: () => AssignUniqueQuestId(q, quests)
                        );
                    }
                }
                else if (list.Count > 1)
                {
                    // Keep first, fix others
                    foreach (var q in list.Skip(1))
                    {
                        AddIssue(
                            category: "Quests",
                            Severity.Error,
                            $"Duplicate questId '{id}' on '{q.name}'",
                            q,
                            fix: () => AssignUniqueQuestId(q, quests)
                        );
                    }
                }
            }

            // Localization presence checks
            foreach (var q in quests)
            {
                if (IsEmptyLocalized(q.questName))
                {
                    AddIssue("Quests", Severity.Warning,
                        $"Quest '{q.name}' missing localized questName entry",
                        q, fix: null);
                }
                if (IsEmptyLocalized(q.description))
                {
                    AddIssue("Quests", Severity.Warning,
                        $"Quest '{q.name}' missing localized description entry",
                        q, fix: null);
                }
                if (IsEmptyLocalized(q.rewardDescription))
                {
                    AddIssue("Quests", Severity.Info,
                        $"Quest '{q.name}' missing rewardDescription entry",
                        q, fix: null);
                }

                // Null required quest references
                if (q.requiredQuests != null && q.requiredQuests.Any(r => r == null))
                {
                    AddIssue("Quests", Severity.Warning,
                        $"Quest '{q.name}' has null references in requiredQuests list",
                        q, fix: null);
                }

                // intentionally do not report empty npcId
            }
        }

        private static bool IsEmptyLocalized(LocalizedString ls)
        {
            if (ls == null) return true;
            if (ls.IsEmpty) return true;
            // Table and entry refs must be set
            var tableEmpty = ls.TableReference == null || string.IsNullOrEmpty(ls.TableReference.TableCollectionName);
            var entryEmpty = ls.TableEntryReference.ReferenceType == UnityEngine.Localization.Tables.TableEntryReference.Type.Empty;
            return tableEmpty || entryEmpty;
        }

        private void AssignUniqueQuestId(QuestData quest, List<QuestData> all)
        {
            var existing = new HashSet<string>(all.Where(a => a != quest).Select(a => a.questId ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);
            var baseId = Slugify(quest.name);
            if (string.IsNullOrEmpty(baseId)) baseId = "quest";

            var candidate = baseId;
            var idx = 1;
            while (existing.Contains(candidate))
            {
                candidate = $"{baseId}-{idx++}";
            }

            Undo.RecordObject(quest, "Assign Unique Quest ID");
            quest.questId = candidate;
            EditorUtility.SetDirty(quest);
        }

        #endregion

        #region Task Scans

        private void ScanTasks()
        {
            var guids = AssetDatabase.FindAssets("t:TaskData");
            var tasks = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<TaskData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();

            // Unique taskID
            var idToAssets = new Dictionary<int, List<TaskData>>();
            foreach (var t in tasks)
            {
                if (!idToAssets.TryGetValue(t.taskID, out var list))
                {
                    list = new List<TaskData>();
                    idToAssets[t.taskID] = list;
                }
                list.Add(t);
            }

            var usedIds = new HashSet<int>(tasks.Where(t => t.taskID > 0).Select(t => t.taskID));
            var nextId = usedIds.Count > 0 ? usedIds.Max() + 1 : 1;

            foreach (var t in tasks)
            {
                if (t.taskID <= 0)
                {
                    AddIssue("Tasks", Severity.Error,
                        $"Task '{t.name}' has invalid taskID ({t.taskID})",
                        t,
                        fix: () => AssignTaskId(t, ref nextId, usedIds));
                }
            }

            foreach (var pair in idToAssets)
            {
                var id = pair.Key;
                var list = pair.Value;
                if (id <= 0) continue; // already handled above
                if (list.Count > 1)
                {
                    foreach (var t in list.Skip(1))
                    {
                        AddIssue("Tasks", Severity.Error,
                            $"Duplicate taskID {id} on '{t.name}'",
                            t,
                            fix: () => AssignTaskId(t, ref nextId, usedIds));
                    }
                }
            }

            foreach (var t in tasks)
            {
                if (t.taskPrefab == null)
                {
                    AddIssue("Tasks", Severity.Error,
                        $"Task '{t.name}' has no taskPrefab assigned",
                        t, fix: null);
                }

                if (t.minX > t.maxX)
                {
                    AddIssue("Tasks", Severity.Error,
                        $"Task '{t.name}' has minX > maxX ({t.minX} > {t.maxX})",
                        t,
                        fix: () =>
                        {
                            Undo.RecordObject(t, "Swap min/max X");
                            var tmp = t.minX;
                            t.minX = t.maxX;
                            t.maxX = tmp;
                            EditorUtility.SetDirty(t);
                        });
                }

                if (t.spawnTerrains == null || t.spawnTerrains.Count == 0)
                {
                    AddIssue("Tasks", Severity.Info,
                        $"Task '{t.name}' has no spawnTerrains configured (may be intended)", t, fix: null);
                }

                if (t.resourceDrops != null && t.resourceDrops.Any(rd => rd == null))
                {
                    AddIssue("Tasks", Severity.Warning,
                        $"Task '{t.name}' has null entries in resourceDrops",
                        t, fix: null);
                }
            }
        }

        private void AssignTaskId(TaskData task, ref int nextId, HashSet<int> used)
        {
            while (used.Contains(nextId)) nextId++;
            Undo.RecordObject(task, "Assign Task ID");
            task.taskID = nextId;
            used.Add(nextId);
            nextId++;
            EditorUtility.SetDirty(task);
        }

        #endregion

        #region Resource Scans

        private void ScanResources()
        {
            var guids = AssetDatabase.FindAssets("t:Resource");
            var resources = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<Resource>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();

            // Unique resourceID mapping
            var idToAssets = new Dictionary<int, List<Resource>>();
            foreach (var r in resources)
            {
                if (!idToAssets.TryGetValue(r.resourceID, out var list))
                {
                    list = new List<Resource>();
                    idToAssets[r.resourceID] = list;
                }
                list.Add(r);
            }

            var usedIds = new HashSet<int>(resources.Where(r => r.resourceID > 0).Select(r => r.resourceID));
            var nextId = usedIds.Count > 0 ? usedIds.Max() + 1 : 1;

            // Invalid IDs
            foreach (var r in resources)
            {
                if (r.resourceID <= 0)
                {
                    AddIssue("Resources", Severity.Error,
                        $"Resource '{r.name}' has invalid resourceID ({r.resourceID})",
                        r,
                        fix: () => AssignResourceId(r, ref nextId, usedIds));
                }
            }

            // Duplicates
            foreach (var pair in idToAssets)
            {
                var id = pair.Key;
                var list = pair.Value;
                if (id <= 0) continue; // handled above
                if (list.Count > 1)
                {
                    foreach (var r in list.Skip(1))
                    {
                        AddIssue("Resources", Severity.Error,
                            $"Duplicate resourceID {id} on '{r.name}'",
                            r,
                            fix: () => AssignResourceId(r, ref nextId, usedIds));
                    }
                }
            }

            // Optional hint: missing icon mapping in ResourceIconLookup
            foreach (var r in resources)
            {
                if (r.resourceID > 0 && !ResourceIconLookup.TryGetIconIndex(r.resourceID, out _))
                {
                    AddIssue("Resources", Severity.Info,
                        $"Resource '{r.name}' (ID {r.resourceID}) has no mapping in ResourceIconLookup",
                        r, fix: null);
                }
            }
        }

        private void AssignResourceId(Resource res, ref int nextId, HashSet<int> used)
        {
            while (used.Contains(nextId)) nextId++;
            Undo.RecordObject(res, "Assign Resource ID");
            res.resourceID = nextId;
            used.Add(nextId);
            nextId++;
            EditorUtility.SetDirty(res);
        }

        #endregion

        #region Buff Scans

        private void ScanBuffs()
        {
            var guids = AssetDatabase.FindAssets("t:BuffRecipe");
            var buffs = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<BuffRecipe>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();

            foreach (var b in buffs)
            {
                // Required quest can be null; if set but missing, warn
                // (Unity ensures object refs are either valid or null, so we only check list-like scenarios in BuffRecipe if needed.)

                // Duration sanity hints
                if (b.baseEffects == null || b.baseEffects.Count == 0)
                {
                    AddIssue("Buffs", Severity.Info,
                        $"Buff '{b.name}' has no baseEffects (durationType: {b.durationType})",
                        b, fix: null);
                }

                if (b.buffIcon == null)
                {
                    AddIssue("Buffs", Severity.Info,
                        $"Buff '{b.name}' has no buffIcon (UI will fallback or look empty)", b, fix: null);
                }
            }
        }

        #endregion

        private void AddIssue(string category, Severity level, string message, UnityEngine.Object context, Action fix)
        {
            issues.Add(new Issue
            {
                Category = category,
                Level = level,
                Message = message,
                Context = context,
                Fix = fix
            });
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            value = value.Trim().ToLowerInvariant();
            value = Regex.Replace(value, @"[^a-z0-9]+", "-");
            value = Regex.Replace(value, @"-+", "-");
            value = value.Trim('-');
            return value;
        }
    }
}
#endif


