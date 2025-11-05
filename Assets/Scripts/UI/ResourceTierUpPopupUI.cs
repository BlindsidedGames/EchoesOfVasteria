using System.Collections;
using System.Collections.Generic;
using System.Text;
using Blindsided.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.UI
{
    public class ResourceTierUpPopupUI : MonoBehaviour
    {
        [Header("Popup Elements")]
        [SerializeField] private GameObject popupObject;
        [SerializeField] private Image resourceIconImage;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private SlicedFilledImage countdownBar;
        [SerializeField] private Image tierBackgroundImage;

        [Header("Config")]
        [SerializeField] private float displaySeconds = 5f;
        [SerializeField] private List<Sprite> tierBackgroundSprites;

        private readonly Dictionary<Skill, List<string>> pendingMilestoneMessages = new();
        private readonly Dictionary<Skill, Dictionary<MilestoneDefinition, int>> milestoneTierCache = new();

        private ResourceManager resourceManager;
        private SkillController skillController;
        private Coroutine countdownRoutine;

        private void Awake()
        {
            CacheManagers();

            if (popupObject != null)
                popupObject.SetActive(false);
        }

        private void OnEnable()
        {
            CacheManagers();

            if (resourceManager != null)
                resourceManager.OnResourceTierUpgraded += OnResourceTierUpgraded;

            if (skillController != null)
            {
                skillController.OnLevelUp += OnSkillLevelUp;
                skillController.OnMilestoneUnlocked += OnMilestoneUnlocked;
                skillController.OnMilestoneTierChanged += OnMilestoneTierChanged;
            }
        }

        private void OnDisable()
        {
            if (resourceManager != null)
                resourceManager.OnResourceTierUpgraded -= OnResourceTierUpgraded;

            if (skillController != null)
            {
                skillController.OnLevelUp -= OnSkillLevelUp;
                skillController.OnMilestoneUnlocked -= OnMilestoneUnlocked;
                skillController.OnMilestoneTierChanged -= OnMilestoneTierChanged;
            }

            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }
        }

        private void OnDestroy()
        {
            pendingMilestoneMessages.Clear();
            milestoneTierCache.Clear();
        }

        private void CacheManagers()
        {
            resourceManager ??= ResourceManager.Instance;
            skillController ??= SkillController.Instance;
        }

        private void OnResourceTierUpgraded(Resource resource, int newTier)
        {
            if (resource == null || newTier <= 1)
                return;

            var oldTier = Mathf.Max(1, newTier - 1);
            var backgroundSprite = GetBackgroundSprite(newTier - 1);
            var text = $"Tier {oldTier}<sprite=194>{newTier}";

            ShowNotification(new NotificationData(resource.icon, text, backgroundSprite));
        }

        private void OnSkillLevelUp(Skill skill, int newLevel)
        {
            if (skill == null || newLevel <= 1)
                return;

            var oldLevel = Mathf.Max(1, newLevel - 1);
            StartCoroutine(HandleSkillLevelUp(skill, oldLevel, newLevel));
        }

        private IEnumerator HandleSkillLevelUp(Skill skill, int oldLevel, int newLevel)
        {
            yield return null; // wait a frame so milestone events populate

            var name = string.IsNullOrWhiteSpace(skill.skillName) ? "Skill" : skill.skillName;
            var backgroundSprite = GetBackgroundSprite(0);

            var builder = new StringBuilder();
            builder.Append($"{name} Lv {oldLevel}<sprite=194>{newLevel}");

            if (pendingMilestoneMessages.TryGetValue(skill, out var messages) && messages.Count > 0)
            {
                foreach (var message in messages)
                {
                    builder.Append('\n');
                    builder.Append(message);
                }

                messages.Clear();
            }

            ShowNotification(new NotificationData(skill.skillIcon, builder.ToString(), backgroundSprite));
        }

        private void OnMilestoneUnlocked(Skill skill, MilestoneDefinition milestone)
        {
            if (skill == null || milestone == null)
                return;

            var messages = GetOrCreateMessages(skill);
            messages.Add($"<size=8>{GetMilestoneName(milestone)} Unlocked!</size>");

            UpdateMilestoneTierCache(skill, milestone);
        }

        private void OnMilestoneTierChanged(Skill skill, MilestoneDefinition milestone, int newTierIndex)
        {
            if (skill == null || milestone == null)
                return;

            var tierMap = GetOrCreateTierMap(skill);
            tierMap.TryGetValue(milestone, out var previousTierIndex);
            tierMap[milestone] = newTierIndex;

            var oldTierDisplay = previousTierIndex >= 0 ? previousTierIndex + 1 : Mathf.Max(1, newTierIndex);
            var newTierDisplay = Mathf.Max(oldTierDisplay, newTierIndex + 1);

            var messages = GetOrCreateMessages(skill);
            messages.Add($"<size=8>{GetMilestoneName(milestone)} {oldTierDisplay}<sprite=194>{newTierDisplay}</size>");
        }

        private List<string> GetOrCreateMessages(Skill skill)
        {
            if (!pendingMilestoneMessages.TryGetValue(skill, out var list))
            {
                list = new List<string>();
                pendingMilestoneMessages[skill] = list;
            }

            return list;
        }

        private Dictionary<MilestoneDefinition, int> GetOrCreateTierMap(Skill skill)
        {
            if (!milestoneTierCache.TryGetValue(skill, out var map))
            {
                map = new Dictionary<MilestoneDefinition, int>();
                milestoneTierCache[skill] = map;
            }

            return map;
        }

        private void UpdateMilestoneTierCache(Skill skill, MilestoneDefinition milestone)
        {
            if (skillController == null)
                return;

            var state = skillController.GetMilestoneState(skill, milestone);
            if (state == null)
                return;

            var map = GetOrCreateTierMap(skill);
            map[milestone] = state.TierIndex;
        }

        private string GetMilestoneName(MilestoneDefinition milestone)
        {
            return string.IsNullOrWhiteSpace(milestone.DisplayName) ? milestone.name : milestone.DisplayName;
        }

        private void ShowNotification(NotificationData data)
        {
            if (resourceIconImage != null)
            {
                resourceIconImage.sprite = data.Icon;
                resourceIconImage.enabled = data.Icon != null;
            }

            if (tierText != null)
                tierText.text = data.Text;

            if (tierBackgroundImage != null)
            {
                tierBackgroundImage.sprite = data.Background;
                tierBackgroundImage.enabled = data.Background != null;
            }

            if (popupObject != null)
                popupObject.SetActive(true);

            if (countdownRoutine != null)
                StopCoroutine(countdownRoutine);

            countdownRoutine = StartCoroutine(RunCountdown());
        }

        private Sprite GetBackgroundSprite(int index)
        {
            if (tierBackgroundSprites == null || tierBackgroundSprites.Count == 0)
                return null;

            var clampedIndex = Mathf.Clamp(index, 0, tierBackgroundSprites.Count - 1);
            return tierBackgroundSprites[clampedIndex];
        }

        private IEnumerator RunCountdown()
        {
            var seconds = Mathf.Max(0.01f, displaySeconds);
            var timeLeft = seconds;

            if (countdownBar != null)
                countdownBar.fillAmount = 1f;

            while (timeLeft > 0f)
            {
                timeLeft -= Time.unscaledDeltaTime;

                if (countdownBar != null)
                    countdownBar.fillAmount = Mathf.Clamp01(timeLeft / seconds);

                yield return null;
            }

            if (popupObject != null)
                popupObject.SetActive(false);

            countdownRoutine = null;
        }

        private readonly struct NotificationData
        {
            public NotificationData(Sprite icon, string text, Sprite background)
            {
                Icon = icon;
                Text = text;
                Background = background;
            }

            public Sprite Icon { get; }
            public string Text { get; }
            public Sprite Background { get; }
        }
    }
}
