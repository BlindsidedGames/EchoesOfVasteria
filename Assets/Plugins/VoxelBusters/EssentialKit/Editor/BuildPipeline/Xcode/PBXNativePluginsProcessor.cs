#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.Editor;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.Editor.NativePlugins;
using VoxelBusters.CoreLibrary.Editor.NativePlugins.Build;
using VoxelBusters.CoreLibrary.Editor.NativePlugins.Build.Xcode;
using VoxelBusters.EssentialKit;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using VoxelBusters.EssentialKit.AddressBookCore.iOS;
using VoxelBusters.EssentialKit.AppShortcutsCore.iOS;
using VoxelBusters.EssentialKit.BillingServicesCore.iOS;
using VoxelBusters.EssentialKit.CloudServicesCore.iOS;
using VoxelBusters.EssentialKit.WebViewCore.iOS;
using VoxelBusters.EssentialKit.SharingServicesCore.iOS;
using VoxelBusters.EssentialKit.NotificationServicesCore.iOS;
using VoxelBusters.EssentialKit.NativeUICore.iOS;
using VoxelBusters.EssentialKit.GameServicesCore.iOS;
using VoxelBusters.EssentialKit.MediaServicesCore.iOS;
using VoxelBusters.EssentialKit.NetworkServicesCore.iOS;
using VoxelBusters.EssentialKit.DeepLinkServicesCore.iOS;
using VoxelBusters.EssentialKit.ExtrasCore.iOS;
using VoxelBusters.EssentialKit.AppUpdaterCore.iOS;

namespace VoxelBusters.EssentialKit.Editor.Build.Xcode
{
    public class PBXNativePluginsProcessor : CoreLibrary.Editor.NativePlugins.Build.Xcode.PBXNativePluginsProcessor
    {
        #region Properties

        private EssentialKitSettings Settings { get; set; }

        #endregion

        #region Base class methods

        public override void OnCheckConfiguration()
        {
            if (!EnsureInitialised()) return;

            foreach (var exporter in NativePluginsExporterObject.FindObjects<PBXNativePluginsExporterObject>(includeInactive: false))
            {
                switch (exporter.name)
                {
                    case NativeFeatureType.kAppUpdater:
                        {
                            var appId = Settings.ApplicationSettings.GetAppStoreIdForActiveOrSimulationPlatform();
                            if (string.IsNullOrEmpty(appId))
                            {
                                throw new Exception("[Essential Kit : Configuration Incomplete] Store Identifier is not set in Essential kit settings inspector. Set it in the Essential Kit settings's General->Store Identifier's if you would like to use App Updater feature. If you don't want to use App Updater feature, disable it in Essential Kit settings.");
                            }
                        }
                        break;
                    case NativeFeatureType.kRateMyApp:
                        {
                            var appId = Settings.ApplicationSettings.GetAppStoreIdForActiveOrSimulationPlatform();
                            if (string.IsNullOrEmpty(appId) && PlatformMappingServices.GetActivePlatform() == NativePlatform.tvOS)
                            {
                                throw new Exception("[Essential Kit : Configuration Incomplete] Store Identifier is not set in Essential kit settings inspector. Set it in the Essential Kit settings's General->Store Identifier's if you would like to use Rate My App feature. If you don't want to use Rate My App feature, disable it in Essential Kit settings.");
                            }
                        }
                        break;
                }
            }
        }

