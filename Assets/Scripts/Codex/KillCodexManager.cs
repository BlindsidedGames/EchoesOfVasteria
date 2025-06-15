using System.Collections.Generic;
using Blindsided.SaveData;
using UnityEngine;

/// <summary>
/// Tracks enemy kill counts and applies codex bonuses when thresholds are reached.
/// </summary>
public class KillCodexManager : MonoBehaviour
{
    [SerializeField] private KillCodexDatabase database;

    private Dictionary<string, int> KillCounts => StaticReferences.GlobalKillCounts;

    public event System.Action<string, int> KillCountChanged;
    public static KillCodexManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Register that an enemy with the given id was killed.
    /// </summary>
    public void RegisterKill(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId) || database == null)
            return;

        KillCounts.TryGetValue(enemyId, out int count);
        count++;
        KillCounts[enemyId] = count;
        KillCountChanged?.Invoke(enemyId, count);
        EvaluateBonuses(enemyId, count);
    }

    public int GetKillCount(string enemyId)
    {
        KillCounts.TryGetValue(enemyId, out int count);
        return count;
    }

    private void EvaluateBonuses(string enemyId, int newCount)
    {
        var entry = database.entries.Find(e => e.enemyId == enemyId);
        if (entry == null)
            return;

        foreach (var threshold in entry.thresholds)
        {
            if (newCount == threshold.killCount)
                KillCodexBuffs.AddBuffs(threshold.globalBonus);
        }
    }

}
