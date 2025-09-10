using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
	public enum AlertDialogDemoActionType
	{
		New,
		SetTitle,
		GetTitle,
		SetMessage,
		GetMessage,
		AddTextInputField,
		AddButton,
		AddCancelButton,
		Show,
		ResourcePage,
	}

	public class AlertDialogDemoAction : DemoActionBehaviour<AlertDialogDemoActionType> 
	{}
}