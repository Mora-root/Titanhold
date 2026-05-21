using UnityEngine;

/// <summary>
/// Component for highlighting targets when pointing
/// </summary>
public class HoverSystem : MonoBehaviour
{
    [SerializeField] private LayerMask hoverMask;

    private IHoverable currentHover;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, hoverMask))
        {
            var hover = hit.collider.GetComponentInParent<IHoverable>();

            if (hover != currentHover)
            {
                currentHover?.OnHoverExit();

                currentHover = hover;
                currentHover?.OnHoverEnter();
            }
        }
        else
        {
            ClearHover();
        }
    }

    private void ClearHover()
    {
        if (currentHover == null) return;

        currentHover.OnHoverExit();
        currentHover = null;
    }
}
