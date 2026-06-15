using TMPro;
using Titanhold.UI.Loot;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class LootLabelPrefabBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/LootLabelView.prefab";

    [MenuItem("Tools/Titanhold/Create Loot Label View Prefab")]
    public static void CreatePrefab()
    {
        GameObject root = new("LootLabelView");

        try
        {
            LootLabelView view = Build(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Selection.activeObject = prefab;
            Debug.Log($"Loot label view prefab created at {PrefabPath}.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static LootLabelView Build(GameObject root)
    {
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(260f, 34f);

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

        Image background = root.AddComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        Outline outline = root.AddComponent<Outline>();
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        LootLabelView view = root.AddComponent<LootLabelView>();

        Image icon = CreateIcon(root.transform);
        TMP_Text nameText = CreateText(root.transform, "NameText", TextAlignmentOptions.Left, 18f);
        TMP_Text amountText = CreateText(root.transform, "AmountText", TextAlignmentOptions.Right, 16f);

        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(38f, 0f);
        nameRect.offsetMax = new Vector2(-54f, 0f);

        RectTransform amountRect = amountText.rectTransform;
        amountRect.anchorMin = new Vector2(1f, 0f);
        amountRect.anchorMax = new Vector2(1f, 1f);
        amountRect.pivot = new Vector2(1f, 0.5f);
        amountRect.sizeDelta = new Vector2(46f, 0f);
        amountRect.anchoredPosition = new Vector2(-8f, 0f);

        view.ConfigureGeneratedRefs(rootRect, background, outline, icon, nameText, amountText, canvasGroup);
        return view;
    }

    private static Image CreateIcon(Transform parent)
    {
        GameObject iconObject = new("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(24f, 24f);
        rect.anchoredPosition = new Vector2(18f, 0f);

        Image image = iconObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, TextAlignmentOptions alignment, float fontSize)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }
}
