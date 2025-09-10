using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
	public enum MediaServicesDemoActionType
	{
		GetGalleryAccessStatus,
        GetCameraAccessStatus,
        SelectMediaContent,
        CaptureMediaContent,
		SaveMediaContent,
        ResourcePage,
	}

	public class MediaServicesDemoAction : DemoActionBehaviour<MediaServicesDemoActionType> 
	{}
}