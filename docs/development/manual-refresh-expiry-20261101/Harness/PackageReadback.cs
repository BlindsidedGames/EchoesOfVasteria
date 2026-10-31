using System;
using System.IO;
using UnityEditor.iOS.Xcode;

// Executes the exact production postprocessor with Unity's actual PBXProject
// implementation. The editor-only environment and unrelated art export are supplied
// here; no postprocessor mutation or expected target membership is mocked.
class PackageReadback {
    static void Main(string[] args) {
        var root = Path.GetFullPath(args[0]);
        var output = Path.GetFullPath(args[2]);
        UnityEngine.Application.dataPath = Path.Combine(root, "Assets");
        Directory.CreateDirectory(Path.Combine(output, "Unity-iPhone.xcodeproj"));
        var projectPath = Path.Combine(output, "Unity-iPhone.xcodeproj/project.pbxproj");
        File.Copy(args[1], projectPath, true);
        File.WriteAllText(Path.Combine(output, "Info.plist"), "<?xml version=\"1.0\" encoding=\"UTF-8\"?><plist version=\"1.0\"><dict/></plist>");
        TimelessEchoes.Editor.LiveActivityIosPostprocess.Configure(UnityEditor.BuildTarget.iOS, output);
        File.Copy(projectPath, Path.Combine(output, "fresh.pbxproj"), true);
        // Create the real old exported topology: phone session belongs to framework.
        var old = new PBXProject(); old.ReadFromFile(projectPath);
        var session = old.FindFileGuidByProjectPath("EchoesLiveActivity/Companion/EOVWatchPhoneSession.swift");
        old.RemoveFileFromBuild(old.GetUnityMainTargetGuid(), session);
        old.AddFileToBuild(old.GetUnityFrameworkTargetGuid(), session);
        old.WriteToFile(projectPath);
        File.Copy(projectPath, Path.Combine(output, "legacy.pbxproj"), true);
        TimelessEchoes.Editor.LiveActivityIosPostprocess.Configure(UnityEditor.BuildTarget.iOS, output);
        TimelessEchoes.Editor.LiveActivityIosPostprocess.Configure(UnityEditor.BuildTarget.iOS, output);
        File.Copy(projectPath, Path.Combine(output, "migrated.pbxproj"), true);
        Console.WriteLine("Production Configure executed for fresh export, real legacy membership, and repeated migration: " + output);
    }
}
namespace UnityEditor {
    public enum BuildTarget { iOS }
    public enum iOSSdkVersion { SimulatorSDK }
    public static class PlayerSettings {
        public static string bundleVersion = "1.5.9";
        public static string GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget target) => "EOV_BACKGROUND_PROTOTYPE";
        public static string GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget target) => "com.blindsidedgames.echoes.packagingqa";
        public static class iOS { public static string buildNumber = "188"; public static iOSSdkVersion sdkVersion = iOSSdkVersion.SimulatorSDK; }
    }
}
namespace UnityEditor.Build { public struct NamedBuildTarget { public static NamedBuildTarget iOS => new NamedBuildTarget(); } }
namespace UnityEditor.Callbacks { public class PostProcessBuildAttribute : Attribute { public PostProcessBuildAttribute(int order) {} } }
namespace UnityEngine {
    public static class Application { public static string dataPath; }
    public static class Debug { public static void Log(object value) => Console.WriteLine(value); }
}
namespace TimelessEchoes.Editor {
    internal static class CharacterPortraitIosAssets {
        // Portrait rendering is independently validated and outside this graph test.
        internal static void Export(string destination) {}
        internal static void ExportWatchAnimation(string destination) {}
    }
}
