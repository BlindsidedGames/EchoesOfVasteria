using System;
using System.Linq;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace TimelessEchoes.EditorTools
{
    /// <summary>One-time authoring import. Runtime Toolkit views never inspect legacy hierarchies.</summary>
    public static class ToolkitBookMigration
    {
        private const string Root = "Assets/UI/Toolkit";

        [MenuItem("Tools/UI Toolkit/Import Credits reference")]
        public static void ImportCredits()
        {
            var source = Resources.FindObjectsOfTypeAll<Transform>()
                .Single(t => t.gameObject.scene.IsValid() && t.name == "Credits_Window");
            var parent = source.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true).content;
            var book = LoadOrCreate<ToolkitBookDefinition>(Root + "/Credits.asset");
            book.presentation = ToolkitBookDefinition.Presentation.Credits;
            book.sections = parent.Cast<Transform>().Where(t => t.gameObject.activeSelf).Select((entry, index) =>
            {
                var texts = entry.GetComponentsInChildren<TMP_Text>(true);
                return new ToolkitBookDefinition.Section
                {
                    id = "credit-" + index,
                    title = ImportText(texts.Single(t => t.name == "Matt"), "credits." + index + ".title"),
                    body = ImportText(texts.Single(t => t.name == "Detail"), "credits." + index + ".body")
                };
            }).ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/UI Toolkit/Import Library reference")]
        public static void ImportLibrary()
        {
            var source = Resources.FindObjectsOfTypeAll<Transform>()
                .Single(t => t.gameObject.scene.IsValid() && t.name == "Library_Window");
            var book = LoadOrCreate<ToolkitBookDefinition>(Root + "/Library.asset");
            book.sections = source.GetComponentsInChildren<WikiUIToggle>(true).Select((toggle, index) =>
            {
                var serialized = new SerializedObject(toggle);
                var bodyRoot = (GameObject)serialized.FindProperty("toggleObject").objectReferenceValue;
                var body = bodyRoot.GetComponentInChildren<TMP_Text>(true);
                var title = toggle.GetComponentsInChildren<TMP_Text>(true).First(t => t != body);
                return new ToolkitBookDefinition.Section
                {
                    id = "chapter-" + index,
                    title = ImportText(title, "library." + index + ".title"),
                    body = ImportText(body, "library." + index + ".body"),
                    expanded = !toggle.startClosed,
                    visibility = ToolkitVisibilityImport.Read(toggle.transform)
                };
            }).ToArray();
            EditorUtility.SetDirty(book);

            var theme = LoadOrCreate<ToolkitTheme>(Root + "/Theme.asset");
            theme.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources_moved/Fonts/Noto_Sans/static/NotoSans-ExtraBold.ttf");
            theme.styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(Root + "/Theme.uss");
            theme.window = Sprite("UI_Frames", "UI_Frames_4");
            theme.recessed = Sprite("UI_Frames", "UI_Frames_1");
            theme.row = Sprite("UI_Frames", "UI_Frames_14");
            theme.button = Sprite("UI_Buttons", "5");
            theme.expand = Sprite("UI_Icons", "UI_Icons_86");
            theme.collapse = Sprite("UI_Icons", "UI_Icons_95");
            theme.scrollThumb = Sprite("UI_Sliders", "UI_Sliders_50");
            var scroll = source.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            theme.scrollTrack = scroll.verticalScrollbar.GetComponent<UnityEngine.UI.Image>().sprite;
            EditorUtility.SetDirty(theme);
            ImportTextSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Imported native Toolkit Library content and sprite references.");
        }

        private static void ImportTextSettings()
        {
            var source = TMP_Settings.defaultSpriteAsset;
            var sprites = LoadOrCreate<SpriteAsset>(Root + "/InlineSprites.asset");
            var serializedSprites = new SerializedObject(sprites);
            serializedSprites.FindProperty("m_SpriteAtlasTexture").objectReferenceValue = source.spriteSheet;
            var serializedSource = new SerializedObject(source);
            serializedSprites.CopyFromSerializedProperty(serializedSource.FindProperty("m_FaceInfo"));
            serializedSprites.ApplyModifiedPropertiesWithoutUndo();
            sprites.spriteGlyphTable.Clear();
            sprites.spriteCharacterTable.Clear();
            foreach (var glyph in source.spriteGlyphTable)
                sprites.spriteGlyphTable.Add(new SpriteGlyph(glyph.index, glyph.metrics,
                    glyph.glyphRect, glyph.scale, glyph.atlasIndex, glyph.sprite));
            // Preserve character ordering: existing localized prose references sprite indices.
            foreach (var character in source.spriteCharacterTable)
            {
                var glyph = sprites.spriteGlyphTable.Single(g => g.index == character.glyphIndex);
                sprites.spriteCharacterTable.Add(new SpriteCharacter(character.unicode, sprites, glyph)
                    { name = character.name, scale = character.scale });
            }
            var materialPath = Root + "/InlineSprites.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(Shader.Find("TextMeshPro/Sprite"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.mainTexture = source.spriteSheet;
            sprites.material = material;
            sprites.UpdateLookupTables();
            var settings = LoadOrCreate<PanelTextSettings>(Root + "/TextSettings.asset");
            settings.defaultSpriteAsset = sprites;
            EditorUtility.SetDirty(sprites);
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(settings);
        }

        internal static ToolkitBookDefinition.Text ImportText(TMP_Text source, string key)
        {
            var result = new ToolkitBookDefinition.Text { key = key, fallback = source.text,
                bold = source.fontStyle.HasFlag(TMPro.FontStyles.Bold),
                smallCaps = source.fontStyle.HasFlag(TMPro.FontStyles.SmallCaps) };
            var localization = source.GetComponent<LocalizeStringEvent>();
            if (localization != null)
            {
                // Copy the table/key, not the source component or its TMP target.
                result.localized.TableReference = localization.StringReference.TableReference;
                result.localized.TableEntryReference = localization.StringReference.TableEntryReference;
            }
            return result;
        }

        private static Sprite Sprite(string sheet, string name) => AssetDatabase
            .LoadAllAssetsAtPath("Assets/Art/Packs/Cute_Fantasy_UI/" + sheet + ".png")
            .OfType<Sprite>().Single(sprite => sprite.name == name);

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
