using System;
using System.Collections.Generic;
using System.Linq;
using Titanhold.Session;
using Titanhold.UI.Run;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.Combat.Flasks.Editor
{
    public static class FlaskVerticalSliceWiringEditor
    {
        private const string PlayerPrefabPath =
            "Assets/_Project/Prefabs/Player.prefab";
        private const string RunScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string DefinitionFolder =
            "Assets/_Project/ScriptableObjects/Flasks";
        private const string HealthFlaskPath =
            DefinitionFolder + "/HealthFlask.asset";
        private const string ResourceFlaskPath =
            DefinitionFolder + "/PrimaryResourceFlask.asset";
        private const string SkillBarName = "SkillBar";
        private const int HealthSlotChildIndex = 5;
        private const int ResourceSlotChildIndex = 6;

        [MenuItem("Tools/Titanhold/Install Flask Vertical Slice")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                EnsureDefinitionFolder();

                FlaskDefinition health = CreateOrLoad(HealthFlaskPath);
                FlaskDefinition resource = CreateOrLoad(ResourceFlaskPath);
                ConfigureDefinition(
                    health,
                    "flask:health",
                    "Health",
                    "Instantly restores 50% of maximum health.",
                    FlaskRecoveryTarget.Health);
                ConfigureDefinition(
                    resource,
                    "flask:primary-resource",
                    "Resource",
                    "Instantly restores 50% of the class primary resource.",
                    FlaskRecoveryTarget.PrimaryResource);
                AssetDatabase.SaveAssets();

                ConfigurePlayerPrefab(health, resource);
                ConfigureRunScene();
                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("Flask vertical slice installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Flask vertical slice installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Flask Vertical Slice Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Flask vertical slice wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Flask vertical slice wiring validation failed: {exception}");
            }
        }

        private static void EnsureDefinitionFolder()
        {
            if (AssetDatabase.IsValidFolder(DefinitionFolder))
                return;

            string parent = "Assets/_Project/ScriptableObjects";
            string guid = AssetDatabase.CreateFolder(parent, "Flasks");
            if (string.IsNullOrWhiteSpace(guid))
            {
                throw new InvalidOperationException(
                    $"Could not create '{DefinitionFolder}'.");
            }
        }

        private static FlaskDefinition CreateOrLoad(string path)
        {
            FlaskDefinition definition =
                AssetDatabase.LoadAssetAtPath<FlaskDefinition>(path);
            if (definition != null)
                return definition;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                throw new InvalidOperationException(
                    $"Flask path is occupied by another asset: {path}");
            }

            definition = ScriptableObject.CreateInstance<FlaskDefinition>();
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static void ConfigureDefinition(
            FlaskDefinition definition,
            string flaskId,
            string displayName,
            string description,
            FlaskRecoveryTarget target)
        {
            SerializedObject data = new(definition);
            data.FindProperty("flaskId").stringValue = flaskId;
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("recoveryTarget").enumValueIndex = (int)target;
            data.FindProperty("recoveryFraction").floatValue = 0.5f;
            data.FindProperty("cooldown").floatValue = 30f;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void ConfigurePlayerPrefab(
            FlaskDefinition healthDefinition,
            FlaskDefinition resourceDefinition)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Health health = root.GetComponent<Health>();
                PlayerResource resource = root.GetComponent<PlayerResource>();
                PlayerInput input = root.GetComponent<PlayerInput>();
                if (health == null || resource == null || input == null)
                {
                    throw new InvalidOperationException(
                        "Player prefab lacks Health, PlayerResource, or PlayerInput.");
                }

                PlayerFlaskController flasks =
                    root.GetComponent<PlayerFlaskController>() ??
                    root.AddComponent<PlayerFlaskController>();
                PlayerFlaskInputController flaskInput =
                    root.GetComponent<PlayerFlaskInputController>() ??
                    root.AddComponent<PlayerFlaskInputController>();
                flasks.ConfigureForEditor(
                    health,
                    resource,
                    new[] { healthDefinition, resourceDefinition });
                flaskInput.ConfigureForEditor(
                    flasks,
                    input,
                    KeyCode.Q,
                    KeyCode.E);
                EditorUtility.SetDirty(flasks);
                EditorUtility.SetDirty(flaskInput);

                if (PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Could not save Player prefab flask wiring.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureRunScene()
        {
            Scene scene = EditorSceneManager.OpenScene(
                RunScenePath,
                OpenSceneMode.Single);
            GameObject skillBar = FindUniqueGameObject(SkillBarName);
            if (skillBar.transform.childCount <= ResourceSlotChildIndex)
            {
                throw new InvalidOperationException(
                    "SkillBar does not contain the two reserved flask slots.");
            }

            RunSceneSessionEntryPoint entryPoint =
                UnityEngine.Object.FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            if (entryPoint == null ||
                entryPoint.Participants.Count != 1 ||
                entryPoint.Participants[0] == null ||
                !entryPoint.Participants[0].IsValid)
            {
                throw new InvalidOperationException(
                    "Flask HUD requires one valid local participant binding.");
            }

            RunFlaskSlotView[] slots =
            {
                ConfigureSlot(
                    skillBar.transform.GetChild(HealthSlotChildIndex),
                    "Q"),
                ConfigureSlot(
                    skillBar.transform.GetChild(ResourceSlotChildIndex),
                    "E")
            };
            RunFlaskHudView view =
                skillBar.GetComponent<RunFlaskHudView>() ??
                skillBar.AddComponent<RunFlaskHudView>();
            RunFlaskHudPresenter presenter =
                skillBar.GetComponent<RunFlaskHudPresenter>() ??
                skillBar.AddComponent<RunFlaskHudPresenter>();
            RunFlaskHudInputController input =
                skillBar.GetComponent<RunFlaskHudInputController>() ??
                skillBar.AddComponent<RunFlaskHudInputController>();
            string playerId = entryPoint.Participants[0].PlayerId;
            view.ConfigureForEditor(slots);
            presenter.ConfigureForEditor(
                entryPoint,
                view,
                playerId,
                new[] { "Q", "E" });
            input.ConfigureForEditor(entryPoint, view, playerId);
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(input);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save run scene.");
        }

        private static RunFlaskSlotView ConfigureSlot(
            Transform slotTransform,
            string keyLabel)
        {
            Graphic clickTarget = slotTransform.GetComponent<Graphic>();
            if (clickTarget == null)
                throw new InvalidOperationException("Flask slot has no UI graphic.");

            clickTarget.raycastTarget = true;
            RunFlaskSlotView slot =
                slotTransform.GetComponent<RunFlaskSlotView>() ??
                slotTransform.gameObject.AddComponent<RunFlaskSlotView>();
            Image icon = GetOrCreateImage(slotTransform, "FlaskIcon");
            Stretch((RectTransform)icon.transform, new Vector2(3f, 3f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            icon.transform.SetAsFirstSibling();

            Image cooldown = GetOrCreateImage(slotTransform, "FlaskCooldownOverlay");
            Stretch((RectTransform)cooldown.transform, new Vector2(2f, 2f));
            cooldown.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");
            cooldown.color = new Color(0f, 0f, 0f, 0.72f);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = 2;
            cooldown.fillClockwise = true;
            cooldown.fillAmount = 0f;
            cooldown.raycastTarget = false;
            cooldown.gameObject.SetActive(false);
            cooldown.transform.SetSiblingIndex(1);

            TMP_Text name = GetOrCreateText(
                slotTransform,
                "FlaskName",
                9f,
                TextAlignmentOptions.Center);
            RectTransform nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = new Vector2(0.05f, 0.18f);
            nameRect.anchorMax = new Vector2(0.95f, 0.86f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            name.enableAutoSizing = true;
            name.fontSizeMin = 6f;
            name.fontSizeMax = 9f;
            name.textWrappingMode = TextWrappingModes.Normal;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.raycastTarget = false;

            TMP_Text key = GetOrCreateText(
                slotTransform,
                "FlaskKeyText",
                11f,
                TextAlignmentOptions.BottomRight);
            Stretch((RectTransform)key.transform, new Vector2(2f, 2f));
            key.text = keyLabel;
            key.raycastTarget = false;

            TMP_Text cooldownText = GetOrCreateText(
                slotTransform,
                "FlaskCooldownText",
                17f,
                TextAlignmentOptions.Center);
            Stretch((RectTransform)cooldownText.transform, new Vector2(2f, 2f));
            cooldownText.fontStyle = FontStyles.Bold;
            cooldownText.raycastTarget = false;
            cooldownText.gameObject.SetActive(false);
            key.transform.SetAsLastSibling();
            cooldownText.transform.SetAsLastSibling();

            slot.ConfigureForEditor(icon, cooldown, key, name, cooldownText);
            EditorUtility.SetDirty(slot);
            return slot;
        }

        private static void ValidateInternal()
        {
            FlaskDefinition health = RequireAsset<FlaskDefinition>(HealthFlaskPath);
            FlaskDefinition resource = RequireAsset<FlaskDefinition>(ResourceFlaskPath);
            ValidateDefinition(
                health,
                "flask:health",
                FlaskRecoveryTarget.Health);
            ValidateDefinition(
                resource,
                "flask:primary-resource",
                FlaskRecoveryTarget.PrimaryResource);
            ValidatePlayerPrefab(health, resource);

            if (SceneManager.GetActiveScene().path != RunScenePath)
                EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);
            ValidateRunScene();
        }

        private static void ValidateDefinition(
            FlaskDefinition definition,
            string expectedId,
            FlaskRecoveryTarget expectedTarget)
        {
            if (!definition.TryCreateSnapshot(out FlaskDefinitionSnapshot snapshot) ||
                snapshot.FlaskId != expectedId ||
                snapshot.RecoveryTarget != expectedTarget ||
                !Mathf.Approximately(snapshot.RecoveryFraction, 0.5f) ||
                Math.Abs(snapshot.Cooldown - 30d) > 0.000001d)
            {
                throw new InvalidOperationException(
                    $"Flask definition '{expectedId}' is invalid.");
            }
        }

        private static void ValidatePlayerPrefab(
            FlaskDefinition health,
            FlaskDefinition resource)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerFlaskController flasks = root.GetComponent<PlayerFlaskController>();
                PlayerFlaskInputController input =
                    root.GetComponent<PlayerFlaskInputController>();
                if (flasks == null ||
                    input == null ||
                    flasks.Health != root.GetComponent<Health>() ||
                    flasks.PrimaryResource != root.GetComponent<PlayerResource>() ||
                    flasks.EquippedFlasks.Count != 2 ||
                    flasks.EquippedFlasks[0] != health ||
                    flasks.EquippedFlasks[1] != resource ||
                    input.Flasks != flasks ||
                    input.PlayerInput != root.GetComponent<PlayerInput>() ||
                    input.HealthFlaskKey != KeyCode.Q ||
                    input.ResourceFlaskKey != KeyCode.E)
                {
                    throw new InvalidOperationException(
                        "Player prefab flask wiring is invalid.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateRunScene()
        {
            GameObject skillBar = FindUniqueGameObject(SkillBarName);
            RunSceneSessionEntryPoint entryPoint =
                UnityEngine.Object.FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            RunFlaskHudView view = skillBar.GetComponent<RunFlaskHudView>();
            RunFlaskHudPresenter presenter =
                skillBar.GetComponent<RunFlaskHudPresenter>();
            RunFlaskHudInputController input =
                skillBar.GetComponent<RunFlaskHudInputController>();
            if (entryPoint == null ||
                entryPoint.Participants.Count != 1 ||
                view == null ||
                presenter == null ||
                input == null ||
                view.SlotCount != 2 ||
                presenter.View != view ||
                presenter.SessionEntryPoint != entryPoint ||
                input.View != view ||
                input.SessionEntryPoint != entryPoint ||
                presenter.PlayerId != entryPoint.Participants[0].PlayerId ||
                input.PlayerId != entryPoint.Participants[0].PlayerId)
            {
                throw new InvalidOperationException("Flask HUD root wiring is invalid.");
            }

            int[] childIndices = { HealthSlotChildIndex, ResourceSlotChildIndex };
            string[] keyLabels = { "Q", "E" };
            HashSet<RunFlaskSlotView> unique = new();
            for (int i = 0; i < view.SlotCount; i++)
            {
                RunFlaskSlotView slot = view.Slots[i];
                Transform expectedTransform =
                    skillBar.transform.GetChild(childIndices[i]);
                Graphic graphic = expectedTransform.GetComponent<Graphic>();
                if (slot == null ||
                    !unique.Add(slot) ||
                    slot.transform != expectedTransform ||
                    graphic == null ||
                    !graphic.raycastTarget ||
                    slot.IconImage == null ||
                    slot.CooldownOverlay == null ||
                    slot.KeyText == null ||
                    slot.KeyText.text != keyLabels[i] ||
                    slot.FlaskNameText == null ||
                    slot.CooldownText == null)
                {
                    throw new InvalidOperationException(
                        $"Flask HUD slot {i} is invalid.");
                }
            }
        }

        private static GameObject FindUniqueGameObject(string name)
        {
            GameObject[] matches = UnityEngine.Object.FindObjectsByType<GameObject>(
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

        private static Image GetOrCreateImage(Transform parent, string name)
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
            Transform existing = parent.Find(name);
            TMP_Text text = existing != null
                ? existing.GetComponent<TMP_Text>()
                : null;
            if (text != null)
                return text;

            GameObject textObject = CreateUiObject(name, parent);
            TextMeshProUGUI created = textObject.AddComponent<TextMeshProUGUI>();
            created.fontSize = fontSize;
            created.color = Color.white;
            created.alignment = alignment;
            created.raycastTarget = false;
            return created;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.layer = LayerMask.NameToLayer("UI");
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(RectTransform rect, Vector2 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        private static T RequireAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Asset is missing: {path}");
            return asset;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before flask {operation}.");
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
