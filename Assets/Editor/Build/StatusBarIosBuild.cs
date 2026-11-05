#if UNITY_IOS
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

internal static class StatusBarIosBuild
{
    [PostProcessBuild(110)]
    private static void Configure(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS) return;

        var controllerPath = Path.Combine(buildPath, "Classes/UI/UnityViewControllerBase+iOS.mm");
        if (!File.Exists(controllerPath))
            throw new BuildFailedException("Status bar: Unity's iOS view controller source was not found.");

        var controller = File.ReadAllText(controllerPath);
        if (!controller.Contains("EOVSetStatusBarVisible"))
        {
            var method = new Regex(@"-\s*\(\s*BOOL\s*\)\s*prefersStatusBarHidden\s*\{");
            if (method.Matches(controller).Count != 1)
                throw new BuildFailedException("Status bar: could not identify Unity's iOS visibility method.");

            // Keep Unity's launch default until the saved preference has loaded.
            controller = method.Replace(controller,
                "$0\n    if (EOVStatusBarVisible >= 0) return EOVStatusBarVisible == 0;", 1);
            File.WriteAllText(controllerPath, NativeBridge + controller);
        }

        var plistPath = Path.Combine(buildPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("UIViewControllerBasedStatusBarAppearance", true);
        plist.WriteToFile(plistPath);
    }

    private const string NativeBridge = @"#import <UIKit/UIKit.h>
#include ""UnityInterface.h""

static int EOVStatusBarVisible = -1;

extern ""C"" void EOVSetStatusBarVisible(int visible)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        EOVStatusBarVisible = visible;
        [UnityGetGLViewController() setNeedsStatusBarAppearanceUpdate];
    });
}

";
}
#endif
