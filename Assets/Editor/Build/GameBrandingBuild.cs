using System.IO;
using System.Linq;
using System.Xml.Linq;
using TimelessEchoes.Platform;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif

namespace BuildTools
{
    /// <summary>Changes public labels without changing Unity's storage identity.</summary>
    public sealed class GameBrandingBuild : IPostprocessBuildWithReport
#if UNITY_ANDROID
        , IPostGenerateGradleAndroidProject
#endif
    {
        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            string plistPath;
            switch (report.summary.platform)
            {
                case BuildTarget.iOS:
                    plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
                    break;
                case BuildTarget.StandaloneOSX:
                    plistPath = Path.Combine(report.summary.outputPath, "Contents", "Info.plist");
                    break;
                default:
                    return;
            }

            SetAppleDisplayName(plistPath);
        }

        internal static void SetAppleDisplayName(string plistPath)
        {
            var document = XDocument.Load(plistPath, LoadOptions.PreserveWhitespace);
            var dictionary = document.Root?.Element("dict");
            if (dictionary == null)
                throw new BuildFailedException("App branding: missing Info.plist dictionary.");

            var key = dictionary.Elements("key")
                .FirstOrDefault(element => element.Value == "CFBundleDisplayName");
            if (key == null)
                dictionary.Add(new XElement("key", "CFBundleDisplayName"),
                    new XElement("string", GameBranding.DisplayName));
            else if (key.ElementsAfterSelf().FirstOrDefault() is XElement value)
                value.ReplaceWith(new XElement("string", GameBranding.DisplayName));
            else
                throw new BuildFailedException("App branding: malformed CFBundleDisplayName.");

            document.Save(plistPath, SaveOptions.DisableFormatting);
        }

#if UNITY_ANDROID
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Unity passes unityLibrary. The launcher owns the application label;
            // the Unity activity may also override that label.
            SetAndroidDisplayName(Path.Combine(path, "src", "main", "AndroidManifest.xml"));
            SetAndroidDisplayName(Path.Combine(path, "..", "launcher", "src", "main", "AndroidManifest.xml"));
            SetAndroidAppNameResources(path);
            SetAndroidAppNameResources(Path.Combine(path, "..", "launcher"));
        }
#endif

        internal static void SetAndroidDisplayName(string manifestPath)
        {
            if (!File.Exists(manifestPath))
                throw new BuildFailedException("App branding: missing generated Android manifest: " + manifestPath);

            var document = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
            XNamespace android = "http://schemas.android.com/apk/res/android";
            var application = document.Root?.Element("application");
            if (application == null)
                throw new BuildFailedException("App branding: missing Android application element.");

            application.SetAttributeValue(android + "label", GameBranding.DisplayName);
            foreach (var activity in application.Elements().Where(element =>
                         element.Name.LocalName == "activity" || element.Name.LocalName == "activity-alias"))
            {
                var isLauncher = activity.Elements("intent-filter").Any(filter =>
                    filter.Elements("action").Any(action =>
                        (string)action.Attribute(android + "name") == "android.intent.action.MAIN") &&
                    filter.Elements("category").Any(category =>
                        (string)category.Attribute(android + "name") == "android.intent.category.LAUNCHER"));
                if (isLauncher)
                    activity.SetAttributeValue(android + "label", GameBranding.DisplayName);
            }

            document.Save(manifestPath, SaveOptions.DisableFormatting);
        }

        internal static void SetAndroidAppNameResources(string modulePath)
        {
            var resources = Path.Combine(modulePath, "src", "main", "res");
            if (!Directory.Exists(resources)) return;
            foreach (var directory in Directory.EnumerateDirectories(resources, "values*"))
            foreach (var file in Directory.EnumerateFiles(directory, "*.xml"))
            {
                var document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
                var names = document.Root?.Elements("string")
                    .Where(element => (string)element.Attribute("name") == "app_name").ToArray();
                if (names == null || names.Length == 0) continue;
                foreach (var name in names) name.Value = GameBranding.DisplayName;
                document.Save(file, SaveOptions.DisableFormatting);
            }
        }
    }
}
