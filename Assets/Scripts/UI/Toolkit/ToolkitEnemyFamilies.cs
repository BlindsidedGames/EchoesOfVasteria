using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Enemies;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitStatisticsScreen
    {
        private sealed class EnemyFamilyRow
        {
            public ToolkitStatisticsDefinition.EnemyFamily Family;
            public VisualElement Root, Fields;
            public VisualElement[] Metrics;
            public ToolkitStatRow Details;
            public Label Expansion;
            public readonly List<(VisualElement metric, Image icon, Label count)> Counts = new();
        }
        private readonly Dictionary<EnemyData, EnemyFamilyRow> enemyFamilyRows = new();
        private readonly HashSet<EnemyData> expandedEnemyFamilies = new();
        private ToolkitStatisticsDefinition.EnemyFamily EnemyFamily(EnemyData enemy) =>
            definition.enemyFamilies.FirstOrDefault(f => f != null && f.representative && f.variants.Contains(enemy));
        private EnemyData CatalogueEnemy(EnemyData enemy) => EnemyFamily(enemy)?.representative ?? enemy;
        private IEnumerable<EnemyData> CatalogueVariants(EnemyData enemy) => EnemyFamily(enemy)?.variants ?? new[] { enemy };
        private bool CatalogueKnown(EnemyData enemy) => killTracker && killTracker.GetProgressKills(CatalogueEnemy(enemy)) > 0;
        private EnemyData CatalogueArtwork(EnemyData enemy)
        {
            var family = EnemyFamily(enemy);
            if (family == null) return enemy;
            var colour = Mathf.Clamp((killTracker ? killTracker.GetRevealLevel(family.representative) : 0) - 1, 0, family.variants.Length - 1);
            return family.variants[colour];
        }
        private string CatalogueName(EnemyData enemy)
        {
            var family = EnemyFamily(enemy);
            return family == null ? ToolkitLocalization.Text("enemy." + enemy.name, enemy.enemyName) : ToolkitLocalization.Text(family.localizationKey, family.label);
        }
        private IEnumerable<EnemyData> CatalogueEnemies(IEnumerable<EnemyData> enemies) => enemies.Select(CatalogueEnemy).Distinct();
        private static void NativeEnemyIcon(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.style.width = sprite ? sprite.rect.width * 16 / sprite.pixelsPerUnit : 0;
            image.style.height = sprite ? sprite.rect.height * 16 / sprite.pixelsPerUnit : 0;
            image.style.flexShrink = 0;
        }
        private void BuildEnemyFamily(ToolkitStatisticsDefinition.EnemyFamily family)
        {
            var row = new EnemyFamilyRow { Family = family };
            row.Root = ToolkitGameplay.E(statContent, "enemy-family"); row.Root.name = "enemy-family-" + family.representative.name;
            row.Details = new ToolkitStatRow("enemy-stat-" + family.representative.name, "", 4, theme, definition.row, definition.iconFrame, definition.progressTrack, definition.progressFill);
            row.Root.Add(row.Details.Root); row.Details.Title.style.flexGrow = 1;
            var oldHeader = row.Details.Title.parent;
            var header = ToolkitGameplay.B(oldHeader.parent, "", () =>
            {
                if (!expandedEnemyFamilies.Add(family.representative)) expandedEnemyFamilies.Remove(family.representative);
                RefreshEnemies();
            });
            header.name = "enemy-family-toggle-" + family.representative.name;
            header.AddToClassList("enemy-family-heading"); header.PlaceBehind(oldHeader); header.Add(row.Details.Title); oldHeader.RemoveFromHierarchy();
            row.Expansion = ToolkitGameplay.L(header, "+", "enemy-family-expansion");
            row.Metrics = row.Details.EnemyValues.Select(v => v.parent).ToArray();
            row.Fields = row.Metrics[0].parent.parent; row.Fields.Clear();
            row.Metrics[6].Q<Label>(className: "enemy-stat-name").RemoveFromHierarchy();
            for (var i = 0; i < family.variants.Length; i++)
            {
                var metric = i == 0 ? row.Metrics[6] : ToolkitGameplay.E(row.Fields, "enemy-stat-metric enemy-family-colour");
                var mask = ToolkitGameplay.E(metric, "enemy-stat-symbol enemy-family-kill-icon"); metric.Insert(0, mask);
                var icon = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; mask.Add(icon);
                var count = i == 0 ? row.Details.EnemyValues[6] : ToolkitGameplay.L(metric, "", "enemy-stat-value enemy-family-colour-count");
                row.Counts.Add((metric, icon, count));
                enemyRows.Add(family.variants[i], row.Details);
            }
            enemyFamilyRows.Add(family.representative, row);
        }
        private void RefreshEnemyFamily(EnemyFamilyRow row, float distance)
        {
            var enemy = row.Family.representative;
            var data = enemyPresentation.Describe(enemy, killTracker, distance);
            row.Details.Title.text = CatalogueKnown(enemy) ? data.title.Replace(ToolkitLocalization.Text("enemy." + enemy.name, enemy.enemyName), CatalogueName(enemy)) : "???";
            for (var i = 0; i < row.Details.EnemyValues.Length; i++) row.Details.EnemyValues[i].text = data.values[i];
            row.Details.SetEntryIcon(CatalogueArtwork(enemy).icon, CatalogueKnown(enemy));
            NativeEnemyIcon(row.Details.Icon, CatalogueKnown(enemy) ? CatalogueArtwork(enemy).icon : null);
            row.Details.SetProgress(data.showProgress, data.progress);
            var expanded = expandedEnemyFamilies.Contains(enemy);
            row.Expansion.text = expanded ? "−" : "+";
            // Preserve the existing landscape columns and portrait reading order.
            var order = ToolkitResponsiveLayout.IsPortrait ? new[] { 0, 1, 2, 3, 4, 5, 7, 6 } : new[] { 0, 2, 4, 7, 1, 3, 5, 6 };
            for (var i = 0; i < order.Length; i++)
                if (row.Fields.IndexOf(row.Metrics[order[i]]) != i) row.Fields.Insert(i, row.Metrics[order[i]]);
            for (var i = 0; i < row.Counts.Count; i++)
            {
                var variant = row.Family.variants[i]; var count = killTracker ? killTracker.GetKills(variant) : 0;
                var (metric, image, label) = row.Counts[i];
                if (i > 0)
                {
                    if (row.Fields.IndexOf(metric) != 7 + i) row.Fields.Insert(7 + i, metric);
                    metric.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                }
                NativeEnemyIcon(image, expanded ? variant.icon : CatalogueKnown(enemy) ? CatalogueArtwork(enemy).icon : null);
                image.tintColor = !expanded || count > 0 ? Color.white : Color.black;
                image.style.opacity = !expanded || count > 0 ? 1 : .55f;
                label.text = expanded ? ItemStatisticsPresentation.Number(count) : data.values[6];
                metric.tooltip = expanded ? ToolkitLocalization.Text("enemy." + variant.name, variant.enemyName) : ToolkitLocalization.Text("statistics.family-kills", "Kills: {0}", data.values[6]);
            }
        }
        private VisualElement RevealEnemyEntry(EnemyData enemy)
        {
            var canonical = CatalogueEnemy(enemy);
            if (enemyFamilyRows.TryGetValue(canonical, out var family)) return family.Root;
            return enemyRows.TryGetValue(enemy, out var row) ? row.Root : null;
        }
    }
}
