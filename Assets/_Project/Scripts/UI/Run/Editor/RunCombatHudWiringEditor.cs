using System;
using System.Collections.Generic;
using System.Linq;
using Titanhold.Session;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.UI.Run.Editor
{
    public static class RunCombatHudWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string SkillBarName = "SkillBar";
        private const string ResourceReadoutName =
            "RunCombatResourceReadout";
        private const int AbilitySlotCount = 5;

        [MenuItem("Tools/Titanhold/Install Run Combat HUD Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

                GameObject skillBar = FindUniqueGameObject(SkillBarName);
                RunSceneSessionEntryPoint entryPoint =
                    UnityEngine.Object.FindAnyObjectByType<
                        RunSceneSessionEntryPoint>(
                        FindObjectsInactive.Include);
                if (entryPoint == null ||
                    entryPoint.Participants.Count != 1 ||
                    entryPoint.Participants[0] == null ||
                    !entryPoint.Participants[0].IsValid)
                {
                    throw new InvalidOperationException(
                        "Run combat HUD requires one valid local participant binding.");
                }

                if (skillBar.transform.childCount < AbilitySlotCount)
                {
                    throw new InvalidOperationException(
                        $"'{SkillBarName}' has fewer than {AbilitySlotCount} reusable slots.");
                }

                RunAbilitySlotView[] slots =
                    new RunAbilitySlotView[AbilitySlotCount];
                for (int slotIndex = 0;
                     slotIndex < AbilitySlotCount;
                     slotIndex++)
                {
                    slots[slotIndex] = ConfigureAbilitySlot(
                        skillBar.transform.GetChild(slotIndex),
                        slotIndex);
                }

                TMP_Text resourceText = ConfigureResourceReadout(
                    skillBar.transform);
                RunCombatHudView view =
                    skillBar.GetComponent<RunCombatHudView>() ??
                    skillBar.AddComponent<RunCombatHudView>();
                RunCombatHudPresenter presenter =
                    skillBar.GetComponent<RunCombatHudPresenter>() ??
                    skillBar.AddComponent<RunCombatHudPresenter>();
                string playerId = entryPoint.Participants[0].PlayerId;
                view.ConfigureForEditor(slots, resourceText);
                presenter.ConfigureForEditor(
                    entryPoint,
                    view,
                    playerId,
                    "resource:rage",
                    "Rage");

                EditorUtility.SetDirty(view);
                EditorUtility.SetDirty(presenter);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save run scene.");

                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("Run Combat HUD wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Combat HUD wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Combat HUD Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Run Combat HUD wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Combat HUD wiring validation failed: {exception}");
            }
        }

        private static RunAbilitySlotView ConfigureAbilitySlot(
            Transform slotTransform,
            int slotIndex)
        {
            RunAbilitySlotView slot =
                slotTransform.GetComponent<RunAbilitySlotView>() ??
                slotTransform.gameObject.AddComponent<RunAbilitySlotView>();
            TMP_Text keyText = FindDirectChild<TMP_Text>(
                slotTransform,
                "KeyText");
            if (keyText == null)
            {
                keyText = CreateText(
                    slotTransform,
                    "KeyText",
                    (slotIndex + 1).ToString(),
                    11f,
                    TextAlignmentOptions.BottomRight);
                RectTransform keyRect = (RectTransform)keyText.transform;
                Stretch(keyRect, new Vector2(2f, 2f));
            }

            keyText.text = (slotIndex + 1).ToString();
            keyText.raycastTarget = false;
            keyText.transform.SetAsLastSibling();

            Image icon = GetOrCreateImage(
                slotTransform,
                "AbilityIcon");
            Stretch((RectTransform)icon.transform, new Vector2(3f, 3f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            icon.transform.SetAsFirstSibling();

            Image cooldownOverlay = GetOrCreateImage(
                slotTransform,
                "CooldownOverlay");
            Stretch(
                (RectTransform)cooldownOverlay.transform,
                new Vector2(2f, 2f));
            cooldownOverlay.sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");
            cooldownOverlay.color = new Color(0f, 0f, 0f, 0.72f);
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = 2;
            cooldownOverlay.fillClockwise = true;
            cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.raycastTarget = false;
            cooldownOverlay.gameObject.SetActive(false);
            cooldownOverlay.transform.SetSiblingIndex(1);

            TMP_Text abilityName = GetOrCreateText(
                slotTransform,
                "AbilityName",
                9f,
                TextAlignmentOptions.Center);
            RectTransform nameRect = (RectTransform)abilityName.transform;
            nameRect.anchorMin = new Vector2(0.05f, 0.18f);
            nameRect.anchorMax = new Vector2(0.95f, 0.86f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            abilityName.enableAutoSizing = true;
            abilityName.fontSizeMin = 6f;
            abilityName.fontSizeMax = 9f;
            abilityName.textWrappingMode = TextWrappingModes.Normal;
            abilityName.overflowMode = TextOverflowModes.Ellipsis;
            abilityName.raycastTarget = false;

            TMP_Text cooldownText = GetOrCreateText(
                slotTransform,
                "CooldownText",
                17f,
                TextAlignmentOptions.Center);
            Stretch(
                (RectTransform)cooldownText.transform,
                new Vector2(2f, 2f));
            cooldownText.fontStyle = FontStyles.Bold;
            cooldownText.raycastTarget = false;
            cooldownText.gameObject.SetActive(false);

            keyText.transform.SetAsLastSibling();
            cooldownText.transform.SetAsLastSibling();
            slot.ConfigureForEditor(
                icon,
                cooldownOverlay,
                keyText,
                abilityName,
                cooldownText);
            EditorUtility.SetDirty(slot);
            return slot;
        }

        private static TMP_Text ConfigureResourceReadout(Transform skillBar)
        {
            Transform existing = skillBar.Find(ResourceReadoutName);
            GameObject root;
            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                root = CreateUiObject(ResourceReadoutName, skillBar);
                Image background = root.AddComponent<Image>();
                background.color =
                    new Color(0.137f, 0.137f, 0.137f, 0.86f);
                background.raycastTarget = false;
                LayoutElement layout = root.AddComponent<LayoutElement>();
                layout.preferredWidth = 86f;
                layout.preferredHeight = 44f;
            }

            TMP_Text text = GetOrCreateText(
                root.transform,
                "Value",
                12f,
                TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform, new Vector2(4f, 2f));
            text.enableAutoSizing = true;
            text.fontSizeMin = 8f;
            text.fontSizeMax = 12f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.text = "RAGE --";
            return text;
        }

        private static void ValidateInternal()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open '{ScenePath}'.");

            GameObject skillBar = FindUniqueGameObject(SkillBarName);
            RunCombatHudView view =
                skillBar.GetComponent<RunCombatHudView>();
            RunCombatHudPresenter presenter =
                skillBar.GetComponent<RunCombatHudPresenter>();
            RunSceneSessionEntryPoint entryPoint =
                UnityEngine.Object.FindAnyObjectByType<
                    RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            if (view == null ||
                presenter == null ||
                presenter.View != view ||
                presenter.SessionEntryPoint != entryPoint ||
                entryPoint == null ||
                entryPoint.Participants.Count != 1 ||
                presenter.PlayerId != entryPoint.Participants[0].PlayerId ||
                presenter.CombatResourceId != "resource:rage" ||
                view.AbilitySlotCount != AbilitySlotCount ||
                view.CombatResourceText == null)
            {
                throw new InvalidOperationException(
                    "Run Combat HUD root wiring is invalid.");
            }

            HashSet<RunAbilitySlotView> uniqueSlots = new();
            for (int i = 0; i < view.AbilitySlotCount; i++)
            {
                RunAbilitySlotView slot = view.AbilitySlots[i];
                if (slot == null ||
                    !uniqueSlots.Add(slot) ||
                    slot.transform.parent != skillBar.transform ||
                    slot.IconImage == null ||
                    slot.CooldownOverlay == null ||
                    slot.KeyText == null ||
                    slot.KeyText.text != (i + 1).ToString() ||
                    slot.AbilityNameText == null ||
                    slot.CooldownText == null)
                {
                    throw new InvalidOperationException(
                        $"Run ability HUD slot {i} is invalid.");
                }
            }

            for (int i = AbilitySlotCount;
                 i < Mathf.Min(8, skillBar.transform.childCount);
                 i++)
            {
                if (skillBar.transform.GetChild(i)
                    .GetComponent<RunAbilitySlotView>() != null)
                {
                    throw new InvalidOperationException(
                        "Reserved non-ability SkillBar slots were modified.");
                }
            }

            Transform resourceReadout =
                skillBar.transform.Find(ResourceReadoutName);
            if (resourceReadout == null ||
                !view.CombatResourceText.transform.IsChildOf(resourceReadout))
            {
                throw new InvalidOperationException(
                    "Run combat resource readout is missing.");
            }
        }

        private static GameObject FindUniqueGameObject(string name)
        {
            GameObject[] matches = UnityEngine.Object
                .FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include)
                .Where(candidate => candidate.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one '{name}' object, found {matches.Length}.");
            }

            return matches[0];
        }

        private static T FindDirectChild<T>(
            Transform parent,
            string name)
            where T : Component
        {
            Transform child = parent.Find(name);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Image GetOrCreateImage(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<Image>() ??
                       existing.gameObject.AddComponent<Image>();

            return CreateUiObject(name, parent).AddComponent<Image>();
        }

        private static TMP_Text GetOrCreateText(
            Transform parent,
            string name,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            TMP_Text existing = FindDirectChild<TMP_Text>(parent, name);
            return existing ?? CreateText(
                parent,
                name,
                string.Empty,
                fontSize,
                alignment);
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string content,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUiObject(name, parent);
            TextMeshProUGUI text =
                textObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(
            string name,
            Transform parent)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.layer = LayerMask.NameToLayer("UI");
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before Run Combat HUD {operation}.");
            }
        }

        private static void RequireCleanOpenScene()
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.IsValid() && current.isDirty)
            {
                throw new InvalidOperationException(
                    $"Save the currently open scene '{current.path}' first.");
            }
        }
    }
}
