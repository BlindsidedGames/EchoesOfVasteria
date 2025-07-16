using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Controls the NPC meeting popup and handles the dialogue sequence.
    /// </summary>
    public class MeetingController : MonoBehaviour
    {
        [SerializeField] private Image npcImage;
        [SerializeField] private Button meetButton;
        [SerializeField] private TMP_Text meetButtonText;
        [SerializeField] private GameObject dialogueObject;
        [SerializeField] private TMP_Text dialogueText;

        private List<string> lines;
        private int index;
        private Action onFinished;

        private void Awake()
        {
            if (meetButton != null)
                meetButton.onClick.AddListener(StartConversation);
            if (dialogueObject != null)
                dialogueObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (meetButton != null)
            {
                meetButton.onClick.RemoveListener(StartConversation);
                meetButton.onClick.RemoveListener(Advance);
            }
        }

        /// <summary>
        /// Initialize the UI with dialogue lines and NPC portrait.
        /// </summary>
        public void Init(Sprite portrait, List<string> dialogue, Action finished)
        {
            npcImage.sprite = portrait;
            lines = dialogue;
            onFinished = finished;
            if (meetButtonText != null)
                meetButtonText.text = "Meet";
        }

        private void StartConversation()
        {
            if (meetButton != null)
            {
                meetButton.onClick.RemoveListener(StartConversation);
                meetButton.onClick.AddListener(Advance);
            }

            index = 0;
            if (dialogueObject != null)
                dialogueObject.SetActive(true);

            ShowLine();
        }

        private void ShowLine()
        {
            if (dialogueText != null && lines != null && index < lines.Count)
                dialogueText.text = lines[index];

            if (meetButtonText != null)
            {
                if (index >= lines.Count - 1)
                    meetButtonText.text = "Close";
                else
                    meetButtonText.text = "Next";
            }
        }

        private void Advance()
        {
            index++;
            if (lines == null || index >= lines.Count)
            {
                onFinished?.Invoke();
                Destroy(gameObject);
            }
            else
            {
                ShowLine();
            }
        }
    }
}
