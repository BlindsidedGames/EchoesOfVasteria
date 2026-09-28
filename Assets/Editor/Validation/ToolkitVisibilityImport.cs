using System.Collections.Generic;
using TimelessEchoes.NPC;
using TimelessEchoes.Quests;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using Rule = TimelessEchoes.UI.Toolkit.ToolkitVisibilityRule;

namespace TimelessEchoes.EditorTools
{
    /// <summary>Authoring-only conversion of gates on an object and its ancestors.</summary>
    public static class ToolkitVisibilityImport
    {
        public static Rule[] Read(Transform target)
        {
            var rules = new List<Rule>();
            foreach (var controller in Object.FindObjectsByType<QuestObjectStateController>(FindObjectsInactive.Include))
            {
                var entries = new SerializedObject(controller).FindProperty("entries");
                for (var i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    var quest = entry.FindPropertyRelative("quest").objectReferenceValue as QuestData;
                    Add(entry, "disableUntilComplete", Rule.Condition.QuestComplete, quest);
                    Add(entry, "enableUntilComplete", Rule.Condition.QuestIncomplete, quest);
                    Add(entry, "enableWhileInProgress", Rule.Condition.QuestInProgress, quest);
                }
            }
            foreach (var controller in Object.FindObjectsByType<NpcObjectStateController>(FindObjectsInactive.Include))
            {
                var entries = new SerializedObject(controller).FindProperty("entries");
                for (var i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    var id = entry.FindPropertyRelative("npcId").stringValue;
                    Add(entry, "disableUntilMet", Rule.Condition.NpcMet, null, id);
                    Add(entry, "enableUntilMet", Rule.Condition.NpcNotMet, null, id);
                }
            }
            foreach (var controller in Object.FindObjectsByType<LocationObjectStateController>(FindObjectsInactive.Include))
            {
                var serialized = new SerializedObject(controller);
                foreach (var name in new[] { "enableInTown", "enableInRun" })
                {
                    var entries = serialized.FindProperty(name);
                    for (var i = 0; i < entries.arraySize; i++)
                    {
                        var entry = entries.GetArrayElementAtIndex(i);
                        if (!Applies(entry.FindPropertyRelative("gameObject").objectReferenceValue)) continue;
                        rules.Add(new Rule { condition = name == "enableInTown" ? Rule.Condition.InTown : Rule.Condition.InRun });
                        var quest = entry.FindPropertyRelative("requiredQuest").objectReferenceValue as QuestData;
                        if (quest) rules.Add(new Rule { condition = Rule.Condition.QuestComplete, quest = quest });
                    }
                }
            }
            return rules.ToArray();

            bool Applies(Object value) => value is GameObject go && (target == go.transform || target.IsChildOf(go.transform));
            void Add(SerializedProperty entry, string field, Rule.Condition condition, QuestData quest, string npcId = null)
            {
                var objects = entry.FindPropertyRelative(field);
                for (var i = 0; i < objects.arraySize; i++)
                    if (Applies(objects.GetArrayElementAtIndex(i).objectReferenceValue))
                        rules.Add(new Rule { condition = condition, quest = quest, npcId = npcId });
            }
        }
    }
}
