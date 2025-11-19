// This file provides menu-driven batch build functions for Linux and Windows (IL2CPP)
// and macOS (Mono).

using System;
using System.IO;
using System.Linq;
using Blindsided;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;

namespace BuildTools
{
    public static class BatchBuild
    {
        // Defaults requested by the user when building from the editor.
        private const string BuildRoot = @"C:\Users\mattr\Documents\Unity\Builds";
        private const string FullBuildFolderName = "Echoes of Vasteria";
        private const string DemoBuildFolderName = "Echoes of Vasteria Demo";
        private const string BetaBuildFolderName = "Echoes of Vasteria Beta";

        [MenuItem("Build/Build All Full (Linux+Windows IL2CPP then Mac Mono)")]
        public static void BuildAllFullFromMenu()
        {
            BuildAllForVariant(isDemo: false, isBeta: false);
        }

        [MenuItem("Build/Build All Demo (Linux+Windows IL2CPP then Mac Mono)")]
        public static void BuildAllDemoFromMenu()
        {
            BuildAllForVariant(isDemo: true, isBeta: false);
        }

        [MenuItem("Build/Build All Beta (Linux+Windows IL2CPP then Mac Mono)")]
        public static void BuildAllBetaFromMenu()
        {
            BuildAllForVariant(isDemo: false, isBeta: true);
        }

        private static void BuildAllForVariant(bool isDemo, bool isBeta)
        {
            var productName = PlayerSettings.productName;
            var scenes = GetEnabledScenes();
            var config = BuildModeConfig.LoadOrCreateAsset();
            var originalDemo = config.Demo;
            var originalBeta = config.Beta;

            try
            {
                ApplyBuildModeFlags(config, isDemo, isBeta);
                BuildPlatform("Linux", BuildTarget.StandaloneLinux64, ScriptingImplementation.IL2CPP, config, isDemo, isBeta, productName, scenes);
                BuildPlatform("Windows", BuildTarget.StandaloneWindows64, ScriptingImplementation.IL2CPP, config, isDemo, isBeta, productName, scenes);
                BuildPlatform("macOS", BuildTarget.StandaloneOSX, ScriptingImplementation.Mono2x, config, isDemo, isBeta, productName, scenes);
            }
            finally
            {
                ApplyBuildModeFlags(config, originalDemo, originalBeta);
                RestoreDefaultBuildTarget();
            }
        }

        private static void BuildPlatform(string platformLabel, BuildTarget target, ScriptingImplementation backend, BuildModeConfig config, bool isDemo, bool isBeta, string productName, string[] scenes)
        {
            if (!IsTargetSupported(BuildTargetGroup.Standalone, target))
            {
                Debug.LogWarning($"Skipping {platformLabel} build: target {target} is not supported (module not installed?).");
                return;
            }

            ApplyBuildModeFlags(config, isDemo, isBeta);
            var backendLabel = GetBackendLabel(backend);
            var variantLabel = GetVariantLabel(isDemo, isBeta);
            var label = $"{platformLabel} {variantLabel}({backendLabel})";
            var locationPath = GetBuildLocation(isDemo, isBeta, target, productName);
            EnsureDirectoryForLocation(locationPath);
            var report = BuildStandalone(target, backend, locationPath, scenes);
            LogReport(label, report);
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

        private static string GetBuildLocation(bool isDemo, bool isBeta, BuildTarget target, string productName)
        {
            var baseDir = GetBaseBuildDirectory(isDemo, isBeta);
            var platformFolder = GetPlatformFolder(target);
            var extension = GetExecutableExtension(target);
            return Path.Combine(baseDir, platformFolder, productName + extension);
        }

        private static string GetBaseBuildDirectory(bool isDemo, bool isBeta)
        {
            string folderName;
            if (isBeta)
                folderName = BetaBuildFolderName;
            else
                folderName = isDemo ? DemoBuildFolderName : FullBuildFolderName;

            return Path.Combine(BuildRoot, folderName);
        }

        private static string GetPlatformFolder(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneLinux64:
                    return "Linux";
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "Mac";
                default:
                    return target.ToString();
            }
        }

        private static string GetExecutableExtension(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneLinux64:
                    return ".x86_64";
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneOSX:
                    return ".app";
                default:
                    return string.Empty;
            }
        }

        private static void ApplyBuildModeFlags(BuildModeConfig config, bool isDemo, bool betaFlag)
        {
            if (config == null)
                return;

            if (config.Demo == isDemo && config.Beta == betaFlag)
                return;

            config.Demo = isDemo;
            config.Beta = betaFlag;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static string GetVariantLabel(bool isDemo, bool isBeta)
        {
            if (isBeta) return "Beta ";
            if (isDemo) return "Demo ";
            return "Full ";
        }

        private static string GetBackendLabel(ScriptingImplementation backend)
        {
            return backend == ScriptingImplementation.Mono2x ? "Mono" : backend.ToString();
        }

        private static void RestoreDefaultBuildTarget()
        {
            if (!IsTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
                return;

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
        }
    }
}

