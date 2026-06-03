using TMPro;
using UnityEngine;

public sealed class PlayerLootInventoryHUD : MonoBehaviour
{
    [SerializeField] private PlayerLootInventory playerLootInventory;
    [SerializeField] private TMP_Text shardsText;

    private void Awake()
    {
        playerLootInventory ??= FindAnyObjectByType<PlayerLootInventory>();
    }

    private void OnEnable()
    {
        if (playerLootInventory != null)
            playerLootInventory.OnCrystalShardsChanged += HandleCrystalShardsChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (playerLootInventory != null)
            playerLootInventory.OnCrystalShardsChanged -= HandleCrystalShardsChanged;
    }

    public void Refresh()
    {
        if (shardsText == null)
            return;

        if (playerLootInventory != null)
        {
            shardsText.text = $"Shards: {playerLootInventory.CrystalShards}";
        }
        else
        {
            shardsText.text = "Shards: Missing";
        }
    }

    private void HandleCrystalShardsChanged(int amount)
    {
        Refresh();
    }
}
