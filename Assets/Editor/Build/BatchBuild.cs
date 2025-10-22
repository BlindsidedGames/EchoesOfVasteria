// This file provides menu-driven batch build functions for Linux and Windows (IL2CPP)
// and macOS (Mono).

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;

namespace BuildTools
{
    public static class BatchBuild
    {
        // Defaults requested by the user when building from the editor.
        private const string DefaultLinuxDir = @"C:\Users\mattr\Documents\Unity\Builds\Echoes of Vasteria\Linux";
        private const string DefaultWindowsDir = @"C:\Users\mattr\Documents\Unity\Builds\Echoes of Vasteria\Windows";
        private const string DefaultMacDir = @"C:\Users\mattr\Documents\Unity\Builds\Echoes of Vasteria\Mac";

        [MenuItem("Build/Build All (Linux+Windows IL2CPP then Mac Mono)")]
        public static void BuildAllFromMenu()
        {
            var productName = PlayerSettings.productName;
            var scenes = GetEnabledScenes();

            // Linux (IL2CPP)
            if (IsTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
            {
                var linuxAppPath = Path.Combine(DefaultLinuxDir, productName + ".x86_64");
                EnsureDirectoryForLocation(linuxAppPath);
                var linuxReport = BuildStandalone(
                    BuildTarget.StandaloneLinux64,
                    ScriptingImplementation.IL2CPP,
                    linuxAppPath,
                    scenes
                );
                LogReport("Linux (IL2CPP)", linuxReport);
            }
            else
            {
                Debug.LogWarning("Skipping Linux build: target StandaloneLinux64 is not supported (module not installed?).");
            }

            // Windows (IL2CPP)
            if (IsTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                var windowsAppPath = Path.Combine(DefaultWindowsDir, productName + ".exe");
                EnsureDirectoryForLocation(windowsAppPath);
                var windowsReport = BuildStandalone(
                    BuildTarget.StandaloneWindows64,
                    ScriptingImplementation.IL2CPP,
                    windowsAppPath,
                    scenes
                );
                LogReport("Windows (IL2CPP)", windowsReport);
            }
            else
            {
                Debug.LogWarning("Skipping Windows build: target StandaloneWindows64 is not supported.");
            }

            // macOS (Mono)
            TryBuildMacMono(productName, scenes);
        }

        private static bool TryBuildMacMono(string productName, string[] scenes)
        {
            // Build macOS app with Mono if the target is supported in this Editor installation.
            if (!IsTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
            {
                Debug.LogWarning("Skipping macOS build: Build target StandaloneOSX is not supported in this Editor installation.");
                return true; // Not a failure; just skipped.
            }

            var macAppPath = Path.Combine(DefaultMacDir, productName + ".app");
            EnsureDirectoryForLocation(macAppPath);

            var macReport = BuildStandalone(
                BuildTarget.StandaloneOSX,
                ScriptingImplementation.Mono2x,
                macAppPath,
                scenes
            );

            LogReport("macOS (Mono)", macReport);
            return macReport.summary.result == BuildResult.Succeeded;
        }

        private static BuildReport BuildStandalone(
            BuildTarget target,
            ScriptingImplementation scriptingBackend,
            string locationPathName,
            string[] scenes
        )
        {
            // All standalone targets share the Standalone group in PlayerSettings.
            var group = BuildTargetGroup.Standalone;

            var namedTarget = NamedBuildTarget.Standalone;
            var previousBackend = PlayerSettings.GetScriptingBackend(namedTarget);
            PlayerSettings.SetScriptingBackend(namedTarget, scriptingBackend);

            // Optional: ensure .NET 4.x
            // PlayerSettings.SetApiCompatibilityLevel(group, ApiCompatibilityLevel.NET_4_6);

            // Switch active target
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

            // Build options
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = target,
                options = BuildOptions.None
            };

            try
            {
                return BuildPipeline.BuildPlayer(buildPlayerOptions);
            }
            finally
            {
                // Restore previous backend to leave the Editor in a stable state
                PlayerSettings.SetScriptingBackend(namedTarget, previousBackend);
            }
        }

        private static string[] GetEnabledScenes()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (enabledScenes.Length == 0)
            {
                throw new Exception("No scenes are enabled in Build Settings.");
            }
            return enabledScenes;
        }

        private static void EnsureDirectoryForLocation(string locationPathName)
        {
            var dir = Path.GetDirectoryName(locationPathName);
            if (string.IsNullOrEmpty(dir)) return;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static void LogReport(string label, BuildReport report)
        {
            var s = report.summary;
            Debug.Log($"Build {label}: result={s.result}, size={s.totalSize} bytes, time={s.totalTime}");
            if (s.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Build {label} failed.");
            }
        }

        private static bool IsTargetSupported(BuildTargetGroup group, BuildTarget target)
        {
            return BuildPipeline.IsBuildTargetSupported(group, target);
        }
    }
}

