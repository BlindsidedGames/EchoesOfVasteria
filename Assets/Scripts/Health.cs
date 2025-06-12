using UnityEngine;

/// <summary>Simple HP container with change / death events.</summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int defense = 1;

    public  int MaxHP      => maxHP;
    public  int CurrentHP  { get; private set; }
    public  int Defense    => defense;

    public  System.Action            OnDeath;
    public  System.Action<int, int>  OnHealthChanged;   // (current, max)

    private void Awake()
    {
        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);      // push initial value
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
    /// Sets base health and defense at runtime.
    /// </summary>
    public void SetBaseStats(int hp, int def)
    {
        maxHP = hp;
        defense = def;
        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }
}