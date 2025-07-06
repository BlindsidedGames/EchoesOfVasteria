using System;
using Blindsided.Utilities;
using References.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Quests
{
    /// <summary>
    ///     Displays a single quest entry and handles its progress bar and turn in button.
    /// </summary>
    public class QuestEntryUI : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public TMP_Text rewardText;
        public Button turnInButton;
        public SlicedFilledImage progressImage;
        public CostResourceUIReferences costSlotPrefab;
        public Transform costParent;

        private Action onTurnIn;

        public void Setup(QuestData data, Action turnIn)
        {
            onTurnIn = turnIn;
            if (nameText != null)
                nameText.text = data != null ? data.questName : string.Empty;
            if (descriptionText != null)
                descriptionText.text = data != null ? data.description : string.Empty;
            if (rewardText != null)
                rewardText.text = data != null ? data.rewardDescription : string.Empty;

            if (turnInButton != null)
            {
                turnInButton.onClick.RemoveAllListeners();
                if (onTurnIn != null)
                    turnInButton.onClick.AddListener(() => onTurnIn());
            }

            if (costParent != null)
            {
                foreach (Transform child in costParent)
                    Destroy(child.gameObject);
                if (data != null && costSlotPrefab != null)
                    foreach (var req in data.requirements)
                    {
                        if (req.type != QuestData.RequirementType.Resource) continue;
                        var slot = Instantiate(costSlotPrefab, costParent);
                        slot.resource = req.resource;
                        if (slot.iconImage != null)
                            slot.iconImage.sprite = req.resource ? req.resource.icon : null;
                        if (slot.countText != null)
                            slot.countText.text = req.amount.ToString();
                    }
            }
        }

        public void SetProgress(float pct)
        {
            pct = Mathf.Clamp01(pct);
            if (progressImage != null)
                progressImage.fillAmount = pct;
            if (turnInButton != null)
                turnInButton.interactable = pct >= 1f;
        }
    }
}