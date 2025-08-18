using System.Collections.Generic;
using UnityEngine;
using static Blindsided.EventHandler;

namespace TimelessEchoes.Gear
{
    public class MeetIvanReward : MonoBehaviour
    {
        private void OnEnable()
        {
            OnQuestHandin += OnQuestHandinHandler;
        }

        private void OnDisable()
        {
            OnQuestHandin -= OnQuestHandinHandler;
        }

        private void OnQuestHandinHandler(string questId)
        {
            if (questId != "Meeting Ivan")
                return;

            var rarity = Resources.Load<RaritySO>("Gear/Rarity Assets/Eznorb");
            var core = Resources.Load<CoreSO>("Gear/Cores/Eznorb");
            var damage = Resources.Load<StatDefSO>("Gear/StatDef/Damage");

            if (rarity == null || core == null || damage == null)
                return;

            var item = new GearItem
            {
                slot = "Weapon",
                rarity = rarity,
                core = core
            };
            item.affixes.Add(new GearAffix { stat = damage, value = 1f });

            var controller = EquipmentController.Instance ?? FindFirstObjectByType<EquipmentController>();
            controller?.Equip(item);
        }
    }
}
