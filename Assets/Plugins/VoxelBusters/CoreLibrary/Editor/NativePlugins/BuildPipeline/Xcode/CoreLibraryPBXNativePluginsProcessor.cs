#if UNITY_IOS || UNITY_TVOS
using UnityEngine;
namespace VoxelBusters.CoreLibrary.Editor.NativePlugins.Build.Xcode
{
    public class CoreLibraryPBXNativePluginsProcessor : PBXNativePluginsProcessor
    {
        public override void OnUpdateConfiguration()
        {
            if (Manager is PBXNativePluginsManager pluginsManager)
            {
                string targetGuid = pluginsManager.Project.GetFrameworkGuid();
            
                var existingSwiftVersion = pluginsManager.Project.GetBuildPropertyForAnyConfig(targetGuid, "SWIFT_VERSION");
                if (string.IsNullOrEmpty(existingSwiftVersion))
                {
                    pluginsManager.Project.AddBuildProperty(targetGuid, "SWIFT_VERSION", "5.0");
                }
            }
        }
    }
}
#endif