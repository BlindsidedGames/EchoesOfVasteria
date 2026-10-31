#if UNITY_IOS
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace TimelessEchoes.Editor
{
    public static class LiveActivityIosPostprocess
    {
        [PostProcessBuild(1200)]
        public static void Configure(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;
            var enabled = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS)
                .Split(';').Contains("EOV_BACKGROUND_PROTOTYPE")
                || Environment.GetCommandLineArgs().Contains("-eov-background-prototype");
            if (!enabled) return;

            const string folder = "EchoesLiveActivity";
            var source = Path.Combine(Application.dataPath, "Editor/iOSLiveActivity");
            var destination = Path.Combine(buildPath, folder);
            Directory.CreateDirectory(destination);
            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            var main = project.GetUnityMainTargetGuid();
            var framework = project.GetUnityFrameworkTargetGuid();

            foreach (var name in new[] { "EOVForecastAttributes.swift", "EOVLiveActivityBridge.swift", "EOVRefreshIntent.swift", "EOVLiveActivity.mm" })
            {
                File.Copy(Path.Combine(source, "Native", name), Path.Combine(destination, name), true);
                if (name.EndsWith(".swift"))
                {
                    var oldGuid = project.FindFileGuidByProjectPath(folder + "/" + name);
                    if (!string.IsNullOrEmpty(oldGuid)) project.RemoveFileFromBuild(framework, oldGuid);
                }
                AddSource(project, name.EndsWith(".mm") ? framework : main, folder + "/" + name);
            }
            foreach (var name in new[] { "EOVForecastWidget.swift", "EOVWatchForecastView.swift" })
                File.Copy(Path.Combine(source, "Templates", name), Path.Combine(destination, name), true);

            var info = new PlistDocument();
            info.root.SetString("CFBundleExecutable", "$(EXECUTABLE_NAME)");
            info.root.SetString("CFBundleIdentifier", "$(PRODUCT_BUNDLE_IDENTIFIER)");
            info.root.SetString("CFBundleName", "$(PRODUCT_NAME)");
            info.root.SetString("CFBundleInfoDictionaryVersion", "6.0");
            info.root.SetString("CFBundleDevelopmentRegion", "en");
            info.root.SetString("CFBundleDisplayName", "Echoes Forecast");
            info.root.SetString("CFBundleShortVersionString", PlayerSettings.bundleVersion);
            info.root.SetString("CFBundleVersion", PlayerSettings.iOS.buildNumber);
            info.root.SetString("CFBundlePackageType", "XPC!");
            info.root.CreateDict("NSExtension").SetString("NSExtensionPointIdentifier", "com.apple.widgetkit-extension");
            var infoPath = folder + "/Info.plist";
            info.WriteToFile(Path.Combine(buildPath, infoPath));
            var extension = project.TargetGuidByName(folder);
            if (string.IsNullOrEmpty(extension))
                extension = project.AddAppExtension(main, folder,
                    PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) + ".liveactivity", infoPath);
            AddSource(project, extension, folder + "/EOVForecastAttributes.swift");
            AddSource(project, extension, folder + "/EOVRefreshIntent.swift");
            project.UpdateBuildProperty(main, "SWIFT_ACTIVE_COMPILATION_CONDITIONS", new[] { "EOV_LIVE_ACTIVITY_HOST" }, null);
            AddSource(project, extension, folder + "/EOVForecastWidget.swift");
            AddSource(project, extension, folder + "/EOVWatchForecastView.swift");
            // WidgetKit resolves named images through a compiled asset catalog.
            var catalog = Path.Combine(destination, "ForecastArt.xcassets");
            Directory.CreateDirectory(catalog);
            File.WriteAllText(Path.Combine(catalog, "Contents.json"), "{\"info\":{\"author\":\"xcode\",\"version\":1}}");
            foreach (var art in Directory.GetFiles(Path.Combine(source, "Art"), "*.png"))
            {
                var name = Path.GetFileName(art);
                var imageSet = Path.Combine(catalog, Path.GetFileNameWithoutExtension(art) + ".imageset");
                Directory.CreateDirectory(imageSet);
                File.Copy(art, Path.Combine(imageSet, name), true);
                File.WriteAllText(Path.Combine(imageSet, "Contents.json"),
                    "{\"images\":[{\"filename\":\"" + name + "\",\"idiom\":\"universal\",\"scale\":\"1x\"}],\"info\":{\"author\":\"xcode\",\"version\":1}}");
            }
            AddSource(project, extension, folder + "/ForecastArt.xcassets");
            ConfigureCompanion(project, main, framework, source, destination, folder);

            foreach (var guid in new[] { main, framework, extension })
            {
                project.SetBuildProperty(guid, "SWIFT_VERSION", "5.0");
                project.SetBuildProperty(guid, "CLANG_ENABLE_MODULES", "YES");
                project.AddFrameworkToProject(guid, "ActivityKit.framework", true);
                project.AddFrameworkToProject(guid, "SwiftUI.framework", true);
                project.AddFrameworkToProject(guid, "AppIntents.framework", true);
            }
            project.AddFrameworkToProject(extension, "WidgetKit.framework", false);
            project.SetBuildProperty(extension, "IPHONEOS_DEPLOYMENT_TARGET", "16.2");
            project.SetBuildProperty(extension, "APPLICATION_EXTENSION_API_ONLY", "YES");
            project.SetBuildProperty(extension, "SKIP_INSTALL", "YES");
            project.SetBuildProperty(extension, "ASSETCATALOG_COMPILER_APPICON_NAME", "");
            project.SetBuildProperty(extension, "TARGETED_DEVICE_FAMILY", "1,2");
            project.SetBuildProperty(extension, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks @executable_path/../../Frameworks");
            project.SetBuildProperty(main, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            project.SetBuildProperty(framework, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            project.WriteToFile(projectPath);
            var appInfoPath = Path.Combine(buildPath, "Info.plist");
            var appInfo = new PlistDocument();
            appInfo.ReadFromFile(appInfoPath);
            appInfo.root.SetBoolean("NSSupportsLiveActivities", true);
            appInfo.WriteToFile(appInfoPath);
            Debug.Log("[EOVLiveActivity] Prototype-only native bridge and widget extension configured.");
        }

        private static void ConfigureCompanion(PBXProject project, string main, string framework,
            string source, string destination, string folder)
        {
            var companionFolder = folder + "/Companion";
            Directory.CreateDirectory(Path.Combine(destination, "Companion"));
            foreach (var name in new[] { "EOVWatchSnapshot.swift", "EOVWatchPhoneSession.swift", "EOVWatchApp.swift" })
                File.Copy(Path.Combine(source, "Companion", name), Path.Combine(destination, "Companion", name), true);
            AddSource(project, framework, companionFolder + "/EOVWatchSnapshot.swift");
            AddSource(project, main, companionFolder + "/EOVWatchSnapshot.swift");
            AddSource(project, framework, companionFolder + "/EOVWatchPhoneSession.swift");
            project.AddFrameworkToProject(framework, "WatchConnectivity.framework", true);

            const string nameOfTarget = "EchoesWatchCompanion";
            var watch = project.TargetGuidByName(nameOfTarget);
            if (string.IsNullOrEmpty(watch))
                {
                watch = project.AddTarget(nameOfTarget, "app", "com.apple.product-type.application");
                project.AddSourcesBuildPhase(watch);
                project.AddResourcesBuildPhase(watch);
                project.AddFrameworksBuildPhase(watch);
            }
            var hostId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            var info = new PlistDocument();
            info.root.SetString("CFBundleExecutable", "$(EXECUTABLE_NAME)");
            info.root.SetString("CFBundleIdentifier", "$(PRODUCT_BUNDLE_IDENTIFIER)");
            info.root.SetString("CFBundleName", "$(PRODUCT_NAME)");
            info.root.SetString("CFBundleDisplayName", "Echoes");
            info.root.SetString("CFBundlePackageType", "APPL");
            info.root.SetString("CFBundleInfoDictionaryVersion", "6.0");
            info.root.SetString("CFBundleShortVersionString", PlayerSettings.bundleVersion);
            info.root.SetString("CFBundleVersion", PlayerSettings.iOS.buildNumber);
            info.root.SetBoolean("WKApplication", true);
            info.root.SetBoolean("WKWatchOnly", false);
            info.root.SetBoolean("WKRunsIndependentlyOfCompanionApp", false);
            info.root.SetString("WKCompanionAppBundleIdentifier", hostId);
            var infoPath = companionFolder + "/WatchInfo.plist";
            info.WriteToFile(Path.Combine(destination, "Companion/WatchInfo.plist"));
            project.SetBuildProperty(watch, "INFOPLIST_FILE", infoPath);
            project.SetBuildProperty(watch, "PRODUCT_NAME", nameOfTarget);
            project.SetBuildProperty(watch, "PRODUCT_BUNDLE_IDENTIFIER", hostId + ".watchkitapp");
            project.SetBuildProperty(watch, "SWIFT_VERSION", "5.0");
            project.SetBuildProperty(watch, "CLANG_ENABLE_MODULES", "YES");
            project.SetBuildProperty(watch, "WATCHOS_DEPLOYMENT_TARGET", "11.0");
            project.SetBuildProperty(watch, "TARGETED_DEVICE_FAMILY", "4");
            // Unity's project-wide arm64 setting is for iOS. watchOS 11 also needs arm64_32.
            project.SetBuildProperty(watch, "ARCHS", "$(ARCHS_STANDARD)");
            project.SetBuildProperty(watch, "ONLY_ACTIVE_ARCH", "NO");
            var simulator = PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK;
            project.SetBuildProperty(watch, "SDKROOT", simulator ? "watchsimulator" : "watchos");
            project.SetBuildProperty(watch, "SUPPORTED_PLATFORMS", simulator ? "watchsimulator" : "watchos watchsimulator");
            var iconCatalog = companionFolder + "/WatchAssets.xcassets";
            var iconSet = Path.Combine(destination, "Companion/WatchAssets.xcassets/AppIcon.appiconset");
            Directory.CreateDirectory(iconSet);
            File.Copy(Path.Combine(Application.dataPath, "Art/AppIcon.png"), Path.Combine(iconSet, "AppIcon.png"), true);
            File.WriteAllText(Path.Combine(iconSet, "Contents.json"),
                "{\"images\":[{\"filename\":\"AppIcon.png\",\"idiom\":\"universal\",\"platform\":\"watchos\",\"size\":\"1024x1024\"}],\"info\":{\"author\":\"xcode\",\"version\":1}}");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(iconSet), "Contents.json"),
                "{\"info\":{\"author\":\"xcode\",\"version\":1}}");
            AddSource(project, watch, iconCatalog);
            project.SetBuildProperty(watch, "ASSETCATALOG_COMPILER_APPICON_NAME", "AppIcon");
            project.SetBuildProperty(watch, "SKIP_INSTALL", "YES");
            project.SetBuildProperty(watch, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            project.AddFrameworkToProject(watch, "SwiftUI.framework", false);
            project.AddFrameworkToProject(watch, "WatchConnectivity.framework", false);
            AddSource(project, watch, companionFolder + "/EOVWatchSnapshot.swift");
            AddSource(project, watch, companionFolder + "/EOVWatchApp.swift");
            AddSource(project, watch, folder + "/EOVWatchForecastView.swift");
            AddSource(project, watch, folder + "/ForecastArt.xcassets");
            project.AddTargetDependency(main, watch);
            // Embed before Unity's dSYM postprocess to avoid an Info.plist dependency cycle.
            var embed = project.GetCopyFilesBuildPhaseByTarget(main, "Embed Watch Content", "$(CONTENTS_FOLDER_PATH)/Watch", "16");
            if (string.IsNullOrEmpty(embed))
            {
                embed = project.AddCopyFilesBuildPhaseBeforeTargetPostprocess(main, "Embed Watch Content", "$(CONTENTS_FOLDER_PATH)/Watch", "16");
                project.AddFileToBuildSection(main, embed, project.GetTargetProductFileRef(watch));
            }
        }

        private static void AddSource(PBXProject project, string target, string path)
        {
            var guid = project.FindFileGuidByProjectPath(path);
            if (string.IsNullOrEmpty(guid)) guid = project.AddFile(path, path, PBXSourceTree.Source);
            project.AddFileToBuild(target, guid);
        }
    }
}
#endif
