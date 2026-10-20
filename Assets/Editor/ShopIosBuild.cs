#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class ShopIosBuild
{
    [PostProcessBuild(110)]
    public static void Configure(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        var file = PBXProject.GetPBXProjectPath(path);
        var project = new PBXProject(); project.ReadFromFile(file);
        const string relative = "Libraries/Plugins/iOS/EOVLegacyPurchase.swift";
        var swift = project.FindFileGuidByProjectPath(relative);
        if (string.IsNullOrEmpty(swift))
        {
            var destination = System.IO.Path.Combine(path, relative);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
            System.IO.File.Copy(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Plugins/iOS/EOVLegacyPurchase.swift"), destination, true);
            swift = project.AddFile(relative, relative, PBXSourceTree.Source);
            project.AddFileToBuild(project.GetUnityFrameworkTargetGuid(), swift);
        }
        project.SetBuildProperty(project.GetUnityFrameworkTargetGuid(), "SWIFT_VERSION", "5.0");
        project.SetBuildProperty(project.GetUnityMainTargetGuid(), "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "StoreKit.framework", false);
        project.WriteToFile(file);
    }
}
#endif
