using System;
using Gear;
using TMPro;
using UnityEngine;

/// <summary>Simple HP container with change / death events.</summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int defense = 1;
    private CharacterBalanceData balance;
    private BalanceHolder balanceHolder;
    private HeroGear gear;

    private LevelSystem levelSystem;

    public Action OnDeath;
    public Action<int, int> OnHealthChanged; // (current, max)

    public int MaxHP => maxHP;
    public int CurrentHP { get; private set; }
    public int Defense => defense;

    private void Awake()
    {
        levelSystem = GetComponent<LevelSystem>();
        balanceHolder = GetComponent<BalanceHolder>();
        balance = balanceHolder ? balanceHolder.Balance : null;
        gear = balanceHolder ? balanceHolder.Gear : null;
        if (balance is HeroBalanceData)
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
        if (balance is HeroBalanceData)
            KillCodexBuffs.BuffsChanged -= ApplyBalance;
    }

    public void TakeDamage(int dmg, GameObject attacker)
    {
        if (CurrentHP <= 0) return;

        var actualDamage = Mathf.Max(dmg - defense, 0);
        CurrentHP -= actualDamage;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (actualDamage > 0)
        {
            var c = Color.white;
            if (attacker && attacker.CompareTag("Hero") && CompareTag("Enemy"))
                c = new Color(1f, 0.5f, 0f); // orange
            else if (attacker && attacker.CompareTag("Enemy") && CompareTag("Hero"))
                c = Color.red;
            ShowFloatingText(actualDamage.ToString(), c);
        }

        if (CurrentHP <= 0)
        {
            OnDeath?.Invoke();

            if (CompareTag("Enemy") && attacker && attacker.CompareTag("Hero"))
            {
                var reward = 5;
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

        if (amount > 0)
        {
            var c = CompareTag("Hero") ? Color.green : new Color(0.5f, 1f, 0f);
            ShowFloatingText(amount.ToString(), c);
        }
    }

    /// <summary>
    ///     Applies balance values based on current level.
    /// </summary>
    private void ApplyBalance()
    {
        var level = levelSystem ? levelSystem.Level : 1;
        if (balance != null)
        {
            maxHP = balance.baseHealth + balance.healthPerLevel * (level - 1);
            defense = balance.baseDefense + balance.defensePerLevel * (level - 1);
        }

        if (gear != null)
        {
            maxHP += gear.TotalHealth;
            defense += gear.TotalDefense;
        }

        if (balance is HeroBalanceData)
        {
            maxHP += KillCodexBuffs.BonusHealth;
            defense += KillCodexBuffs.BonusDefense;
        }


        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    private void OnLevelChanged(int newLevel)
    {
        ApplyBalance();
    }

    /* ─── Floating Text ─── */
    private void ShowFloatingText(string message, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = transform.position + Vector3.up;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = 3;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 10;
        var effect = go.AddComponent<FloatingTextEffect>();
        effect.Init(color);
    }

    private class FloatingTextEffect : MonoBehaviour
    {
        private const float Duration = 1f;
        private const float Speed = 1f;
        private TMP_Text text;
        private float time;

        private void Update()
        {
            transform.position += Vector3.up * Speed * Time.deltaTime;
            time += Time.deltaTime;
            var c = text.color;
            c.a = 1f - time / Duration;
            text.color = c;
            if (time >= Duration) Destroy(gameObject);
        }

        public void Init(Color color)
        {
            text = GetComponent<TMP_Text>();
            text.color = color;
        }
    }
}