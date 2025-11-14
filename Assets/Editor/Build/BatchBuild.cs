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

        [MenuItem("Build/Build All (Linux+Windows IL2CPP then Mac Mono)")]
        public static void BuildAllFromMenu()
        {
            BuildAllInternal(betaFlag: false);
        }

        [MenuItem("Build/Build All Beta (Linux+Windows IL2CPP then Mac Mono)")]
        public static void BuildAllBetaFromMenu()
        {
            BuildAllInternal(betaFlag: true);
        }

        private static void BuildAllInternal(bool betaFlag)
        {
            var productName = PlayerSettings.productName;
            var scenes = GetEnabledScenes();
            var config = BuildModeConfig.LoadOrCreateAsset();
            var originalDemo = config.Demo;
            var originalBeta = config.Beta;

            try
            {
                BuildPlatformVariants("Linux", BuildTarget.StandaloneLinux64, ScriptingImplementation.IL2CPP, config, betaFlag, productName, scenes);
                BuildPlatformVariants("Windows", BuildTarget.StandaloneWindows64, ScriptingImplementation.IL2CPP, config, betaFlag, productName, scenes);
                BuildPlatformVariants("macOS", BuildTarget.StandaloneOSX, ScriptingImplementation.Mono2x, config, betaFlag, productName, scenes);
            }
            finally
            {
                ApplyBuildModeFlags(config, originalDemo, originalBeta);
            }
        }

        private static void BuildPlatformVariants(string platformLabel, BuildTarget target, ScriptingImplementation backend, BuildModeConfig config, bool betaFlag, string productName, string[] scenes)
        {
            if (!IsTargetSupported(BuildTargetGroup.Standalone, target))
            {
                Debug.LogWarning($"Skipping {platformLabel} build: target {target} is not supported (module not installed?).");
                return;
            }

            var backendLabel = GetBackendLabel(backend);
            var variantPrefix = betaFlag ? "Beta " : string.Empty;
            BuildVariant(config, false, betaFlag, $"{platformLabel} {variantPrefix}Full ({backendLabel})", target, backend, productName, scenes);
            if (!betaFlag)
            {
                BuildVariant(config, true, betaFlag, $"{platformLabel} Demo ({backendLabel})", target, backend, productName, scenes);
            }
        }

        private static void BuildVariant(BuildModeConfig config, bool isDemo, bool betaFlag, string label, BuildTarget target, ScriptingImplementation backend, string productName, string[] scenes)
        {
            ApplyBuildModeFlags(config, isDemo, betaFlag);

            var locationPath = GetBuildLocation(isDemo, betaFlag, target, productName);
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

        private static string GetBackendLabel(ScriptingImplementation backend)
        {
            return backend == ScriptingImplementation.Mono2x ? "Mono" : backend.ToString();
        }
    }
}

