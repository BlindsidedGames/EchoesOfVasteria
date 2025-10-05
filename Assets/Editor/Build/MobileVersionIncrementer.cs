using UnityEditor;
using UnityEngine;

namespace BuildTools
{
    public static class MobileVersionIncrementer
    {
        [MenuItem("Build/Increment Mobile Bundle Versions")]
        public static void IncrementMobileBundleVersions()
        {
            var previousAndroidCode = PlayerSettings.Android.bundleVersionCode;
            var newAndroidCode = previousAndroidCode + 1;
            PlayerSettings.Android.bundleVersionCode = newAndroidCode;

            var previousIosBuild = PlayerSettings.iOS.buildNumber;
            if (!int.TryParse(string.IsNullOrWhiteSpace(previousIosBuild) ? "0" : previousIosBuild, out var iosBuildNumber))
            {
                Debug.LogWarning($"iOS build number '{previousIosBuild}' is not numeric. Resetting to 0 before incrementing.");
                iosBuildNumber = 0;
            }
            iosBuildNumber++;
            var newIosBuild = iosBuildNumber.ToString();
            PlayerSettings.iOS.buildNumber = newIosBuild;

            Debug.Log($"Incremented bundle versions -> Android: {previousAndroidCode} -> {newAndroidCode}, iOS: {previousIosBuild ?? "(unset)"} -> {newIosBuild}");
        }
    }
}