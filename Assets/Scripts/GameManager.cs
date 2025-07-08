using System.Collections;
using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Tasks;
using TimelessEchoes.Buffs;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Stats;
using Pathfinding;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Blindsided.Utilities;
using TimelessEchoes.NPC;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes
{
    /// <summary>
    ///     Controls map generation, camera switching and UI visibility.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [Header("Prefabs")] [SerializeField] private GameObject mapPrefab;
        [SerializeField] private GameObject gravestonePrefab;

        [Header("UI References")] [SerializeField]
        private Button startRunButton;

        [SerializeField] private Button returnToTavernButton;
        [SerializeField] private TMP_Text returnToTavernText;
        [SerializeField] private TMP_Text retreatBonusText;
        [SerializeField] [Min(0f)] private float bonusPercentPerKill = 2f;
        [SerializeField] private GameObject tavernUI;
        [SerializeField] private GameObject mapUI;
        [SerializeField] private RunDropUI runDropUI;
        [SerializeField] private RunCalebUIReferences runCalebUI;
        [SerializeField] private GameObject deathWindow;
        [SerializeField] private Button deathRunButton;
        [SerializeField] private Button deathReturnButton;
        [SerializeField] private SlicedFilledImage deathTimerImage;
        [SerializeField] private float deathWindowDuration = 20f;

        [Header("Cameras")] [SerializeField] private CinemachineCamera tavernCamera;

        private GameObject currentMap;
        private bool runEndedByDeath;
        private Coroutine deathWindowCoroutine;
        private HeroController hero;
        private CinemachineCamera mapCamera;
        private TaskController taskController;
        private NpcObjectStateController npcObjectStateController;
        private GameplayStatTracker statTracker;

        private void Awake()
        {
            Instance = this;
            if (startRunButton != null)
                startRunButton.onClick.AddListener(StartRun);
            if (returnToTavernButton != null)
                returnToTavernButton.onClick.AddListener(ReturnToTavern);
            if (deathRunButton != null)
                deathRunButton.onClick.AddListener(OnDeathRunButton);
            if (deathReturnButton != null)
                deathReturnButton.onClick.AddListener(OnDeathReturnButton);
            npcObjectStateController = NpcObjectStateController.Instance;
            if (npcObjectStateController == null)
                TELogger.Log("NpcObjectStateController missing", TELogCategory.General, this);
            statTracker = GameplayStatTracker.Instance;
            if (statTracker == null)
                TELogger.Log("GameplayStatTracker missing", TELogCategory.General, this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (startRunButton != null)
                startRunButton.onClick.RemoveListener(StartRun);
            if (returnToTavernButton != null)
                returnToTavernButton.onClick.RemoveListener(ReturnToTavern);
            if (deathRunButton != null)
                deathRunButton.onClick.RemoveListener(OnDeathRunButton);
            if (deathReturnButton != null)
                deathReturnButton.onClick.RemoveListener(OnDeathReturnButton);
        }

        private void Start()
        {
            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
            if (deathWindow != null)
                deathWindow.SetActive(false);
            if (returnToTavernText != null)
                returnToTavernText.text = "Return To Town";
            npcObjectStateController?.UpdateObjectStates();
        }

        private void Update()
        {
            if (returnToTavernButton != null)
            {
                bool active = hero != null && !hero.InCombat;
                returnToTavernButton.interactable = active;
                if (returnToTavernText != null)
                    returnToTavernText.text = active ? "Return To Town" : "In Combat...";
                if (retreatBonusText != null)
                {
                    if (active)
                    {
                        int kills = statTracker != null ? statTracker.CurrentRunKills : 0;
                        float percent = kills * bonusPercentPerKill;
                        retreatBonusText.text = $"+{percent:0}% Resources";
                    }
                    else
                    {
                        retreatBonusText.text = "+0% Resources";
                    }
                }
            }
        }

        private void HideTooltip()
        {
            var tooltip = FindFirstObjectByType<TooltipUIReferences>();
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }

        private void StartRun()
        {
            HideTooltip();
            TELogger.Log("Run starting", TELogCategory.Run, this);
            runEndedByDeath = false;
            if (deathWindowCoroutine != null)
            {
                StopCoroutine(deathWindowCoroutine);
                deathWindowCoroutine = null;
            }
            if (deathWindow != null)
                deathWindow.SetActive(false);
            StartCoroutine(StartRunRoutine());
        }

        private IEnumerator StartRunRoutine()
        {
            yield return StartCoroutine(CleanupMapRoutine());
            if (statTracker == null)
            {
                statTracker = GameplayStatTracker.Instance;
                if (statTracker == null)
                    TELogger.Log("GameplayStatTracker missing", TELogCategory.General, this);
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
                var hp = hero.GetComponent<Health>();
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
                mapCamera.gameObject.SetActive(true);

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
            if (hero != null)
            {
                var hp = hero.GetComponent<Health>();
                if (hp != null)
                    hp.OnDeath -= OnHeroDeath;

                // Disable hero so it stops moving during the death window
                var ai = hero.GetComponent<AIPath>();
                if (ai != null)
                    ai.enabled = false;
                hero.gameObject.SetActive(false);
                if (gravestonePrefab != null && currentMap != null)
                    Instantiate(gravestonePrefab, hero.transform.position, Quaternion.identity,
                        currentMap.transform);
            }

            TELogger.Log("Hero death", TELogCategory.Hero, this);

            if (statTracker == null)
            {
                statTracker = GameplayStatTracker.Instance;
                if (statTracker == null)
                    TELogger.Log("GameplayStatTracker missing", TELogCategory.General, this);
            }
            if (statTracker != null)
            {
                statTracker.AddDeath();
                statTracker.EndRun(true);
            }

            runEndedByDeath = true;
            if (runDropUI != null)
                runDropUI.ResetDrops();

            deathWindowCoroutine = StartCoroutine(DeathWindowRoutine());
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
            float t = 0f;
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
            if (statTracker == null)
            {
                statTracker = GameplayStatTracker.Instance;
                if (statTracker == null)
                    TELogger.Log("GameplayStatTracker missing", TELogCategory.General, this);
            }

            if (!runEndedByDeath && runDropUI != null)
            {
                var manager = ResourceManager.Instance;
                if (manager == null)
                {
                    TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
                }
                else
                {
                    var drops = new List<KeyValuePair<Resource, double>>(runDropUI.Amounts);
                    int kills = statTracker != null ? statTracker.CurrentRunKills : 0;
                    float bonusPercent = kills * bonusPercentPerKill * 0.01f;
                    foreach (var pair in drops)
                        manager.Add(pair.Key, pair.Value * bonusPercent, true);
                }
                runDropUI.ResetDrops();
            }
            statTracker?.EndRun(false);
            yield return StartCoroutine(CleanupMapRoutine());
            if (tavernCamera != null)
                tavernCamera.gameObject.SetActive(true);
            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
            if (runCalebUI != null)
                runCalebUI.gameObject.SetActive(false);
            npcObjectStateController?.UpdateObjectStates();
            TELogger.Log("Returned to tavern", TELogCategory.Run, this);
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
    }
}
