using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Range(1, 3)]
    public int maxHealth = 3;

    private int currentHealth;
    private bool initialized;

    // Initializes itself on first read, so it is correct even if another script reads it before Awake runs
    public int CurrentHealth
    {
        get
        {
            EnsureInitialized();
            return currentHealth;
        }
        private set => currentHealth = value;
    }

    public int MaxHealth
    {
        get
        {
            EnsureInitialized();
            return maxHealth;
        }
    }

    public bool IsDead => CurrentHealth <= 0;

    [Header("Damage Immunity")]
    [Min(0f)]
    public float immunityDuration = 1.5f;

    public bool IsImmune { get; private set; }
    public float ImmunityTimeRemaining { get; private set; }

    public event Action<int, int> HealthChanged;
    public event Action Damaged;
    public event Action<bool> ImmunityChanged;
    public event Action Died;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        maxHealth = Mathf.Clamp(maxHealth, 1, 3);
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (!IsImmune)
            return;

        ImmunityTimeRemaining -= Time.deltaTime;

        if (ImmunityTimeRemaining <= 0f)
        {
            ImmunityTimeRemaining = 0f;
            IsImmune = false;
            ImmunityChanged?.Invoke(false);
        }
    }

    public bool TakeDamage(int damage = 1)
    {
        if (IsDead || IsImmune || damage <= 0)
            return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        Damaged?.Invoke();

        if (CurrentHealth <= 0)
        {
            Died?.Invoke();

            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();

            return true;
        }

        StartImmunity();
        return true;
    }

    private void StartImmunity()
    {
        ImmunityTimeRemaining = immunityDuration;
        IsImmune = immunityDuration > 0f;

        if (IsImmune)
            ImmunityChanged?.Invoke(true);
    }

    public void Heal(int amount = 1)
    {
        if (IsDead || amount <= 0)
            return;

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);

        if (CurrentHealth != previousHealth)
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void RestoreHealth()
    {
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}