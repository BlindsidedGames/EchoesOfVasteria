#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AOT;
using VoxelBusters.EssentialKit;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary;
using System;
using System.Runtime.InteropServices;
using UnityEngine.PlayerLoop;

namespace VoxelBusters.EssentialKit.AppShortcutsCore.iOS
{
    public sealed class AppShortcutsInterface : NativeFeatureInterfaceBase, INativeAppShortcutsInterface
    {
        #region Private fields 

        private static  AppShortcutsInterface   s_instance      = null;
        
        #endregion
        #region Constructors

        public AppShortcutsInterface() 
            : base(isAvailable: true)
        {
            s_instance = this;
            AppShortcutsBinding.NPAppShortcutsSetShortcutClickedCallback(HandleShortcutClicked);
        }

        #endregion

        #region INativeAppShortcutsInterface implementation methods

        public event ShortcutClickedInternalCallback OnShortcutClicked;
        
        public void Add(AppShortcutItem item)
        {
            NativeAppShortcutItem nativeItem = new NativeAppShortcutItem();
            nativeItem.Identifier           = item.Identifier;
            nativeItem.Title                = item.Title;
            nativeItem.Subtitle             = item.Subtitle;
            nativeItem.IconFileName         = item.IconFileName;
            
            AppShortcutsBinding.NPAppShortcutsAddShortcut(nativeItem);
        }
        public void Remove(string shortcutItemId)
        {
            AppShortcutsBinding.NPAppShortcutsRemoveShortcut(shortcutItemId);
        }

        #endregion

        #region Native callback methods
        
        [MonoPInvokeCallback(typeof(OnShortcutClickedNativeCallback))]
        private static void HandleShortcutClicked(string shortcutId)
        {
            s_instance.OnShortcutClicked?.Invoke(shortcutId);
        }

        #endregion

    }
}
#endif