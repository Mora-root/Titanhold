using System.Collections.Generic;
using System;
using TMPro;
using Titanhold.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Loot
{
    public sealed class LootLabelManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform labelsRoot;
        [SerializeField] private LootLabelView labelPrefab;
        [SerializeField] private ItemTooltipController tooltipController;

        [Header("Fallback")]
        [SerializeField] private bool useMainCameraFallback = true;
        [SerializeField] private bool createFallbackLabelView = true;

        [Header("Layout")]
        [SerializeField] private bool hideOffscreen = true;
        [SerializeField] private float overlapHorizontalRange = 120f;
        [SerializeField] private float overlapVerticalSpacing = 28f;
        [SerializeField] private int overlapResolveIterations = 8;

        private readonly Dictionary<LootLabelTarget, LootLabelView> labels = new();
        private readonly List<LootLabelTarget> targetsToRemove = new();
        private readonly List<Vector2> occupiedPositions = new();
        private Canvas rootCanvas;

        public event Action<LootLabelTarget> LabelClicked;

        private void Awake()
        {
            labelsRoot ??= transform as RectTransform;
            rootCanvas = GetComponentInParent<Canvas>();
            ResolveTooltipController();
        }

        private void OnEnable()
        {
            LootLabelTarget.Registered += HandleTargetRegistered;
            LootLabelTarget.Unregistered += HandleTargetUnregistered;
        }

        private void OnDisable()
        {
            LootLabelTarget.Registered -= HandleTargetRegistered;
            LootLabelTarget.Unregistered -= HandleTargetUnregistered;
            ClearAllLabels();
        }

        private void LateUpdate()
        {
            Camera cameraToUse = ResolveCamera();
            if (cameraToUse == null || labelsRoot == null)
                return;

            occupiedPositions.Clear();
            targetsToRemove.Clear();

            foreach (KeyValuePair<LootLabelTarget, LootLabelView> pair in labels)
            {
                LootLabelTarget target = pair.Key;
                LootLabelView view = pair.Value;

                if (target == null || view == null)
                {
                    targetsToRemove.Add(target);
                    continue;
                }

                UpdateLabelPosition(cameraToUse, target, view);
            }

            for (int i = 0; i < targetsToRemove.Count; i++)
                RemoveLabel(targetsToRemove[i]);
        }

        public void Register(LootLabelTarget target)
        {
            HandleTargetRegistered(target);
        }

        public void Unregister(LootLabelTarget target)
        {
            HandleTargetUnregistered(target);
        }

        private void HandleTargetRegistered(LootLabelTarget target)
        {
            if (target == null || labels.ContainsKey(target))
                return;

            LootLabelView view = CreateLabelView();
            if (view == null)
                return;

            labels[target] = view;
            view.Clicked += HandleLabelClicked;
            view.SetTarget(target, tooltipController);
        }

        private void HandleTargetUnregistered(LootLabelTarget target)
        {
            RemoveLabel(target);
        }

        private void UpdateLabelPosition(Camera cameraToUse, LootLabelTarget target, LootLabelView view)
        {
            Vector3 screenPosition = cameraToUse.WorldToScreenPoint(target.LabelWorldPosition);
            bool visible = target.IsLabelVisible && screenPosition.z > 0f;

            if (hideOffscreen)
            {
                visible &= screenPosition.x >= 0f &&
                           screenPosition.x <= Screen.width &&
                           screenPosition.y >= 0f &&
                           screenPosition.y <= Screen.height;
            }

            if (!visible)
            {
                view.SetWorldVisible(false);
                return;
            }

            if (!view.gameObject.activeSelf)
                view.gameObject.SetActive(true);

            view.SetWorldVisible(true);

            Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    labelsRoot,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPosition))
            {
                view.SetWorldVisible(false);
                return;
            }

            localPosition = ResolveOverlap(localPosition);
            view.SetAnchoredPosition(localPosition);
        }

        private Vector2 ResolveOverlap(Vector2 position)
        {
            for (int iteration = 0; iteration < overlapResolveIterations; iteration++)
            {
                bool shifted = false;

                for (int i = 0; i < occupiedPositions.Count; i++)
                {
                    Vector2 occupied = occupiedPositions[i];
                    if (Mathf.Abs(position.x - occupied.x) > overlapHorizontalRange)
                        continue;

                    if (Mathf.Abs(position.y - occupied.y) > overlapVerticalSpacing)
                        continue;

                    position.y = occupied.y + overlapVerticalSpacing;
                    shifted = true;
                }

                if (!shifted)
                    break;
            }

            occupiedPositions.Add(position);
            return position;
        }

        private LootLabelView CreateLabelView()
        {
            if (labelPrefab != null)
                return Instantiate(labelPrefab, labelsRoot);

            if (!createFallbackLabelView)
                return null;

            return CreateFallbackLabelView();
        }

        private LootLabelView CreateFallbackLabelView()
        {
            if (labelsRoot == null)
                return null;

            GameObject root = new("LootLabelView", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(LootLabelView));
            root.transform.SetParent(labelsRoot, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(220f, 30f);

            Image background = root.GetComponent<Image>();
            background.color = Color.black;
            background.raycastTarget = true;

            Outline border = root.AddComponent<Outline>();
            border.effectDistance = new Vector2(2f, -2f);

            TMP_Text nameText = CreateText(root.transform, "NameText", new Vector2(10f, 0f), TextAlignmentOptions.Left);
            TMP_Text amountText = CreateText(root.transform, "AmountText", new Vector2(-10f, 0f), TextAlignmentOptions.Right);

            LootLabelView view = root.GetComponent<LootLabelView>();
            view.ConfigureGeneratedRefs(rootRect, background, border, null, nameText, amountText, root.GetComponent<CanvasGroup>());
            return view;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 horizontalPadding, TextAlignmentOptions alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalPadding.x, 0f);
            rect.offsetMax = new Vector2(horizontalPadding.y, 0f);

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.alignment = alignment;
            text.fontSize = 18f;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private void RemoveLabel(LootLabelTarget target)
        {
            if (target == null)
                return;

            if (!labels.TryGetValue(target, out LootLabelView view))
                return;

            labels.Remove(target);

            if (view != null)
            {
                view.Clicked -= HandleLabelClicked;
                view.ClearTarget();
                Destroy(view.gameObject);
            }
        }

        private void ClearAllLabels()
        {
            foreach (LootLabelView view in labels.Values)
            {
                if (view != null)
                {
                    view.Clicked -= HandleLabelClicked;
                    view.ClearTarget();
                    Destroy(view.gameObject);
                }
            }

            labels.Clear();
        }

        private Camera ResolveCamera()
        {
            if (worldCamera != null)
                return worldCamera;

            return useMainCameraFallback ? Camera.main : null;
        }

        private void ResolveTooltipController()
        {
            if (tooltipController != null)
                return;

            tooltipController = GetComponentInParent<ItemTooltipController>(true);
            if (tooltipController != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>(true);
            if (canvas != null)
                tooltipController = canvas.GetComponentInChildren<ItemTooltipController>(true);
        }

        private void HandleLabelClicked(LootLabelTarget target)
        {
            LabelClicked?.Invoke(target);
        }
    }
}
