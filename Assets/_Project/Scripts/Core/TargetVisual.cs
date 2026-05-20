using UnityEngine;

public class TargetVisual : MonoBehaviour
{
    [SerializeField] private GameObject selectedCircle;

    private Renderer[] renderers;

    private bool isSelected;
    private Color originalEmission;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalEmission = renderers[0].material.GetColor("_EmissionColor");

        if (selectedCircle != null)
            selectedCircle.SetActive(false);
    }

    // 🔥 HOVER
    public void SetHover(bool value)
    {
        foreach (var r in renderers)
        {
            if (value)
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", originalEmission);
            }
            else
            {
                r.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    // 🔥 SELECT
    public void SetSelected(bool value)
    {
        isSelected = value;

        if (selectedCircle != null)
            selectedCircle.SetActive(value);
    }
}
