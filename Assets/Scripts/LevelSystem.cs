using UnityEngine;
using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using static Blindsided.Oracle;

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

    private void Awake()
    {
        if (oracle != null)
        {
            oracle.saveData.HeroStates ??= new Dictionary<string, SaveData.HeroState>();
            if (oracle.saveData.HeroStates.TryGetValue(gameObject.name, out var state))
            {
                level = state.Level;
                currentXP = state.CurrentXP;
            }
            else
            {
                oracle.saveData.HeroStates[gameObject.name] = new SaveData.HeroState { Level = level, CurrentXP = currentXP };
            }
        }

        EventHandler.OnSaveData += SaveState;
        OnXPChanged?.Invoke(currentXP, XPNeeded); // initial push
    }

    private void OnDestroy()
    {
        EventHandler.OnSaveData -= SaveState;
    }

    private void SaveState()
    {
        if (oracle == null) return;
        var states = oracle.saveData.HeroStates;
        if (states == null)
            oracle.saveData.HeroStates = states = new Dictionary<string, SaveData.HeroState>();

        if (!states.TryGetValue(gameObject.name, out var state))
            states[gameObject.name] = state = new SaveData.HeroState();

        state.Level = level;
        state.CurrentXP = currentXP;
    }
}