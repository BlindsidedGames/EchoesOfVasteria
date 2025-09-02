using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.Editor
{
	public class UninstallPlugin
	{
		#region Constants
	
		private const	string		kAlertTitle		= "Uninstall - Essential Kit";
        
		private const	string		kAlertMessage	= "Backup before doing this step to preserve changes done in this plugin. This deletes files only related to the Essential Kit plugin. Do you want to proceed?";
		
		#endregion	
	
		#region Static methods
	
		public static void Uninstall(bool completely = true)
		{
			bool	confirmationStatus	= EditorUtility.DisplayDialog(kAlertTitle, kAlertMessage, "Uninstall", "Cancel");
			if (!confirmationStatus) return;

			// delete all the files and folders belonging to the plugin
			List<string> pluginFoldersToBeDeleted = GetPluginFoldersToDelete(completely);
			
			foreach (string folder in pluginFoldersToBeDeleted)
			{
                string	absolutePath	= Application.dataPath + "/../" + folder;
                IOServices.DeleteFileOrDirectory(absolutePath);
                IOServices.DeleteFileOrDirectory(absolutePath + ".meta");
			}
			EssentialKitSettingsEditorUtility.RemoveGlobalDefines();
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("Essential Kit",
				                        "Uninstall successful!", 
				                        "Ok");
		}

		private static List<string> GetPluginFoldersToDelete(bool completely)
		{
			var list = new List<string>();
			list.Add(EssentialKitSettings.Package.GetInstallPath());
			foreach (var each in EssentialKitSettings.Package.Dependencies)
			{
				list.Add(each.GetInstallPath());
			}

			if (completely)
			{
				list.Add("Assets/ExternalDependencyManager");
			}

			return list;
		}
		#endregion
	}
}