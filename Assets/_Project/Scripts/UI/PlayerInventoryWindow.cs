using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerInventoryWindow : MonoBehaviour
{
    private enum InventoryTab
    {
        Items,
        Materials
    }

    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button itemsTabButton;
    [SerializeField] private Button materialsTabButton;
    [SerializeField] private GameObject itemsTabRoot;
    [SerializeField] private GameObject materialsTabRoot;
    [SerializeField] private PlayerItemInventoryTab itemsTab;
    [SerializeField] private PlayerLootInventoryTab materialsTab;

    private InventoryTab currentTab = InventoryTab.Items;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        if (root != null)
            root.SetActive(false);

        IsOpen = false;
    }

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (itemsTabButton != null)
            itemsTabButton.onClick.AddListener(ShowItemsTab);

        if (materialsTabButton != null)
            materialsTabButton.onClick.AddListener(ShowMaterialsTab);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (itemsTabButton != null)
            itemsTabButton.onClick.RemoveListener(ShowItemsTab);

        if (materialsTabButton != null)
            materialsTabButton.onClick.RemoveListener(ShowMaterialsTab);
    }

    public void Open()
    {
        if (root != null)
            root.SetActive(true);

        IsOpen = true;
        ShowTab(currentTab);
        Refresh();
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);

        IsOpen = false;
    }

    public void Refresh()
    {
        itemsTab?.Refresh();
        materialsTab?.Refresh();
    }

    public void ShowItemsTab()
    {
        currentTab = InventoryTab.Items;
        ShowTab(currentTab);
    }

    public void ShowMaterialsTab()
    {
        currentTab = InventoryTab.Materials;
        ShowTab(currentTab);
    }

    private void ShowTab(InventoryTab tab)
    {
        if (itemsTabRoot != null)
            itemsTabRoot.SetActive(tab == InventoryTab.Items);

        if (materialsTabRoot != null)
            materialsTabRoot.SetActive(tab == InventoryTab.Materials);

        if (tab == InventoryTab.Items)
            itemsTab?.Refresh();
        else
            materialsTab?.Refresh();
    }
}
