using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerLootInventoryDragController : MonoBehaviour
{
    [SerializeField] private PlayerLootInventory inventory;
    [SerializeField] private GameObject ghostRoot;
    [SerializeField] private Image ghostIcon;
    [SerializeField] private TMP_Text ghostAmountText;

    private bool isDragging;
    private int sourceIndex = -1;

    public bool IsDragging => isDragging;
    public int SourceIndex => sourceIndex;

    private void Awake()
    {
        inventory ??= FindAnyObjectByType<PlayerLootInventory>();

        if (ghostRoot != null)
            ghostRoot.SetActive(false);
    }

    public void BeginDrag(int sourceIndex, LootItemDefinition item, int amount, Vector2 screenPosition)
    {
        if (item == null)
            return;

        this.sourceIndex = sourceIndex;
        isDragging = true;

        if (ghostIcon != null)
        {
            ghostIcon.sprite = item.Icon;
            ghostIcon.enabled = item.Icon != null;
        }

        if (ghostAmountText != null)
            ghostAmountText.text = amount > 1 ? amount.ToString() : "";

        if (ghostRoot != null)
            ghostRoot.SetActive(true);

        Drag(screenPosition);
    }

    public void Drag(Vector2 screenPosition)
    {
        if (!isDragging)
            return;

        if (ghostRoot != null)
            ghostRoot.transform.position = screenPosition;
    }

    public bool DropOn(int targetIndex)
    {
        if (!isDragging)
            return false;

        if (inventory == null)
        {
            EndDrag();
            return false;
        }

        bool moved = inventory.MoveSlot(sourceIndex, targetIndex);
        EndDrag();
        return moved;
    }

    public void EndDrag()
    {
        isDragging = false;
        sourceIndex = -1;

        if (ghostRoot != null)
            ghostRoot.SetActive(false);
    }

    private void OnDisable()
    {
        EndDrag();
    }
}
