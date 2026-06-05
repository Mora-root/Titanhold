using UnityEngine;

// Temporary prototype helper. Do not use in production.
public sealed class PlayerItemInventoryDebugAdder : MonoBehaviour
{
    [SerializeField] private PlayerItemInventory itemInventory;
    [SerializeField] private ItemDefinition itemToAdd;
    [SerializeField] private KeyCode addKey = KeyCode.U;

    private void Awake()
    {
        itemInventory ??= GetComponent<PlayerItemInventory>();
        itemInventory ??= FindAnyObjectByType<PlayerItemInventory>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(addKey))
            return;

        if (itemInventory == null || itemToAdd == null)
            return;

        itemInventory.TryAdd(itemToAdd);
    }
}
