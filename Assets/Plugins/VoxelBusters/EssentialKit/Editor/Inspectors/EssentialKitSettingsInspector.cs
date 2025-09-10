using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.Editor;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit.AddressBookCore.Simulator;
using VoxelBusters.EssentialKit.BillingServicesCore.Simulator;
using VoxelBusters.EssentialKit.CloudServicesCore.Simulator;
using VoxelBusters.EssentialKit.GameServicesCore.Simulator;
using VoxelBusters.EssentialKit.NotificationServicesCore.Simulator;
using VoxelBusters.EssentialKit.MediaServicesCore.Simulator;

namespace VoxelBusters.EssentialKit.Editor
{
    [CustomEditor(typeof(EssentialKitSettings))]
    public class EssentialKitSettingsInspector : SettingsObjectInspector
    {
        #region Fields

        private     SerializedProperty          m_appSettingsProperty;

        private     EditorSectionInfo[]         m_serviceSections;

        private     ButtonMeta[]                m_resourceButtons;

        #endregion

        #region Base class methods

        protected override void OnEnable()
        {
            // Set properties
            m_appSettingsProperty   = serializedObject.FindProperty("m_applicationSettings");
            m_serviceSections       = new EditorSectionInfo[]
            {
                new EditorSectionInfo(displayName: "Address Book",
                                      description: "Access the user's contacts, and format and localize contact information.",
                                      property: serializedObject.FindProperty("m_addressBookSettings"),
                                      drawDetailsCallback: DrawAddressBookSettingsDetails),
                new EditorSectionInfo(displayName: "App Shortcuts",
                                    description: "Create shortcuts to quickly access content directly within your app.",
                                    property: serializedObject.FindProperty("m_appShortcutsSettings"),
                                    drawDetailsCallback: DrawAppShortcutsSettingsDetails),
                new EditorSectionInfo(displayName: "App Updater",
                                    description: "Provides a provision to update app forcefully or optionally.",
                                    property: serializedObject.FindProperty("m_appUpdaterSettings"),
                                    drawDetailsCallback: DrawAppUpdaterSettingsDetails),
                new EditorSectionInfo(displayName: "Billing",
                                      description: "Support in-app purchases and interactions with the App Store.",
                                      property: serializedObject.FindProperty("m_billingServicesSettings"),
                                      drawDetailsCallback: DrawBillingServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Cloud Services",
                                      description: "Store the user's data persistent across devices.",
                                      property: serializedObject.FindProperty("m_cloudServicesSettings"),
                                      drawDetailsCallback: DrawCloudServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Deep Link",
                                      description: "Easy mechanism to navigate to the desired content in your app.",
                                      property: serializedObject.FindProperty("m_deepLinkServicesSettings"),
                                      drawDetailsCallback: DrawDeepLinkServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Game Services",
                                      description: "Support social gaming features.",
                                      property: serializedObject.FindProperty("m_gameServicesSettings"),
                                      drawDetailsCallback: DrawGameServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Media Services",
                                      description: "Support access to the media content.",
                                      property: serializedObject.FindProperty("m_mediaServicesSettings"),
                                      drawDetailsCallback: DrawMediaServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Native UI",
                                      description: "Support display of native dialogs within your apps.",
                                      property: serializedObject.FindProperty("m_nativeUISettings"),
                                      drawDetailsCallback: DrawNativeUISettingsDetails),
                new EditorSectionInfo(displayName: "Network Services",
                                      description: "Detect the user's device network capabilities.",
                                      property: serializedObject.FindProperty("m_networkServicesSettings"),
                                      drawDetailsCallback: DrawNetworkServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Notification Services",
                                      description: "Push user-facing notifications to the user’s device from a server, or generate them locally from your app.",
                                      property: serializedObject.FindProperty("m_notificationServicesSettings"),
                                      drawDetailsCallback: DrawNotificationServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Rate My App",
                                      description: "Allow your users to rate your app on the Market Stores.",
                                      property: serializedObject.FindProperty("m_rateMyAppSettings"),
                                      drawDetailsCallback: DrawRateMyAppSettingsDetails),
                new EditorSectionInfo(displayName: "Sharing",
                                      description: "Support sharing capabilities on social media platforms.",
                                      property: serializedObject.FindProperty("m_sharingServicesSettings"),
                                      drawDetailsCallback: DrawSharingServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Task Services",
                                    description: "Provides a way to continue tasks/operations even when app is in background (suspended/paused).",
                                    property: serializedObject.FindProperty("m_taskServicesSettings"),
                                    drawDetailsCallback: DrawTaskServicesSettingsDetails),
                new EditorSectionInfo(displayName: "Utilities",
                                    description: "Other useful features that are most commonly used on mobile games.",
                                    property: serializedObject.FindProperty("m_utilitySettings"),
                                    drawDetailsCallback: DrawUtilitySettingsDetails),
                new EditorSectionInfo(displayName: "WebView",
                                      description: "Displays interactive web content directly within your app.",
                                      property: serializedObject.FindProperty("m_webViewSettings"),
                                      drawDetailsCallback: DrawWebViewSettingsDetails), 
            };
            m_resourceButtons   = new ButtonMeta[]
            {
                new ButtonMeta(label: "Documentation",  onClick: EssentialKitEditorUtility.OpenDocumentation),
                new ButtonMeta(label: "Tutorials",      onClick: EssentialKitEditorUtility.OpenTutorials),
                new ButtonMeta(label: "Forum",          onClick: EssentialKitEditorUtility.OpenForum),
                new ButtonMeta(label: "Discord",        onClick: EssentialKitEditorUtility.OpenSupport),
                new ButtonMeta(label: "Write Review",	onClick: () => EssentialKitEditorUtility.OpenAssetStorePage(true)),
            };

            base.OnEnable();
        }

