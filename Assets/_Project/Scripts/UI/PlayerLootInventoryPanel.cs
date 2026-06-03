using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerLootInventoryPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private PlayerLootInventory playerLootInventory;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LootInventoryItemRow rowPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text emptyText;

    private readonly List<PlayerLootInventory.LootItemStackView> cachedStacks = new();

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        playerLootInventory ??= FindAnyObjectByType<PlayerLootInventory>();

        if (root != null)
            root.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerLootInventory != null)
            playerLootInventory.OnChanged += HandleInventoryChanged;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (playerLootInventory != null)
            playerLootInventory.OnChanged -= HandleInventoryChanged;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (root != null)
            root.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Refresh()
    {
        ClearRows();

        if (playerLootInventory == null || contentRoot == null || rowPrefab == null)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(true);

            return;
        }

        playerLootInventory.GetStacks(cachedStacks);

        foreach (PlayerLootInventory.LootItemStackView stack in cachedStacks)
        {
            LootInventoryItemRow row = Instantiate(rowPrefab, contentRoot);
            row.Setup(stack.Item, stack.Amount);
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(cachedStacks.Count == 0);
    }

    private void ClearRows()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }

    private void HandleInventoryChanged()
    {
        Refresh();
    }
}
