using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.UI.Hub.Editor
{
    public static class HubQuitButtonWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/HubScene.unity";
        private const string QuitButtonName = "QuitButton";

        [MenuItem("Tools/Titanhold/Install Hub Quit Button")]
        public static void Install()
        {
            try
            {
                Scene scene = RequireActiveScene(requireClean: true);
                Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                    FindObjectsInactive.Include);
                HubRunPreparationView preparationView =
                    UnityEngine.Object.FindAnyObjectByType<
                        HubRunPreparationView>(FindObjectsInactive.Include);
                if (canvas == null || preparationView == null ||
                    preparationView.StartRunButton == null)
                {
                    throw new InvalidOperationException(
                        "Hub Canvas or Start Run button is missing.");
                }

                Transform background = canvas.transform.Find("Background");
                if (background == null)
                {
                    throw new InvalidOperationException(
                        "Hub background is missing.");
                }

                Transform existing = background.Find(QuitButtonName);
                GameObject buttonObject = existing != null
                    ? existing.gameObject
                    : UnityEngine.Object.Instantiate(
                        preparationView.StartRunButton.gameObject,
                        background,
                        false);
                buttonObject.name = QuitButtonName;

                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.right;
                rect.anchorMax = Vector2.right;
                rect.pivot = Vector2.right;
                rect.anchoredPosition = new Vector2(-40f, 40f);
                rect.sizeDelta = new Vector2(180f, 56f);

                Button button = buttonObject.GetComponent<Button>();
                TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(
                    true);
                if (button == null || label == null)
                {
                    throw new InvalidOperationException(
                        "Quit button template is incomplete.");
                }

                label.text = "QUIT";
                label.fontSize = 18f;
                HubQuitButtonController controller =
                    buttonObject.GetComponent<HubQuitButtonController>();
                controller ??=
                    buttonObject.AddComponent<HubQuitButtonController>();
                controller.ConfigureForEditor(button);
                EditorUtility.SetDirty(buttonObject);
                EditorUtility.SetDirty(controller);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save Hub scene.");

                ValidateInternal(scene);
                Debug.Log("Hub quit button wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Hub quit button wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Hub Quit Button Wiring")]
        public static void Validate()
        {
            try
            {
                Scene scene = RequireActiveScene(requireClean: false);
                ValidateInternal(scene);
                Debug.Log("Hub quit button wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Hub quit button wiring validation failed: {exception}");
            }
        }

        private static void ValidateInternal(Scene scene)
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                FindObjectsInactive.Include);
            Transform buttonTransform = canvas != null
                ? canvas.transform.Find($"Background/{QuitButtonName}")
                : null;
            Button button = buttonTransform != null
                ? buttonTransform.GetComponent<Button>()
                : null;
            HubQuitButtonController controller = buttonTransform != null
                ? buttonTransform.GetComponent<HubQuitButtonController>()
                : null;
            if (button == null || controller == null ||
                controller.QuitButton != button)
            {
                throw new InvalidOperationException(
                    "Hub quit button wiring is incomplete.");
            }
        }

        private static Scene RequireActiveScene(bool requireClean)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} first.");

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "The active Hub scene has unrelated unsaved changes.");
            }

            return scene;
        }
    }
}