        protected override UnityPackageDefinition GetOwner()
        {
            return EssentialKitSettings.Package;
        }

        protected override string[] GetTabNames()
        {
            return new string[]
            {
                DefaultTabs.kGeneral,
                DefaultTabs.kServices,
                DefaultTabs.kMisc,
            };
        }

        protected override EditorSectionInfo[] GetSectionsForTab(string tab)
        {
            if (tab == DefaultTabs.kServices)
            {
                return m_serviceSections;
            }
            return null;
        }

        protected override bool DrawTabView(string tab)
        {
            switch (tab)
            {
                case DefaultTabs.kGeneral:
                    DrawGeneralTabView();
                    return true;

                case DefaultTabs.kMisc:
                    DrawMiscTabView();
                    return true;

                default:
                    return false;
            }
        }

        protected override void DrawFooter(string tab)
        {
            base.DrawFooter(tab);

            if (tab == DefaultTabs.kGeneral)
            {
                // provide option to add resources
                /*EditorLayoutUtility.Helpbox(title: "Essentials",
                                            description: "Add resources to your project that are essential for using Essential Kit plugin.",
                                            actionLabel: "Import Essentials",
                                            onClick: EssentialKitEditorUtility.ImportEssentialResources,
                                            style: GroupBackgroundStyle);*/

                //ShowMigrateToUpmOption();
            }
        }

        #endregion

        #region Section methods

        private void DrawGeneralTabView()
        {
            var     storeIdsSection     = new EditorSectionInfo(displayName: "Store Identifier's",
                                                                property: m_appSettingsProperty.FindPropertyRelative("m_appStoreIds"),
                                                                drawStyle: EditorSectionDrawStyle.Expand);
            var     permissionSection   = new EditorSectionInfo(displayName: "Usage Permissions",
                                                                property: m_appSettingsProperty.FindPropertyRelative("m_usagePermissionSettings"),
                                                                drawStyle: EditorSectionDrawStyle.Expand);
            EditorGUILayout.BeginVertical(GroupBackgroundStyle);
            EditorGUILayout.PropertyField(m_appSettingsProperty.FindPropertyRelative("m_logLevel"));
            EditorGUILayout.EndVertical();
                                                  
            LayoutBuilder.DrawSection(storeIdsSection,
                                      showDetails: true,
                                      selectable: false);
            LayoutBuilder.DrawSection(permissionSection,
                                      showDetails: true,
                                      selectable: false);
        }

        private void DrawMiscTabView()
        {
            DrawButtonList(m_resourceButtons);
        }

        private void DrawAddressBookSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kAddressBook);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                AddressBookSimulator.Reset();
            }
            GUILayout.EndVertical();
        }
        
        private void DrawAppShortcutsSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kAppShortcuts);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                //AppShortcutsCore.Simulator.TaskServicesInterface.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawAppUpdaterSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kAppUpdater);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                AppUpdaterCore.Simulator.AppUpdaterInterface.Reset();
            }
            GUILayout.EndVertical();
        }
        
        private void DrawTaskServicesSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kTaskServices);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                TaskServicesCore.Simulator.TaskServicesInterface.Reset();
            }
            GUILayout.EndVertical();
        }
        
        private void DrawBillingServicesSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kBillingServices);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                BillingStoreSimulator.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawCloudServicesSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kCloudServices);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                CloudServicesSimulator.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawDeepLinkServicesSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kDeepLinkServices);
            }
            GUILayout.EndVertical();
        }

        private void DrawGameServicesSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kGameServices);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                GameServicesSimulator.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawNetworkServicesSettingsDetails(EditorSectionInfo section)
        {
           EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kNetworkServices);
            }
            GUILayout.EndVertical();
        }

        private void DrawNotificationServicesSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kNotificationServices);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                NotificationServicesSimulator.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawRateMyAppSettingsDetails(EditorSectionInfo section)
        {
             EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kRateMyApp);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                //RateMyAppSimulator.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawNativeUISettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kNativeUI);
            }
            GUILayout.EndVertical();
        }

        private void DrawMediaServicesSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kMediaServices);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                MediaServicesSimulator.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawSharingServicesSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kSharingServices);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                MediaServicesSimulator.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawWebViewSettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kWebView);
            }
            if (GUILayout.Button("Reset Simulator"))
            {
                MediaServicesSimulator.Reset();
            }
            GUILayout.EndVertical();
        }

        private void DrawUtilitySettingsDetails(EditorSectionInfo section)
        {
            EditorLayoutBuilder.DrawChildProperties(property: section.Property,
                                                    ignoreProperties: section.IgnoreProperties);

            GUILayout.BeginVertical();
            if (GUILayout.Button("Resource Page"))
            {
                ProductResources.OpenResourcePage(NativeFeatureType.kExtras);
            }
            GUILayout.EndVertical();
        }

        #endregion
    }
}