#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System.Collections;
using System.Collections.Generic;
using Blindsided.SaveData;
using Blindsided.Utilities;
using Pathfinding;
using References.UI;
using TimelessEchoes.Audio;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.NPC;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TimelessEchoes.UI;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Utilities;
using static TimelessEchoes.Quests.QuestUtils;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static TimelessEchoes.TELogger;
using static Blindsided.Oracle;
using static Blindsided.SaveData.StaticReferences;
using Sirenix.OdinInspector;

namespace TimelessEchoes
{
    /// <summary>
    ///     Controls map generation, camera switching and UI visibility.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public static MapGenerationConfig CurrentGenerationConfig { get; private set; }
        [TitleGroup("Prefabs")]
        [SerializeField] private GameObject mapPrefab;
        [TitleGroup("Prefabs")]
        [SerializeField] private GameObject gravestonePrefab;
        [TitleGroup("Prefabs")]
        [SerializeField] private GameObject reaperPrefab;
        [TitleGroup("Prefabs")]
        [SerializeField] private Vector3 reaperSpawnOffset = Vector3.zero;

        [TitleGroup("UI")]
        [TitleGroup("UI/General")]
        [SerializeField] private Button returnToTavernButton;
        [TitleGroup("UI/General")]
        [SerializeField] private TMP_Text returnToTavernText;
        [TitleGroup("UI/General")]
        [SerializeField] private TMP_Text retreatBonusText;
        [TitleGroup("UI/General")]
        [SerializeField] private Button returnOnDeathButton;
        [TitleGroup("UI/General")]
        [SerializeField] private TMP_Text returnOnDeathText;
        [TitleGroup("UI/General")]
        [TitleGroup("UI/General")]
        [SerializeField] [Min(0f)] private float bonusPercentPerKill = 2f;
        [TitleGroup("UI/General")]
        [SerializeField] private GameObject tavernUI;
        [TitleGroup("UI/General")]
        [SerializeField] private GameObject mapUI;
        [TitleGroup("UI/General")]
        [SerializeField] private RunDropUI runDropUI;
        [TitleGroup("UI/General")]
        [SerializeField] private RunResourceTrackerUI runResourceTracker;
        [TitleGroup("UI/General")]
        [SerializeField] private RunCalebUIReferences runCalebUI;
        [TitleGroup("UI/General")]
        [SerializeField] private Transform meetingParent;
        [TitleGroup("UI/General")]
        [SerializeField] private GameObject savesObject;

        [TitleGroup("UI/Death Window")]
        [SerializeField] private GameObject deathWindow;
        [TitleGroup("UI/Death Window")]
        [SerializeField] private Button deathRunButton;
        [TitleGroup("UI/Death Window")]
        [SerializeField] private Button deathReturnButton;
        [TitleGroup("UI/Death Window")]
        [SerializeField] private SlicedFilledImage deathTimerImage;
        [TitleGroup("UI/Death Window")]
        [SerializeField] private float deathWindowDuration = 20f;
        [TitleGroup("UI/General")]
        [SerializeField] public string mildredQuestId;

        public GameObject ReaperPrefab => reaperPrefab;
        public GameObject GravestonePrefab => gravestonePrefab;
        public Vector3 ReaperSpawnOffset => reaperSpawnOffset;
        public Transform MeetingParent => meetingParent;
        public GameObject CurrentMap => currentMap;
        public float BonusPercentPerKill => bonusPercentPerKill;

        public event System.Action HeroDied;

        [TitleGroup("Map Generation")]
        [SerializeField] private List<MapGenerationButton> generationButtons = new();
        [SerializeField] private float fadeDuration = 1f;

        [Header("Cameras")] [SerializeField] private CinemachineCamera tavernCamera;

        private CloudSpawner cloudSpawner;

