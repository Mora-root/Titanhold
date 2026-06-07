using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Common
{
    public sealed class ItemTooltipController : MonoBehaviour
    {
        [SerializeField] private ItemTooltipView tooltipView;
        [SerializeField] private RectTransform tooltipRoot;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private float padding = 8f;

        private readonly Vector3[] sourceCorners = new Vector3[4];
        private CanvasGroup canvasGroup;

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            if (tooltipRoot == null)
                tooltipRoot = transform as RectTransform;

            if (tooltipView == null)
                tooltipView = GetComponent<ItemTooltipView>();

            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            EnsureCanvasGroup();
            Hide();
        }

        public void Show(ItemTooltipData data, RectTransform sourceRect)
        {
            if (data == null || tooltipView == null || tooltipRoot == null)
            {
                Hide();
                return;
            }

            SetAlpha(0f);
            tooltipView.Render(data);
            tooltipRoot.gameObject.SetActive(true);
            IsVisible = true;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
            Canvas.ForceUpdateCanvases();
            Position(sourceRect);
            SetAlpha(1f);
        }

        public void Hide()
        {
            IsVisible = false;

            if (tooltipView != null)
                tooltipView.Hide();
            else if (tooltipRoot != null)
                tooltipRoot.gameObject.SetActive(false);

            SetAlpha(0f);
        }

        private void EnsureCanvasGroup()
        {
            if (tooltipRoot == null)
                return;

            canvasGroup = tooltipRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = tooltipRoot.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup == null)
                EnsureCanvasGroup();

            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }

        private void Position(RectTransform sourceRect)
        {
            if (sourceRect == null || rootCanvas == null || !(rootCanvas.transform is RectTransform canvasRect))
                return;

            sourceRect.GetWorldCorners(sourceCorners);

            Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

            Vector2 sourceBottomLeft = WorldCornerToCanvasLocal(sourceCorners[0], canvasRect, eventCamera);
            Vector2 sourceTopLeft = WorldCornerToCanvasLocal(sourceCorners[1], canvasRect, eventCamera);
            Vector2 sourceTopRight = WorldCornerToCanvasLocal(sourceCorners[2], canvasRect, eventCamera);

            Rect canvas = canvasRect.rect;
            Vector2 size = tooltipRoot.rect.size;

            float left = sourceBottomLeft.x - padding - size.x;
            float top = sourceTopLeft.y;

            if (left < canvas.xMin)
                left = sourceTopRight.x + padding;

            top = Mathf.Clamp(top, canvas.yMin + size.y, canvas.yMax);

            if (left + size.x > canvas.xMax)
                left = canvas.xMax - size.x;

            if (left < canvas.xMin)
                left = canvas.xMin;

            Vector2 pivot = tooltipRoot.pivot;
            tooltipRoot.anchoredPosition = new Vector2(
                left + size.x * pivot.x,
                top - size.y * (1f - pivot.y));
        }

        private static Vector2 WorldCornerToCanvasLocal(Vector3 worldCorner, RectTransform canvasRect, Camera eventCamera)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCorner);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return localPoint;
            }

            return canvasRect.InverseTransformPoint(worldCorner);
        }
    }
}
