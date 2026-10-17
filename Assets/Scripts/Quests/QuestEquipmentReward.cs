using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Gear;
using UnityEngine;

namespace TimelessEchoes.Quests
{
    [CreateAssetMenu(menuName = "SO/Quest Equipment Reward")]
    public sealed class QuestEquipmentReward : ScriptableObject
    {
        public string slot = "Boots";
        public RaritySO rarity;
        public CoreSO core;
        public Sprite icon;
        public string displayName;
        public List<GearAffix> affixes = new();

        public bool IsValid => !string.IsNullOrWhiteSpace(slot) && rarity && core && affixes != null
                               && affixes.Count > 0 && affixes.All(a => a != null && a.stat
                                   && !float.IsNaN(a.value) && !float.IsInfinity(a.value));

        private static QuestEquipmentReward[] appearances;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAppearances() => appearances = null;

        // Fixed rewards can also be recognized in older saves that predate reward artwork.
        public static Sprite GetIcon(GearItem item) => Find(item)?.icon;

        public static string GetDisplayName(GearItem item)
        {
            var reward = Find(item);
            return reward && !string.IsNullOrWhiteSpace(reward.displayName)
                ? TimelessEchoes.UI.Toolkit.ToolkitLocalization.Text("equipment-reward." + reward.name + ".name", reward.displayName)
                : null;
        }

        private static QuestEquipmentReward Find(GearItem item)
        {
            if (item == null) return null;
            appearances ??= Resources.LoadAll<QuestEquipmentReward>("Quest Rewards");
            foreach (var reward in appearances)
                if (reward && item.slot == reward.slot && item.rarity == reward.rarity &&
                    item.affixes != null && item.affixes.Count == reward.affixes.Count &&
                    reward.affixes.All(a => a?.stat && item.affixes.Any(b => b?.stat == a.stat && Mathf.Abs(b.value - a.value) < .0001f)))
                    return reward;
            return null;
        }

        public GearItem CreateItem()
        {
            if (!IsValid) return null;
            var item = new GearItem { slot = slot, rarity = rarity, core = core };
            foreach (var affix in affixes)
                item.affixes.Add(new GearAffix { stat = affix.stat, value = affix.value });
            return item;
        }
    }
}
