#if UNITY_ANDROID
using System.Xml;
using UnityEditor;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.Editor.NativePlugins.Build.Android;

namespace VoxelBusters.EssentialKit.Editor.Build.Android
{
    public class AndroidManifestGenerator
    {
#region Static fields

        private static string s_androidLibraryRootPackageName = "com.voxelbusters.essentialkit";

#endregion

#region Public methods

        public static void GenerateManifest(EssentialKitSettings settings, string savePath = null)
        {
            Manifest manifest = new Manifest();
            manifest.AddAttribute("xmlns:android", "http://schemas.android.com/apk/res/android");
            manifest.AddAttribute("xmlns:tools", "http://schemas.android.com/tools");
            manifest.AddAttribute("android:versionCode", "1");
            manifest.AddAttribute("android:versionName", "1.0");

            AddQueries(manifest, settings);

            Application application = new Application();

            AddActivities(application, settings);
            AddProviders(application, settings);
            AddServices(application, settings);
            AddReceivers(application, settings);
            AddMetaData(application, settings);

            manifest.Add(application);

            AddPermissions(manifest, settings);
            AddFeatures(manifest, settings);


            XmlDocument xmlDocument = new XmlDocument();
            XmlNode xmlNode = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);

            // Append xml node
            xmlDocument.AppendChild(xmlNode);

            // Get xml hierarchy
            XmlElement element = manifest.GenerateXml(xmlDocument);
            xmlDocument.AppendChild(element);

            // Save to androidmanifest.xml
            xmlDocument.Save(savePath == null ? IOServices.CombinePath(EssentialKitPackageLayout.AndroidProjectPath, "AndroidManifest.xml") : savePath);
        }

#endregion

#region Private methods

        private static void AddQueries(Manifest manifest, EssentialKitSettings settings)
        {
            Queries queries = new Queries();

            if (settings.WebViewSettings.IsEnabled && settings.WebViewSettings.AndroidProperties.UsesCamera)
            {
                Intent intent   = new Intent();
                Action action = new Action();
                action.AddAttribute("android:name", "android.media.action.IMAGE_CAPTURE");
                intent.Add(action);
                queries.Add(intent);
            }

            manifest.Add(queries);
        }

        private static void AddActivities(Application application, EssentialKitSettings settings)
        {
            if (settings.DeepLinkServicesSettings.IsEnabled)
            {
                var universalLinks  = settings.DeepLinkServicesSettings.AndroidProperties.UniversalLinks;
                var urlSchemes      = settings.DeepLinkServicesSettings.AndroidProperties.CustomSchemeUrls;

                if(universalLinks.Length > 0 || urlSchemes.Length > 0)
                {
                    Activity activity = new Activity();
                    activity.AddAttribute("android:name", s_androidLibraryRootPackageName + ".deeplinkservices.DeepLinkRedirectActivity");
                    activity.AddAttribute("android:label", UnityEditor.PlayerSettings.productName);
                    activity.AddAttribute("android:exported", "true");

                    foreach (var each in universalLinks)
                    {
                        IntentFilter intentFilter = CreateIntentFilterForDeepLink(true, each.Identifier, each.Scheme, each.Host, each.Path);
                        activity.Add(intentFilter);
                    }

                    foreach (var each in urlSchemes)
                    {
                        IntentFilter intentFilter = CreateIntentFilterForDeepLink(false, each.Identifier, each.Scheme, each.Host, each.Path);
                        activity.Add(intentFilter);
                    }
                    application.Add(activity);
                }
            }
        }

        private static void AddProviders(Application application, EssentialKitSettings settings)
        {
            if (!settings.GameServicesSettings.AndroidProperties.EnableAutoInitialization) //This is automatically added by play games sdk. So removing if we don't need auto initialization.
            {
                //There is an issue once we disable this provider which auto initializes play games sdk. It won't show welcome message, but shows only once back to app. But this is fine, if you really want to disable auto initialization.
                Provider provider = new Provider();
                provider.AddAttribute("android:name", "com.google.android.gms.games.provider.PlayGamesInitProvider");
                provider.AddAttribute("android:enabled", "false");
                provider.AddAttribute("tools:replace", "android:enabled");
                application.Add(provider);
            }
        }

        private static void AddServices(Application application, EssentialKitSettings settings)
        {
        }

        private static void AddReceivers(Application application, EssentialKitSettings settings)
        {   
        }

        private static void AddMetaData(Application application, EssentialKitSettings settings)
        {
            if(settings.GameServicesSettings.IsEnabled || settings.CloudServicesSettings.IsEnabled)
            {
                MetaData metaData = new MetaData();
                metaData.AddAttribute("android:name", "com.google.android.gms.games.APP_ID");
                if(settings.GameServicesSettings.AndroidProperties.PlayServicesApplicationId != null)
                {
                    metaData.AddAttribute("android:value", string.Format("\\u003{0}", settings.GameServicesSettings.AndroidProperties.PlayServicesApplicationId.Trim()));
                    metaData.AddAttribute("tools:replace", "android:value");
                }
                application.Add(metaData);
            }
        }

