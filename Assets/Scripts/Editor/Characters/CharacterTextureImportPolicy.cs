using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TimelessEchoes.Editor.Characters
{
    /// <summary>Keep the modular wardrobe compressed on iOS without reducing its pixel dimensions.</summary>
    public sealed class CharacterTextureImportPolicy : IPreprocessBuildWithReport
    {
        private const string ArtRoot = "Assets/Art/Character/Modular/";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Do not install a global texture postprocessor: changing editor code
            // would invalidate unrelated texture imports throughout the project.
            if (report.summary.platform == BuildTarget.iOS) ApplyExisting();
        }

        private static bool Configure(TextureImporter importer)
        {
            var settings = importer.GetPlatformTextureSettings("iPhone");
            // An inactive platform override can contain a lower legacy size limit.
            // Preserve the effective default size when enabling the override.
            var size = settings.overridden ? settings.maxTextureSize : importer.maxTextureSize;
            if (settings.overridden && settings.format == TextureImporterFormat.ASTC_4x4 &&
                settings.maxTextureSize == size && settings.compressionQuality == 100)
                return false;

            settings.name = "iPhone";
            settings.overridden = true;
            settings.maxTextureSize = size;
            settings.format = TextureImporterFormat.ASTC_4x4;
            settings.compressionQuality = 100;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        [MenuItem("Tools/Characters/Apply iOS Texture Compression")]
        public static int ApplyExisting()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play mode before changing texture imports.");

            var changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot.TrimEnd('/') }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer || !Configure(importer))
                        continue;
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            Debug.Log($"Applied iOS ASTC 4x4 compression to {changed} character textures.");
            return changed;
        }
    }
}
