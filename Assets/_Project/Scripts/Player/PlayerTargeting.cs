using UnityEngine;

public class PlayerTargeting : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float maxDistance = 100f;

    public ITargetable CurrentTarget { get; private set; }

    public void TrySelectTarget()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetMask))
        {
            var target = hit.collider.GetComponentInParent<ITargetable>();

            if (target != null && target.IsTargetable)
            {
                CurrentTarget = target;
                return;
            }
        }

        CurrentTarget = null;
    }

    public void ClearTarget()
    {
        CurrentTarget = null;
    }
}
