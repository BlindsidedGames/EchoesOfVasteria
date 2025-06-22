/*using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;
using static Blindsided.SaveData.StaticReferences;
using static Blindsided.SaveData.TextColourStrings;

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
                if (timerFill)
                    timerFill.fillAmount = Mathf.Clamp01(timer / duration);
                if (timer <= 0f)
                    Dismantle();
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

        private void OnHeroChanged(GameObject hero)
        {
            if (references && references.statsText)
                references.statsText.text = BuildStatList();
        }

        private string BuildStatList()
        {
            var hero = FindFirstObjectByType<PartyManager>()?.ActiveHero;
            GearItem equipped = null;
            if (hero && hero.TryGetComponent(out BalanceHolder holder))
            {
                var gear = holder.Gear;
                if (gear != null)
                {
                    equipped = item.slot switch
                    {
                        GearSlot.Ring => gear.ring,
                        GearSlot.Necklace => gear.necklace,
                        GearSlot.Brooch => gear.brooch,
                        GearSlot.Pocket => gear.pocket,
                        _ => null
                    };
                }
            }

            StringBuilder sb = new();
            AppendStat(sb, "Damage", item.damage, equipped?.damage ?? 0);
            AppendStat(sb, "AtkSpd", item.attackSpeed, equipped?.attackSpeed ?? 0f);
            AppendStat(sb, "Health", item.health, equipped?.health ?? 0);
            AppendStat(sb, "Defense", item.defense, equipped?.defense ?? 0);
            AppendStat(sb, "Move", item.moveSpeed, equipped?.moveSpeed ?? 0f);
            return sb.ToString();
        }

        private void AppendStat(StringBuilder sb, string name, float newVal, float oldVal)
        {
            var diff = newVal - oldVal;
            if (Mathf.Approximately(diff, 0f)) return;
            if (sb.Length > 0) sb.Append("\n");

            var sign = diff >= 0f ? "+" : "-";
            var colour = diff >= 0f ? ColourGreen : ColourRed;
            var abs = Mathf.Abs(diff);
            var formatted = Mathf.Approximately(abs % 1f, 0f) ? abs.ToString("0") : abs.ToString("0.##");
            sb.Append($"{colour}{sign}{formatted} {name}{EndColour}");
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
            var hero = FindFirstObjectByType<PartyManager>()?.ActiveHero;
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
}*/

