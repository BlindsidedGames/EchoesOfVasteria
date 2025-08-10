using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

#if UNITY_ANDROID
using UnityEditor.Android;
#endif

// If Addressables package is present, this using will resolve. If not, you can remove the addressables build call below.
// ReSharper disable once RedundantUsingDirective
using UnityEditor.AddressableAssets.Settings;

public static class BuildAll
{
    private const string WindowsOutputDir = @"C:\\Users\\mattr\\Documents\\Unity\\Builds\\Echoes of Vasteria\\Windows";
    private const string LinuxOutputDir   = @"C:\\Users\\mattr\\Documents\\Unity\\Builds\\Echoes of Vasteria\\Linux";
    private const string AndroidOutput    = @"C:\\Users\\mattr\\Documents\\Unity\\Builds\\Android\\Timeless"; // file base; will append .apk or .aab
    private const string MacOutputDir     = @"C:\\Users\\mattr\\Documents\\Unity\\Builds\\Echoes of Vasteria\\Mac";

    [MenuItem("Tools/Build/Build All (Windows, Linux, Android, Mac)")]
    public static void BuildAllTargets()
    {
        try
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Build All", "No enabled scenes found in Build Settings.", "OK");
                return;
            }

            // Build Addressables content first if package is available
            TryBuildAddressables();

            var productName = PlayerSettings.productName;
            var options = BuildOptions.None;

            // Windows (IL2CPP)
            SafeBuild(
                buildTargetGroup: BuildTargetGroup.Standalone,
                buildTarget: BuildTarget.StandaloneWindows64,
                scripting: ScriptingImplementation.IL2CPP,
                locationPathName: Path.Combine(WindowsOutputDir, productName + ".exe"),
                scenes: scenes,
                options: options
            );

            // Linux (IL2CPP)
            SafeBuild(
                buildTargetGroup: BuildTargetGroup.Standalone,
                buildTarget: BuildTarget.StandaloneLinux64,
                scripting: ScriptingImplementation.IL2CPP,
                locationPathName: Path.Combine(LinuxOutputDir, productName + ".x86_64"),
                scenes: scenes,
                options: options
            );

            // Android (IL2CPP)
            // Choose APK by default; change to AAB by setting buildAppBundle = true
            bool buildAppBundle = true;
            #if UNITY_ANDROID
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            // Target common Android architectures
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            #endif

            var androidFile = AndroidOutput + (buildAppBundle ? ".aab" : ".apk");
            SafeBuild(
                buildTargetGroup: BuildTargetGroup.Android,
                buildTarget: BuildTarget.Android,
                scripting: ScriptingImplementation.IL2CPP,
                locationPathName: androidFile,
                scenes: scenes,
                options: options
            );

            // macOS (Mono)
            SafeBuild(
                buildTargetGroup: BuildTargetGroup.Standalone,
                buildTarget: BuildTarget.StandaloneOSX,
                scripting: ScriptingImplementation.Mono2x,
                locationPathName: Path.Combine(MacOutputDir, productName + ".app"),
                scenes: scenes,
                options: options
            );

            EditorUtility.DisplayDialog("Build All", "All builds completed successfully.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("Build All failed: " + ex);
            EditorUtility.DisplayDialog("Build All", "Build failed: " + ex.Message, "OK");
        }
    }

    private static void SafeBuild(
        BuildTargetGroup buildTargetGroup,
        BuildTarget buildTarget,
        ScriptingImplementation scripting,
        string locationPathName,
        string[] scenes,
        BuildOptions options)
    {
        var directory = GetDirectoryForPath(locationPathName);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        EnsureScriptingBackend(buildTargetGroup, scripting);
        SwitchTarget(buildTargetGroup, buildTarget);

        var buildOptions = new BuildPlayerOptions
        {
            targetGroup = buildTargetGroup,
            target = buildTarget,
            locationPathName = locationPathName,
            scenes = scenes,
            options = options
        };

        var report = BuildPipeline.BuildPlayer(buildOptions);
        var summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"Build for {buildTarget} failed: {summary.result} (errors: {summary.totalErrors}, warnings: {summary.totalWarnings})");
        }

        Debug.Log($"Build for {buildTarget} succeeded. Size: {summary.totalSize / (1024f * 1024f):F1} MB at {locationPathName}");
    }

    private static void EnsureScriptingBackend(BuildTargetGroup group, ScriptingImplementation desired)
    {
        var current = PlayerSettings.GetScriptingBackend(group);
        if (current != desired)
        {
            PlayerSettings.SetScriptingBackend(group, desired);
            Debug.Log($"Switched scripting backend for {group} to {desired}.");
        }
    }

    private static void SwitchTarget(BuildTargetGroup group, BuildTarget target)
    {
        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            Debug.Log($"Switched active build target to {target}.");
        }
    }

    private static string GetDirectoryForPath(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (Directory.Exists(path)) return path; // already a directory
            var dir = Path.GetDirectoryName(path);
            return string.IsNullOrEmpty(dir) ? null : dir;
        }
        catch
        {
            return null;
        }
    }

    private static void TryBuildAddressables()
    {
        try
        {
            // This call will succeed if Addressables editor package is present
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("Addressables content built successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Skipping Addressables build (package not present or failed): " + ex.Message);
        }
    }
}
