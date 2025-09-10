#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;
using System;
using UnityEngine;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;
using System.Linq;
using VoxelBusters.EssentialKit;

namespace VoxelBusters.EssentialKit.Editor.Build.Android
{
    public class AndroidGradleDependenciesPostProcessor : IPostGenerateGradleAndroidProject
    {
        private static string kLibraryPrefix                = $"{EssentialKitPackageLayout.AndroidProjectRootNamespace}";
        private static string kLibraryExtension             = "aar";
        private static string kAddressBookLibrary           = LibraryFileName("address-book");
        private static string kBillingServicesLibrary       = LibraryFileName("billing-services");
        private static string kCloudServicesLibrary         = LibraryFileName("cloud-services");
        private static string kGameServicesLibrary          = LibraryFileName("game-services");
        private static string kMediaServicesLibrary         = LibraryFileName("media-services");
        private static string kNativeUILibrary              = LibraryFileName("ui-views");
        private static string kNetworkServicesLibrary       = LibraryFileName("network-services");
        private static string kNotificationServicesLibrary  = LibraryFileName("notification-services");
        private static string kSharingServicesLibrary       = LibraryFileName("sharing-services");
        private static string kWebViewLibrary               = LibraryFileName("web-view");
        private static string kDeepLinkServicesLibrary      = LibraryFileName("deep-link-services");
        private static string kExtrasLibrary                = LibraryFileName("extras");
        private static string kSocialAuthLibrary            = LibraryFileName("social-auth");
        private static string kRateMyAppLibrary            = LibraryFileName("rate-my-app");

        private static string LibraryFileName(string module) => $"{kLibraryPrefix}-{module}.{kLibraryExtension}";

        Dictionary<string, string> m_libraryMap = new Dictionary<string, string>()
        {
            { NativeFeatureType.kAddressBook,           kAddressBookLibrary},
            { NativeFeatureType.kBillingServices,       kBillingServicesLibrary},
            { NativeFeatureType.kCloudServices,         kCloudServicesLibrary},
            { NativeFeatureType.kGameServices,          kGameServicesLibrary},
            { NativeFeatureType.kMediaServices,         kMediaServicesLibrary},
            { NativeFeatureType.kNativeUI,              kNativeUILibrary},
            { NativeFeatureType.kNetworkServices,       kNetworkServicesLibrary },
            { NativeFeatureType.kNotificationServices,  kNotificationServicesLibrary },
            { NativeFeatureType.kSharingServices,       kSharingServicesLibrary },
            { NativeFeatureType.kWebView,               kWebViewLibrary },
            { NativeFeatureType.kDeepLinkServices,      kDeepLinkServicesLibrary },
            { NativeFeatureType.kExtras,                kExtrasLibrary },
            { NativeFeatureType.kRateMyApp,             kRateMyAppLibrary },
        };

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            if (!EssentialKitSettingsEditorUtility.SettingsExists)
            {
                EssentialKitSettingsEditorUtility.ShowSettingsNotFoundErrorDialog();
                return;
            }

            List<string> deletedLibraries = new List<string>();

            foreach(string eachFeature in m_libraryMap.Keys)
            {
                
                if(!EssentialKitSettings.Instance.IsFeatureUsed(eachFeature))
                {
                    try
                    {
                        string libraryName = m_libraryMap[eachFeature];
                        UpdateDeletedLibrary(basePath, libraryName, deletedLibraries);
                    }
                    catch(Exception e)
                    {
                        DebugLogger.LogError($"Failed finding the library file for feature:{eachFeature} to delete with exception: {e}");
                    }
                }
            }

            //Special cases
            if (!EssentialKitSettings.Instance.IsFeatureUsed(NativeFeatureType.kGameServices) && !EssentialKitSettings.Instance.IsFeatureUsed(NativeFeatureType.kCloudServices))
            {
                UpdateDeletedLibrary(basePath, kSocialAuthLibrary, deletedLibraries);
            }

            //Update build.gradle file
            UpdateGradleFile(basePath, deletedLibraries);
        }

        public int callbackOrder { get { return 1; } }

        private void UpdateDeletedLibrary(string basePath, string libraryName, List<string> deletedList)
        {
            IOServices.DeleteFile(Path.Combine(basePath, "libs", libraryName), true);
            deletedList.Add(libraryName.Replace(".aar", ""));
        }

        private void UpdateGradleFile(string basePath, List<string> deletedLibraries)
        {
            string gradlePath = IOServices.CombinePath(basePath, "build.gradle");
            Debug.Log("Updating build.gradle at : " + gradlePath);
            string[] lines = File.ReadAllLines(gradlePath);
            StringBuilder builder = new StringBuilder();

            foreach (string each in lines)
            {
                if(!deletedLibraries.Any(eachLib => each.Contains(eachLib)))
                {
                    builder.AppendLine(each);
                }
            }
            File.WriteAllText(gradlePath, builder.ToString());
        }
    }
}
#endif
