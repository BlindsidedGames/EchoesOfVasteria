using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

internal static class LocalizationFontsSetup
{
    [MenuItem("Tools/Localization/Create CJK fallback font assets")]
    private static void CreateFonts()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PanelTextSettings>("Assets/UI/Toolkit/TextSettings.asset");
        foreach (var region in new[] { "sc", "jp" })
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Localization/NotoSansCJK" + region + "-Regular.otf");
            if (!font) throw new System.InvalidOperationException("Missing official Noto font " + region);
            var path = "Assets/Resources/Fonts/Localization/NotoCJK-" + region + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (!asset)
            {
                asset = FontAsset.CreateFontAsset(font);
                asset.name = "NotoCJK-" + region;
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                foreach (var texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
            }
            settings.fallbackFontAssets ??= new List<FontAsset>();
            if (!settings.fallbackFontAssets.Contains(asset)) settings.fallbackFontAssets.Add(asset);
            AssetDatabase.SaveAssetIfDirty(asset);

            var tmpPath = "Assets/Resources/Fonts/Localization/NotoCJK-" + region + " TMP.asset";
            var tmp = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(tmpPath);
            if (!tmp)
            {
                tmp = TMP_FontAsset.CreateFontAsset(font);
                tmp.name = "NotoCJK-" + region + " TMP";
                AssetDatabase.CreateAsset(tmp, tmpPath);
                AssetDatabase.AddObjectToAsset(tmp.material, tmp);
                foreach (var texture in tmp.atlasTextures) AssetDatabase.AddObjectToAsset(texture, tmp);
            }
            if (!TMP_Settings.fallbackFontAssets.Contains(tmp)) TMP_Settings.fallbackFontAssets.Add(tmp);
            AssetDatabase.SaveAssetIfDirty(tmp);
        }
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings);
        EditorUtility.SetDirty(TMP_Settings.instance);
        AssetDatabase.SaveAssetIfDirty(TMP_Settings.instance);
        Debug.Log("Created Chinese and Japanese dynamic fallback fonts for UI Toolkit and TMP.");
    }
}
