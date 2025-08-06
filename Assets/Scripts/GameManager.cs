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
using static TimelessEchoes.Quests.QuestUtils;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;
using static Blindsided.Oracle;
using static Blindsided.SaveData.StaticReferences;
using Sirenix.OdinInspector;

namespace TimelessEchoes
{
    /// <summary>
    ///     Controls map generation, camera switching and UI visibility.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
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

        [TitleGroup("Map Generation")]
        [SerializeField] private List<MapGenerationButton> generationButtons = new();

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
        private GameplayStatTracker statTracker;
        private System.Action<bool> runEndedAction;
        private bool returnOnDeathQueued;
        private bool retreatQueued;
        // Track whether the run resource tracker should reset on the next run
        private bool resetRunResourceTracker = true;
        private readonly Dictionary<Button, UnityEngine.Events.UnityAction> _buttonActions = new();

        [SerializeField] private float statsUpdateInterval = 0.1f;
        private float nextStatsUpdateTime;

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
                    entry.statsUI.distanceLongestTasksText.text = $"Steps Taken: {dist}\nLongest Trek: {longest}\nTasks Completed: {tasks}\nResources Gathered: {resources}";
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

        private void Awake()
        {
            Instance = this;
            cloudSpawner = CloudSpawner.Instance;
            foreach (var entry in generationButtons)
            {
                if (entry?.button == null) continue;
                var cfg = entry.config;
                UnityEngine.Events.UnityAction action = () => StartRun(cfg);
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
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                Log("GameplayStatTracker missing", TELogCategory.General, this);
            else
            {
                runEndedAction = _ => UpdateGenerationButtonStats();
                statTracker.OnRunEnded += runEndedAction;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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
        }

        private void OnHeroDeath()
        {
            DestroyAllEchoes();
            heroDead = true;
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

            if (statTracker != null) statTracker.AddDeath();

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
        }

        private void OnDeathRunButton()
        {
            if (deathWindowCoroutine != null)
                StopCoroutine(deathWindowCoroutine);
            if (deathWindow != null)
                deathWindow.SetActive(false);
            StartRun();
        }

        private void OnDeathReturnButton()
        {
            if (deathWindowCoroutine != null)
                StopCoroutine(deathWindowCoroutine);
            if (deathWindow != null)
                deathWindow.SetActive(false);
            StartCoroutine(ReturnToTavernRoutine());
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
            StartRun();
        }

        private void ReturnToTavern()
        {
            StartCoroutine(ReturnToTavernRoutine());
        }

        private IEnumerator ReturnToTavernRoutine(bool abandon = false)
        {
            HideTooltip();
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
#if !DISABLESTEAMWORKS
            RichPresenceManager.Instance?.SetInTown();
#endif
            Log("Returned to tavern", TELogCategory.Run, this);
        }

        private void CleanupMap()
        {
            StartCoroutine(CleanupMapRoutine());
        }

        private IEnumerator CleanupMapRoutine()
        {
            BuffManager.Instance?.Pause();
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