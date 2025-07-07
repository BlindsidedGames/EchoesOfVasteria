using System.Collections;
using References.UI;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Tasks;
using TimelessEchoes.Buffs;
using Pathfinding;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
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

        [Header("Cameras")] [SerializeField] private CinemachineCamera tavernCamera;

        private GameObject currentMap;
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
            if (npcObjectStateController == null)
                npcObjectStateController = FindFirstObjectByType<NpcObjectStateController>();
        }

        private void Start()
        {
            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
            npcObjectStateController?.UpdateObjectStates();
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
            StartCoroutine(StartRunRoutine());
        }

        private IEnumerator StartRunRoutine()
        {
            yield return StartCoroutine(CleanupMapRoutine());
            var tracker = FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
            tracker?.BeginRun();
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
            }

            TELogger.Log("Hero death", TELogCategory.Hero, this);

            var tracker = FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
            if (tracker != null)
            {
                tracker.AddDeath();
                tracker.EndRun(true);
            }

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
