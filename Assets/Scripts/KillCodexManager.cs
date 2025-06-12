using System.Collections.Generic;
using UnityEngine;
using Blindsided;
using Blindsided.SaveData;

public static class KillCodexManager
{
    private static Dictionary<string, Dictionary<string, int>> heroKills = new();
    private static Dictionary<string, int> globalKills = new();
    private static GlobalCodexBuffData buffData;

    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
        Load();
        EventHandler.OnSaveData += Save;
        EventHandler.OnLoadData += Load;
    }

    public static void SetBuffData(GlobalCodexBuffData data) => buffData = data;

    public static void RegisterEnemy(Health enemy)
    {
        if (enemy == null) return;
        enemy.OnDeath += () => OnEnemyDeath(enemy);
    }

    private static void OnEnemyDeath(Health enemy)
    {
        if (enemy == null) return;
        var info = enemy.GetComponent<EnemyCodexInfo>();
        if (info == null) return;

        string id = info.enemyId;
        if (!globalKills.ContainsKey(id)) globalKills[id] = 0;
        globalKills[id]++;

        var heroGo = enemy.LastHeroAttacker;
        if (heroGo)
        {
            string hero = heroGo.name;
            if (!heroKills.TryGetValue(hero, out var dict))
            {
                dict = new Dictionary<string, int>();
                heroKills[hero] = dict;
            }
            if (!dict.ContainsKey(id)) dict[id] = 0;
            dict[id]++;

            CheckHeroThreshold(hero);
        }

        CheckGlobalThreshold();
    }

    private static int GetTotalGlobalKills()
    {
        int total = 0;
        foreach (var v in globalKills.Values)
            total += v;
        return total;
    }

    private static int GetTotalKillsForHero(string hero)
    {
        if (!heroKills.TryGetValue(hero, out var dict)) return 0;
        int total = 0;
        foreach (var v in dict.Values)
            total += v;
        return total;
    }

    private static void CheckGlobalThreshold()
    {
        if (buffData == null) return;
        int total = GetTotalGlobalKills();
        foreach (var t in buffData.globalThresholds)
        {
            if (t.killsRequired == total)
                KillCodexBuffs.ApplyGlobalBuff(t.damageBonus, t.healthBonus, t.critChanceBonus);
        }
    }

    private static void CheckHeroThreshold(string hero)
    {
        if (buffData == null) return;
        int total = GetTotalKillsForHero(hero);
        foreach (var h in buffData.heroThresholds)
        {
            if (h.heroName != hero) continue;
            foreach (var t in h.thresholds)
            {
                if (t.killsRequired == total)
                    KillCodexBuffs.ApplyHeroBuff(hero, t.damageBonus, t.healthBonus, t.critChanceBonus);
            }
        }
    }

    private static void Save()
    {
        if (Oracle.oracle == null) return;
        var codex = Oracle.oracle.saveData.Codex;
        if (codex == null) Oracle.oracle.saveData.Codex = codex = new SaveData.CodexData();
        codex.HeroKillCounts = heroKills;
        codex.GlobalKillCounts = globalKills;
    }

    private static void Load()
    {
        if (Oracle.oracle == null) return;
        var codex = Oracle.oracle.saveData.Codex;
        if (codex == null)
        {
            codex = new SaveData.CodexData();
            Oracle.oracle.saveData.Codex = codex;
        }
        heroKills = codex.HeroKillCounts ?? new Dictionary<string, Dictionary<string, int>>();
        globalKills = codex.GlobalKillCounts ?? new Dictionary<string, int>();
    }

    public static int GetGlobalKillCount(string enemyId) =>
        globalKills.TryGetValue(enemyId, out var c) ? c : 0;
}