        public override void OnUpdateExporterObjects()
        {
            // Check whether plugin is configured
            if (!EnsureInitialised()) return;

            DebugLogger.Log(EssentialKitDomain.Default, "Updating native plugins exporter settings.");

            var currentPlatform = PlatformMappingServices.GetActivePlatform();
            foreach (var exporter in NativePluginsExporterObject.FindObjects<PBXNativePluginsExporterObject>(includeInactive: true))
            {
                switch (exporter.name)
                {
                    case NativeFeatureType.kAddressBook:
                    case NativeFeatureType.kAppUpdater:
                    case NativeFeatureType.kAppShortcuts:
                    case NativeFeatureType.kNativeUI:
                    case NativeFeatureType.kSharingServices:
                    case NativeFeatureType.kCloudServices:
                    case NativeFeatureType.kGameServices:
                    case NativeFeatureType.kBillingServices:
                    case NativeFeatureType.kNetworkServices:
                    case NativeFeatureType.kWebView:
                    case NativeFeatureType.kMediaServices:
                    case NativeFeatureType.kRateMyApp:
                    case NativeFeatureType.kTaskServices:
                        exporter.IsEnabled = Settings.IsFeatureUsed(exporter.name);
                        break;

                    case NativeFeatureType.kNotificationServices:
                        var notificationServicesSettings = Settings.NotificationServicesSettings;
                        exporter.IsEnabled = notificationServicesSettings.IsEnabled;
                        exporter.ClearCapabilities();
                        exporter.ClearMacros();
                        exporter.AddMacro(name: "NATIVE_PLUGINS_USES_NOTIFICATION", value: "1");
                        exporter.RemoveFramework(new PBXFramework("CoreLocation.framework"));
                        if ((PushNotificationServiceType.Custom == notificationServicesSettings.PushNotificationServiceType) && exporter.IsEnabled)
                        {
                            exporter.AddMacro(name: "NATIVE_PLUGINS_USES_PUSH_NOTIFICATION", value: "1");
                            exporter.AddCapability(PBXCapability.PushNotifications());
                        }
                        if (notificationServicesSettings.UsesLocationBasedNotification)
                        {
                            exporter.AddMacro(name: "NATIVE_PLUGINS_USES_CORE_LOCATION", value: "1");
                            exporter.AddFramework(new PBXFramework("CoreLocation.framework"));
                        }
                        break;

                    case NativeFeatureType.kDeepLinkServices:
                        var deepLinkSettings = Settings.DeepLinkServicesSettings;
                        var associatedDomains = deepLinkSettings.GetUniversalLinksForPlatform(currentPlatform);
                        exporter.IsEnabled = deepLinkSettings.IsEnabled;
                        exporter.ClearCapabilities();
                        if (deepLinkSettings.IsEnabled && associatedDomains.Length > 0)
                        {
                            var domains = Array.ConvertAll(associatedDomains, (item) =>
                            {
                                string serviceType = string.IsNullOrEmpty(item.ServiceType) ? "applinks" : item.ServiceType;
                                return string.Format("{0}:{1}", serviceType, item.Host);
                            });
                            exporter.AddCapability(PBXCapability.AssociatedDomains(domains));
                        }
                        break;

                    default:
                        if (!exporter.name.Equals("Base"))
                            DebugLogger.LogWarning("[Developer Check] : This feature is not handled : " + exporter.name);

                        break;
                }
                EditorUtility.SetDirty(exporter);
            }
        }

        public override void OnUpdateLinkXml(LinkXmlWriter writer)
        {
            // Check whether plugin is configured
            if (!EnsureInitialised()) return;

            // Add active configurations
            var usedFeaturesMap = ImplementationSchema.GetAllRuntimeConfigurations(settings: Settings);
            if (EssentialKitBuildUtility.IsReleaseBuild() && usedFeaturesMap.Length > 0)
            {
                var platform = EditorApplicationUtility.ConvertBuildTargetToRuntimePlatform(BuildTarget);
                foreach (var current in usedFeaturesMap)
                {
                    string name = current.Key;
                    var config = current.Value;
                    writer.AddConfiguration(name, config, platform, useFallbackPackage: !Settings.IsFeatureUsed(name));
                }
            }
        }

        public override void OnUpdateConfiguration()
        {
            // Check whether plugin is configured
            if (!EnsureInitialised()) return;

            UpdateUnityPreprocessor();
        }

        public override void OnAddFiles()
        {
            // Check whether plugin is configured
            if (!EnsureInitialised()) return;

            var buildTarget = Manager.BuildTarget;
            if ((buildTarget == BuildTarget.iOS) ||
                (buildTarget == BuildTarget.tvOS))
            {
                var pluginsManager = Manager as PBXNativePluginsManager;
                var compilerFlags = new string[0];
                var files = GeneratePBXBindingsFilesForUnusedFeatures(buildTarget);
                foreach (var item in files)
                {
                    pluginsManager.AddFile(item, "VoxelBusters/EssentialKit/Unused/", compilerFlags);
                }
            }
        }

