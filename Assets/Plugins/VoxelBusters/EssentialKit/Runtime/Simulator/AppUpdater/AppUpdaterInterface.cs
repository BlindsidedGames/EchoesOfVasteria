using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.Simulator
{
    public sealed class AppUpdaterInterface : NativeFeatureInterfaceBase, INativeAppUpdaterInterface
    {
        #region Constants
            private const string kBuildTagKey  = "EssentialKit.AppUpdaterBuildTagKey";
        #endregion

        #region Fields

        private AppUpdaterUpdateInfo m_cachedUpdateInfo;

        #endregion

        #region Constructors

        public AppUpdaterInterface()
            : base(isAvailable: true)
        { 

        }

        #endregion

        #region INativeAppUpdaterInterface implementation methods

        public void RequestUpdateInfo(EventCallback<AppUpdaterUpdateInfo> callback)
        {
            bool isNewUpdateAvailable = IsNewUpdateAvailable();
            if(isNewUpdateAvailable)
            {
                m_cachedUpdateInfo = new AppUpdaterUpdateInfo(AppUpdaterUpdateStatus.Available, GetBuildTag());
                callback(m_cachedUpdateInfo, null);
            }
            else
            {
                if(IsFirstTimeInstall())
                {
                    RecordCurrentBuildTag();
                }

                m_cachedUpdateInfo = new AppUpdaterUpdateInfo(AppUpdaterUpdateStatus.NotAvailable, -1);
                callback(m_cachedUpdateInfo, null);
            }
        }

        public void PromptUpdate(PromptUpdateOptions options, EventCallback<float> callback)
        {
            if (m_cachedUpdateInfo != null)
            {
                if (m_cachedUpdateInfo.Status == AppUpdaterUpdateStatus.Available)
                {
                    var alertDialogBuilder = new AlertDialogBuilder()
                                                    .SetTitle(AppUpdater.UnitySettings.DefaultPromptTitle)
                                                    .SetMessage(AppUpdater.UnitySettings.DefaultPromptMessage)
                                                    .AddButton("Update", () =>
                                                    {
                                                        if (options.IsForceUpdate || options.AllowInstallationIfDownloaded)
                                                        {
                                                            RecordCurrentBuildTag();    
                                                        }
                                                        callback(1f, null);
                                                    });

                    if (!options.IsForceUpdate)
                    {
                        alertDialogBuilder.AddCancelButton("Cancel", () =>
                        {
                            callback(0f, AppUpdaterError.UpdateCancelled);
                        });
                    }

                    var alertDialog = alertDialogBuilder.Build();
                    alertDialog.Show();
                }
                else
                {
                    callback(0f, AppUpdaterError.UpdateNotAvailable);
                }
            }
            else
            {
                callback(0f, AppUpdaterError.UpdateInfoNotAvailable);
            }
        }


        #endregion

        #region Private methods

         private bool IsNewUpdateAvailable()
        {
            if(IsFirstTimeInstall())
            {
                return false;
            }

            var existingVersionCode     = PlayerPrefs.GetInt(kBuildTagKey);
            var currentVersionCode      = GetBuildTag();

            return currentVersionCode > existingVersionCode;
        }

        private bool IsFirstTimeInstall()
        {
            return !PlayerPrefs.HasKey(kBuildTagKey);
        }

        private void RecordCurrentBuildTag()
        {
            PlayerPrefs.SetInt(kBuildTagKey, GetBuildTag());
        }
       

        private int GetBuildTag()
        {
            var platform = ApplicationServices.GetActiveOrSimulationPlatform();
            switch(platform)
            {
                case RuntimePlatform.Android:
                    return PlayerSettings.Android.bundleVersionCode;
                case RuntimePlatform.IPhonePlayer:
                    int.TryParse(PlayerSettings.iOS.buildNumber, out int result);
                    return result;
            }

            return -1;
        }

        #endregion

        #region Static methods
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(kBuildTagKey);
        }
        #endregion
    }
}