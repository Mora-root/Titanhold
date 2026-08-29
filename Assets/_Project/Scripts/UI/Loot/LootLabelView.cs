using System;
using TMPro;
using Titanhold.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Titanhold.UI.Loot
{
    public sealed class LootLabelView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static readonly Color DefaultBackgroundColor = new Color(0f, 0f, 0f, 1f);

        [SerializeField] private RectTransform root;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Outline borderOutline;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool overrideBackgroundColor;
        [SerializeField] private Color customBackgroundColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField, Range(0f, 1f)] private float amountTextBrightness = 0.85f;

        private LootLabelTarget target;
        private ItemTooltipController tooltipController;

        public event Action<LootLabelTarget> Clicked;

        public RectTransform Root
        {
            get
            {
                if (root == null)
                    root = transform as RectTransform;

                return root;
            }
        }

        private void Awake()
        {
            EnsureRaycastSetup();
        }

        private void OnDisable()
        {
            HideTooltip();
        }

        public void ConfigureGeneratedRefs(
            RectTransform root,
            Image backgroundImage,
            Outline borderOutline,
            Image iconImage,
            TMP_Text nameText,
            TMP_Text amountText,
            CanvasGroup canvasGroup)
        {
            this.root = root;
            this.backgroundImage = backgroundImage;
            this.borderOutline = borderOutline;
            this.iconImage = iconImage;
            this.nameText = nameText;
            this.amountText = amountText;
            this.canvasGroup = canvasGroup;
            EnsureRaycastSetup();
        }

        public void SetTarget(LootLabelTarget target, ItemTooltipController tooltipController)
        {
            Unsubscribe();
            this.target = target;
            this.tooltipController = tooltipController;

            if (this.target != null)
                this.target.Changed += Refresh;

            Refresh();
        }

        public void ClearTarget()
        {
            Unsubscribe();
            target = null;
            HideTooltip();
            SetVisible(false);
        }

        public void SetAnchoredPosition(Vector2 position)
        {
            if (Root != null)
                Root.anchoredPosition = position;
        }

        public void SetWorldVisible(bool visible)
        {
            SetVisible(visible);
        }

        public void Refresh()
        {
            ItemStack stack = target != null ? target.Stack : null;
            ItemDefinition definition = stack != null ? stack.Definition : null;

            if (definition == null)
            {
                SetVisible(false);
                HideTooltip();
                return;
            }

            SetVisible(target.IsLabelVisible);

            Color rarityColor = definition.PickupLabelColor;

            if (backgroundImage != null)
                backgroundImage.color = overrideBackgroundColor ? customBackgroundColor : DefaultBackgroundColor;

            if (borderOutline != null)
                borderOutline.effectColor = rarityColor;

            if (iconImage != null)
            {
                iconImage.sprite = definition.Icon;
                iconImage.enabled = definition.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = definition.DisplayName;
                nameText.color = rarityColor;
            }

            if (amountText != null)
            {
                bool showAmount = stack.Amount > 1;
                amountText.gameObject.SetActive(showAmount);
                amountText.text = showAmount ? $"x{stack.Amount}" : string.Empty;
                amountText.color = Color.Lerp(Color.black, rarityColor, amountTextBrightness);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (target == null || !target.IsLabelVisible)
                return;

            Clicked?.Invoke(target);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                return;
            }

            gameObject.SetActive(visible);
        }

        private void ShowTooltip()
        {
            if (tooltipController == null || target == null || target.Stack == null)
                return;

            ItemTooltipData data = ItemTooltipBuilder.Build(target.Stack);
            if (data != null)
                tooltipController.Show(data, Root);
        }

        private void HideTooltip()
        {
            tooltipController?.Hide();
        }

        private void EnsureRaycastSetup()
        {
            if (backgroundImage != null)
                backgroundImage.raycastTarget = true;

            if (iconImage != null)
                iconImage.raycastTarget = false;

            if (nameText != null)
                nameText.raycastTarget = false;

            if (amountText != null)
                amountText.raycastTarget = false;
        }

        private void Unsubscribe()
        {
            if (target != null)
                target.Changed -= Refresh;
        }
    }
}
