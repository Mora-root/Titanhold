using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Common
{
    public sealed class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text blocksText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text footerText;
        [SerializeField] private GameObject bottomRow;
        [SerializeField] private TMP_Text sellPriceText;
        [SerializeField] private Image goldIcon;
        [SerializeField] private TMP_Text stackText;

        private void Awake()
        {
            if (root == null)
                root = transform as RectTransform;

            DisableRaycasts();
            Hide();
        }

        private void OnEnable()
        {
            DisableRaycasts();
        }

        public void Render(ItemTooltipData data)
        {
            if (data == null)
            {
                Hide();
                return;
            }

            if (root != null)
                root.gameObject.SetActive(true);

            SetText(titleText, data.Title);
            SetText(subtitleText, data.Subtitle);
            SetText(blocksText, BuildBlocksText(data));
            SetText(descriptionText, data.Description);
            SetText(footerText, data.Footer);
            RenderBottomRow(data);

            RebuildLayout();
        }

        public void Clear()
        {
            SetText(titleText, string.Empty);
            SetText(subtitleText, string.Empty);
            SetText(blocksText, string.Empty);
            SetText(descriptionText, string.Empty);
            SetText(footerText, string.Empty);
            SetText(sellPriceText, string.Empty);
            SetText(stackText, string.Empty);

            if (goldIcon != null)
                goldIcon.gameObject.SetActive(false);

            if (bottomRow != null)
                bottomRow.SetActive(false);
        }

        public void Hide()
        {
            Clear();

            if (root != null)
                root.gameObject.SetActive(false);
        }

        private void RenderBottomRow(ItemTooltipData data)
        {
            bool hasSellPrice = !string.IsNullOrWhiteSpace(data.SellPriceText);
            bool hasStack = !string.IsNullOrWhiteSpace(data.StackText);

            SetText(sellPriceText, data.SellPriceText);
            SetText(stackText, data.StackText);

            if (goldIcon != null)
                goldIcon.gameObject.SetActive(hasSellPrice);

            if (bottomRow != null)
                bottomRow.SetActive(hasSellPrice || hasStack);
        }

        private void RebuildLayout()
        {
            if (root != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        private void DisableRaycasts()
        {
            if (root == null)
                return;

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                    graphic.raycastTarget = false;
            }
        }

        private static string BuildBlocksText(ItemTooltipData data)
        {
            if (data == null || data.Blocks == null || data.Blocks.Count == 0)
                return string.Empty;

            StringBuilder builder = new StringBuilder();

            foreach (ItemTooltipBlock block in data.Blocks)
            {
                string blockText = BuildBlockText(block);
                if (string.IsNullOrWhiteSpace(blockText))
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();

                builder.Append(blockText);
            }

            return builder.ToString();
        }

        private static string BuildBlockText(ItemTooltipBlock block)
        {
            if (block == null)
                return string.Empty;

            StringBuilder builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(block.Title))
                builder.Append(block.Title);

            foreach (string line in block.Lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append(line);
            }

            return builder.ToString();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
                return;

            bool hasValue = !string.IsNullOrWhiteSpace(value);
            text.text = hasValue ? value : string.Empty;
            text.gameObject.SetActive(hasValue);
        }
    }
}
