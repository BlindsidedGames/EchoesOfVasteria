using System.Collections.Generic;
using Blindsided.SaveData;
using TMPro;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.UI;
using TimelessEchoes.Hero;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Simple conversation task for interacting with an NPC.
    /// Shows text lines sequentially and completes when finished.
    /// </summary>
    public class TalkToNpcTask : BaseTask
    {
        [SerializeField] private string npcId;
        [SerializeField] private Transform chatPoint;
        [SerializeField] private GameObject dialogueObject;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Button continueButton;
        [TextArea]
        [SerializeField] private List<string> lines = new();

        private int index;
        private bool talked;

        public override Transform Target => chatPoint != null ? chatPoint : transform;
        public override bool BlocksMovement => dialogueObject != null && dialogueObject.activeSelf;

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(Advance);
        }

        public override void StartTask()
        {
            talked = false;
            index = 0;
            if (dialogueObject != null)
                dialogueObject.SetActive(false);
        }

        public override void OnArrival(HeroController hero)
        {
            StartConversation();
        }

        private void StartConversation()
        {
            if (dialogueObject != null)
                dialogueObject.SetActive(true);
            ShowLine();
        }

        private void ShowLine()
        {
            if (dialogueText != null && index < lines.Count)
                dialogueText.text = lines[index];
        }

        private void Advance()
        {
            index++;
            if (index >= lines.Count)
            {
                EndConversation();
            }
            else
            {
                ShowLine();
            }
        }

        private void EndConversation()
        {
            if (dialogueObject != null)
                dialogueObject.SetActive(false);
            talked = true;
            if (!string.IsNullOrEmpty(npcId))
            {
                StaticReferences.CompletedNpcTasks.Add(npcId);
                var qm = FindFirstObjectByType<QuestManager>();
                qm?.OnNpcMet(npcId);
            }
            GrantCompletionXP();
        }

        public override bool IsComplete()
        {
            return talked;
        }
    }
}
