using System;
using UnityEngine;

public sealed class LootLabelTarget : MonoBehaviour
{
    [SerializeField] private PlayerInventoryItemStackLootReward reward;
    [SerializeField] private LootPickup lootPickup;
    [SerializeField] private Transform labelAnchor;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    public static event Action<LootLabelTarget> Registered;
    public static event Action<LootLabelTarget> Unregistered;

    public event Action Changed;

    public ItemStack Stack => reward != null ? reward.Stack : null;
    public LootPickup Pickup
    {
        get
        {
            ResolveReferences();
            return lootPickup;
        }
    }

    public bool HasStack => Stack != null && Stack.Definition != null && Stack.Amount > 0;
    public bool IsLabelVisible => HasStack && (lootPickup == null || lootPickup.IsLootable);
    public Vector3 LabelWorldPosition => (labelAnchor != null ? labelAnchor.position : transform.position) + worldOffset;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Registered?.Invoke(this);
    }

    private void OnDisable()
    {
        Unregistered?.Invoke(this);
    }

    public void Refresh()
    {
        ResolveReferences();
        Changed?.Invoke();
    }

    private void ResolveReferences()
    {
        if (reward == null)
            reward = GetComponent<PlayerInventoryItemStackLootReward>();

        if (lootPickup == null)
            lootPickup = GetComponent<LootPickup>();
    }
}
