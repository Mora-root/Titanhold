using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask enemyMask;

    public Vector3 TargetPosition { get; private set; }
    public Enemy TargetEnemy { get; private set; }

    public bool HasPosition { get; private set; }
    public bool HasEnemy => TargetEnemy != null && TargetEnemy.IsTargetable;

    public bool IsHoldingMouse { get; private set; }

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        IsHoldingMouse = Input.GetMouseButton(0);

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // 1. Enemy
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, enemyMask))
            {
                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.IsTargetable)
                {
                    TargetEnemy = enemy;
                    HasPosition = false;
                    return;
                }
            }

            // 2. Ground
            if (Physics.Raycast(ray, out hit, 200f, groundMask))
            {
                TargetPosition = hit.point;
                HasPosition = true;
                TargetEnemy = null;
            }
        }

        // Holding = обновляем позицию
        if (IsHoldingMouse && TargetEnemy == null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
            {
                TargetPosition = hit.point;
                HasPosition = true;
            }
        }
    }

    public void ClearAll()
    {
        TargetEnemy = null;
        HasPosition = false;
    }
}
