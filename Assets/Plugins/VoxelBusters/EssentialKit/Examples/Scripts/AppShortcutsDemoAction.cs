using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
	public enum AppShortcutsDemoActionType
    {
        Add,
        Remove,
        ResourcePage
    }

    public class AppShortcutsDemoAction : DemoActionBehaviour<AppShortcutsDemoActionType> 
    {}
}