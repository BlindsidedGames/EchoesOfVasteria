using Blindsided.SaveData;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace Blindsided
{
    /// <summary>
    ///     Single-instance save manager using Easy Save 3 with
    ///     • caching enabled   • 8 KB buffer   • one backup / session
    /// </summary>
    [DefaultExecutionOrder(0)]
    public partial class Oracle : SerializedMonoBehaviour
    {
        public static Oracle oracle;

        private void Awake()
        {
            if (oracle == null)
            {
                oracle = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            CurrentSlot = Mathf.Clamp(PlayerPrefs.GetInt(SlotPrefKey, 0), 0, 2);
            _settings = new ES3Settings(_fileName, ES3.Location.Cache)
            {
                bufferSize = 8192
            };
            wipeInProgress = false;
            TryMigrateFromBetaIfNeeded();
        }

        [TabGroup("SaveData", "Beta")] public bool beta;
        [TabGroup("SaveData", "Beta")] public int betaSaveIteration;

		[TabGroup("SaveData")] [ShowInInspector] public int CurrentSlot { get; private set; }

        private string _dataName => (beta ? $"Beta{betaSaveIteration}" : "") + $"Data{CurrentSlot}";
        private string _fileName => (beta ? $"Beta{betaSaveIteration}" : "") + $"Sd{CurrentSlot}.es3";

        public string DataName => _dataName;
        public string FileName => _fileName;
        public ES3Settings Settings => _settings;

		[TabGroup("SaveData")] public GameData saveData = new();

		[Header("Backups")] 
		[Tooltip("How many timestamped session backups to keep per slot in PersistentDataPath/Backups/<FileNameWithoutExtension>.")]
		[Range(1, 50)] public int backupsToKeepPerSlot = 10;

        private ES3Settings _settings;
        private bool loaded;
        private bool wipeInProgress;
        private const string SlotPrefKey = "SaveSlot";
        private const string BetaMigrationPrefKey = "BetaToLiveMigrationDone";
        private const string GenericMigrationPrefKey = "GenericEs3MigrationDone";

        // Defer showing load-failure notice until UI is ready
        private bool _pendingLoadFailureNotice;
        private string _pendingLoadFailureMessage;
        private bool _mainSceneLoadDeferred;

    }
}
