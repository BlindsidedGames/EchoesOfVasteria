using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.SaveData.StaticReferences;

namespace Gear
{
    public class GearDrop : MonoBehaviour
    {
        private float duration;
        private GearItem item;
        private float timer;

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
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 3f);

            // Icon (placeholder)
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(canvasGO.transform);
            var icon = iconGO.AddComponent<Image>();
            icon.rectTransform.anchoredPosition = new Vector2(0, 1f);

            // Name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(canvasGO.transform);
            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = item.name;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.rectTransform.anchoredPosition = new Vector2(0, 0.5f);

            // Stats
            var statsGO = new GameObject("Stats");
            statsGO.transform.SetParent(canvasGO.transform);
            var statsText = statsGO.AddComponent<TextMeshProUGUI>();
            statsText.text = BuildStatList();
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.rectTransform.anchoredPosition = new Vector2(0, 0f);

            // Equip button
            var equipGO = new GameObject("EquipButton");
            equipGO.transform.SetParent(canvasGO.transform);
            var equipBtn = equipGO.AddComponent<Button>();
            var equipTxt = equipGO.AddComponent<TextMeshProUGUI>();
            equipTxt.text = "Equip";
            equipTxt.alignment = TextAlignmentOptions.Center;
            equipBtn.targetGraphic = equipTxt;
            equipBtn.onClick.AddListener(Equip);
            equipTxt.rectTransform.anchoredPosition = new Vector2(-0.5f, -0.8f);

            // Dismantle button
            var disGO = new GameObject("DismantleButton");
            disGO.transform.SetParent(canvasGO.transform);
            var disBtn = disGO.AddComponent<Button>();
            var disTxt = disGO.AddComponent<TextMeshProUGUI>();
            disTxt.text = "Break";
            disTxt.alignment = TextAlignmentOptions.Center;
            disBtn.targetGraphic = disTxt;
            disBtn.onClick.AddListener(Dismantle);
            disTxt.rectTransform.anchoredPosition = new Vector2(0.5f, -0.8f);

            // Timer bar
            var barGO = new GameObject("Timer");
            barGO.transform.SetParent(canvasGO.transform);
            timerFill = barGO.AddComponent<Image>();
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.rectTransform.sizeDelta = new Vector2(1.5f, 0.2f);
            timerFill.rectTransform.anchoredPosition = new Vector2(0, -1.2f);
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