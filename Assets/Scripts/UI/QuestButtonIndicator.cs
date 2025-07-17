using UnityEngine;
using TimelessEchoes.Quests;
using Blindsided;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Highlights the quest button when quests can be turned in.
    /// </summary>
    public class QuestButtonIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject indicatorImage;
        [SerializeField] private QuestManager questManager;

        private void Awake()
        {
            if (questManager == null)
                questManager = Object.FindFirstObjectByType<QuestManager>();
        }

        private void Update()
        {
            UpdateIndicator();
        }

        private void OnEnable()
        {
            EventHandler.OnQuestHandin += OnQuestHandin;
            EventHandler.OnLoadData += OnLoadData;
            UpdateIndicator();
        }

        private void OnDisable()
        {
            EventHandler.OnQuestHandin -= OnQuestHandin;
            EventHandler.OnLoadData -= OnLoadData;
        }

        private void OnQuestHandin(string _)
        {
            UpdateIndicator();
        }

        private void OnLoadData()
        {
            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            if (indicatorImage == null || questManager == null)
                return;
            indicatorImage.SetActive(questManager.HasQuestsReadyForTurnIn());
        }
    }
}