        private GameObject currentMap;
        private bool runEndedByDeath;
        private bool runEndedByReaper;
        private bool heroDead;
        private Coroutine deathWindowCoroutine;
        private HeroController hero;
        private CinemachineCamera mapCamera;
        private TaskController taskController;
        private NpcObjectStateController npcObjectStateController;
        private LocationObjectStateController locationObjectStateController;
        private GameplayStatTracker statTracker;
        private System.Action<bool> runEndedAction;
        private bool returnOnDeathQueued;
        private bool retreatQueued;
        // Track whether the run resource tracker should reset on the next run
        private bool resetRunResourceTracker = true;
        private readonly Dictionary<Button, UnityEngine.Events.UnityAction> _buttonActions = new();

        [SerializeField] private float statsUpdateInterval = 0.1f;
        private float nextStatsUpdateTime;

        [SerializeField] private float startingDistance;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private float warpDuration = 1f;

        // Auto-restart on stall settings
        [TitleGroup("Run Stall Monitor")] [SerializeField]
        private bool enableStallAutoRestart = true;
        [TitleGroup("Run Stall Monitor")] [SerializeField] [Min(1f)]
        private float stallTimeoutSeconds = 60f;
        [TitleGroup("Run Stall Monitor")] [SerializeField] [Min(0.1f)]
        private float stallCheckIntervalSeconds = 5f;
        [TitleGroup("Run Stall Monitor")] [SerializeField] [Min(0f)]
        private float stallDistanceEpsilon = 0.01f;
        private Coroutine stallMonitorCoroutine;
        private float lastObservedRunDistance;
        private float lastRunDistanceChangeTime;
        private bool pendingAutoRestartFromStall;
        // One-shot watchdog to ensure the death UI or auto-return engages after hero death.
        private float deathUiFailsafeCheckAt = -1f;

