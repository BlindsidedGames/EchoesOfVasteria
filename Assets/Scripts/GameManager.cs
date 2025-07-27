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
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using static TimelessEchoes.TELogger;
using static Blindsided.Oracle;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes
{
    /// <summary>
    ///     Controls map generation, camera switching and UI visibility.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static MapGenerationConfig CurrentGenerationConfig { get; private set; }
        [Header("Prefabs")] [SerializeField] private GameObject mapPrefab;
        [SerializeField] private GameObject gravestonePrefab;
        [SerializeField] private GameObject reaperPrefab;
        [SerializeField] private Vector3 reaperSpawnOffset = Vector3.zero;

        [Header("UI References")] [SerializeField]
        private Button startRunButton;

        [SerializeField] private Button returnToTavernButton;
        [SerializeField] private TMP_Text returnToTavernText;
        [SerializeField] private TMP_Text retreatBonusText;
        [SerializeField] private Button returnOnDeathButton;
        [SerializeField] private TMP_Text returnOnDeathText;
        [SerializeField] private GameObject autoBuffRoot;
        [SerializeField] private Button autoBuffButton;
        [SerializeField] private TMP_Text autoBuffText;
        [SerializeField] [Min(0f)] private float bonusPercentPerKill = 2f;
        [SerializeField] private GameObject tavernUI;
        [SerializeField] private GameObject mapUI;
        [SerializeField] private RunDropUI runDropUI;
        [SerializeField] private RunCalebUIReferences runCalebUI;
        [SerializeField] private Transform meetingParent;
        [SerializeField] private GameObject deathWindow;
        [SerializeField] private Button deathRunButton;
        [SerializeField] private Button deathReturnButton;
        [SerializeField] private SlicedFilledImage deathTimerImage;
        [SerializeField] private float deathWindowDuration = 20f;
        [SerializeField] public string mildredQuestId;

        public GameObject ReaperPrefab => reaperPrefab;
        public GameObject GravestonePrefab => gravestonePrefab;
        public Vector3 ReaperSpawnOffset => reaperSpawnOffset;
        public Transform MeetingParent => meetingParent;
        public GameObject CurrentMap => currentMap;

        [Header("Map Generation")] [SerializeField]
        private List<MapGenerationConfig> generationConfigs = new();

        [SerializeField] private int currentConfigIndex;

        [Header("Cameras")] [SerializeField] private CinemachineCamera tavernCamera;

        private CloudSpawner cloudSpawner;

        private GameObject currentMap;
        private bool runEndedByDeath;
        private Coroutine deathWindowCoroutine;
        private HeroController hero;
        private CinemachineCamera mapCamera;
        private TaskController taskController;
        private NpcObjectStateController npcObjectStateController;
        private GameplayStatTracker statTracker;
        private bool returnOnDeathQueued;
        private bool retreatQueued;

        private void Awake()
        {
            Instance = this;
            cloudSpawner = CloudSpawner.Instance;
            if (startRunButton != null)
                startRunButton.onClick.AddListener(StartRun);
            if (returnToTavernButton != null)
                returnToTavernButton.onClick.AddListener(OnReturnToTavernButton);
            if (returnOnDeathButton != null)
                returnOnDeathButton.onClick.AddListener(QueueReturnOnDeath);
            if (deathRunButton != null)
                deathRunButton.onClick.AddListener(OnDeathRunButton);
            if (deathReturnButton != null)
                deathReturnButton.onClick.AddListener(OnDeathReturnButton);
            if (autoBuffButton != null)
                autoBuffButton.onClick.AddListener(ToggleAutoBuff);
            npcObjectStateController = NpcObjectStateController.Instance;
            if (npcObjectStateController == null)
                Log("NpcObjectStateController missing", TELogCategory.General, this);
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                Log("GameplayStatTracker missing", TELogCategory.General, this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (startRunButton != null)
                startRunButton.onClick.RemoveListener(StartRun);
            if (returnToTavernButton != null)
                returnToTavernButton.onClick.RemoveListener(OnReturnToTavernButton);
            if (returnOnDeathButton != null)
                returnOnDeathButton.onClick.RemoveListener(QueueReturnOnDeath);
            if (deathRunButton != null)
                deathRunButton.onClick.RemoveListener(OnDeathRunButton);
            if (deathReturnButton != null)
                deathReturnButton.onClick.RemoveListener(OnDeathReturnButton);
            if (autoBuffButton != null)
                autoBuffButton.onClick.RemoveListener(ToggleAutoBuff);
        }

        private void Start()
        {
            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
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
            UpdateAutoBuffUI();
            if (autoBuffRoot != null)
                autoBuffRoot.SetActive(false);
        }

        private void Update()
        {
            UpdateAutoBuffUI();
            if (returnToTavernButton != null)
            {
                var active = hero != null;
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

            if (retreatQueued && hero != null && !hero.InCombat)
            {
                retreatQueued = false;
                StartCoroutine(ReturnToTavernRoutine());
            }

            if (autoBuffRoot != null && statTracker != null)
                autoBuffRoot.SetActive(statTracker.BuffsCast >= 100);
        }

        private void HideTooltip()
        {
            var tooltip = FindFirstObjectByType<TooltipUIReferences>();
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }

        private void ToggleAutoBuff()
        {
            AutoBuff = !AutoBuff;
            UpdateAutoBuffUI();
        }

        private void UpdateAutoBuffUI()
        {
            if (autoBuffText != null)
                autoBuffText.text = AutoBuff ? "Autobuff | On" : "Autobuff | Off";
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

        private void StartRun()
        {
            HideTooltip();
            // Re-enable autobuff for the new run based on the saved preference.
            SetAutoBuffRunDisabled(false);
            returnOnDeathQueued = false;
            retreatQueued = false;
            if (returnOnDeathText != null)
                returnOnDeathText.text = "Return On Death";
            if (returnToTavernText != null)
                returnToTavernText.text = "Return To Town";
#if !DISABLESTEAMWORKS
            RichPresenceManager.Instance?.SetInRun();
#endif
            if (runEndedByDeath && statTracker != null) statTracker.EndRun(true);
            Log("Run starting", TELogCategory.Run, this);
            runEndedByDeath = false;
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
            if (generationConfigs != null && generationConfigs.Count > 0)
            {
                var index = Mathf.Clamp(currentConfigIndex, 0, generationConfigs.Count - 1);
                CurrentGenerationConfig = generationConfigs[index];
            }
            else
            {
                CurrentGenerationConfig = null;
            }

            if (statTracker == null)
            {
                statTracker = GameplayStatTracker.Instance;
                if (statTracker == null)
                    Log("GameplayStatTracker missing", TELogCategory.General, this);
            }

            statTracker?.BeginRun();
            runDropUI?.ResetDrops();
            currentMap = Instantiate(mapPrefab);
            taskController = currentMap.GetComponentInChildren<TaskController>();
            if (taskController == null)
                yield break;

            hero = taskController.hero;
            if (hero != null)
            {
                hero.gameObject.SetActive(true);
                if (hero.GetComponent<EchoActivator>() == null)
                    hero.gameObject.AddComponent<EchoActivator>();
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

                if (!distanceReaper && reaperPrefab != null && currentMap != null)
                {
                    ReaperManager.Spawn(reaperPrefab, hero.gameObject, currentMap.transform, false,
                        () =>
                        {
                            hero.gameObject.SetActive(false);
                            if (gravestonePrefab != null)
                                Instantiate(gravestonePrefab, hero.transform.position, Quaternion.identity,
                                    currentMap.transform);
                        }, reaperSpawnOffset);
                }
                else if (!distanceReaper)
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
            // Temporarily disable autobuff for the remainder of this run.
            SetAutoBuffRunDisabled(true);
            UpdateAutoBuffUI();

            runEndedByDeath = true;
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

        private IEnumerator ReturnToTavernRoutine()
        {
            HideTooltip();
            // Reactivate autobuff if it was temporarily disabled by death.
            SetAutoBuffRunDisabled(false);
            UpdateAutoBuffUI();
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

            if (statTracker != null)
            {
                if (runEndedByDeath)
                    statTracker.EndRun(true);
                else
                    statTracker.EndRun(false);
            }

            BuffManager.Instance?.ClearActiveBuffs();
            yield return StartCoroutine(CleanupMapRoutine());
            if (tavernCamera != null)
            {
                tavernCamera.gameObject.SetActive(true);
                cloudSpawner?.ResetClouds(true);
            }

            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
            if (runCalebUI != null)
                runCalebUI.gameObject.SetActive(false);
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

        private static bool QuestCompleted(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return true;
            if (oracle == null)
                return false;
            oracle.saveData.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            return oracle.saveData.Quests.TryGetValue(questId, out var rec) && rec.Completed;
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