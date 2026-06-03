using TMPro;
using UnityEngine;

public sealed class LootItemTooltip : MonoBehaviour
{
    private const float AnchorPadding = 8f;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text descriptionText;

    private readonly Vector3[] anchorCorners = new Vector3[4];
    private RectTransform rootRectTransform;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        rootRectTransform = root != null ? root.GetComponent<RectTransform>() : transform as RectTransform;

        Hide();
    }

    public void Show(LootItemDefinition item, Vector2 screenPosition)
    {
        if (!SetItem(item))
            return;

        transform.position = screenPosition;
        ShowRoot();
    }

    public void ShowLeftOf(LootItemDefinition item, RectTransform anchor)
    {
        if (!SetItem(item))
            return;

        if (anchor == null || rootRectTransform == null)
        {
            ShowRoot();
            return;
        }

        anchor.GetWorldCorners(anchorCorners);

        float anchorLeft = anchorCorners[0].x;
        float anchorTop = anchorCorners[1].y;
        float tooltipWidth = rootRectTransform.rect.width * Mathf.Abs(rootRectTransform.lossyScale.x);
        float tooltipHeight = rootRectTransform.rect.height * Mathf.Abs(rootRectTransform.lossyScale.y);
        Vector2 pivot = rootRectTransform.pivot;

        transform.position = new Vector3(
            anchorLeft - AnchorPadding - tooltipWidth * (1f - pivot.x),
            anchorTop - tooltipHeight * (1f - pivot.y),
            transform.position.z);

        ShowRoot();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private bool SetItem(LootItemDefinition item)
    {
        if (item == null)
        {
            Hide();
            return false;
        }

        if (nameText != null)
            nameText.text = item.DisplayName;

        if (categoryText != null)
            categoryText.text = item.CategoryDisplayName;

        if (descriptionText != null)
        {
            descriptionText.text = item.Description;
            descriptionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(item.Description));
        }

        return true;
    }

    private void ShowRoot()
    {
        if (root != null)
            root.SetActive(true);
    }
}
