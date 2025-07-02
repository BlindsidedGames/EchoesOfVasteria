using System.Collections;
using References.UI;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.Tasks;
using TimelessEchoes.Buffs;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using TimelessEchoes.NPC;

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
            StartCoroutine(StartRunRoutine());
        }

        private IEnumerator StartRunRoutine()
        {
            CleanupMap();
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

            StartRun();
        }

        private void ReturnToTavern()
        {
            HideTooltip();
            CleanupMap();
            if (tavernCamera != null)
                tavernCamera.gameObject.SetActive(true);
            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
            npcObjectStateController?.UpdateObjectStates();
        }

        private void CleanupMap()
        {
            BuffManager.Instance?.Pause();
            if (mapCamera != null)
                mapCamera.gameObject.SetActive(false);

            if (hero != null) // deactivate AIPath first
                hero.gameObject.SetActive(false);

            if (currentMap != null)
            {
                Destroy(currentMap); // safe to destroy AstarPath now
                currentMap = null;
            }
        }
    }
}
