using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

// Reads authored tables directly: changing the preview never changes player/Editor locale preferences.
internal sealed class LocalizationReviewWindow : EditorWindow
{
    private static readonly string[] Codes = { "en", "fr", "de", "es-419", "pt-BR", "zh-CN", "ru", "ja" };
    private string code = "en";
    private VisualElement preview;
    [MenuItem("Tools/Localization/Review translated layout and glyphs")]
    private static void Open() => GetWindow<LocalizationReviewWindow>("Localization review");

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        var chooser = new DropdownField("Preview locale", Codes.ToList(), System.Array.IndexOf(Codes, code));
        chooser.RegisterValueChangedCallback(change => { code = change.newValue; Render(); });
        rootVisualElement.Add(chooser);
        rootVisualElement.Add(new Label("Authored table and font preview; no game state, saves or account services."));
        var scroll = new ScrollView(); scroll.style.flexGrow = 1; rootVisualElement.Add(scroll);
        preview = new VisualElement(); preview.style.width = 768; preview.style.minHeight = 432;
        preview.style.paddingLeft = preview.style.paddingRight = preview.style.paddingTop = preview.style.paddingBottom = 12;
        preview.style.backgroundColor = new Color(.1f, .11f, .15f); preview.style.color = new Color(.92f, .9f, .83f);
        preview.style.fontSize = 12; scroll.Add(preview); Render();
    }
    private string Text(string collection, string key, params object[] arguments)
    {
        var table = LocalizationEditorSettings.GetStringTableCollection(collection)?.GetTable(new LocaleIdentifier(code)) as StringTable;
        var entry = table?.GetEntry(key);
        if (entry == null) return "[Missing " + collection + "/" + key + "]";
        return entry.GetLocalizedString(arguments);
    }
    private Label Label(VisualElement parent, string text, float size = 12)
    {
        var label = new Label(text) { enableRichText = true };
        label.style.whiteSpace = WhiteSpace.Normal; label.style.fontSize = size;
        label.style.marginBottom = 8; parent.Add(label); return label;
    }
    private void Render()
    {
        if (preview == null) return;
        preview.Clear();
        var theme = AssetDatabase.LoadAssetAtPath<ToolkitTheme>("Assets/UI/Toolkit/Theme.asset");
        var font = code == "ja" || code == "zh-CN"
            ? Resources.Load<FontAsset>("Fonts/Localization/NotoCJK-" + (code == "ja" ? "jp" : "sc")) : null;
        preview.style.unityFontDefinition = font ? FontDefinition.FromSDFFont(font) : FontDefinition.FromFont(theme.gameplayFont ? theme.gameplayFont : theme.font);
        Label(preview, Text("Quests", "You get this one for free!.name"), 20);
        Label(preview, Text("Quests", "You get this one for free!.desc"));
        Label(preview, Text("TownUI", "fields.heading", "Fields", 20), 18);
        Label(preview, Text("TownUI", "fields.rates", 1.25, .8));
        var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; preview.Add(row);
        foreach (var heading in new[] { "fields.seeds-title", "fields.beds-title", "fields.construction-title" })
        {
            var column = new VisualElement(); column.style.width = 236; column.style.paddingRight = 12; row.Add(column);
            Label(column, Text("TownUI", heading), 15);
            Label(column, Text("TownUI", "fields.recipe-level-required", "Radish", 35));
            Label(column, Text("TownUI", "fields.repeat-help"));
            var button = new Button { text = Text("TownUI", "fields.action.harvest-ready") };
            button.style.whiteSpace = WhiteSpace.Normal; button.style.minHeight = 32; column.Add(button);
        }
        Label(preview, Text("TownUI", "options.transfer.import-committed"));
        Label(preview, "Actual language-selector contents; selection actions removed in this review.", 12);
        var languageFrame = new VisualElement();
        theme.Apply(languageFrame); ToolkitGameplay.Apply(languageFrame, theme);
        languageFrame.AddToClassList("surface");
        languageFrame.style.position = Position.Relative;
        languageFrame.style.left = languageFrame.style.right = languageFrame.style.top = languageFrame.style.bottom = StyleKeyword.Auto;
        languageFrame.style.height = StyleKeyword.Auto;
        languageFrame.style.flexShrink = 0;
        preview.Add(languageFrame);
        var prefab = PrefabUtility.LoadPrefabContents("Assets/UI/Toolkit/OptionsDialogs.prefab");
        try
        {
            var dialog = prefab.GetComponent<ToolkitOptionsDialogs>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(ToolkitOptionsDialogs).GetField("frame", flags).SetValue(dialog, languageFrame);
            typeof(ToolkitOptionsDialogs).GetMethod("BuildLanguages", flags).Invoke(dialog, null);
            foreach (var button in languageFrame.Query<Button>().ToList()) button.clickable = null;
            // A preview must never select a live player preference. Explicit CJK primary
            // covers the native labels in this Editor panel's separate text environment.
            languageFrame.style.unityFontDefinition = FontDefinition.FromSDFFont(Resources.Load<FontAsset>("Fonts/Localization/NotoCJK-sc"));
            typeof(ToolkitOptionsDialogs).GetField("frame", flags).SetValue(dialog, null);
            Debug.Log("Authored language-selector review: " + languageFrame.Query<Button>().ToList().Count + " normal player buttons.");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        if (font)
        {
            var characters = string.Join("", new[] { "Quests", "Wiki", "TownUI" }.SelectMany(collection =>
                ((StringTable)LocalizationEditorSettings.GetStringTableCollection(collection).GetTable(new LocaleIdentifier(code)))
                    .Values.Select(entry => entry.LocalizedValue))).Distinct();
            var missing = new List<uint>();
            foreach (var character in characters)
                if (!char.IsControl(character) && !char.IsSurrogate(character) && !font.HasCharacter((uint)character, true, true)) missing.Add(character);
            Debug.Log("Localization glyph review " + code + ": " + missing.Count + " missing catalog characters: " + string.Join(",", missing.Select(c => "U+" + c.ToString("X4"))));
        }
    }
}