        public override void OnUpdateInfoPlist(PlistDocument doc)
        {
            // Check whether plugin is configured
            if (!EnsureInitialised()) return;

            // Add usage permissions
            var rootDict = doc.root;
            var permissions = GetUsagePermissions();
            foreach (string key in permissions.Keys)
            {
                rootDict.SetString(key, permissions[key]);
            }

            // Add LSApplicationQueriesSchemes
            string[] appQuerySchemes = GetApplicationQueriesSchemes();
            if (appQuerySchemes.Length > 0)
            {
                PlistElementArray array;
                if (!rootDict.TryGetElement(InfoPlistKey.kNSQuerySchemes, out array))
                {
                    array = rootDict.CreateArray(InfoPlistKey.kNSQuerySchemes);
                }

                for (int iter = 0; iter < appQuerySchemes.Length; iter++)
                {
                    if (false == array.Contains(appQuerySchemes[iter]))
                    {
                        array.AddString(appQuerySchemes[iter]);
                    }
                }
            }

            // Add deeplinks
            var deepLinkCustomSchemeUrls = GetDeepLinkCustomSchemeUrls();
            if (deepLinkCustomSchemeUrls.Length > 0)
            {
                PlistElementArray urlTypes;
                if (false == rootDict.TryGetElement(InfoPlistKey.kCFBundleURLTypes, out urlTypes))
                {
                    urlTypes = rootDict.CreateArray(InfoPlistKey.kCFBundleURLTypes);
                }

                foreach (var current in deepLinkCustomSchemeUrls)
                {
                    var newElement = urlTypes.AddDict();
                    newElement.SetString(InfoPlistKey.kCFBundleURLName, current.Identifier);

                    var schemeArray = newElement.CreateArray(InfoPlistKey.kCFBundleURLSchemes);
                    schemeArray.AddString(current.Scheme);
                }
            }
        }

        public override void OnAddResources()
        {
            var pluginsManager = Manager as PBXNativePluginsManager;
            string mainTargetGuid = pluginsManager.Project.GetMainTargetGuid();
            var project = pluginsManager.Project;
            var bundledResourcesRelativeProjectPath = "VoxelBusters/BundledResources";

            var notificationSettings = Settings.NotificationServicesSettings;
            if (notificationSettings.IsEnabled)
            {
                // Copy audio files from streaming assets if any to Raw folder
                var formats = new string[]
                {
                    ".mp3", ".wav", ".ogg", ".aiff"
                };

                AddAssetFolderToTarget(project, mainTargetGuid, Application.streamingAssetsPath, OutputPath, bundledResourcesRelativeProjectPath, formats);
            }

            AppShortcutsUnitySettings shortcutsSettings = Settings.AppShortcutsSettings;
            if (shortcutsSettings.IsEnabled && shortcutsSettings.Icons != null && shortcutsSettings.Icons.Count > 0)
            {
                foreach (var icon in shortcutsSettings.Icons)
                {
                    var sourcePath = AssetDatabase.GetAssetPath(icon);
                    AddAssetToTarget(project, mainTargetGuid,  Path.Combine(Directory.GetParent(Application.dataPath)?.ToString() ?? string.Empty, sourcePath),OutputPath, bundledResourcesRelativeProjectPath);
                }
            }
        }

        public override void OnUpdateEntitlementsPlist(PlistDocument doc)
        {
            // Check whether plugin is configured
            if (!EnsureInitialised()) 
                return;

            if (Settings.CloudServicesSettings.IsEnabled)
            {
                bool substituteAbsoluteValues = IsCloudBuild() || Settings.CloudServicesSettings.IosProperties.SubstituteEntitlementIdentifiers;
                if (!substituteAbsoluteValues)
                    return;
                
                var rootDict = doc.root;
                //Fix for UCB build issue. Unity may fix this later (It's been more than 5 years and they didn't fix it yet!)
                string kvstoreEntryKey = "com.apple.developer.ubiquity-kvstore-identifier";
                if (rootDict.TryGetElement(kvstoreEntryKey, out PlistElementString _))
                {
                    rootDict.SetString(kvstoreEntryKey, $"{PlayerSettings.iOS.appleDeveloperTeamID}.{Application.identifier}");
                }
            }
        }

        #endregion

        #region Private methods

        private bool EnsureInitialised()
        {
            if (Settings != null) return true;

            if (EssentialKitSettingsEditorUtility.TryGetDefaultSettings(out EssentialKitSettings settings))
            {
                Settings = settings;
                return true;
            }

            return false;
        }

