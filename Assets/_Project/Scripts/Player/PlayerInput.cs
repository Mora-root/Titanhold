using UnityEngine;
using UnityEngine.EventSystems;
using Titanhold.UI.SectionInventory;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private ItemDragContext itemDragContext;

    public Vector3 TargetPosition { get; private set; }
    public bool HasPosition { get; private set; }
    public bool LeftClicked { get; private set; }
    public bool RightClicked { get; private set; }
    public bool IsDragging { get; private set; }
    public bool IsHolding { get; private set; }
    public bool Skill1Pressed { get; private set; }
    public PlayerInputIntent CurrentIntent { get; private set; }

    private Camera cam;
    private float holdTimer;
    [SerializeField] private float dragThreshold = 0.25f;

    [Header("Right Click Gesture")]
    [SerializeField, Min(0f)] private float rightClickHoldThreshold = 0.3f;
    [SerializeField, Min(0f)] private float rightClickDragThresholdPixels = 8f;

    private bool rightPressActive;
    private bool rightPressBecameCameraRotation;
    private float rightHoldTimer;
    private Vector3 rightPressScreenPosition;

    private void Awake()
    {
        cam = Camera.main;
    }

    public void SetItemDragContext(ItemDragContext context)
    {
        itemDragContext = context;
    }

    private void Update()
    {
        if (itemDragContext != null && itemDragContext.IsDragging)
        {
            ClearFrameInput();
            CurrentIntent = PlayerInputIntent.Empty;
            return;
        }

        LeftClicked = false;
        RightClicked = false;
        Skill1Pressed = Input.GetKeyDown(KeyCode.Alpha1);

        if (IsPointerOverUi())
        {
            LeftClicked = false;
            RightClicked = false;
            Skill1Pressed = false;
            IsHolding = false;
            IsDragging = false;
            CancelRightClickGesture();
            RefreshCurrentIntent();
            return;
        }

        UpdateRightClickGesture();

        if (Input.GetMouseButtonDown(0))
        {
            holdTimer = 0f;
            IsDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            IsHolding = true;

            holdTimer += Time.deltaTime;

            if (holdTimer > dragThreshold)
            {
                IsDragging = true;
            }

            UpdateMousePosition();
        }
        else
        {
            IsHolding = false;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!IsDragging)
            {
                LeftClicked = true;
            }

            IsDragging = false;
        }
        RefreshCurrentIntent();
    }

    private void UpdateRightClickGesture()
    {
        if (Input.GetMouseButtonDown(1))
        {
            rightPressActive = true;
            rightPressBecameCameraRotation = false;
            rightHoldTimer = 0f;
            rightPressScreenPosition = Input.mousePosition;
        }

        if (rightPressActive && Input.GetMouseButton(1))
        {
            rightHoldTimer += Time.deltaTime;
            float dragDistance = Vector3.Distance(
                rightPressScreenPosition,
                Input.mousePosition);
            if (rightHoldTimer > rightClickHoldThreshold ||
                dragDistance > rightClickDragThresholdPixels)
            {
                rightPressBecameCameraRotation = true;
            }
        }

        if (!rightPressActive || !Input.GetMouseButtonUp(1))
            return;

        float releaseDragDistance = Vector3.Distance(
            rightPressScreenPosition,
            Input.mousePosition);
        RightClicked = !rightPressBecameCameraRotation &&
                       rightHoldTimer <= rightClickHoldThreshold &&
                       releaseDragDistance <= rightClickDragThresholdPixels;
        CancelRightClickGesture();
    }

    private void UpdateMousePosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
        {
            TargetPosition = hit.point;
            HasPosition = true;
        }
    }

    public void ClearAll()
    {
        HasPosition = false;
        RefreshCurrentIntent();
    }

    private void ClearFrameInput()
    {
        TargetPosition = default;
        HasPosition = false;
        LeftClicked = false;
        RightClicked = false;
        IsDragging = false;
        IsHolding = false;
        Skill1Pressed = false;
        holdTimer = 0f;
        CancelRightClickGesture();
    }

    private void CancelRightClickGesture()
    {
        rightPressActive = false;
        rightPressBecameCameraRotation = false;
        rightHoldTimer = 0f;
        rightPressScreenPosition = default;
    }

    private bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void RefreshCurrentIntent()
    {
        CurrentIntent = new PlayerInputIntent(
            TargetPosition,
            HasPosition,
            LeftClicked,
            RightClicked,
            IsDragging,
            IsHolding,
            Skill1Pressed
        );
    }

    private void OnValidate()
    {
        dragThreshold = Mathf.Max(0f, dragThreshold);
        rightClickHoldThreshold = Mathf.Max(0f, rightClickHoldThreshold);
        rightClickDragThresholdPixels = Mathf.Max(
            0f,
            rightClickDragThresholdPixels);
    }
}
