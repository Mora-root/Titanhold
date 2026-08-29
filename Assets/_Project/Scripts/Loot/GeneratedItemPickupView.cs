using UnityEngine;

public sealed class GeneratedItemPickupView : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerInventoryItemStackLootReward reward;
    [SerializeField] private bool refreshOnStart = true;

    [Header("World Visual")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private GameObject fallbackVisual;
    [SerializeField] private Renderer[] rarityTintRenderers;
    [SerializeField] private Light rarityLight;

    private GameObject spawnedModel;
    private GameObject spawnedEffect;
    private MaterialPropertyBlock propertyBlock;

    private void Start()
    {
        if (refreshOnStart)
            Refresh();
    }

    private void OnDestroy()
    {
        ClearSpawned();
    }

    public void Refresh()
    {
        reward ??= GetComponent<PlayerInventoryItemStackLootReward>();

        ItemStack stack = reward != null ? reward.Stack : null;
        ItemDefinition definition = stack != null ? stack.Definition : null;

        if (definition == null)
        {
            Clear();
            return;
        }

        ApplyModel(definition);
        ApplyRarity(definition);
    }

    public void Clear()
    {
        ClearSpawned();

        if (fallbackVisual != null)
            fallbackVisual.SetActive(true);
    }

    private void ApplyModel(ItemDefinition definition)
    {
        ClearSpawned();

        Transform parent = modelRoot != null ? modelRoot : transform;
        GameObject modelPrefab = definition.WorldPickupVisualPrefab;

        if (fallbackVisual != null)
            fallbackVisual.SetActive(modelPrefab == null);

        if (modelPrefab != null)
            spawnedModel = Instantiate(modelPrefab, parent);

        GameObject effectPrefab = definition.WorldPickupEffectPrefab;
        if (effectPrefab != null)
            spawnedEffect = Instantiate(effectPrefab, parent);
    }

    private void ApplyRarity(ItemDefinition definition)
    {
        Color rarityColor = definition.PickupLabelColor;

        if (rarityLight != null)
            rarityLight.color = rarityColor;

        if (rarityTintRenderers == null)
            return;

        for (int i = 0; i < rarityTintRenderers.Length; i++)
            ApplyRendererTint(rarityTintRenderers[i], rarityColor);
    }

    private void ApplyRendererTint(Renderer targetRenderer, Color color)
    {
        if (targetRenderer == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ClearSpawned()
    {
        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
            spawnedModel = null;
        }

        if (spawnedEffect != null)
        {
            Destroy(spawnedEffect);
            spawnedEffect = null;
        }
    }
}
