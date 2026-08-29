using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Common
{
    public sealed class ItemDragVisual : MonoBehaviour
    {
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Canvas rootCanvas;

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            if (iconImage != null)
                iconImage.raycastTarget = false;

            if (amountText != null)
                amountText.raycastTarget = false;

            Hide();
        }

        private void Update()
        {
            if (!IsVisible)
                return;

            SetPosition(Input.mousePosition);
        }

        public void Show(Sprite icon, int amount = 1)
        {
            if (visualRoot == null)
                return;

            IsVisible = true;
            visualRoot.gameObject.SetActive(true);

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }

            if (amountText != null)
                amountText.text = amount > 1 ? amount.ToString() : string.Empty;

            SetPosition(Input.mousePosition);
        }

        public void Hide()
        {
            IsVisible = false;

            if (iconImage != null)
            {
                iconImage.sprite = null;
            }

            if (amountText != null)
                amountText.text = string.Empty;

            if (visualRoot != null)
                visualRoot.gameObject.SetActive(false);
        }

        public void SetPosition(Vector2 screenPosition)
        {
            if (visualRoot == null)
                return;

            if (rootCanvas != null && rootCanvas.transform is RectTransform canvasRect)
            {
                Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPosition,
                        eventCamera,
                        out Vector2 localPoint))
                {
                    visualRoot.anchoredPosition = localPoint;
                    return;
                }
            }

            visualRoot.position = screenPosition;
        }
    }
}
