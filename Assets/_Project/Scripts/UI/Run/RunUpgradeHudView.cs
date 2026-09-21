using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunUpgradeHudView : MonoBehaviour
    {
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private TMP_Text rowTemplate;

        private readonly List<TMP_Text> rows = new();

        public bool HasRequiredReferences =>
            contentRoot != null &&
            titleText != null &&
            rowsRoot != null &&
            rowTemplate != null &&
            rowTemplate.transform.IsChildOf(rowsRoot);
        public GameObject ContentRoot => contentRoot;
        public TMP_Text TitleText => titleText;
        public RectTransform RowsRoot => rowsRoot;
        public TMP_Text RowTemplate => rowTemplate;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            GameObject configuredContentRoot,
            TMP_Text configuredTitleText,
            RectTransform configuredRowsRoot,
            TMP_Text configuredRowTemplate)
        {
            contentRoot = configuredContentRoot;
            titleText = configuredTitleText;
            rowsRoot = configuredRowsRoot;
            rowTemplate = configuredRowTemplate;
        }
#endif

        private void Awake()
        {
            if (rowTemplate != null)
                rowTemplate.gameObject.SetActive(false);

            Clear();
        }

        public bool Render(RunUpgradeHudModel model)
        {
            if (!HasRequiredReferences || model == null)
                return false;

            titleText.text = "RUN UPGRADES";
            EnsureRowCount(model.Entries.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                bool isVisible = i < model.Entries.Count;
                rows[i].gameObject.SetActive(isVisible);
                if (!isVisible)
                    continue;

                RunUpgradeHudEntry entry = model.Entries[i];
                rows[i].text = entry.StackCount > 1
                    ? $"{entry.DisplayName} ×{entry.StackCount}"
                    : entry.DisplayName;
            }

            contentRoot.SetActive(model.Entries.Count > 0);
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                    rows[i].gameObject.SetActive(false);
            }

            if (contentRoot != null)
                contentRoot.SetActive(false);
        }

        private void EnsureRowCount(int requiredCount)
        {
            if (rowTemplate == null || rowsRoot == null)
                return;

            while (rows.Count < requiredCount)
            {
                TMP_Text row = Instantiate(rowTemplate, rowsRoot);
                row.name = $"UpgradeRow_{rows.Count + 1}";
                row.gameObject.SetActive(false);
                rows.Add(row);
            }
        }
    }
}