        private string[] GeneratePBXBindingsFilesForUnusedFeatures(BuildTarget buildTarget)
        {
            var files = new List<string>();
            string outputPath = IOServices.CombinePath(Manager.OutputPath, "VoxelBusters", "EssentialKit", "Unused");
            var generator = new NativeBindingsGenerator(outputPath: outputPath, options: NativeBindingsGeneratorOptions.Source)
                .SetAuthor("Ashwin Kumar")
                .SetProduct("Cross-Platform Essential Kit")
                .SetCopyrights("Copyright (c) 2025 Voxel Busters Interactive LLP. All rights reserved.");
            var targetPlatform = ApplicationServices.ConvertBuildTargetToRuntimePlatform(buildTarget);
            foreach (var item in ImplementationSchema.GetAllRuntimeConfigurations())
            {
                if (Settings.IsFeatureUsed(item.Key)) continue;

                if (item.Key.Equals(NativeFeatureType.kExtras)) continue;

                var runtimePackage = item.Value.GetPackageForPlatform(targetPlatform);
                foreach (var bindingType in runtimePackage.GetBindingTypeReferences())
                {
                    Type[] customTypes = null;
                    if (bindingType == typeof(BillingServicesBinding))
                    {
                        customTypes = new Type[]
                        {
                            typeof(SKPaymentTransactionData), typeof(SKBuyProductOptionsData)
                        };
                    }
                    else if (bindingType == typeof(AddressBookBinding))
                    {
                        customTypes = new Type[]
                        {
                            typeof(NativeReadContactsOptionsData)
                        };
                    }
                    else if (bindingType == typeof(MediaServicesBinding))
                    {
                        customTypes = new Type[]
                        {
                            typeof(NativeMediaContentSelectionOptionsData), typeof(NativeMediaContentCaptureOptionsData), typeof(NativeMediaContentSaveOptionsData)
                        };
                    }
                    else if (bindingType == typeof(AppUpdaterBinding))
                    {
                        customTypes = new Type[]
                        {
                            typeof(NativeAppUpdaterPromptOptionsData)
                        };
                    }
                    else if (bindingType == typeof(AppShortcutsBinding))
                    {
                        customTypes = new Type[]
                        {
                            typeof(NativeAppShortcutItem)
                        };
                    }
                    else if (bindingType == typeof(AlertControllerBinding))
                    {
                        customTypes = new Type[]
                        {
                            typeof(NativeTextInputFieldOptions)
                        };
                    }
                        

                    generator.Generate(bindingType,
                        $"NP{bindingType.Name}",
                        buildTarget,
                        customTypes,
                        out string[] outputFiles);
                    files.AddRange(outputFiles);
                }
            }
            return files.ToArray();
        }

        private Dictionary<string, string> GetUsagePermissions()
        {
            var requiredPermissionsDict = new Dictionary<string, string>(4);
            var permissionSettings = Settings.ApplicationSettings.UsagePermissionSettings;

            // AddressBook permissions
            var abSettings = Settings.AddressBookSettings;
            if (abSettings.IsEnabled)
            {
                requiredPermissionsDict[InfoPlistKey.kNSContactsUsage] = permissionSettings.AddressBookUsagePermission.GetDescription(RuntimePlatform.IPhonePlayer);
            }

            // Game Services Permissions
            var gameServicesSettings = Settings.GameServicesSettings;
            if (gameServicesSettings.IsEnabled)
            {
                if (gameServicesSettings.AllowFriendsAccess)
                {
                    requiredPermissionsDict[InfoPlistKey.kNSGKFriendListUsage] = permissionSettings.AccessFriendsPermission.GetDescription(RuntimePlatform.IPhonePlayer);
                }
            }


            // Media permissions
            var mediaSettings = Settings.MediaServicesSettings;
            if (mediaSettings.IsEnabled)
            {
                if (mediaSettings.UsesCameraForImageCapture || mediaSettings.UsesCameraForVideoCapture)
                {
                    requiredPermissionsDict[InfoPlistKey.kNSCameraUsage] = permissionSettings.CameraUsagePermission.GetDescription(RuntimePlatform.IPhonePlayer);

                    if (mediaSettings.UsesCameraForVideoCapture)
                    {
                        //Microphone usage permission is required for recording video
                        requiredPermissionsDict[InfoPlistKey.kNSMicrophoneUsage] = permissionSettings.CameraUsagePermission.GetDescription(RuntimePlatform.IPhonePlayer);
                    }
                }

                if (mediaSettings.SavesFilesToPhotoGallery)
                {
                    requiredPermissionsDict[InfoPlistKey.kNSPhotoLibraryAdd] = permissionSettings.GalleryWritePermission.GetDescription(RuntimePlatform.IPhonePlayer);
                }

                if (mediaSettings.SavesFilesToCustomDirectories) //Once we create a custom asset collection folder, we need to fetch with that name to add a file to the album. For this we need read access.
                {
                    requiredPermissionsDict[InfoPlistKey.kNSPhotoLibraryUsage] = permissionSettings.GalleryUsagePermission.GetDescription(RuntimePlatform.IPhonePlayer);
                }
            }

            // Notification permissions
            var notificationSettings = Settings.NotificationServicesSettings;
            if (notificationSettings.IsEnabled)
            {
                if (notificationSettings.UsesLocationBasedNotification)
                {
                    requiredPermissionsDict[InfoPlistKey.kNSLocationWhenInUse] = permissionSettings.LocationWhenInUsePermission.GetDescription(RuntimePlatform.IPhonePlayer);
                }
            }

            // Sharing permissions
            var sharingSettings = Settings.SharingServicesSettings;
            if (sharingSettings.IsEnabled)
            {
                // added for supporting sharing/saving to gallery when share sheet is shown
                requiredPermissionsDict[InfoPlistKey.kNSPhotoLibraryAdd] = permissionSettings.GalleryWritePermission.GetDescription(RuntimePlatform.IPhonePlayer);
            }

            return requiredPermissionsDict;
        }

