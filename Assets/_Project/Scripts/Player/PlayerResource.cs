using System;
using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    [SerializeField] private float maxResource = 100f;
    [SerializeField] private float startResource = 100f;
    [SerializeField] private float regenerationPerSecond = 5f;
    [SerializeField] private CharacterStats characterStats;

    public float MaxResource
    {
        get
        {
            if (characterStats != null)
            {
                float statValue = characterStats.GetValue(StatType.MaxResource);
                if (statValue > 0f)
                    return statValue;
            }

            return maxResource;
        }
    }

    public float CurrentResource { get; private set; }

    public event Action<float, float> OnResourceChanged;

    private void Awake()
    {
        ResolveStats();
        CurrentResource = Mathf.Clamp(startResource, 0f, MaxResource);
    }

    private void OnEnable()
    {
        ResolveStats();

        if (characterStats != null)
            characterStats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        if (characterStats != null)
            characterStats.OnStatChanged -= HandleStatChanged;
    }

    private void Start()
    {
        NotifyResourceChanged();
    }

    private void Update()
    {
        TickRegeneration(Time.deltaTime);
    }

    public bool CanSpend(float amount)
    {
        return CurrentResource >= amount;
    }

    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (!CanSpend(amount)) return false;

        CurrentResource -= amount;
        CurrentResource = Mathf.Clamp(CurrentResource, 0f, MaxResource);

        NotifyResourceChanged();
        return true;
    }

    public void Restore(float amount)
    {
        if (amount <= 0f) return;

        CurrentResource += amount;
        CurrentResource = Mathf.Clamp(CurrentResource, 0f, MaxResource);

        NotifyResourceChanged();
    }

    public void TickRegeneration(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (CurrentResource >= MaxResource) return;

        float regenPerSecond = GetRegenerationPerSecond();
        if (regenPerSecond <= 0f) return;

        CurrentResource += regenPerSecond * deltaTime;
        CurrentResource = Mathf.Clamp(CurrentResource, 0f, MaxResource);

        NotifyResourceChanged();
    }

    private void HandleStatChanged(StatType type)
    {
        if (type != StatType.MaxResource)
            return;

        CurrentResource = Mathf.Clamp(CurrentResource, 0f, MaxResource);
        NotifyResourceChanged();
    }

    private void NotifyResourceChanged()
    {
        OnResourceChanged?.Invoke(CurrentResource, MaxResource);
    }

    private float GetRegenerationPerSecond()
    {
        if (characterStats != null)
            return characterStats.GetValue(StatType.ResourceRegen);

        return regenerationPerSecond;
    }

    private void ResolveStats()
    {
        if (characterStats != null)
            return;

        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
            characterStats = GetComponentInParent<CharacterStats>();
    }
}
