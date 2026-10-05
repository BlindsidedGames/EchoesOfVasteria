using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

// Uses the installed Unity Localization authoring APIs, including Addressables registration.
// Preflight the full frozen English catalogue before writing any locale or table asset.
internal static class TranslationCatalogImport
{
    [Serializable] private sealed class SourceCatalog { public string englishHash; public SourceRow[] entries; }
    [Serializable] private sealed class SourceRow { public string collection, id, key, english; }
    [Serializable] private sealed class TranslationCatalog { public string locale, sourceHash; public TranslationRow[] entries; }
    [Serializable] private sealed class TranslationRow { public string collection, id, key, translation; }
    [Serializable] private sealed class NumericReview { public string locale, sourceHash; public NumericReviewRow[] entries, rows; }
    [Serializable] private sealed class NumericReviewRow { public string collection, id, key, english, translation, reason; }
    private static readonly HashSet<string> Supported = new HashSet<string> { "fr", "de", "es-419", "pt-BR", "zh-CN", "ru", "ja" };
    [MenuItem("Tools/Localization/Set native locale display names")]
    private static void SetNativeNames()
    {
        var names = new[] { ("en", "English (en)"), ("fr", "Français (fr)"), ("de", "Deutsch (de)"),
            ("es-419", "Español · Latinoamérica (es-419)"), ("pt-BR", "Português · Brasil (pt-BR)"),
            ("zh-CN", "简体中文 (zh-CN)"), ("ru", "Русский (ru)"), ("ja", "日本語 (ja)") };
        foreach (var item in names)
        {
            var locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(item.Item1));
            if (!locale) throw new InvalidOperationException("Missing locale " + item.Item1);
        }
        foreach (var item in names)
        {
            var locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(item.Item1));
            locale.LocaleName = item.Item2;
            EditorUtility.SetDirty(locale); AssetDatabase.SaveAssetIfDirty(locale);
        }
        Debug.Log("Eight locale display names now use native language names; locale identifiers unchanged.");
    }
    // Unity and YAML tooling can differ in trailing spaces on folded lines; wording remains exact.
    private static string NormalizeSource(string value) => string.Join("\n", (value ?? "").Split('\n').Select(line => line.TrimEnd(' ', '\t', '\r')));
    private static string Contracts(string value)
    {
        var remaining = Regex.Replace(value ?? "", @"\{[^{}]*\}", "");
        if (remaining.Contains("{") || remaining.Contains("}"))
            throw new InvalidOperationException("Unsupported nested or unbalanced format; validate and extend the importer before authoring it.");
        return string.Join("\n", Regex.Matches(value ?? "", @"\{[^{}]*\}|<[^>]+>|[\x00-\x1f]|\\[nrt]")
            .Cast<Match>().Select(match => match.Value).OrderBy(token => token, StringComparer.Ordinal));
    }
    private static string Numbers(string value) => string.Join("\n", Regex.Matches(value ?? "", @"\d+(?:[.,]\d+)*")
        .Cast<Match>().Select(match => match.Value).OrderBy(token => token, StringComparer.Ordinal));

    [MenuItem("Tools/Localization/Import complete translation folder")]
    private static void ImportFolder()
    {
        var sourcePath = SessionState.GetString("EoV.Localization.FrozenSource", "");
        if (!File.Exists(sourcePath)) sourcePath = EditorUtility.OpenFilePanel("Frozen English catalog", "", "json");
        if (string.IsNullOrEmpty(sourcePath)) return;
        var directory = EditorUtility.OpenFolderPanel("Validated translation catalogs", Path.GetDirectoryName(sourcePath), "");
        if (string.IsNullOrEmpty(directory)) return;
        SessionState.SetString("EoV.Localization.FrozenSource", sourcePath);
        foreach (var locale in Supported.OrderBy(code => code))
        {
            var path = Path.Combine(directory, "translation-" + locale + ".json");
            if (!File.Exists(path)) continue;
            try { ImportFiles(sourcePath, path); }
            catch (Exception ex) { Debug.LogError("Translation import stopped for " + locale + ": " + ex.Message); break; }
        }
    }

    [MenuItem("Tools/Localization/Import validated translation catalog")]
    private static void Import()
    {
        var sourcePath = SessionState.GetString("EoV.Localization.FrozenSource", "");
        if (!File.Exists(sourcePath)) sourcePath = EditorUtility.OpenFilePanel("Frozen English catalog", "", "json");
        if (string.IsNullOrEmpty(sourcePath)) return;
        var translationPath = EditorUtility.OpenFilePanel("Complete translation catalog", Path.GetDirectoryName(sourcePath), "json");
        if (string.IsNullOrEmpty(translationPath)) return;
        SessionState.SetString("EoV.Localization.FrozenSource", sourcePath);
        try { ImportFiles(sourcePath, translationPath); }
        catch (Exception ex) { Debug.LogError("Translation import stopped: " + ex.Message); }
    }

    private static void ImportFiles(string sourcePath, string translationPath)
    {
        var source = JsonUtility.FromJson<SourceCatalog>(File.ReadAllText(sourcePath));
        var translation = JsonUtility.FromJson<TranslationCatalog>(File.ReadAllText(translationPath));
        if (source?.entries == null || source.entries.Length == 0 || translation?.entries == null ||
            !Supported.Contains(translation.locale) || string.IsNullOrEmpty(source.englishHash) ||
            source.englishHash != translation.sourceHash) throw new InvalidOperationException("Locale, source hash or catalog is invalid.");
        var collections = new Dictionary<string, StringTableCollection>();
        var sourceRows = new Dictionary<string, SourceRow>();
        string Identity(string collection, string id) => collection + "\n" + id;
        var reviews = new Dictionary<string, NumericReviewRow>();
        var reviewPath = Path.Combine(Path.GetDirectoryName(translationPath), Path.GetFileNameWithoutExtension(translationPath) + ".numeric-review.json");
        if (File.Exists(reviewPath))
        {
            var review = JsonUtility.FromJson<NumericReview>(File.ReadAllText(reviewPath));
            if (review.sourceHash != source.englishHash || review.locale != translation.locale)
                throw new InvalidOperationException("Numeric equivalence review belongs to another source or locale.");
            foreach (var row in review.entries ?? review.rows ?? Array.Empty<NumericReviewRow>())
                reviews.Add(Identity(row.collection, row.id), row);
        }
        foreach (var row in source.entries)
        {
            if (!collections.TryGetValue(row.collection, out var collection))
            {
                collection = LocalizationEditorSettings.GetStringTableCollection(row.collection);
                if (!collection) throw new InvalidOperationException("Unknown collection " + row.collection);
                collections.Add(row.collection, collection);
            }
            var id = long.Parse(row.id, CultureInfo.InvariantCulture);
            var shared = collection.SharedData.GetEntry(id);
            var english = collection.GetTable(new LocaleIdentifier("en")) as StringTable;
            if (shared == null || shared.Key != row.key || NormalizeSource(english?.GetEntry(id)?.LocalizedValue) != NormalizeSource(row.english))
            {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(sourcePath), "unity-english-mismatch.json"),
                    JsonUtility.ToJson(new SourceRow { collection = row.collection, id = row.id, key = row.key,
                        english = english?.GetEntry(id)?.LocalizedValue }, true));
                throw new InvalidOperationException("English source changed: " + row.collection + "/" + row.key);
            }
            sourceRows.Add(Identity(row.collection, row.id), row);
        }
        foreach (var pair in collections)
            if (((StringTable)pair.Value.GetTable(new LocaleIdentifier("en"))).Count != source.entries.Count(row => row.collection == pair.Key))
                throw new InvalidOperationException("English source inventory changed: " + pair.Key + "; actual " + ((StringTable)pair.Value.GetTable(new LocaleIdentifier("en"))).Count +
                    ", frozen " + source.entries.Count(row => row.collection == pair.Key));
        var seen = new HashSet<string>();
        foreach (var row in translation.entries)
        {
            var identity = Identity(row.collection, row.id);
            if (!seen.Add(identity) || !sourceRows.TryGetValue(identity, out var original) || row.key != original.key ||
                row.translation == null || (!string.IsNullOrEmpty(original.english) && string.IsNullOrWhiteSpace(row.translation)))
                throw new InvalidOperationException("Invalid, duplicate or blank translation: " + row.key);
            if (Contracts(original.english) != Contracts(row.translation) || string.IsNullOrEmpty(original.english) != string.IsNullOrEmpty(row.translation))
                throw new InvalidOperationException("Translation formatting contract changed: " + row.key);
            if (Numbers(original.english) != Numbers(row.translation) &&
                (!reviews.TryGetValue(identity, out var review) || review.key != row.key || review.english != original.english ||
                    review.translation != row.translation || string.IsNullOrWhiteSpace(review.reason)))
                throw new InvalidOperationException("Unreviewed numeric representation changed: " + row.key);
        }
        if (seen.Count != sourceRows.Count) throw new InvalidOperationException("Translation does not cover the complete source catalog.");

        var locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(translation.locale));
        if (!locale)
        {
            locale = Locale.CreateLocale(translation.locale);
            locale.Metadata.AddMetadata(new FallbackLocale(LocalizationEditorSettings.GetLocale(new LocaleIdentifier("en"))));
            AssetDatabase.CreateAsset(locale, "Assets/Localization/Languages/" + translation.locale + ".asset");
            LocalizationEditorSettings.AddLocale(locale);
        }
        foreach (var group in translation.entries.GroupBy(row => row.collection))
        {
            var collection = collections[group.Key];
            var table = collection.GetTable(locale.Identifier) as StringTable;
            if (!table) table = (StringTable)collection.AddNewTable(locale.Identifier);
            var english = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            foreach (var row in group)
            {
                var id = long.Parse(row.id, CultureInfo.InvariantCulture);
                var entry = table.AddEntry(id, row.translation);
                entry.IsSmart = english.GetEntry(id)?.IsSmart == true;
            }
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssetIfDirty(table);
            AssetDatabase.SaveAssetIfDirty(collection);
        }
        Debug.Log("Imported " + translation.entries.Length + " translations for " + translation.locale + "; English, shared IDs and other locales preserved.");
    }
}
