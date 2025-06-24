using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using TimelessEchoes.Tasks;
using TimelessEchoes.Hero;
using TimelessEchoes.MapGeneration;

namespace TimelessEchoes
{
    /// <summary>
    /// Controls map generation, camera switching and UI visibility.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject mapPrefab;

        [Header("UI References")]
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button returnToTavernButton;
        [SerializeField] private GameObject tavernUI;
        [SerializeField] private GameObject mapUI;

        [Header("Cameras")]
        [SerializeField] private CinemachineCamera tavernCamera;

        private GameObject currentMap;
        private TaskController taskController;
        private HeroController hero;
        private CinemachineCamera mapCamera;

        private void Awake()
        {
            if (startRunButton != null)
                startRunButton.onClick.AddListener(StartRun);
            if (returnToTavernButton != null)
                returnToTavernButton.onClick.AddListener(ReturnToTavern);
        }

        private void StartRun()
        {
            CleanupMap();
            currentMap = Instantiate(mapPrefab);
            taskController = currentMap.GetComponentInChildren<TaskController>();
            if (taskController == null)
                return;

            var chunk = taskController.GetComponent<TilemapChunkGenerator>();
            chunk?.Generate();
            var taskGen = taskController.GetComponent<ProceduralTaskGenerator>();
            taskGen?.Generate();

            hero = taskController.GetComponent<HeroController>();
            if (hero != null)
            {
                hero.gameObject.SetActive(true);
                var hp = hero.GetComponent<Enemies.Health>();
                if (hp != null)
                {
                    hp.Init((int)hp.MaxHealth);
                    hp.OnDeath += OnHeroDeath;
                }
            }

            mapCamera = taskController.MapCamera;
            if (tavernCamera != null)
                tavernCamera.gameObject.SetActive(false);
            if (mapCamera != null)
                mapCamera.gameObject.SetActive(true);

            tavernUI?.SetActive(false);
            mapUI?.SetActive(true);
        }

        private void OnHeroDeath()
        {
            if (hero != null)
            {
                var hp = hero.GetComponent<Enemies.Health>();
                if (hp != null)
                    hp.OnDeath -= OnHeroDeath;
            }
            StartRun();
        }

        private void ReturnToTavern()
        {
            CleanupMap();
            if (tavernCamera != null)
                tavernCamera.gameObject.SetActive(true);
            tavernUI?.SetActive(true);
            mapUI?.SetActive(false);
        }

        private void CleanupMap()
        {
            if (mapCamera != null)
                mapCamera.gameObject.SetActive(false);
            if (currentMap != null)
            {
                Destroy(currentMap);
                currentMap = null;
            }
            if (hero != null)
                hero.gameObject.SetActive(false);
        }
    }
}
