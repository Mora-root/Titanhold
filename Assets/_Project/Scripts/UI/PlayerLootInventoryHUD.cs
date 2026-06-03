using TMPro;
using UnityEngine;

public sealed class PlayerLootInventoryHUD : MonoBehaviour
{
    [SerializeField] private PlayerLootInventory playerLootInventory;
    [SerializeField] private LootItemDefinition trackedItem;
    [SerializeField] private TMP_Text amountText;

    private void Awake()
    {
        playerLootInventory ??= FindAnyObjectByType<PlayerLootInventory>();
    }

    private void OnEnable()
    {
        if (playerLootInventory != null)
            playerLootInventory.OnChanged += HandleInventoryChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (playerLootInventory != null)
            playerLootInventory.OnChanged -= HandleInventoryChanged;
    }

    public void Refresh()
    {
        if (amountText == null)
            return;

        if (playerLootInventory == null || trackedItem == null)
        {
            amountText.text = "Loot: Missing";
            return;
        }

        int amount = playerLootInventory.GetAmount(trackedItem);
        amountText.text = $"{trackedItem.DisplayName}: {amount}";
    }

    private void HandleInventoryChanged()
    {
        Refresh();
    }
}
