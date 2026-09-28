using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Enemies;
using TimelessEchoes.Stats;
using UnityEngine;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed class EnemyStatisticsPresentation
    {
        private readonly List<EnemyData> enemies = AssetCache.GetAll<EnemyData>("").OrderBy(e => e.displayOrder).ThenBy(e => e.enemyName).ToList();
        public List<EnemyData> Ordered(EnemyStatsPanelUI.SortMode mode, EnemyKillTracker tracker)
        {
            var threshold = mode switch { EnemyStatsPanelUI.SortMode.Damage => 1, EnemyStatsPanelUI.SortMode.Health => 2, EnemyStatsPanelUI.SortMode.Defense => 3, EnemyStatsPanelUI.SortMode.AttackRate => 4, EnemyStatsPanelUI.SortMode.MoveSpeed => 5, EnemyStatsPanelUI.SortMode.Vision => 6, _ => 0 };
            bool Known(EnemyData e) => tracker && (mode == EnemyStatsPanelUI.SortMode.Default ? tracker.GetKills(e) > 0 : tracker.GetRevealLevel(e) >= threshold);
            var known = enemies.Where(Known).ToList(); var unknown = enemies.Where(e => !Known(e)).ToList();
            int Tie(EnemyData a, EnemyData b) { var c = a.displayOrder.CompareTo(b.displayOrder); return c != 0 ? c : string.Compare(a.enemyName, b.enemyName, System.StringComparison.Ordinal); }
            if (mode == EnemyStatsPanelUI.SortMode.Default) { known.Sort(Tie); unknown.Sort(Tie); }
            else known.Sort((a, b) => { var c = Value(b, mode).CompareTo(Value(a, mode)); return c != 0 ? c : a.displayOrder.CompareTo(b.displayOrder); });
            known.AddRange(unknown); return known;
        }
        private static float Value(EnemyData e, EnemyStatsPanelUI.SortMode mode) => mode switch
        {
            EnemyStatsPanelUI.SortMode.Damage => e.damage, EnemyStatsPanelUI.SortMode.Health => e.maxHealth,
            EnemyStatsPanelUI.SortMode.Defense => e.defense, EnemyStatsPanelUI.SortMode.AttackRate => e.attackSpeed,
            EnemyStatsPanelUI.SortMode.MoveSpeed => e.moveSpeed, EnemyStatsPanelUI.SortMode.Vision => e.visionRange, _ => 0
        };
        public (string title, string health, string defense, string movement, string kills, Sprite icon, bool showProgress, float progress) Describe(EnemyData enemy, EnemyKillTracker tracker, float distance)
        {
            var kills = tracker ? tracker.GetKills(enemy) : 0; var reveal = tracker ? tracker.GetRevealLevel(enemy) : 0;
            var bonus = (tracker ? tracker.GetDamageMultiplier(enemy) : 1) - 1;
            var level = enemy.GetLevel(Mathf.Max(0, distance - enemy.minX));
            var spawnable = distance >= enemy.minX && (float.IsInfinity(enemy.maxX) || distance <= enemy.maxX);
            string Number(double n) => CalcUtils.FormatNumber(n, true, 400, false);
            string Stat(int required, double value) => reveal >= required ? spawnable ? Number(value) : "-" : "???";
            var health = $"Health: {Stat(2, enemy.GetMaxHealthForLevel(level))}\nDamage: {Stat(1, enemy.GetDamageForLevel(level))}";
            var defense = reveal >= 3 ? spawnable ? ((1 - TimelessEchoes.Combat.ApplyDefense(1, enemy.GetDefenseForLevel(level))) * 100).ToString("0") + "%" : "-" : "???";
            var defenseText = $"Defense: {defense}\nAttack Rate: {Stat(4, enemy.attackSpeed)}";
            var movement = $"Movement: {Stat(5, enemy.moveSpeed)}\nVision: {Stat(6, enemy.visionRange)}";
            var showProgress = reveal < EnemyKillTracker.Thresholds.Length;
            var killsText = Number(kills); float progress = 0;
            if (showProgress) { var next = EnemyKillTracker.Thresholds[reveal]; killsText += " / " + Number(next); progress = Mathf.Clamp01((float)(kills / next)); }
            return (kills > 0 ? enemy.enemyName + " | " + (spawnable ? level.ToString() : "-") : "???", health, defenseText, movement, $"Kills: {killsText}\nBonus Damage: {bonus * 100:0}%", kills > 0 ? enemy.icon : null, showProgress, progress);
        }
    }
}
