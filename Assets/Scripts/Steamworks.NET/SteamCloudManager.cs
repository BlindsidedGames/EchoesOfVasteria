#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.IO;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace TimelessEchoes
{
    /// <summary>
    /// Utility methods for syncing save files with Steam Cloud.
    /// </summary>
    public static class SteamCloudManager
    {
#if !DISABLESTEAMWORKS
        /// <summary>
        /// Uploads a local file to Steam Cloud if Steam is initialized.
        /// </summary>
        public static void UploadFile(string fileName)
        {
            if (!SteamManager.Initialized)
                return;

            var path = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(path))
                return;

            var data = File.ReadAllBytes(path);
            SteamRemoteStorage.FileWrite(fileName, data, data.Length);
        }

        /// <summary>
        /// Downloads a file from Steam Cloud if available. Returns true when a file was retrieved.
        /// </summary>
        public static bool DownloadFile(string fileName)
        {
            if (!SteamManager.Initialized)
                return false;

            if (!SteamRemoteStorage.FileExists(fileName))
                return false;

            var size = SteamRemoteStorage.GetFileSize(fileName);
            var buffer = new byte[size];
            var read = SteamRemoteStorage.FileRead(fileName, buffer, size);
            if (read != size)
                return false;

            var path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(path, buffer);
            return true;
        }

        /// <summary>
        /// Deletes a save file from both Steam Cloud and local storage.
        /// </summary>
        public static void DeleteFile(string fileName)
        {
            var path = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(path))
                File.Delete(path);

            var backupPath = path + ".bac";
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            if (!SteamManager.Initialized)
                return;

            if (SteamRemoteStorage.FileExists(fileName))
                SteamRemoteStorage.FileDelete(fileName);
        }
#endif
    }
}
