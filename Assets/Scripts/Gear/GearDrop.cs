using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;
using static Blindsided.SaveData.StaticReferences;

namespace Gear
{
    public class GearDrop : MonoBehaviour
    {
        private float duration;
        private GearItem item;
        private float timer;

        [SerializeField] private DropReferences references;
        private Image timerFill;

        private void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (timerFill) timerFill.fillAmount = timer / duration;
                if (timer <= 0f) Dismantle();
            }
        }

        public void Init(GearItem gear)
        {
            item = gear;
            duration = timer = GetDuration(gear.rarity);
            if (!references)
                references = GetComponentInChildren<DropReferences>();

            SetupUI();
        }

        private void SetupUI()
        {
            if (references == null) return;

            if (references.nameText) references.nameText.text = item.name;
            if (references.statsText) references.statsText.text = BuildStatList();
            if (references.equipButton) references.equipButton.onClick.AddListener(Equip);
            if (references.dismantleButton) references.dismantleButton.onClick.AddListener(Dismantle);

            timerFill = references.timerFillBar;
            if (timerFill)
                timerFill.fillAmount = 1f;

            if (references.rarityImage)
                references.rarityImage.OutlineColor = GetRarityColor(item.rarity);
        }

        private string BuildStatList()
        {
            StringBuilder sb = new();
            AppendStat(sb, "Damage", item.damage);
            AppendStat(sb, "AtkSpd", item.attackSpeed);
            AppendStat(sb, "Health", item.health);
            AppendStat(sb, "Defense", item.defense);
            AppendStat(sb, "Move", item.moveSpeed);
            return sb.ToString();
        }

        private void AppendStat(StringBuilder sb, string name, float val)
        {
            if (Mathf.Approximately(val, 0f)) return;
            if (sb.Length > 0) sb.Append("\n");
            var sign = val > 0 ? "+" : "-";
            sb.Append($"{sign}{val} {name}");
        }

        private float GetDuration(GearRarity r)
        {
            return r switch
            {
                GearRarity.Common => 8f,
                GearRarity.Uncommon => 12f,
                GearRarity.Rare => 15f,
                GearRarity.Epic => 18f,
                _ => 20f
            };
        }

        private Color GetRarityColor(GearRarity r)
        {
            return r switch
            {
                GearRarity.Common => Color.white,
                GearRarity.Uncommon => Color.green,
                GearRarity.Rare => Color.blue,
                GearRarity.Epic => new Color(0.6f, 0.2f, 0.8f),
                _ => Color.yellow
            };
        }

        private void Equip()
        {
            var hero = FindObjectOfType<PartyManager>()?.ActiveHero;
            if (hero && hero.TryGetComponent(out BalanceHolder holder))
            {
                var gear = holder.Gear;
                if (gear != null)
                    gear.Equip(item);
            }

            Destroy(gameObject);
        }

        private void Dismantle()
        {
            ItemShards += (int)item.rarity + 1;
            Destroy(gameObject);
        }
    }
}