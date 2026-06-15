using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterAttributeDerivedStatsBinder : MonoBehaviour
{
    private enum PrimaryDamageAttribute
    {
        Strength,
        Agility,
        Intelligence
    }

    private static readonly StatModifierSource DerivedSource =
        StatModifierSource.ForSystem("AttributeDerivedStats");

    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private PrimaryDamageAttribute primaryDamageStat = PrimaryDamageAttribute.Strength;

    [Header("Coefficients")]
    [SerializeField] private float damagePerPrimary = 2f;
    [SerializeField] private float armorPerStrength = 0.5f;
    [SerializeField] private float attackSpeedIncreasedPerAgility = 1f;
    [SerializeField] private float moveSpeedFlatPerAgility = 0.02f;
    [SerializeField] private float maxResourcePerIntelligence = 8f;
    [SerializeField] private float resourceRegenPerIntelligence = 0.1f;
    [SerializeField] private float maxHealthPerStamina = 10f;
    [SerializeField] private float hpRegenPerStamina = 0.1f;

    private bool subscribed;
    private bool recalculating;
    private bool loggedMissingStats;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ClearDerivedModifiers();
    }

    private void OnValidate()
    {
        damagePerPrimary = Mathf.Max(0f, damagePerPrimary);
        armorPerStrength = Mathf.Max(0f, armorPerStrength);
        attackSpeedIncreasedPerAgility = Mathf.Max(0f, attackSpeedIncreasedPerAgility);
        moveSpeedFlatPerAgility = Mathf.Max(0f, moveSpeedFlatPerAgility);
        maxResourcePerIntelligence = Mathf.Max(0f, maxResourcePerIntelligence);
        resourceRegenPerIntelligence = Mathf.Max(0f, resourceRegenPerIntelligence);
        maxHealthPerStamina = Mathf.Max(0f, maxHealthPerStamina);
        hpRegenPerStamina = Mathf.Max(0f, hpRegenPerStamina);
    }

    public void Refresh()
    {
        ResolveReferences();
        Subscribe();
        Recalculate();
    }

    public void Recalculate()
    {
        if (characterStats == null)
        {
            LogMissingStats();
            return;
        }

        if (recalculating)
            return;

        recalculating = true;

        try
        {
            ClearDerivedModifiers();

            float strength = characterStats.GetValue(StatType.Strength);
            float agility = characterStats.GetValue(StatType.Agility);
            float intelligence = characterStats.GetValue(StatType.Intelligence);
            float stamina = characterStats.GetValue(StatType.Stamina);
            float primary = characterStats.GetValue(ToStatType(primaryDamageStat));

            AddFlat(StatType.Damage, primary * damagePerPrimary);
            AddFlat(StatType.Armor, strength * armorPerStrength);
            AddFlat(StatType.AttackSpeed, agility * attackSpeedIncreasedPerAgility);
            AddFlat(StatType.MoveSpeed, agility * moveSpeedFlatPerAgility);
            AddFlat(StatType.MaxResource, intelligence * maxResourcePerIntelligence);
            AddFlat(StatType.ResourceRegen, intelligence * resourceRegenPerIntelligence);
            AddFlat(StatType.MaxHealth, stamina * maxHealthPerStamina);
            AddFlat(StatType.HPRegen, stamina * hpRegenPerStamina);
        }
        finally
        {
            recalculating = false;
        }
    }

    private void ResolveReferences()
    {
        if (characterStats == null)
            characterStats = GetComponent<CharacterStats>();
    }

    private void Subscribe()
    {
        if (characterStats == null || subscribed)
            return;

        characterStats.OnStatChanged += HandleStatChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (characterStats != null && subscribed)
            characterStats.OnStatChanged -= HandleStatChanged;

        subscribed = false;
    }

    private void HandleStatChanged(StatType type)
    {
        if (recalculating)
            return;

        if (!IsPrimaryAttribute(type))
            return;

        Recalculate();
    }

    private void AddFlat(StatType type, float value)
    {
        if (value <= 0f)
            return;

        characterStats.AddModifier(new StatModifier(type, StatModifierType.Flat, value), DerivedSource);
    }

    public void ClearDerivedModifiers()
    {
        if (characterStats == null)
            return;

        characterStats.RemoveModifiersFromSource(DerivedSource);
    }

    private void LogMissingStats()
    {
        if (loggedMissingStats)
            return;

        Debug.LogWarning($"{nameof(CharacterAttributeDerivedStatsBinder)} requires a CharacterStats reference.", this);
        loggedMissingStats = true;
    }

    private static bool IsPrimaryAttribute(StatType type)
    {
        return type == StatType.Strength ||
               type == StatType.Agility ||
               type == StatType.Intelligence ||
               type == StatType.Stamina;
    }

    private static StatType ToStatType(PrimaryDamageAttribute attribute)
    {
        return attribute switch
        {
            PrimaryDamageAttribute.Agility => StatType.Agility,
            PrimaryDamageAttribute.Intelligence => StatType.Intelligence,
            _ => StatType.Strength
        };
    }
}
