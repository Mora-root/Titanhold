using Titanhold.UI.Containers;
using Titanhold.UI.SectionInventory;
using UnityEngine;

public sealed class PlayerChestInteractionController : MonoBehaviour
{
    [SerializeField] private ItemContainerWindowController itemContainerInventoryWindowController;
    [SerializeField] private InventoryWindowController playerInventoryWindowController;
    [SerializeField] private ChestWindowController chestWindowController;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private float closeDistancePadding = 0.25f;

    private ChestInteractable activeChest;
    private bool loggedMissingChestWindow;

    private Transform PlayerTransform => playerRoot != null ? playerRoot : transform;

    public void Configure(
        ItemContainerWindowController itemContainerController,
        InventoryWindowController inventoryController,
        ChestWindowController chestController,
        Transform root = null)
    {
        itemContainerInventoryWindowController = itemContainerController;
        playerInventoryWindowController = inventoryController;
        chestWindowController = chestController;

        if (root != null)
            playerRoot = root;
    }

    private void Update()
    {
        if (activeChest == null)
            return;

        if (chestWindowController == null || !chestWindowController.IsOpen)
        {
            activeChest = null;
            return;
        }

        if (!activeChest.IsInteractable)
        {
            CloseChest();
            return;
        }

        float maxDistance = activeChest.InteractionRange + Mathf.Max(0f, closeDistancePadding);
        float distance = Vector3.Distance(
            PlayerTransform.position,
            activeChest.InteractionPoint.position);

        if (distance > maxDistance)
            CloseChest();
    }

    public void OpenChest(ChestInteractable chest)
    {
        if (chest == null || chest.Inventory == null)
            return;

        if (chestWindowController == null)
        {
            LogMissingChestWindow();
            return;
        }

        activeChest = chest;
        OpenPlayerInventoryWindow();
        chestWindowController.Open(chest.Inventory);
    }

    public void CloseChest()
    {
        chestWindowController?.Close();
        activeChest = null;
    }

    private void LogMissingChestWindow()
    {
        if (loggedMissingChestWindow)
            return;

        Debug.LogWarning($"{nameof(PlayerChestInteractionController)} requires a ChestWindowController reference.", this);
        loggedMissingChestWindow = true;
    }

    private void OpenPlayerInventoryWindow()
    {
        if (itemContainerInventoryWindowController != null)
        {
            itemContainerInventoryWindowController.Open();
            return;
        }

        playerInventoryWindowController?.Open();
    }
}
