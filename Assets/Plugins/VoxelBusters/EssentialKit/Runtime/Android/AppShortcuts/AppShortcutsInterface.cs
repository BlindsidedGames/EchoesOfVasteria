#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.AppShortcutsCore.Android
{

    public sealed class AppShortcutsInterface : NativeFeatureInterfaceBase, INativeAppShortcutsInterface
    {
        #region Private fields
        private readonly NativeAppShortcuts m_instance;
        #endregion
        
        #region Constructors

        public AppShortcutsInterface() : base(isAvailable: true)
        {
            m_instance = new NativeAppShortcuts(NativeUnityPluginUtility.GetContext());
            m_instance.Initialize(new NativeAppShortcutClickListener()
            {
                onClickCallback = (string identifier) =>
                {
                    OnShortcutClicked?.Invoke(identifier);
                }
            });
        }


        #endregion
        
        #region INativeAppShortcutsInterface implementation

        public event ShortcutClickedInternalCallback OnShortcutClicked;
        public void Add(AppShortcutItem item)
        {
            NativeAppShortcutItem nativeItem = new NativeAppShortcutItem()
            {
                Identifier = item.Identifier,
                Title = item.Title,
                Subtitle = item.Title,
                IconFileName = item.IconFileName
            };
            m_instance.Add(nativeItem);
        }
        public void Remove(string shortcutItemId)
        {
            m_instance.Remove(shortcutItemId);
        }
        
        #endregion

       
    }
}
#endif