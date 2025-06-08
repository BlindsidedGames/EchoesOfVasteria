using UnityEngine;

/// <summary>Simple HP container with change / death events.</summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP = 10;

    public  int MaxHP      => maxHP;
    public  int CurrentHP  { get; private set; }

    public  System.Action            OnDeath;
    public  System.Action<int, int>  OnHealthChanged;   // (current, max)

    private void Awake()
    {
        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);      // push initial value
    }

    public void TakeDamage(int dmg)
    {
        if (CurrentHP <= 0) return;

        CurrentHP -= dmg;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (CurrentHP <= 0) OnDeath?.Invoke();
    }

    /* Optional heal helper */
    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(CurrentHP + amount, maxHP);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }
}