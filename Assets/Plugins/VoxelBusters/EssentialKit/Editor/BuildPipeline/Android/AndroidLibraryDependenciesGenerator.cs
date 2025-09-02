#if UNITY_EDITOR && UNITY_ANDROID
using UnityEditor;
using System.Collections.Generic;
using System.Xml;
using System;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.Editor.Build.Android
{
	/// <summary>
	/// Play-Services Dependencies for Cross Platform Native Plugins 2.0 : Essential Kit.
	/// </summary>
	[InitializeOnLoad]
	public class AndroidLibraryDependenciesGenerator
	{
		/// <summary>
		/// This is used to create a settings file
		/// which contains the dependencies specific to your plugin.
		/// </summary>
		private static readonly string DependencyName = "CrossPlatformEssentialKitDependencies.xml";

		
		private static readonly string BillingClientVersionString				= "7.1.1+";
		private static readonly string PlayServicesVersionString				= "21.0.0+";
		private static readonly string AndroidXCoreVersionString				= "1.7.0+";
		private static readonly string AndroidXAppCompatVersionString			= "1.5.0+";
		private static readonly string AndroidXActivityVersionString			= "1.6.0+";
		private static readonly string ExifInterfaceVersionString				= "1.3.0+";
        private static readonly string FCMVersionString							= "23.1.1+";
		private static readonly string PlayCoreReviewVersionString				= "2.0.1+";

		private static readonly string PlayCoreInAppUpdateVersionString			=  "2.1.0";
		private static readonly string AndroidXWorkRuntimeVersionString			=  "2.7.1";
		private static readonly string AndroidXCoreShortcutsVersionString		= "1.0.0+";
		

		/// <summary>
		/// Initializes static members of the <see cref="AndroidLibraryDependenciesGenerator"/> class.
		/// </summary>
		static AndroidLibraryDependenciesGenerator()
		{
			EditorApplication.update -= Update;
			EditorApplication.update += Update;
			EditorPrefs.SetBool("refresh-feature-dependencies", true);
		}

		public static bool CreateLibraryDependencies()
        {
		    return CreateLibraryDependenciesInternal(IOServices.CombinePath($"{EssentialKitPackageLayout.EditorExtrasPath}",$"{DependencyName}"));
		}

		private static void Update()
        {
			if (!EditorPrefs.HasKey("refresh-feature-dependencies") || EditorPrefs.GetBool("refresh-feature-dependencies")) // TODO: Remove this static key and have a callback system to get triggered when feature selection happens.
            {
				if (CreateLibraryDependencies())
				{
					EditorPrefs.SetBool("refresh-feature-dependencies", false);
				}
			}
        }

		private static bool CreateLibraryDependenciesInternal(string path)
		{
			// Settings
			XmlWriterSettings xmlSettings 	= new XmlWriterSettings();
			xmlSettings.Encoding			= new System.Text.UTF8Encoding(true);
			xmlSettings.ConformanceLevel	= ConformanceLevel.Document;
			xmlSettings.Indent 				= true;
			xmlSettings.NewLineOnAttributes	= true;
			xmlSettings.IndentChars			= "\t";
            try
            {
				if (!EssentialKitSettingsEditorUtility.SettingsExists) //Cross check this usage once if it needs to have a try/catch block
				{
					return false;
				}
			}
			catch (Exception)
            {
				return false;
            }

			EssentialKitSettings settings = EssentialKitSettingsEditorUtility.DefaultSettings;

			// Generate and write dependecies
			using (XmlWriter xmlWriter = XmlWriter.Create(path, xmlSettings))
			{
				xmlWriter.WriteStartDocument();
				{
					xmlWriter.WriteComment("DONT MODIFY HERE. AUTO GENERATED DEPENDENCIES FROM AndroidLibraryDependenciesGenerator.cs [Cross Platform Essential Kit : Native Plugins 2.0");

					xmlWriter.WriteStartElement("dependencies");
					{
						xmlWriter.WriteStartElement ("androidPackages");
						{
							if (settings.AppUpdaterSettings.IsEnabled)
							{
								xmlWriter.WriteComment("Dependency added for using App Updater feature.");
								AndroidDependency appUpdaterDependency = new AndroidDependency("com.google.android.play", "app-update", PlayCoreInAppUpdateVersionString);
								WritePackageDependency(xmlWriter, appUpdaterDependency);
							}

							if (settings.AppShortcutsSettings.IsEnabled)
							{
								xmlWriter.WriteComment("Dependency added for using App Shortcuts feature.");
								AndroidDependency shortcutsDependency = new AndroidDependency("androidx.core", "core-google-shortcuts", AndroidXCoreShortcutsVersionString);
								WritePackageDependency(xmlWriter, shortcutsDependency);
							}

							if (settings.TaskServicesSettings.IsEnabled)
							{
								//AndroidXWorkRuntimeVersionString
								xmlWriter.WriteComment("Dependency added for using Background Task Services feature.");
								AndroidDependency workRuntimeDependency = new AndroidDependency("androidx.work", "work-runtime", AndroidXWorkRuntimeVersionString);
								WritePackageDependency(xmlWriter, workRuntimeDependency);
							}

							if (settings.BillingServicesSettings.IsEnabled)
                            {
								xmlWriter.WriteComment("Dependency added for using Billing Services");
								AndroidDependency billingDependency = new AndroidDependency("com.android.billingclient", "billing", BillingClientVersionString);
								WritePackageDependency(xmlWriter, billingDependency);
							}

							if (settings.GameServicesSettings.IsEnabled || settings.CloudServicesSettings.IsEnabled)
							{
								xmlWriter.WriteComment("Dependency added for using Google Play Services");

								AndroidDependency playServicesGamesDependency = new AndroidDependency("com.google.android.gms", "play-services-games-v2", PlayServicesVersionString);
								//playServicesGamesDependency.AddPackageID("extra-google-m2repository");
								//playServicesGamesDependency.AddPackageID("extra-android-m2repository");

								WritePackageDependency(xmlWriter, playServicesGamesDependency);
                            }

							if (settings.NotificationServicesSettings.IsEnabled && (settings.NotificationServicesSettings.PushNotificationServiceType != PushNotificationServiceType.None))
							{
								xmlWriter.WriteComment("Dependency added for using Remote Notifications");
                                AndroidDependency fcmDependency = new AndroidDependency("com.google.firebase", "firebase-messaging", FCMVersionString);
								WritePackageDependency(xmlWriter, fcmDependency);
							}

							if (settings.MediaServicesSettings.IsEnabled)
							{
								AndroidDependency exifDependency	= new AndroidDependency("androidx.exifinterface", "exifinterface", ExifInterfaceVersionString);
								WritePackageDependency(xmlWriter, exifDependency);

								AndroidDependency androidXActivityDependency	= new AndroidDependency("androidx.activity", "activity", AndroidXActivityVersionString);
								WritePackageDependency(xmlWriter, androidXActivityDependency);
							}

							if (settings.RateMyAppSettings.IsEnabled)
							{
								xmlWriter.WriteComment("Dependency added for using RateMyApp");
								AndroidDependency playCoreReviewDependency = new AndroidDependency("com.google.android.play", "review", PlayCoreReviewVersionString);
								WritePackageDependency(xmlWriter, playCoreReviewDependency);
							}

							// AndroidX Core Library
							xmlWriter.WriteComment("Dependency added for using AndroidX Core Libraries");
							AndroidDependency androidXDependency	= new AndroidDependency("androidx.core", "core", AndroidXCoreVersionString);
							WritePackageDependency(xmlWriter, androidXDependency);

							// AndroidX AppCompat Library - Used in utilities for adding AppCompatActivity theme and by Media Services library for PickMedia classes.
							xmlWriter.WriteComment("Dependency added for using AndroidX Core Libraries");
							androidXDependency	= new AndroidDependency("androidx.appcompat", "appcompat", AndroidXAppCompatVersionString);
							WritePackageDependency(xmlWriter, androidXDependency);

							//AppCompat resources for supporting older devices.
							androidXDependency	= new AndroidDependency("androidx.appcompat", "appcompat-resources", AndroidXAppCompatVersionString);
							WritePackageDependency(xmlWriter, androidXDependency);
							
						}
						xmlWriter.WriteEndElement();
					}
					xmlWriter.WriteEndElement();
				}
				xmlWriter.WriteEndDocument();
			}

			return true;
		}


		private static void WritePackageDependency(XmlWriter xmlWriter, AndroidDependency dependency)
		{
			xmlWriter.WriteStartElement ("androidPackage");
			{
				xmlWriter.WriteAttributeString ("spec", String.Format("{0}:{1}:{2}", dependency.Group, dependency.Artifact, dependency.Version));

				List<string> packageIDs = dependency.PackageIDs;

				if (packageIDs != null)
				{
					xmlWriter.WriteStartElement ("androidSdkPackageIds");
					{
						foreach(string _each in packageIDs)
						{
							xmlWriter.WriteStartElement ("androidSdkPackageId");
							{
								xmlWriter.WriteString (_each);
							}
							xmlWriter.WriteEndElement ();
						}
					}
					xmlWriter.WriteEndElement ();
				}

			}
			xmlWriter.WriteEndElement ();
		}
	}

	public class AndroidDependency
	{
        private readonly string m_group;
		private readonly string m_artifact;
		private readonly string m_version;

		private	List<string>	m_packageIDs;


		public string Group
		{
			get
			{
				return m_group;
			}
		}

		public string Artifact
		{
			get
			{
				return m_artifact;
			}
		}

		public string Version
		{
			get
			{
				return m_version;
			}
		}

		public List<string> PackageIDs
		{
			get
			{
				return m_packageIDs;
			}
		}

		public AndroidDependency(string _group, string _artifact, string _version)
		{
			m_group = _group;
			m_artifact = _artifact;
			m_version = _version;
		}

		public void AddPackageID(string _packageID)
		{
			if (m_packageIDs == null)
				m_packageIDs = new List<string>();


			m_packageIDs.Add(_packageID);
		}
	}
}
#endif