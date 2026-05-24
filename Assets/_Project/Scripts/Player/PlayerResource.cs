using System;
using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    [SerializeField] private float maxResource = 100f;
    [SerializeField] private float startResource = 100f;
    [SerializeField] private float regenerationPerSecond = 5f;

    private CharacterStats stats;

    public float MaxResource
    {
        get
        {
            if (stats != null)
            {
                float statValue = stats.GetValue(StatType.MaxResource);
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
        stats = GetComponent<CharacterStats>();
        CurrentResource = MaxResource;
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.OnStatChanged -= HandleStatChanged;
    }

    private void Start()
    {
        NotifyResourceChanged();
    }

    private void Update()
    {
        Regenerate();
        if (Input.GetKeyDown(KeyCode.R))
        {
            TrySpend(20f);
        }
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

    private void Regenerate()
    {
        if (regenerationPerSecond <= 0f) return;
        if (CurrentResource >= MaxResource) return;

        CurrentResource += regenerationPerSecond * Time.deltaTime;
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
}
