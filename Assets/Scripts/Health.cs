using UnityEngine;

/// <summary>Simple HP container with change / death events.</summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private HeroBalanceData balance;
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int defense = 1;

    private GameObject lastHeroAttacker;

    private LevelSystem levelSystem;

    public  int MaxHP      => maxHP;
    public  int CurrentHP  { get; private set; }
    public  int Defense    => defense;

    public  System.Action            OnDeath;
    public  System.Action<int, int>  OnHealthChanged;   // (current, max)

    public GameObject LastHeroAttacker => lastHeroAttacker;

    private void Awake()
    {
        levelSystem = GetComponent<LevelSystem>();
        KillCodexBuffs.OnBuffsChanged += ApplyBalance;
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

        KillCodexBuffs.OnBuffsChanged -= ApplyBalance;
    }

    public void TakeDamage(int dmg, GameObject attacker)
    {
        if (CurrentHP <= 0) return;

        int actualDamage = Mathf.Max(dmg - defense, 0);
        if (attacker && attacker.CompareTag("Hero"))
            lastHeroAttacker = attacker;
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
            maxHP = balance.baseHealth + balance.healthPerLevel * (level - 1);
            defense = balance.baseDefense + balance.defensePerLevel * (level - 1);
        }

        if (CompareTag("Hero"))
            maxHP += KillCodexBuffs.GetHealthBonus(name);

        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    private void OnLevelChanged(int newLevel) => ApplyBalance();
}