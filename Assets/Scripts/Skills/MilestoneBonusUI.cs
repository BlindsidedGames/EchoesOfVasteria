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
                controller = FindFirstObjectByType<SkillController>();
        }

        private void OnEnable()
        {
            if (controller == null)
                controller = FindFirstObjectByType<SkillController>();

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
                controller = FindFirstObjectByType<SkillController>();

            var ordered = skill.milestones.Where(m => m != null).OrderBy(m => m.UnlockLevel);

            foreach (var definition in ordered)
            {
                var entry = Instantiate(entryPrefab, entryParent);
                var refs = entry.GetComponent<MilestoneEntryUIReferences>();
                if (refs == null)
                    continue;

                var binding = new EntryBinding(skill, definition, refs, entry.GetComponent<Image>() ?? refs.ToggleImage);
                bindings.Add(binding);
                ConfigureEntry(binding);
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
            var unlockEffect = GetTaskUnlockEffect(definition);
            ConfigureTaskIcon(refs, unlockEffect);

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

        private static MilestoneTaskUnlockEffectDefinition GetTaskUnlockEffect(MilestoneDefinition definition)
        {
            if (definition == null)
                return null;

            if (definition.PassiveEffect is MilestoneTaskUnlockEffectDefinition passive && passive != null)
                return passive;

            if (definition.ActiveEffect is MilestoneTaskUnlockEffectDefinition active && active != null)
                return active;

            return null;
        }

        private static void ConfigureTaskIcon(MilestoneEntryUIReferences refs, MilestoneTaskUnlockEffectDefinition effect)
        {
            if (refs == null)
                return;

            var iconObject = refs.TaskImageObject;
            var iconImage = refs.TaskImage;
            if (iconObject == null || iconImage == null)
                return;

            if (effect == null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                iconObject.SetActive(false);
                return;
            }

            Sprite sprite = null;
            foreach (var task in effect.EnumerateTasks())
            {
                if (task?.taskIcon == null)
                    continue;
                sprite = task.taskIcon;
                break;
            }

            if (sprite == null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                iconObject.SetActive(false);
                return;
            }

            iconImage.sprite = sprite;
            iconImage.enabled = true;
            iconImage.SetNativeSize();
            iconObject.SetActive(true);
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


