using System;
using Titanhold.Run;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.UI.Run.Editor
{
    public static class RunProgressionHudWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";

        [MenuItem("Tools/Titanhold/Install Run Progression HUD Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

                RunProgressionCombatAdapter adapter =
                    UnityEngine.Object.FindAnyObjectByType<
                        RunProgressionCombatAdapter>(
                        FindObjectsInactive.Include);
                PlayerExperienceHUD legacyHud =
                    UnityEngine.Object.FindAnyObjectByType<
                        PlayerExperienceHUD>(
                        FindObjectsInactive.Include);
                PlayerLevelUpNotification legacyNotification =
                    UnityEngine.Object.FindAnyObjectByType<
                        PlayerLevelUpNotification>(
                        FindObjectsInactive.Include);
                if (adapter == null ||
                    legacyHud == null ||
                    legacyNotification == null ||
                    legacyHud.gameObject != legacyNotification.gameObject)
                {
                    throw new InvalidOperationException(
                        "Run progression adapter or reusable legacy HUD components are missing.");
                }

                SerializedObject serializedHud = new(legacyHud);
                TMP_Text progressionText =
                    serializedHud.FindProperty("experienceText")
                        .objectReferenceValue as TMP_Text;
                SerializedObject serializedNotification =
                    new(legacyNotification);
                GameObject levelUpRoot =
                    serializedNotification.FindProperty("root")
                        .objectReferenceValue as GameObject;
                TMP_Text levelUpText =
                    serializedNotification.FindProperty("messageText")
                        .objectReferenceValue as TMP_Text;
                float visibleDuration =
                    serializedNotification.FindProperty("visibleDuration")
                        .floatValue;
                if (progressionText == null ||
                    levelUpRoot == null ||
                    levelUpText == null)
                {
                    throw new InvalidOperationException(
                        "Reusable run progression HUD references are incomplete.");
                }

                progressionText.enableAutoSizing = true;
                progressionText.fontSizeMin = 12f;
                progressionText.fontSizeMax = 18f;
                progressionText.textWrappingMode = TextWrappingModes.NoWrap;

                GameObject hudObject = legacyHud.gameObject;
                RunProgressionHudView view =
                    hudObject.GetComponent<RunProgressionHudView>();
                if (view == null)
                    view = hudObject.AddComponent<RunProgressionHudView>();

                RunProgressionHudPresenter presenter =
                    hudObject.GetComponent<RunProgressionHudPresenter>();
                if (presenter == null)
                {
                    presenter = hudObject.AddComponent<
                        RunProgressionHudPresenter>();
                }

                string playerId = ResolveLocalPlayerId(adapter);
                view.ConfigureForEditor(
                    progressionText,
                    levelUpRoot,
                    levelUpText);
                presenter.ConfigureForEditor(
                    adapter,
                    view,
                    playerId,
                    visibleDuration);
                legacyHud.enabled = false;
                legacyNotification.enabled = false;

                EditorUtility.SetDirty(view);
                EditorUtility.SetDirty(presenter);
                EditorUtility.SetDirty(progressionText);
                EditorUtility.SetDirty(legacyHud);
                EditorUtility.SetDirty(legacyNotification);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save run scene.");

                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("Run Progression HUD wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Progression HUD wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Progression HUD Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Run Progression HUD wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Progression HUD wiring validation failed: {exception}");
            }
        }

        private static void ValidateInternal()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open '{ScenePath}'.");

            RunProgressionCombatAdapter adapter =
                UnityEngine.Object.FindAnyObjectByType<
                    RunProgressionCombatAdapter>(
                    FindObjectsInactive.Include);
            RunProgressionHudView view =
                UnityEngine.Object.FindAnyObjectByType<
                    RunProgressionHudView>(
                    FindObjectsInactive.Include);
            RunProgressionHudPresenter presenter =
                UnityEngine.Object.FindAnyObjectByType<
                    RunProgressionHudPresenter>(
                    FindObjectsInactive.Include);
            PlayerExperienceHUD legacyHud =
                UnityEngine.Object.FindAnyObjectByType<
                    PlayerExperienceHUD>(
                    FindObjectsInactive.Include);
            PlayerLevelUpNotification legacyNotification =
                UnityEngine.Object.FindAnyObjectByType<
                    PlayerLevelUpNotification>(
                    FindObjectsInactive.Include);

            if (adapter == null ||
                view == null ||
                presenter == null ||
                view.gameObject != presenter.gameObject ||
                presenter.ProgressionAdapter != adapter ||
                presenter.View != view ||
                string.IsNullOrWhiteSpace(presenter.PlayerId) ||
                view.ProgressionText == null ||
                !view.ProgressionText.enableAutoSizing ||
                view.ProgressionText.textWrappingMode !=
                    TextWrappingModes.NoWrap ||
                view.LevelUpRoot == null ||
                view.LevelUpText == null)
            {
                throw new InvalidOperationException(
                    "Run Progression HUD view or presenter wiring is invalid.");
            }

            if (legacyHud == null ||
                legacyNotification == null ||
                legacyHud.enabled ||
                legacyNotification.enabled)
            {
                throw new InvalidOperationException(
                    "Legacy permanent-experience HUD components must remain present and disabled.");
            }

            string expectedPlayerId = ResolveLocalPlayerId(adapter);
            if (presenter.PlayerId != expectedPlayerId)
            {
                throw new InvalidOperationException(
                    "Run Progression HUD participant identity is invalid.");
            }
        }

        private static string ResolveLocalPlayerId(
            RunProgressionCombatAdapter adapter)
        {
            if (adapter.SessionEntryPoint == null ||
                adapter.SessionEntryPoint.Participants.Count != 1 ||
                adapter.SessionEntryPoint.Participants[0] == null ||
                !adapter.SessionEntryPoint.Participants[0].IsValid)
            {
                throw new InvalidOperationException(
                    "Vertical slice requires one valid local participant binding.");
            }

            return adapter.SessionEntryPoint.Participants[0].PlayerId;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before Run Progression HUD {operation}.");
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
