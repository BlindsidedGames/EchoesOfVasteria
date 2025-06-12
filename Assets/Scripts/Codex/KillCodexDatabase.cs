using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "KillCodexDatabase", menuName = "SO/Kill Codex Database")]
public class KillCodexDatabase : ScriptableObject
{
    public List<CodexEntry> entries = new();
}

[Serializable]
public class CodexEntry
{
    [Tooltip("Enemy identifier from EnemyCodexInfo")]
    public string enemyId;
    [TableList]
    public List<CodexThreshold> thresholds = new();
}

[Serializable]
public class CodexThreshold
{
    [MinValue(1)] public int killCount = 1;
    [InlineProperty] public CodexBonusStats globalBonus = new();
    [TableList] public List<HeroBonus> heroBonuses = new();
}

[Serializable]
public class HeroBonus : CodexBonusStats
{
    public string heroId;
}
