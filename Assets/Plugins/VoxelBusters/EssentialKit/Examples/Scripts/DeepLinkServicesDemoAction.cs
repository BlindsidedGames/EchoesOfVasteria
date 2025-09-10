using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
	public enum DeepLinkServicesDemoActionType
    {
        ResourcePage
    }

    public class DeepLinkServicesDemoAction : DemoActionBehaviour<DeepLinkServicesDemoActionType> 
    {}
}