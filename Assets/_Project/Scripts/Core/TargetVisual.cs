using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// It is responsible for highlighting the target when pointing and displaying the effect when the target is selected
/// </summary>
public class TargetVisual : MonoBehaviour
{
    private static readonly int BaseColorProperty =
        Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorProperty =
        Shader.PropertyToID("_Color");

    [SerializeField] private GameObject selectedCircle;
    [SerializeField] private Color hoverEmissionColor = new Color(77f / 255f, 75f / 255f, 75f / 255f);
    [SerializeField] private float hoverEmissionIntensity = 0.5f;

    private MaterialPropertyBlock propertyBlock;
    private HoverMaterial[] hoverMaterials;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        hoverMaterials = CollectHoverMaterials();

        if (selectedCircle != null)
            selectedCircle.SetActive(false);
    }

    private void OnDisable()
    {
        SetHover(false);
    }

    public void SetHover(bool value)
    {
        if (propertyBlock == null || hoverMaterials == null)
            return;

        Color hoverContribution =
            hoverEmissionColor * Mathf.Max(0f, hoverEmissionIntensity);
        for (int i = 0; i < hoverMaterials.Length; i++)
        {
            HoverMaterial hoverMaterial = hoverMaterials[i];
            if (hoverMaterial.Renderer == null)
                continue;

            Color color = value
                ? AddRgb(hoverMaterial.BaseColor, hoverContribution)
                : hoverMaterial.BaseColor;
            propertyBlock.Clear();
            hoverMaterial.Renderer.GetPropertyBlock(
                propertyBlock,
                hoverMaterial.MaterialIndex);
            propertyBlock.SetColor(hoverMaterial.ColorProperty, color);
            hoverMaterial.Renderer.SetPropertyBlock(
                propertyBlock,
                hoverMaterial.MaterialIndex);
        }
    }

    public void SetSelected(bool value)
    {
        if (selectedCircle != null)
            selectedCircle.SetActive(value);
    }

    private HoverMaterial[] CollectHoverMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        List<HoverMaterial> collected = new();

        for (int rendererIndex = 0;
             rendererIndex < renderers.Length;
             rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            Material[] sharedMaterials = renderer.sharedMaterials;
            for (int materialIndex = 0;
                 materialIndex < sharedMaterials.Length;
                 materialIndex++)
            {
                Material material = sharedMaterials[materialIndex];
                if (material == null)
                    continue;

                int colorProperty = material.HasProperty(BaseColorProperty)
                    ? BaseColorProperty
                    : material.HasProperty(LegacyColorProperty)
                        ? LegacyColorProperty
                        : -1;
                if (colorProperty < 0)
                    continue;

                collected.Add(new HoverMaterial(
                    renderer,
                    materialIndex,
                    colorProperty,
                    material.GetColor(colorProperty)));
            }
        }

        return collected.ToArray();
    }

    private static Color AddRgb(Color baseColor, Color contribution)
    {
        return new Color(
            baseColor.r + contribution.r,
            baseColor.g + contribution.g,
            baseColor.b + contribution.b,
            baseColor.a);
    }

    private readonly struct HoverMaterial
    {
        public HoverMaterial(
            Renderer renderer,
            int materialIndex,
            int colorProperty,
            Color baseColor)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
            ColorProperty = colorProperty;
            BaseColor = baseColor;
        }

        public Renderer Renderer { get; }
        public int MaterialIndex { get; }
        public int ColorProperty { get; }
        public Color BaseColor { get; }
    }
}