        private string[] GetApplicationQueriesSchemes()
        {
            var sharingSettings = Settings.SharingServicesSettings;
            var schemeList = new List<string>();
            if (sharingSettings.IsEnabled)
            {
                schemeList.Add("fb");
                schemeList.Add("twitter");
                schemeList.Add("whatsapp");
            }

            return schemeList.ToArray();
        }

        private DeepLinkDefinition[] GetDeepLinkCustomSchemeUrls()
        {
            var deepLinkSettings = Settings.DeepLinkServicesSettings;
            if (deepLinkSettings.IsEnabled)
            {
                var currentPlatform = PlatformMappingServices.GetActivePlatform();
                return deepLinkSettings.GetCustomSchemeUrlsForPlatform(currentPlatform);
            }

            return new DeepLinkDefinition[0];
        }

        private void UpdateUnityPreprocessor()
        {
            var notificationSettings = Settings.NotificationServicesSettings;
            if (notificationSettings.IsEnabled && notificationSettings.PushNotificationServiceType == PushNotificationServiceType.Custom)
            {
                string preprocessorPath = OutputPath + "/Classes/Preprocessor.h";
                string text = File.ReadAllText(preprocessorPath);
                text = text.Replace("UNITY_USES_REMOTE_NOTIFICATIONS 0", "UNITY_USES_REMOTE_NOTIFICATIONS 1");
                File.WriteAllText(preprocessorPath, text);
            }
        }


        // Added for supporting notification services custom sound files
        private void AddAssetFolderToTarget(PBXProject project, string targetGuid, string sourceFolder, string projectRootPath, string destinationTargetRelativePath, string[] fileFormats = null)
        {
            if (IOServices.DirectoryExists(sourceFolder))
            {
                var files = System.IO.Directory.GetFiles(sourceFolder);
                
                for (int i = 0; i < files.Length; i++)
                {
                    string extension = IOServices.GetExtension(files[i]);
                    var file = files[i];
                    if (fileFormats == null || fileFormats.Contains(extension.ToLower()))
                    {
                        AddAssetToTarget(project, targetGuid, file, projectRootPath, destinationTargetRelativePath);
                    }
                }
            }
        }

        private void AddAssetToTarget(PBXProject project, string targetGuid, string sourceFilePath, string projectRootPath, string destinationTargetFolderRelativePath)
        {
            string destinationPath = IOServices.CombinePath(projectRootPath, destinationTargetFolderRelativePath);
            if (!IOServices.DirectoryExists(destinationPath))
            {
                IOServices.CreateDirectory(destinationPath);
            }
            string destinationRelativeFilePath = IOServices.CombinePath(destinationTargetFolderRelativePath, IOServices.GetFileName(sourceFilePath));
            IOServices.CopyFile(sourceFilePath, IOServices.CombinePath(destinationPath, IOServices.GetFileName(sourceFilePath)));
            DebugLogger.Log(CoreLibraryDomain.NativePlugins, $"Coping asset with relativePath: {destinationTargetFolderRelativePath}.");
            project.AddFileToBuild(targetGuid, project.AddFile(destinationRelativeFilePath, destinationRelativeFilePath));
        }

        private bool IsCloudBuild()
        {
#if UNITY_CLOUD_BUILD
            return true;
#else
            return false;
#endif
        }


        #endregion
    }
}
#endif