using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimelessEchoes
{
    /// <summary>
    /// Persistent helper for synchronizing save files with Steam Cloud.
    /// Prevents cloud data from overwriting local wipes.
    /// </summary>
    public class SteamCloudSync : MonoBehaviour
    {
        private static SteamCloudSync instance;
        private string fileName;
        private bool skipDownload;
        private bool uploadAfterLoad;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static SteamCloudSync Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SteamCloudSync>();
                    if (instance == null)
                        instance = new GameObject("SteamCloudSync").AddComponent<SteamCloudSync>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Sets the save file name used for cloud operations.
        /// </summary>
        public void SetFileName(string name)
        {
            fileName = name;
        }

        /// <summary>
        /// Downloads the save file from Steam Cloud unless skipped.
        /// </summary>
        public void Download()
        {
            if (skipDownload)
            {
                skipDownload = false;
                return;
            }

            if (!string.IsNullOrEmpty(fileName))
                SteamCloudManager.DownloadFile(fileName);
        }

        /// <summary>
        /// Uploads the local save file to Steam Cloud.
        /// </summary>
        public void Upload()
        {
            if (!string.IsNullOrEmpty(fileName))
                SteamCloudManager.UploadFile(fileName);
        }

        /// <summary>
        /// Skips the next download attempt. Used when wiping saves.
        /// </summary>
        public void SkipNextDownload()
        {
            skipDownload = true;
        }

        /// <summary>
        /// Uploads to Steam Cloud when the next scene finishes loading.
        /// </summary>
        public void QueueUploadAfterSceneLoad()
        {
            if (uploadAfterLoad)
                return;

            uploadAfterLoad = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (uploadAfterLoad)
            {
                uploadAfterLoad = false;
                Upload();
            }
        }
    }
}
