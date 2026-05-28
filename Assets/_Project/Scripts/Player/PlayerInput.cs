using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;

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

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        LeftClicked = false;
        RightClicked = Input.GetMouseButtonDown(1);
        Skill1Pressed = Input.GetKeyDown(KeyCode.Alpha1);

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
}
