using TMPro;
using UnityEngine;

public sealed class LootInventoryItemRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text amountText;

    public void Setup(LootItemDefinition item, int amount)
    {
        if (nameText != null)
            nameText.text = item != null ? item.DisplayName : "Unknown";

        if (amountText != null)
            amountText.text = amount.ToString();
    }
}
