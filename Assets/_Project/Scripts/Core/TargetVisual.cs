using UnityEngine;

/// <summary>
/// It is responsible for highlighting the target when pointing and displaying the effect when the target is selected
/// </summary>
public class TargetVisual : MonoBehaviour
{
    [SerializeField] private GameObject selectedCircle;

    private Renderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (selectedCircle != null)
            selectedCircle.SetActive(false);
    }

    public void SetHover(bool value)
    {
        // Highlighting
        foreach (var r in renderers)
        {
            if (value)
                r.material.EnableKeyword("_EMISSION");
            else
                r.material.DisableKeyword("_EMISSION");
        }
    }

    public void SetSelected(bool value)
    {
        // Displaying the effect
        if (selectedCircle != null)
            selectedCircle.SetActive(value);
    }
}
