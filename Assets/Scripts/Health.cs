using UnityEngine;

/// <summary>Simple HP container with change / death events.</summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private HeroBalanceData balance;
    [SerializeField] private string heroId = "";
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int defense = 1;

    private LevelSystem levelSystem;

    public  int MaxHP      => maxHP;
    public  int CurrentHP  { get; private set; }
    public  int Defense    => defense;

    public  System.Action            OnDeath;
    public  System.Action<int, int>  OnHealthChanged;   // (current, max)

    private void Awake()
    {
        levelSystem = GetComponent<LevelSystem>();
        if (string.IsNullOrEmpty(heroId) && TryGetComponent(out HeroCodexInfo info))
            heroId = info.HeroId;
        KillCodexBuffs.BuffsChanged += ApplyBalance;
    }

    private void Start()
    {
        ApplyBalance();
        if (levelSystem != null)
            levelSystem.OnLevelUp += OnLevelChanged;
    }

    private void OnDestroy()
    {
        if (levelSystem != null)
            levelSystem.OnLevelUp -= OnLevelChanged;
        KillCodexBuffs.BuffsChanged -= ApplyBalance;
    }

    public void TakeDamage(int dmg, GameObject attacker)
    {
        if (CurrentHP <= 0) return;

        int actualDamage = Mathf.Max(dmg - defense, 0);
        CurrentHP -= actualDamage;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (CurrentHP <= 0)
        {
            OnDeath?.Invoke();

            if (CompareTag("Enemy") && attacker && attacker.CompareTag("Hero"))
            {
                int reward = 5;
                if (TryGetComponent(out EnemyAI enemy))
                    reward = enemy.XPReward;

                if (attacker.TryGetComponent(out LevelSystem lvl))
                    lvl.GrantXP(reward);

                if (TryGetComponent(out EnemyCodexInfo codex))
                    KillCodexManager.Instance?.RegisterKill(codex.EnemyId);
            }
        }
    }

    /* Optional heal helper */
    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(CurrentHP + amount, maxHP);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    /// <summary>
    /// Applies balance values based on current level.
    /// </summary>
    private void ApplyBalance()
    {
        int level = levelSystem ? levelSystem.Level : 1;
        if (balance != null)
        {
            maxHP = balance.baseHealth + balance.healthPerLevel * (level - 1) + KillCodexBuffs.BonusHealth;
            defense = balance.baseDefense + balance.defensePerLevel * (level - 1) + KillCodexBuffs.BonusDefense;
        }
        else
        {
            maxHP += KillCodexBuffs.BonusHealth;
            defense += KillCodexBuffs.BonusDefense;
        }

        if (KillCodexManager.Instance != null && !string.IsNullOrEmpty(heroId))
        {
            var bonus = KillCodexManager.Instance.GetHeroBonuses(heroId);
            if (bonus != null)
            {
                maxHP += bonus.bonusHealth;
                defense += bonus.bonusDefense;
            }
        }

        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    private void OnLevelChanged(int newLevel) => ApplyBalance();
}