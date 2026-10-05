using TimelessEchoes.UI.Toolkit;
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
        private IReadOnlyList<string> lineKeys;
        private int index;
        private bool started;
        private Action onFinished;

        private void Awake()
        {
            if (meetButton != null)
                meetButton.onClick.AddListener(StartConversation);
            if (dialogueObject != null)
                dialogueObject.SetActive(false);
        }

        private void OnEnable() => ToolkitLocalization.Changed += RefreshLocale;
        private void OnDisable() => ToolkitLocalization.Changed -= RefreshLocale;
        private void RefreshLocale()
        {
            if (started) ShowLine();
            else if (meetButtonText != null) meetButtonText.text = ToolkitLocalization.Text("meeting.meet", "Meet");
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
        public void Init(Sprite portrait, List<string> dialogue, Action finished, IReadOnlyList<string> localizedLineKeys = null)
        {
            npcImage.sprite = portrait;
            lines = dialogue;
            lineKeys = localizedLineKeys;
            onFinished = finished;
            if (meetButtonText != null)
                meetButtonText.text = ToolkitLocalization.Text("meeting.meet", "Meet");
        }

        private void StartConversation()
        {
            if (meetButton != null)
            {
                meetButton.onClick.RemoveListener(StartConversation);
                meetButton.onClick.AddListener(Advance);
            }

            started = true;
            index = 0;
            if (dialogueObject != null)
                dialogueObject.SetActive(true);

            ShowLine();
        }

        private void ShowLine()
        {
            if (dialogueText != null && lines != null && index < lines.Count)
                dialogueText.text = lineKeys != null && index < lineKeys.Count && !string.IsNullOrEmpty(lineKeys[index])
                    ? ToolkitLocalization.Text(lineKeys[index], lines[index]) : lines[index];

            if (meetButtonText != null)
            {
                if (index >= lines.Count - 1)
                    meetButtonText.text = ToolkitLocalization.Text("meeting.close", "Close");
                else
                    meetButtonText.text = ToolkitLocalization.Text("meeting.next", "Next");
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
