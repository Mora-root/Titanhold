using UnityEngine;

/// <summary>
/// It is responsible for highlighting the target when pointing and displaying the effect when the target is selected
/// </summary>
public class TargetVisual : MonoBehaviour
{
    [SerializeField] private GameObject selectedCircle;
    [SerializeField] private Color hoverEmissionColor = new Color(77f / 255f, 75f / 255f, 75f / 255f);
    [SerializeField] private float hoverEmissionIntensity = 0.5f;

    private Renderer[] renderers;
    private Material[] materials;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        materials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            materials[i] = renderers[i].material;

        if (selectedCircle != null)
            selectedCircle.SetActive(false);
    }

    public void SetHover(bool value)
    {
        // Highlighting
        foreach (var material in materials)
        {
            if (material == null)
                continue;

            if (value)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", hoverEmissionColor * hoverEmissionIntensity);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
        }
    }

    public void SetSelected(bool value)
    {
        // Displaying the effect
        if (selectedCircle != null)
            selectedCircle.SetActive(value);
    }
}
