using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks enemy kill counts and applies codex bonuses when thresholds are reached.
/// </summary>
public class KillCodexManager : MonoBehaviour
{
    [SerializeField] private KillCodexDatabase database;

    private readonly Dictionary<string, int> killCounts = new();
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

        killCounts.TryGetValue(enemyId, out int count);
        count++;
        killCounts[enemyId] = count;
        EvaluateBonuses(enemyId, count);
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
