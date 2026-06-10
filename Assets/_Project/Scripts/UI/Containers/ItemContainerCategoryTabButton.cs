using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Containers
{
    public sealed class ItemContainerCategoryTabButton : MonoBehaviour
    {
        [SerializeField] private global::ItemCategory category;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color unselectedColor = new Color(0.75f, 0.75f, 0.75f, 1f);

        private ItemContainerWindow owner;

        public global::ItemCategory Category => category;

        private void Awake()
        {
            button ??= GetComponent<Button>();
            targetGraphic ??= button != null ? button.targetGraphic : null;
            RefreshLabel();
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        private void OnValidate()
        {
            RefreshLabel();
        }

        public void Initialize(ItemContainerWindow window)
        {
            owner = window;
            button ??= GetComponent<Button>();
            targetGraphic ??= button != null ? button.targetGraphic : null;
            RefreshLabel();
        }

        public void SetSelected(bool selected)
        {
            if (selectedRoot != null)
                selectedRoot.SetActive(selected);

            if (targetGraphic != null)
                targetGraphic.color = selected ? selectedColor : unselectedColor;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void HandleClicked()
        {
            owner?.SelectCategory(category);
        }

        private void RefreshLabel()
        {
            if (label != null)
                label.text = category.ToString();
        }
    }
}