        private static void AddFeatures(Manifest manifest, EssentialKitSettings settings)
        {
            if ((settings.MediaServicesSettings.IsEnabled && (settings.MediaServicesSettings.UsesCameraForImageCapture || settings.MediaServicesSettings.UsesCameraForVideoCapture)) || (settings.WebViewSettings.IsEnabled && settings.WebViewSettings.AndroidProperties.UsesCamera))
            {
                Feature feature = new Feature();
                feature.AddAttribute("android:name", "android.hardware.camera");
                if(settings.ApplicationSettings.IsAndroidPcSupported())
                {
                    feature.AddAttribute("android:required", "false");
                }
                manifest.Add(feature);

                feature = new Feature();
                feature.AddAttribute("android:name", "android.hardware.camera.autofocus");
                feature.AddAttribute("android:required", "false");
                manifest.Add(feature);
            }
        }

        private static void AddPermissions(Manifest manifest, EssentialKitSettings settings)
        {
            Permission permission;
            if (settings.NotificationServicesSettings.IsEnabled)
            {
                //TODO: Find a fix so that this won't be problem if project wants to add vibrate permission
                if (!settings.NotificationServicesSettings.AndroidProperties.AllowVibration) //Don't limit maxSdk if vibration is set explicitly. Vibrate permission is required on some devices (4.0, 4.1) for notifications. So enabling for those by default
                {
                    permission = new Permission();
                    permission.AddAttribute("android:name", "android.permission.VIBRATE");
                    permission.AddAttribute("android:maxSdkVersion", "18");
                    manifest.Add(permission);
                }
                if(settings.NotificationServicesSettings.AndroidProperties.AllowExactTimeScheduling)
                {
                    permission = new Permission();
                    permission.AddAttribute("android:name", "android.permission.SCHEDULE_EXACT_ALARM");
                    permission.AddAttribute("android:maxSdkVersion", "32");
                    manifest.Add(permission);

                    //Starting from API 33
                    permission = new Permission();
                    permission.AddAttribute("android:name", "android.permission.USE_EXACT_ALARM");
                    manifest.Add(permission);
                }
            }

            if (settings.MediaServicesSettings.IsEnabled && (settings.MediaServicesSettings.SavesFilesToPhotoGallery || settings.MediaServicesSettings.SavesFilesToCustomDirectories))
            {
                permission = new Permission();
                permission.AddAttribute("android:name", "android.permission.WRITE_EXTERNAL_STORAGE");
                permission.AddAttribute("android:maxSdkVersion", "32");
                manifest.Add(permission);
            }

            if (settings.MediaServicesSettings.IsEnabled)
            {
                //To support media file access on devices that run Android 9 (API level 28) or lower, declare the READ_EXTERNAL_STORAGE permission and set the maxSdkVersion to 28. But we need to read media files created by other apps too so maxSdkVersion to 32.
                permission = new Permission();
                permission.AddAttribute("android:name", "android.permission.READ_EXTERNAL_STORAGE");
                permission.AddAttribute("android:maxSdkVersion", "32");
                manifest.Add(permission);
            }

            if ((settings.MediaServicesSettings.IsEnabled && (settings.MediaServicesSettings.UsesCameraForImageCapture || settings.MediaServicesSettings.UsesCameraForVideoCapture) || (settings.WebViewSettings.IsEnabled && settings.WebViewSettings.AndroidProperties.UsesCamera)))
            {
                permission = new Permission();
                permission.AddAttribute("android:name", "android.permission.CAMERA");
                manifest.Add(permission);
            }

            if(settings.WebViewSettings.IsEnabled && settings.WebViewSettings.AndroidProperties.UsesMicrophone)
            {
                permission = new Permission();
                permission.AddAttribute("android:name", "android.permission.RECORD_AUDIO");
                manifest.Add(permission);

                permission = new Permission();
                permission.AddAttribute("android:name", "android.permission.MODIFY_AUDIO_SETTINGS");
                manifest.Add(permission);
            }
        }

        private static IntentFilter CreateIntentFilterForDeepLink(bool isUniversalLinkFilter, string label, string scheme, string host, string pathPrefix = null)
        {
            IntentFilter intentFilter = new IntentFilter();
            intentFilter.AddAttribute("android:label", label);

            if(isUniversalLinkFilter)
                intentFilter.AddAttribute("android:autoVerify", "true");

            Action action = new Action();
            action.AddAttribute("android:name", "android.intent.action.VIEW");
            intentFilter.Add(action);

            Category category = new Category();
            category.AddAttribute("android:name", "android.intent.category.DEFAULT");
            intentFilter.Add(category);

            category = new Category();
            category.AddAttribute("android:name", "android.intent.category.BROWSABLE");
            intentFilter.Add(category);

            Data data = new Data();
            data.AddAttribute("android:scheme", scheme);
            if (!string.IsNullOrEmpty(host))
            {
                data.AddAttribute("android:host", host);
            }

            if (!string.IsNullOrEmpty(pathPrefix))
            {
                data.AddAttribute("android:pathPrefix", pathPrefix.StartsWith("/") ? pathPrefix :  "/" + pathPrefix);
            }

            intentFilter.Add(data);

            return intentFilter;
        }

#endregion
    }
}
#endif