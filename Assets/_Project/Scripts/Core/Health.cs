using System;
using Titanhold.Combat;
using UnityEngine;

public class Health : MonoBehaviour, IContextualDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private PlayerExperience playerExperience;

    public float MaxHealth
    {
        get
        {
            if (characterStats != null)
            {
                float statValue = characterStats.GetValue(StatType.MaxHealth);
                if (statValue > 0f)
                    return statValue;
            }

            return maxHealth;
        }
    }

    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0f;
    public DeathContext LastDeathContext { get; private set; }

    public event Action<float> OnDamage;
    public event Action<DamageResult> OnDamageResolved;
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action<DeathContext> OnDeathContext;

    private bool isDead;

    private void Awake()
    {
        ResolveReferences();

        CurrentHealth = MaxHealth;
        isDead = false;
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (characterStats != null)
            characterStats.OnStatChanged += HandleStatChanged;

        if (playerExperience != null)
            playerExperience.OnLevelChanged += HandleLevelChanged;
    }

    private void OnDisable()
    {
        if (characterStats != null)
            characterStats.OnStatChanged -= HandleStatChanged;

        if (playerExperience != null)
            playerExperience.OnLevelChanged -= HandleLevelChanged;
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    private void Update()
    {
        TickRegeneration(Time.deltaTime);
    }

    public void TakeDamage(float rawDamage)
    {
        ApplyDamage(DamageRequest.CreateUnattributed(rawDamage));
    }

    public DamageResult ApplyDamage(DamageRequest request)
    {
        if (isDead)
            return DamageResult.Rejected(request, DamageRejectionReason.TargetAlreadyDead);

        if (!request.ExecutionId.IsValid)
            return DamageResult.Rejected(request, DamageRejectionReason.InvalidExecutionId);

        if (request.RawDamage <= 0f || float.IsNaN(request.RawDamage) || float.IsInfinity(request.RawDamage))
            return DamageResult.Rejected(request, DamageRejectionReason.InvalidAmount);

        float armor = characterStats != null ? characterStats.GetValue(StatType.Armor) : 0f;
        float finalDamage = DamageMitigationCalculator.ApplyArmor(request.RawDamage, armor);
        if (finalDamage <= 0f)
            return DamageResult.Rejected(request, DamageRejectionReason.FullyMitigated);

        float healthBefore = CurrentHealth;
        CurrentHealth -= finalDamage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);
        float appliedDamage = healthBefore - CurrentHealth;
        bool killed = CurrentHealth <= 0f;
        DeathContext deathContext = killed
            ? new DeathContext(request, appliedDamage)
            : default;

        if (killed)
        {
            isDead = true;
            LastDeathContext = deathContext;
        }

        DamageResult result = DamageResult.Applied(
            request,
            healthBefore,
            CurrentHealth,
            appliedDamage,
            killed,
            deathContext);

        OnDamage?.Invoke(appliedDamage);
        NotifyHealthChanged();
        OnDamageResolved?.Invoke(result);

        if (killed)
        {
            OnDeathContext?.Invoke(deathContext);
            OnDeath?.Invoke();
        }

        return result;
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        NotifyHealthChanged();
    }

    public void TickRegeneration(float deltaTime)
    {
        if (isDead) return;
        if (deltaTime <= 0f) return;
        if (CurrentHealth >= MaxHealth) return;

        float regenPerSecond = characterStats != null ? characterStats.GetValue(StatType.HPRegen) : 0f;
        if (regenPerSecond <= 0f) return;

        Heal(regenPerSecond * deltaTime);
    }

    public void RestoreFull()
    {
        CurrentHealth = MaxHealth;
        isDead = false;
        LastDeathContext = default;
        NotifyHealthChanged();
    }

    private void HandleStatChanged(StatType type)
    {
        if (type != StatType.MaxHealth)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);
        NotifyHealthChanged();
    }

    private void HandleLevelChanged(int level)
    {
        RestoreFull();
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    private void ResolveReferences()
    {
        if (characterStats == null)
        {
            characterStats = GetComponent<CharacterStats>();
        }
        if (characterStats == null)
        {
            characterStats = GetComponentInParent<CharacterStats>();
        }

        if (playerExperience == null)
        {
            playerExperience = GetComponent<PlayerExperience>();
        }
        if (playerExperience == null)
        {
            playerExperience = GetComponentInParent<PlayerExperience>();
        }
    }

}
