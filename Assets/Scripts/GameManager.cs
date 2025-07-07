using System.Collections;
using System.Collections.Generic;
using References.UI;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Tasks;
using TimelessEchoes.Buffs;
using TimelessEchoes.Upgrades;
using Pathfinding;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
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
        [Header("Prefabs")] [SerializeField] private GameObject mapPrefab;

        [Header("UI References")] [SerializeField]
        private Button startRunButton;

        [SerializeField] private Button returnToTavernButton;
        [SerializeField] private GameObject tavernUI;
        [SerializeField] private GameObject mapUI;
        [SerializeField] private RunDropUI runDropUI;
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
        [SerializeField] private NpcObjectStateController npcObjectStateController;

        private void Awake()
        {
            if (startRunButton != null)
                startRunButton.onClick.AddListener(StartRun);
            if (returnToTavernButton != null)
                returnToTavernButton.onClick.AddListener(ReturnToTavern);
            if (deathRunButton != null)
                deathRunButton.onClick.AddListener(OnDeathRunButton);
            if (deathReturnButton != null)
                deathReturnButton.onClick.AddListener(OnDeathReturnButton);
            if (npcObjectStateController == null)
                npcObjectStateController = FindFirstObjectByType<NpcObjectStateController>();
        }

        private void OnDestroy()
        {
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
            npcObjectStateController?.UpdateObjectStates();
        }

        private void Update()
        {
            if (returnToTavernButton != null)
                returnToTavernButton.interactable = hero != null && !hero.InCombat;
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
            var tracker = FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
            tracker?.BeginRun();
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
            }

            TELogger.Log("Hero death", TELogCategory.Hero, this);

            var tracker = FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
            if (tracker != null)
            {
                tracker.AddDeath();
                tracker.EndRun(true);
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
            var tracker = FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
            tracker?.EndRun(false);

            if (!runEndedByDeath && runDropUI != null)
            {
                var manager = FindFirstObjectByType<ResourceManager>();
                if (manager != null)
                {
                    // copy to list so enumeration isn't affected by OnResourceAdded modifying the dictionary
                    var drops = new List<KeyValuePair<Resource, double>>(runDropUI.Amounts);
                    foreach (var pair in drops)
                        manager.Add(pair.Key, pair.Value * 0.5f);
                }
                runDropUI.ResetDrops();
            }
            yield return StartCoroutine(CleanupMapRoutine());
            if (tavernCamera != null)
                tavernCamera.gameObject.SetActive(true);
            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
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
