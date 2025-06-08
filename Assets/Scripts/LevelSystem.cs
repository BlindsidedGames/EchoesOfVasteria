using UnityEngine;
using System;

/// <summary>Tracks XP / level and fires events when either changes.</summary>
public class LevelSystem : MonoBehaviour
{
    [SerializeField] private int   level        = 1;
    [SerializeField] private int   currentXP    = 0;
    [SerializeField] private int   baseXPNeeded = 30;   // XP for level-up from 1 ➜ 2
    [SerializeField] private float xpGrowthMult = 1.5f; // geometric curve (30, 45, 68 …)

    public int  Level     => level;
    public int  CurrentXP => currentXP;
    public int  XPNeeded  => Mathf.CeilToInt(baseXPNeeded * Mathf.Pow(xpGrowthMult, level - 1));

    public Action<int, int> OnXPChanged;   // (current, needed)
    public Action<int>      OnLevelUp;     // new level

    public void GrantXP(int amount)
    {
        currentXP += amount;

        while (currentXP >= XPNeeded)
        {
            currentXP -= XPNeeded;
            level++;
            OnLevelUp?.Invoke(level);
        }

        OnXPChanged?.Invoke(currentXP, XPNeeded);
    }

    private void Awake() => OnXPChanged?.Invoke(currentXP, XPNeeded);  // initial push
}