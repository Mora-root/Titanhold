using UnityEngine;

public class PlayerInputAI : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask enemyMask;

    // Для движения (зажатая ЛКМ)
    public Vector3 TargetPosition { get; private set; }
    public bool HasTargetPosition { get; private set; }

    // Для атаки (цель)
    public Enemy TargetEnemy { get; private set; }
    public bool HasAttackTarget => TargetEnemy != null && TargetEnemy.IsTargetable;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 1. Сначала проверяем врага
            if (Physics.Raycast(ray, out hit, 200f, enemyMask))
            {
                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.IsTargetable)
                {
                    TargetEnemy = enemy;
                    HasTargetPosition = false;          // цель атаки, позиция не нужна
                    return;                             // в этом кадре больше ничего
                }
            }

            // 2. Не попали по врагу – идём по земле
            if (Physics.Raycast(ray, out hit, 200f, groundMask))
            {
                TargetPosition = hit.point;
                HasTargetPosition = true;
                TargetEnemy = null;                     // сбрасываем цель атаки
            }
            return;
        }

        // --- Кнопка удерживается (второй и последующие кадры) ---
        if (Input.GetMouseButton(0))
        {
            // Никаких проверок врагов, только обновляем точку ходьбы
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
            {
                TargetPosition = hit.point;
                HasTargetPosition = true;
                // цель атаки НЕ сбрасываем – персонаж продолжит бежать к врагу после отпускания
            }
        }
    }
    public void ClearTargetPosition()
    {
        HasTargetPosition = false;
        TargetPosition = Vector3.zero;
    }
}
    