        private void UpdateGenerationButtonStats()
        {
            if (statTracker == null) return;
            foreach (var entry in generationButtons)
            {
                if (entry?.config == null) continue;
                var stats = statTracker.GetMapStats(entry.config) ?? new GameData.MapStatistics();
                if (entry.statsUI != null && entry.statsUI.distanceLongestTasksText != null)
                {
                    var dist = CalcUtils.FormatNumber(stats.Steps, true);
                    var longest = CalcUtils.FormatNumber(stats.LongestTrek, true);
                    var tasks = CalcUtils.FormatNumber(stats.TasksCompleted, true);
                    var resources = CalcUtils.FormatNumber(stats.ResourcesGathered, true);
                    entry.statsUI.distanceLongestTasksText.text = $"Steps Taken: {dist}\nLongest Run: {longest}\nTasks Completed: {tasks}\nResources Gathered: {resources}";
                }
                if (entry.statsUI != null && entry.statsUI.killsDamageDeathsText != null)
                {
                    var kills = CalcUtils.FormatNumber(stats.Kills, true);
                    var dealt = CalcUtils.FormatNumber(stats.DamageDealt, true);
                    var deaths = CalcUtils.FormatNumber(stats.Deaths, true);
                    var taken = CalcUtils.FormatNumber(stats.DamageTaken, true);
                    entry.statsUI.killsDamageDeathsText.text = $"Kills: {kills}\nDamage Dealt: {dealt}\nDeaths: {deaths}\nDamage Taken: {taken}";
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            cloudSpawner = CloudSpawner.Instance;
            foreach (var entry in generationButtons)
            {
                if (entry?.button == null) continue;
                var cfg = entry.config;
                var track = entry.musicTrack;
                UnityEngine.Events.UnityAction action = () =>
                {
                    AudioManager.Instance.PlayMusic(track, fadeDuration);
                    StartRun(cfg);
                };
                entry.button.onClick.AddListener(action);
                _buttonActions.Add(entry.button, action);
            }
            if (returnToTavernButton != null)
                returnToTavernButton.onClick.AddListener(OnReturnToTavernButton);
            if (returnOnDeathButton != null)
                returnOnDeathButton.onClick.AddListener(QueueReturnOnDeath);
            if (deathRunButton != null)
                deathRunButton.onClick.AddListener(OnDeathRunButton);
            if (deathReturnButton != null)
                deathReturnButton.onClick.AddListener(OnDeathReturnButton);
            npcObjectStateController = NpcObjectStateController.Instance;
            if (npcObjectStateController == null)
                Log("NpcObjectStateController missing", TELogCategory.General, this);
            locationObjectStateController = LocationObjectStateController.Instance;
            if (locationObjectStateController == null)
                Log("LocationObjectStateController missing", TELogCategory.General, this);
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                Log("GameplayStatTracker missing", TELogCategory.General, this);
            else
            {
                runEndedAction = _ => UpdateGenerationButtonStats();
                statTracker.OnRunEnded += runEndedAction;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (returnToTavernButton != null)
                returnToTavernButton.onClick.RemoveListener(OnReturnToTavernButton);
            if (returnOnDeathButton != null)
                returnOnDeathButton.onClick.RemoveListener(QueueReturnOnDeath);
            if (deathRunButton != null)
                deathRunButton.onClick.RemoveListener(OnDeathRunButton);
            if (deathReturnButton != null)
                deathReturnButton.onClick.RemoveListener(OnDeathReturnButton);
            foreach (var pair in _buttonActions)
            {
                if (pair.Key != null)
                    pair.Key.onClick.RemoveListener(pair.Value);
            }
            _buttonActions.Clear();
            if (statTracker != null && runEndedAction != null)
            {
                statTracker.OnRunEnded -= runEndedAction;
                runEndedAction = null;
            }
        }

        private void Start()
        {
            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
            savesObject?.SetActive(true);
#if !DISABLESTEAMWORKS
            RichPresenceManager.Instance?.SetInTown();
#endif
            if (deathWindow != null)
                deathWindow.SetActive(false);
            if (returnToTavernText != null)
                returnToTavernText.text = "Return To Town";
            if (returnOnDeathText != null)
                returnOnDeathText.text = "Return On Death";
            npcObjectStateController?.UpdateObjectStates();
            locationObjectStateController?.UpdateObjectStates();

            UpdateGenerationButtonStats();
            nextStatsUpdateTime = Time.time + statsUpdateInterval;
        }

        private void Update()
        {
            if (Time.time >= nextStatsUpdateTime)
            {
                UpdateGenerationButtonStats();
                nextStatsUpdateTime = Time.time + statsUpdateInterval;
            }
            if (returnToTavernButton != null)
            {
                var active = hero != null && hero.gameObject.activeSelf && !heroDead;
                returnToTavernButton.interactable = active;
                if (returnToTavernText != null)
                {
                    if (retreatQueued)
                        returnToTavernText.text = "Retreating...";
                    else if (hero != null && hero.InCombat)
                        returnToTavernText.text = "In Combat...";
                    else
                        returnToTavernText.text = "Return To Town";
                }

                if (retreatBonusText != null)
                {
                    if (active && hero != null && !hero.InCombat)
                    {
                        var kills = statTracker != null ? statTracker.CurrentRunKills : 0;
                        var percent = kills * bonusPercentPerKill;
                        retreatBonusText.text = $"+{percent:0}% Resources";
                    }
                    else if (retreatQueued && hero != null && hero.InCombat)
                    {
                        var kills = statTracker != null ? statTracker.CurrentRunKills : 0;
                        var percent = kills * bonusPercentPerKill;
                        retreatBonusText.text = $"Retreat Queued +{percent:0}%";
                    }
                    else if (hero != null && hero.InCombat)
                    {
                        retreatBonusText.text = "Queue Retreat";
                    }
                    else
                    {
                        retreatBonusText.text = "+0% Resources";
                    }
                }
            }

            if (returnOnDeathButton != null)
            {
                var active = hero != null && hero.gameObject.activeSelf && !heroDead;
                returnOnDeathButton.interactable = active;
            }

            if (retreatQueued && hero != null && !hero.InCombat)
            {
                retreatQueued = false;
                StartCoroutine(ReturnToTavernRoutine());
            }

            // Death UI failsafe: if the hero is dead but neither the death window nor the
            // tavern UI is visible after a short delay, re-trigger the expected flow.
            if (heroDead && deathUiFailsafeCheckAt > 0f && Time.time >= deathUiFailsafeCheckAt)
            {
                bool deathWindowActive = deathWindow != null && deathWindow.activeInHierarchy;
                bool tavernActive = tavernUI != null && tavernUI.activeInHierarchy;
                if (!deathWindowActive && !tavernActive)
                {
                    if (returnOnDeathQueued || retreatQueued)
                    {
                        returnOnDeathQueued = false;
                        retreatQueued = false;
                        StartCoroutine(ReturnToTavernRoutine());
                    }
                    else
                    {
                        if (deathWindowCoroutine == null)
                            deathWindowCoroutine = StartCoroutine(DeathWindowRoutine());
                    }
                    // Check again in case the first attempt is interrupted by ordering.
                    deathUiFailsafeCheckAt = Time.time + 2f;
                }
                else
                {
                    // Flow engaged; stop checking.
                    deathUiFailsafeCheckAt = -1f;
                }
            }
        }

        private void HideTooltip()
        {
            var tooltip = FindFirstObjectByType<TooltipUIReferences>();
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }


        private void OnReturnToTavernButton()
        {
            if (hero != null && hero.InCombat)
            {
                retreatQueued = true;
                if (returnToTavernText != null)
                    returnToTavernText.text = "Retreating...";
                if (retreatBonusText != null)
                {
                    var kills = statTracker != null ? statTracker.CurrentRunKills : 0;
                    var percent = kills * bonusPercentPerKill;
                    retreatBonusText.text = $"Retreat Queued +{percent:0}%";
                }
            }
            else
            {
                StartCoroutine(ReturnToTavernRoutine());
            }
        }

        private void QueueReturnOnDeath()
        {
            returnOnDeathQueued = true;
            if (returnOnDeathText != null)
                returnOnDeathText.text = "Queued";
        }

        public void AbandonRun()
        {
            StartCoroutine(ReturnToTavernRoutine(true));
        }

        private void StartRun(MapGenerationConfig config)
        {
            CurrentGenerationConfig = config;
            cloudSpawner?.SetAllowClouds(config == null || config.allowClouds);
            TownWindowManager.Instance?.CloseAllWindows();
            StartRun();
        }

        private void StartRun()
        {
            HideTooltip();
            cloudSpawner?.SetAllowClouds(CurrentGenerationConfig == null || CurrentGenerationConfig.allowClouds);
            heroDead = false;
            returnOnDeathQueued = false;
            retreatQueued = false;
            savesObject?.SetActive(false);
            if (returnOnDeathText != null)
                returnOnDeathText.text = "Return On Death";
            if (returnToTavernText != null)
                returnToTavernText.text = "Return To Town";
#if !DISABLESTEAMWORKS
            RichPresenceManager.Instance?.SetInRun();
#endif
            if (runEndedByDeath && statTracker != null)
                statTracker.EndRun(true, runEndedByReaper);
            Log("Run starting", TELogCategory.Run, this);
            runEndedByDeath = false;
            runEndedByReaper = false;
            if (deathWindowCoroutine != null)
            {
                StopCoroutine(deathWindowCoroutine);
                deathWindowCoroutine = null;
            }

            if (deathWindow != null)
                deathWindow.SetActive(false);
            BuffManager.Instance?.ClearActiveBuffs();
            BuffManager.Instance?.UpdateDistance(0f);
            // If this is the first run of a session (tavern was active), reset session aggregates
            if (statTracker != null && (tavernUI != null && tavernUI.activeSelf))
                statTracker.BeginSession();
            StartCoroutine(StartRunRoutine());
        }

        private IEnumerator StartRunRoutine()
        {
            yield return StartCoroutine(CleanupMapRoutine());

            if (statTracker == null)
            {
                statTracker = GameplayStatTracker.Instance;
                if (statTracker == null)
                    Log("GameplayStatTracker missing", TELogCategory.General, this);
            }

            statTracker?.BeginRun(CurrentGenerationConfig);
            runDropUI?.ResetDrops();
            if (resetRunResourceTracker)
            {
                runResourceTracker?.BeginRun();
                resetRunResourceTracker = false;
            }
            currentMap = Instantiate(mapPrefab);
            taskController = currentMap.GetComponentInChildren<TaskController>();
            if (taskController == null)
                yield break;

            hero = taskController.hero;
            if (hero != null)
            {
                hero.gameObject.SetActive(true);
                var hp = hero.GetComponent<HeroHealth>();
                if (hp != null)
                {
                    hp.Init((int)hp.MaxHealth);
                    hp.OnDeath += OnHeroDeath;
                    if (runCalebUI == null)
                        runCalebUI = FindFirstObjectByType<RunCalebUIReferences>();
                    if (runCalebUI != null)
                        hp.HealthBar = runCalebUI.healthBar;
                }

                if (CurrentGenerationConfig != null)
                {
                    var heroPos = hero.transform.position;
                    heroPos.y = CurrentGenerationConfig.heroStartY;
                    hero.transform.position = heroPos;
                }
            }

            EnableMildred();

            Physics2D.SyncTransforms();
            yield return null;


            mapCamera = taskController.MapCamera;
            if (mapCamera != null)
            {
                // Snap the camera to the hero's position so there is no
                // visible panning when the run begins.
                if (hero != null)
                {
                    var camPos = hero.transform.position;
                    camPos += mapCamera.transform.rotation * Vector3.forward * -10f;
                    mapCamera.ForceCameraPosition(camPos, mapCamera.transform.rotation);
                }

                mapCamera.Priority = 10;
            }

            if (tavernCamera != null)
                tavernCamera.gameObject.SetActive(false);
            if (mapCamera != null)
            {
                mapCamera.gameObject.SetActive(true);
                cloudSpawner?.ResetClouds(false);
            }

            tavernUI?.SetActive(false);
            mapUI?.SetActive(true);
            if (runCalebUI == null)
                runCalebUI = FindFirstObjectByType<RunCalebUIReferences>();
            if (runCalebUI != null)
                runCalebUI.gameObject.SetActive(true);
            npcObjectStateController?.UpdateObjectStates();
            locationObjectStateController?.UpdateObjectStates();

            yield return StartCoroutine(FastForwardStart());

            // Start monitoring for stalled distance after run begins
            if (stallMonitorCoroutine != null)
            {
                StopCoroutine(stallMonitorCoroutine);
                stallMonitorCoroutine = null;
            }
            if (enableStallAutoRestart)
                stallMonitorCoroutine = StartCoroutine(MonitorRunStallRoutine());
        }

        private IEnumerator FastForwardStart()
        {
            if (hero == null)
                yield break;

            var playerInput = hero.GetComponent<PlayerInput>();
            if (playerInput != null)
                playerInput.enabled = false;
            hero.SetActiveState(false);
            if (loadingOverlay != null)
                loadingOverlay.SetActive(true);

            var startPos = hero.transform.position;
            if (warpDuration > 0f)
            {
                var speed = Mathf.Abs(startingDistance - startPos.x) / warpDuration;
                float elapsed = 0f;
                while (elapsed < warpDuration && !Mathf.Approximately(hero.transform.position.x, startingDistance))
                {
                    var pos = hero.transform.position;
                    pos.x = Mathf.MoveTowards(pos.x, startingDistance, speed * Time.deltaTime);
                    hero.transform.position = pos;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            var finalPos = hero.transform.position;
            finalPos.x = startingDistance;
            hero.transform.position = finalPos;

            yield return null;

            taskController?.RemoveTasksLeftOf(hero.transform.position.x);

            var enemies = EnemyActivator.ActiveEnemies;
            if (enemies != null)
            {
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var enemy = enemies[i];
                    if (enemy != null && enemy.transform.position.x < hero.transform.position.x)
                        Destroy(enemy.gameObject);
                }
            }

            hero.SetActiveState(true);
            if (playerInput != null)
                playerInput.enabled = true;
            if (loadingOverlay != null)
                loadingOverlay.SetActive(false);
        }

        private void OnHeroDeath()
        {
            DestroyAllEchoes();
            heroDead = true;
            HeroDied?.Invoke();
            if (returnToTavernButton != null)
                returnToTavernButton.interactable = false;
            if (returnOnDeathButton != null)
                returnOnDeathButton.interactable = false;
            bool distanceReaper = false;
            if (hero != null)
            {
                var controller = hero.GetComponent<Hero.HeroController>();
                if (controller != null)
                    distanceReaper = controller.ReaperSpawnedByDistance;

                var hp = hero.GetComponent<HeroHealth>();
                if (hp != null)
                    hp.OnDeath -= OnHeroDeath;

                var ai = hero.GetComponent<AIPath>();
                if (ai != null)
                    ai.enabled = false;

                if (!distanceReaper)
                {
                    hero.gameObject.SetActive(false);
                    if (gravestonePrefab != null && currentMap != null)
                        Instantiate(gravestonePrefab, hero.transform.position, Quaternion.identity,
                            currentMap.transform);
                }
            }

            AudioManager.Instance?.PlayHeroDeathClip();

            Log("Hero death", TELogCategory.Hero, this);

            if (statTracker == null)
            {
                statTracker = GameplayStatTracker.Instance;
                if (statTracker == null)
                    Log("GameplayStatTracker missing", TELogCategory.General, this);
            }

            // Only count a death if it was not caused by the distance reaper
            if (statTracker != null && !distanceReaper)
                statTracker.AddDeath();

            BuffManager.Instance?.ClearActiveBuffs();

            runEndedByDeath = true;
            runEndedByReaper = distanceReaper;
            if (runDropUI != null)
                runDropUI.ResetDrops();

            if (returnOnDeathQueued || retreatQueued)
            {
                returnOnDeathQueued = false;
                retreatQueued = false;
                StartCoroutine(ReturnToTavernRoutine());
            }
            else
            {
                deathWindowCoroutine = StartCoroutine(DeathWindowRoutine());
            }
            // Engage failsafe in case UI does not appear due to timing/ordering on some setups.
            deathUiFailsafeCheckAt = Time.time + 1f;
        }

        private void OnDeathRunButton()
        {
            if (deathWindowCoroutine != null)
                StopCoroutine(deathWindowCoroutine);
            if (deathWindow != null)
                deathWindow.SetActive(false);
            StartRun();
            deathUiFailsafeCheckAt = -1f;
        }

        private void OnDeathReturnButton()
        {
            if (deathWindowCoroutine != null)
                StopCoroutine(deathWindowCoroutine);
            if (deathWindow != null)
                deathWindow.SetActive(false);
            StartCoroutine(ReturnToTavernRoutine());
            deathUiFailsafeCheckAt = -1f;
        }

        /// <summary>
        ///     Fallback invoked when the hero is reaped. If a return was queued (return-on-death
        ///     or retreat), force an immediate return to the tavern and mark the run as ended by
        ///     a reaper death. This guards against any edge cases where the death event flow is
        ///     interrupted (e.g., due to timing) and ensures the expected UX.
        /// </summary>
        public void EnsureAutoReturnOnReapIfQueued()
        {
            if (!(returnOnDeathQueued || retreatQueued))
                return;

            // Ensure state reflects a death by reaper
            heroDead = true;
            runEndedByDeath = true;
            runEndedByReaper = true;

            // Close any pending death window flow if it exists
            if (deathWindowCoroutine != null)
            {
                StopCoroutine(deathWindowCoroutine);
                deathWindowCoroutine = null;
            }
            if (deathWindow != null)
                deathWindow.SetActive(false);

            // Start returning to tavern now
            StartCoroutine(ReturnToTavernRoutine());
        }

        /// <summary>
        ///     If, for any reason, the standard OnDeath event chain did not invoke
        ///     <see cref="OnHeroDeath"/>, this method triggers it exactly once.
        /// </summary>
        public void ForceHandleHeroDeath()
        {
            if (heroDead)
                return;
            OnHeroDeath();
        }

        private IEnumerator DeathWindowRoutine()
        {
            if (deathWindow == null)
            {
                StartRun();
                yield break;
            }

            deathWindow.SetActive(true);
            var t = 0f;
            if (deathTimerImage != null)
                deathTimerImage.fillAmount = 0f;

            while (t < deathWindowDuration)
            {
                if (deathTimerImage != null)
                    deathTimerImage.fillAmount = Mathf.Clamp01(t / deathWindowDuration);
                t += Time.deltaTime;
                yield return null;
            }

            if (deathWindow != null)
                deathWindow.SetActive(false);
            // If we've already returned to the tavern or death state was cleared,
            // do not auto-start a new run from the death timer.
            if (!heroDead || (tavernUI != null && tavernUI.activeInHierarchy))
                yield break;
            StartRun();
        }

        private void ReturnToTavern()
        {
            StartCoroutine(ReturnToTavernRoutine());
        }

        private IEnumerator ReturnToTavernRoutine(bool abandon = false)
        {
            HideTooltip();
            // Ensure any death window countdown is cancelled to avoid unintended auto-run
            if (deathWindowCoroutine != null)
            {
                StopCoroutine(deathWindowCoroutine);
                deathWindowCoroutine = null;
            }
            if (deathWindow != null)
                deathWindow.SetActive(false);
            deathUiFailsafeCheckAt = -1f;
            // Reset state after death.
            heroDead = false;
            returnOnDeathQueued = false;
            retreatQueued = false;
            if (returnOnDeathText != null)
                returnOnDeathText.text = "Return On Death";
            if (returnToTavernText != null)
                returnToTavernText.text = "Return To Town";
            if (statTracker == null)
            {
                statTracker = GameplayStatTracker.Instance;
                if (statTracker == null)
                    Log("GameplayStatTracker missing", TELogCategory.General, this);
            }

            if (!runEndedByDeath && runDropUI != null)
            {
                if (abandon)
                {
                    runDropUI.ResetDrops();
                }
                else
                {
                    var manager = ResourceManager.Instance;
                    if (manager == null)
                    {
                        Log("ResourceManager missing", TELogCategory.Resource, this);
                    }
                    else
                    {
                        var drops = new List<KeyValuePair<Resource, double>>(runDropUI.Amounts);
                        var kills = statTracker != null ? statTracker.CurrentRunKills : 0;
                        var bonusPercent = kills * bonusPercentPerKill * 0.01f;
                        foreach (var pair in drops)
                            manager.Add(pair.Key, pair.Value * bonusPercent, true);
                    }

                    runDropUI.ResetDrops();
                }
            }

            if (statTracker != null)
            {
                if (abandon)
                    statTracker.AbandonRun();
                else if (runEndedByDeath)
                    statTracker.EndRun(true, runEndedByReaper);
                else
                    statTracker.EndRun(false);
            }

            BuffManager.Instance?.ClearActiveBuffs();
            yield return StartCoroutine(CleanupMapRoutine());
            if (tavernCamera != null)
            {
                tavernCamera.gameObject.SetActive(true);
                cloudSpawner?.SetAllowClouds(true);
                cloudSpawner?.ResetClouds(true);
            }

            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
            savesObject?.SetActive(true);
            if (runCalebUI != null)
                runCalebUI.gameObject.SetActive(false);
            runResourceTracker?.ShowWindow();
            resetRunResourceTracker = true;
            npcObjectStateController?.UpdateObjectStates();
            locationObjectStateController?.UpdateObjectStates();
#if !DISABLESTEAMWORKS
            RichPresenceManager.Instance?.SetInTown();
#endif
            Log("Returned to tavern", TELogCategory.Run, this);

            // If we abandoned due to stall, auto-start a fresh run with the same config
            if (pendingAutoRestartFromStall && CurrentGenerationConfig != null)
            {
                pendingAutoRestartFromStall = false;
                StartRun();
            }

            // Ensure resource/inventory state is immediately reflected in save data after a run ends.
            // This synchronizes the in-memory save snapshot (oracle.saveData) with the live
            // ResourceManager amounts so switching slots or inspecting the save right now shows
            // the correct values instead of interim zeros.
            try
            {
                Blindsided.EventHandler.SaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Immediate save after return failed: {ex}");
            }
        }

        private void CleanupMap()
        {
            StartCoroutine(CleanupMapRoutine());
        }

        private IEnumerator CleanupMapRoutine()
        {
            if (stallMonitorCoroutine != null)
            {
                StopCoroutine(stallMonitorCoroutine);
                stallMonitorCoroutine = null;
            }
            if (mapCamera != null)
                mapCamera.gameObject.SetActive(false);

            if (runCalebUI != null)
                runCalebUI.gameObject.SetActive(false);

            if (hero != null)
            {
                var ai = hero.GetComponent<AIPath>();
                if (ai != null)
                    ai.enabled = false;
                hero.gameObject.SetActive(false);
            }

            yield return null; // let AIBase.OnDisable remove the agent

            if (currentMap != null)
            {
                Destroy(currentMap); // safe to destroy AstarPath now
                currentMap = null;
            }
        }

        /// <summary>
        ///     Periodically checks the hero's current run distance and, if it hasn't
        ///     increased for more than <see cref="stallTimeoutSeconds"/>, abandons
        ///     the run and starts a fresh one.
        /// </summary>
        private IEnumerator MonitorRunStallRoutine()
        {
            // Small delay to allow the run to fully initialize
            yield return new WaitForSeconds(2f);

            if (statTracker == null)
                statTracker = GameplayStatTracker.Instance;

            lastObservedRunDistance = statTracker != null ? statTracker.CurrentRunDistance : 0f;
            lastRunDistanceChangeTime = Time.time;

            while (true)
            {
                // If the run ends or hero/map is gone, stop monitoring
                if (statTracker == null || !statTracker.RunInProgress || hero == null || currentMap == null)
                    yield break;

                // Skip while the hero is dead
                if (heroDead)
                {
                    yield return new WaitForSeconds(stallCheckIntervalSeconds);
                    continue;
                }

                var currentDistance = statTracker.CurrentRunDistance;
                if (Mathf.Abs(currentDistance - lastObservedRunDistance) > stallDistanceEpsilon)
                {
                    lastObservedRunDistance = currentDistance;
                    lastRunDistanceChangeTime = Time.time;
                }
                else if (Time.time - lastRunDistanceChangeTime >= stallTimeoutSeconds)
                {
                    Log("Run stalled; abandoning and restarting.", TELogCategory.Run, this);
                    pendingAutoRestartFromStall = true;
                    AbandonRun();
                    yield break;
                }

                yield return new WaitForSeconds(stallCheckIntervalSeconds);
            }
        }

        private static void DestroyAllEchoes()
        {
#if UNITY_6000_0_OR_NEWER
            var echoes = FindObjectsByType<EchoController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var echoes = Object.FindObjectsOfType<EchoController>(true);
#endif
            foreach (var echo in echoes)
                if (echo != null)
                    Destroy(echo.gameObject);
        }


        private void EnableMildred()
        {
            if (currentMap == null)
                return;
            var unlocked = QuestCompleted(mildredQuestId);
            foreach (var t in currentMap.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.name == "Mildred")
                {
                    t.gameObject.SetActive(unlocked);
                    break;
                }
        }
    }
}