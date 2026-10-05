using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

// Narrow, preflighted authoring changes after a frozen translation batch is complete.
internal static class LocalizationSourceDelta
{
    [Serializable] private sealed class Delta { public Row[] entries; }
    [Serializable] private sealed class Row { public string collection, id, key, expectedEnglish; public bool newEntry; public Value[] values; }
    [Serializable] private sealed class Value { public string locale, text; }
    [MenuItem("Tools/Localization/Apply reviewed source delta")]
    private static void Apply()
    {
        var path = EditorUtility.OpenFilePanel("Reviewed localization source delta", "", "json");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var delta = JsonUtility.FromJson<Delta>(File.ReadAllText(path));
            if (delta?.entries == null || delta.entries.Length == 0) throw new InvalidOperationException("Empty delta.");
            // Check every affected key, ID, English value and destination before changing assets.
            foreach (var row in delta.entries)
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(row.collection);
                if (!collection) throw new InvalidOperationException("Unknown collection.");
                var id = long.Parse(row.id, CultureInfo.InvariantCulture);
                var shared = collection.SharedData.GetEntry(id);
                var english = collection.GetTable(new LocaleIdentifier("en")) as StringTable;
                if (row.newEntry)
                {
                    if (shared != null || collection.SharedData.GetEntry(row.key) != null) throw new InvalidOperationException("New key or ID already exists.");
                }
                else if (shared?.Key != row.key || english?.GetEntry(id)?.LocalizedValue != row.expectedEnglish)
                    throw new InvalidOperationException("Source changed: " + row.key);
                if (row.values == null || row.values.Length == 0) throw new InvalidOperationException("No localized values.");
                foreach (var value in row.values)
                    if (!(collection.GetTable(new LocaleIdentifier(value.locale)) is StringTable) || value.text == null)
                        throw new InvalidOperationException("Missing destination: " + value.locale);
            }
            foreach (var row in delta.entries)
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(row.collection);
                var id = long.Parse(row.id, CultureInfo.InvariantCulture);
                if (row.newEntry) collection.SharedData.AddKey(row.key, id);
                foreach (var value in row.values)
                {
                    var table = (StringTable)collection.GetTable(new LocaleIdentifier(value.locale));
                    table.AddEntry(id, value.text);
                    EditorUtility.SetDirty(table); AssetDatabase.SaveAssetIfDirty(table);
                }
                if (row.newEntry) { EditorUtility.SetDirty(collection.SharedData); AssetDatabase.SaveAssetIfDirty(collection.SharedData); }
            }
            Debug.Log("Applied " + delta.entries.Length + " reviewed localization source changes; shared IDs preserved.");
        }
        catch (Exception ex) { Debug.LogError("Localization delta stopped: " + ex.Message); }
    }
}
