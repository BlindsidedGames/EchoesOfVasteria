using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine;
using UnityEngine.Localization;

// Configures standard authoring mappings only. It never pulls or pushes a Sheet.
internal static class LocalizationSheetsSetup
{
    private const string Workbook = "1OVkC28D9Mb7R6WX-FZuJ0q1dsyqF7WdozTQFKOoMTLg";
    private static readonly string[] Codes = { "en", "ru", "fr", "de", "es-419", "pt-BR", "zh-CN", "ja" };
    [MenuItem("Tools/Localization/Prepare replacement local Sheet service")]
    private static void PrepareLocalService()
    {
        const string path = "Assets/Localization/Sheets Service Provider.asset";
        // This exact asset and its meta are gitignored. Never reset an existing provider.
        var provider = AssetDatabase.LoadAssetAtPath<SheetsServiceProvider>(path);
        if (!provider)
        {
            if (System.IO.File.Exists(path)) throw new InvalidOperationException("An unreadable provider asset exists; preserving it.");
            provider = ScriptableObject.CreateInstance<SheetsServiceProvider>();
            provider.name = "Sheets Service Provider";
            provider.ApplicationName = "Echoes of Vasteria Localization Editor";
            provider.SetOAuthCredentials(string.Empty, string.Empty);
            AssetDatabase.CreateAsset(provider, path);
            AssetDatabase.SaveAssetIfDirty(provider);
        }
        foreach (var name in new[] { "Quests", "Wiki", "TownUI" })
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(name);
            var extension = collection.Extensions.OfType<GoogleSheetsExtension>().Single();
            if (extension.SpreadsheetId != Workbook) throw new InvalidOperationException("Unexpected workbook; provider not relinked.");
            extension.SheetsServiceProvider = provider;
            EditorUtility.SetDirty(collection); AssetDatabase.SaveAssetIfDirty(collection);
        }
        Selection.activeObject = provider;
        Debug.Log("Prepared local Google Sheets Service configuration and linked existing workbook tabs. OAuth fields are unconfigured; no authorization or API request occurred.");
    }
    [MenuItem("Tools/Localization/Configure eight-language Sheet mappings")]
    private static void Configure()
    {
        try
        {
            var quests = LocalizationEditorSettings.GetStringTableCollection("Quests");
            var reference = quests.Extensions.OfType<GoogleSheetsExtension>().Single();
            // A restored PPtr can remain unresolved in an already-loaded collection;
            // rebind the same approved local provider without touching its credentials.
            var localProvider = AssetDatabase.LoadAssetAtPath<SheetsServiceProvider>("Assets/Localization/Sheets Service Provider.asset");
            if (reference.SpreadsheetId != Workbook)
                throw new InvalidOperationException("Existing workbook differs from the reviewed configuration.");
            foreach (var name in new[] { "Quests", "Wiki", "TownUI" })
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(name);
                if (!collection || Codes.Any(code => !collection.GetTable(new LocaleIdentifier(code))))
                    throw new InvalidOperationException("Missing collection or locale table: " + name);
                var extension = collection.Extensions.OfType<GoogleSheetsExtension>().SingleOrDefault();
                if (extension != null && (extension.SpreadsheetId != Workbook ||
                    extension.Columns.Any(column => column.Column == "A" && !(column is KeyColumn)) ||
                    extension.Columns.Any(column => column.Column == "B" && !(column is KeyCommentColumn))))
                    throw new InvalidOperationException("Existing mapping conflicts: " + name);
                if (extension != null)
                    for (var i = 0; i < Codes.Length; i++)
                    {
                        var column = ((char)('C' + i)).ToString();
                        var current = extension.Columns.SingleOrDefault(value => value.Column == column);
                        if (current != null && (!(current is LocaleColumn locale) || locale.LocaleIdentifier.Code != Codes[i]))
                            throw new InvalidOperationException("Unexpected locale column " + name + "/" + column);
                    }
            }
            foreach (var name in new[] { "Quests", "Wiki", "TownUI" })
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(name);
                var extension = collection.Extensions.OfType<GoogleSheetsExtension>().SingleOrDefault();
                if (extension == null)
                {
                    extension = new GoogleSheetsExtension { SpreadsheetId = Workbook, SheetId = 1914820999,
                        SheetsServiceProvider = reference.SheetsServiceProvider, RemoveMissingPulledKeys = false };
                    extension.Columns.Add(new KeyColumn { Column = "A" });
                    extension.Columns.Add(new KeyCommentColumn { Column = "B" });
                    collection.AddExtension(extension);
                }
                if (localProvider) extension.SheetsServiceProvider = localProvider;
                for (var i = 0; i < Codes.Length; i++)
                {
                    var column = ((char)('C' + i)).ToString();
                    var current = extension.Columns.SingleOrDefault(value => value.Column == column);
                    if (current != null && (!(current is LocaleColumn locale) || locale.LocaleIdentifier.Code != Codes[i]))
                        throw new InvalidOperationException("Unexpected locale column " + name + "/" + column);
                    if (current == null) extension.Columns.Add(new LocaleColumn { Column = column,
                        LocaleIdentifier = new LocaleIdentifier(Codes[i]), IncludeComments = false });
                }
                EditorUtility.SetDirty(collection); AssetDatabase.SaveAssetIfDirty(collection);
            }
            Debug.Log("Configured Quests/Wiki/Interface Sheet mappings A:J for all eight locales. Local provider resolved: " + (bool)localProvider + ". No Sheet data transferred; K remains context only.");
            if (!reference.SheetsServiceProvider)
                Debug.LogWarning("Existing Google Sheets authorization asset is missing. Restore/connect the local service provider before direct Unity sync; no credentials were created.");
        }
        catch (Exception ex) { Debug.LogError("Sheet mapping configuration stopped: " + ex.Message); }
    }
}
