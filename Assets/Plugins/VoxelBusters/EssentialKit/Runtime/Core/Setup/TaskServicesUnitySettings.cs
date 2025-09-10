using System;
using VoxelBusters.CoreLibrary;
namespace VoxelBusters.EssentialKit
{
    [Serializable]
    public class TaskServicesUnitySettings : SettingsPropertyGroup
    {
        #region Constructors

        public TaskServicesUnitySettings(bool isEnabled = true)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kTaskServices)
        { 
   
        }

        #endregion
    }
}