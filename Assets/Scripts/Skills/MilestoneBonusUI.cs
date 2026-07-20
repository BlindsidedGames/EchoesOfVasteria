using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using References.UI;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.Skills
{
    public class MilestoneBonusUI : MonoBehaviour
    {
        [SerializeField] private Transform entryParent;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private SkillController controller;
        [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Sprite activeToggleSprite;
        [SerializeField] private Sprite inactiveToggleSprite;

        private Sprite overrideActiveToggleSprite;
        private Sprite overrideInactiveToggleSprite;

        private readonly List<EntryBinding> bindings = new();
        private Skill currentSkill;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<SkillController>();
        }

        private void OnEnable()
        {
            if (controller == null)
                controller = FindAnyObjectByType<SkillController>();

            if (controller != null)
                controller.OnMilestoneDataChanged += HandleMilestoneDataChanged;
        }

        private void OnDisable()
        {
            if (controller != null)
                controller.OnMilestoneDataChanged -= HandleMilestoneDataChanged;
        }

        public void SetEntryParent(Transform parent)
        {
            entryParent = parent;
        }

        public void SetToggleSprites(Sprite activeSprite, Sprite inactiveSprite)
        {
            overrideActiveToggleSprite = activeSprite;
            overrideInactiveToggleSprite = inactiveSprite;
            HandleMilestoneDataChanged();
        }

        public void PopulateMilestones(Skill skill)
        {
            currentSkill = skill;

            if (entryPrefab == null)
                return;

            if (entryParent == null)
            {
                var group = GetComponentInChildren<VerticalLayoutGroup>(true);
                if (group != null)
                    entryParent = group.transform;
            }

            if (entryParent == null)
                return;

            UIUtils.ClearChildren(entryParent);
            bindings.Clear();

            if (skill == null)
                return;

            if (controller == null)
                controller = FindAnyObjectByType<SkillController>();

            var progress = controller != null ? controller.GetProgress(skill) : null;
            int currentLevel = progress != null ? progress.Level : 1;

            // Combine milestones and resource unlocks, sorted by required level
            var milestoneEntries = skill.milestones
                .Where(m => m != null)
                .Select(m => (Level: m.UnlockLevel, IsMilestone: true, Milestone: m, Unlock: (ResourceUnlockEntry)null));

            var unlockEntries = (skill.resourceUnlocks ?? new List<ResourceUnlockEntry>())
                .Where(u => u != null && u.task != null)
                .Select(u => (Level: u.requiredLevel, IsMilestone: false, Milestone: (MilestoneDefinition)null, Unlock: u));

            var allEntries = milestoneEntries.Concat(unlockEntries)
                .OrderBy(e => e.Level)
                .ToList();

            foreach (var item in allEntries)
            {
                var entry = Instantiate(entryPrefab, entryParent);
                var refs = entry.GetComponent<MilestoneEntryUIReferences>();
                if (refs == null)
                    continue;

                var rootImage = entry.GetComponent<Image>() ?? refs.ToggleImage;

                if (item.IsMilestone)
                {
                    var binding = new EntryBinding(skill, item.Milestone, refs, rootImage);
                    bindings.Add(binding);
                    ConfigureEntry(binding);
                }
                else
                {
                    ConfigureResourceUnlockEntry(refs, item.Unlock, currentLevel, rootImage);
                }
            }
        }

        private void HandleMilestoneDataChanged()
        {
            if (currentSkill == null)
                return;

            foreach (var binding in bindings)
                ConfigureEntry(binding);
        }

        private void ConfigureEntry(EntryBinding binding)
        {
            if (binding == null || binding.References == null)
                return;

            var refs = binding.References;
            var definition = binding.Definition;
            var skill = binding.Skill;
            var state = controller != null ? controller.GetMilestoneState(skill, definition) : null;
            var progress = controller != null ? controller.GetProgress(skill) : null;
            int currentLevel = progress != null ? progress.Level : 1;
            int tierIndex = state != null ? state.TierIndex : -1;
            bool unlocked = tierIndex >= 0;
            bool active = state != null && state.IsActive;
            bool hasActiveSlots = controller != null && controller.TotalActiveSlots > 0;

            string title = definition.DisplayName;
            int nextTierLevel = definition.GetNextTierLevel(currentLevel);
            if (unlocked)
            {
                int tierDisplay = Mathf.Max(1, tierIndex + 1);
                title = $"{title} {tierDisplay}";
                if (nextTierLevel > 0)
                    title += $" | <size=5>Improves at level {nextTierLevel}</size>";
            }
            else if (definition.UnlockLevel < int.MaxValue)
            {
                title += $" | Unlocks at level {definition.UnlockLevel}";
            }

            refs.NameText?.SetText(title);

            int displayTier = unlocked ? tierIndex : 0;
            if (definition.TierCount > 0)
                displayTier = Mathf.Clamp(displayTier, 0, definition.TierCount - 1);

            string passiveDesc = definition.GetPassiveDescriptionForTier(displayTier, skill?.skillName);
            if (refs.PassiveText != null)
            {
                if (string.IsNullOrEmpty(passiveDesc))
                {
                    refs.PassiveText.text = string.Empty;
                }
                else
                {
                    bool showPassivePrefix = hasActiveSlots && definition.HasActiveEffect;
                    refs.PassiveText.text = showPassivePrefix ? $"Passive: {passiveDesc}" : passiveDesc;
                }
            }

            if (refs.ActiveText != null)
            {
                bool canShowActive = unlocked && definition.HasActiveEffect && hasActiveSlots;
                if (canShowActive)
                {
                    string activeDesc = definition.GetActiveDescriptionForTier(displayTier, skill?.skillName);
                    refs.ActiveText.gameObject.SetActive(true);
                    refs.ActiveText.text = string.IsNullOrEmpty(activeDesc) ? string.Empty : $"Active: {activeDesc}";
                }
                else
                {
                    refs.ActiveText.text = string.Empty;
                    refs.ActiveText.gameObject.SetActive(false);
                }
            }
            if (refs.SetText != null)
            {
                if (definition.Set != MilestoneSet.None)
                {
                    var setDef = controller?.GetSetDefinition(definition.Set);
                    string setName = setDef != null ? setDef.DisplayName : definition.Set.ToString();
                    refs.SetText.text = $"Set: {setName}";
                    if (refs.SetIcon != null)
                    {
                        var sprite = definition.SetIcon != null ? definition.SetIcon : setDef?.Icon;
                        refs.SetIcon.sprite = sprite;
                        refs.SetIcon.enabled = sprite != null;
                    }
                }
                else
                {
                    refs.SetText.text = string.Empty;
                    if (refs.SetIcon != null)
                        refs.SetIcon.enabled = false;
                }
            }

            if (refs.ToggleButton != null)
            {
                refs.ToggleButton.onClick.RemoveAllListeners();
                bool canToggle = unlocked && definition.CanActivate && controller != null && hasActiveSlots;
                refs.ToggleButton.gameObject.SetActive(hasActiveSlots && unlocked && definition.CanActivate);
                refs.ToggleButton.interactable = canToggle;
                if (canToggle)
                    refs.ToggleButton.onClick.AddListener(() => HandleToggle(binding));
            }

            if (refs.ToggleImage != null)
            {
                bool canDisplayToggle = hasActiveSlots && unlocked && definition.CanActivate;
                if (!canDisplayToggle)
                {
                    refs.ToggleImage.sprite = null;
                    refs.ToggleImage.color = unlocked && hasActiveSlots ? unlockedColor : lockedColor;
                    refs.ToggleImage.gameObject.SetActive(false);
                }
                else
                {
                    Sprite activeSprite = overrideActiveToggleSprite ?? activeToggleSprite;
                    Sprite inactiveSprite = overrideInactiveToggleSprite ?? inactiveToggleSprite;
                    Sprite selectedSprite = active ? activeSprite : inactiveSprite;

                    if (selectedSprite != null)
                    {
                        refs.ToggleImage.sprite = selectedSprite;
                        refs.ToggleImage.color = Color.white;
                    }
                    else
                    {
                        refs.ToggleImage.sprite = null;
                        refs.ToggleImage.color = active ? unlockedColor : lockedColor;
                    }

                    refs.ToggleImage.gameObject.SetActive(true);
                }
            }
            if (binding.RootImage != null)
                binding.RootImage.color = unlocked ? unlockedColor : lockedColor;
        }

        private void ConfigureResourceUnlockEntry(MilestoneEntryUIReferences refs,
            ResourceUnlockEntry unlock, int currentLevel, Image rootImage)
        {
            bool isUnlocked = currentLevel >= unlock.requiredLevel;

            // Show task icon only when unlocked (match Tasks panel behavior)
            if (refs.TaskImageObject != null)
                refs.TaskImageObject.SetActive(isUnlocked);
            if (refs.TaskImage != null && isUnlocked)
            {
                var iconSprite = unlock.useOverrideIcon ? unlock.overrideIcon : unlock.task?.taskIcon;
                refs.TaskImage.sprite = iconSprite;
                refs.TaskImage.color = Color.white;
                if (iconSprite != null)
                    refs.TaskImage.SetNativeSize();
            }

            // Title - show "???" when locked (match Tasks panel behavior)
            string name = isUnlocked ? (unlock.task?.taskName ?? "Resource") : "???";
            refs.NameText?.SetText(isUnlocked
                ? $"{name} | <size=80%>Unlocked at level {unlock.requiredLevel}</size>"
                : $"{name} | <size=80%>Unlocks at level {unlock.requiredLevel}</size>");

            // Description only when unlocked
            if (refs.PassiveText != null)
            {
                refs.PassiveText.text = isUnlocked && !string.IsNullOrEmpty(unlock.description)
                    ? unlock.description
                    : string.Empty;
            }

            // Hide toggle and other milestone-specific elements
            if (refs.ActiveText != null) refs.ActiveText.gameObject.SetActive(false);
            if (refs.ToggleButton != null) refs.ToggleButton.gameObject.SetActive(false);
            if (refs.ToggleImage != null) refs.ToggleImage.gameObject.SetActive(false);
            if (refs.SetText != null) refs.SetText.text = string.Empty;
            if (refs.SetIcon != null) refs.SetIcon.enabled = false;

            // Grey out the whole entry if locked (same as milestones)
            if (rootImage != null)
                rootImage.color = isUnlocked ? unlockedColor : lockedColor;
        }

        private void HandleToggle(EntryBinding binding)
        {
            if (controller == null || binding == null)
                return;

            var state = controller.GetMilestoneState(binding.Skill, binding.Definition);
            bool current = state != null && state.IsActive;
            bool target = !current;
            controller.TrySetMilestoneActive(binding.Skill, binding.Definition, target);
            ConfigureEntry(binding);
        }

        private class EntryBinding
        {
            public EntryBinding(Skill skill, MilestoneDefinition definition, MilestoneEntryUIReferences references, Image rootImage)
            {
                Skill = skill;
                Definition = definition;
                References = references;
                RootImage = rootImage;
            }

            public Skill Skill { get; }
            public MilestoneDefinition Definition { get; }
            public MilestoneEntryUIReferences References { get; }
            public Image RootImage { get; }
        }
    }
}


