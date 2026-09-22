using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunExperienceDebugWiringEditor
    {
        private const string PlayerPrefabPath =
            "Assets/_Project/Prefabs/Player.prefab";
        private const int ExperiencePerGrant = 500;
        private const KeyCode GrantKey = KeyCode.F6;

        [MenuItem("Tools/Titanhold/Install RunXP Debug Component")]
        public static void Install()
        {
            try
            {
                RequireEditMode();
                GameObject root = PrefabUtility.LoadPrefabContents(
                    PlayerPrefabPath);
                try
                {
                    RunExperienceDebugController controller =
                        root.GetComponent<RunExperienceDebugController>();
                    controller ??=
                        root.AddComponent<RunExperienceDebugController>();
                    controller.ConfigureForEditor(
                        ExperiencePerGrant,
                        GrantKey);
                    EditorUtility.SetDirty(controller);
                    PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("RunXP debug component installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"RunXP debug component installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate RunXP Debug Component")]
        public static void Validate()
        {
            try
            {
                RequireEditMode();
                ValidateInternal();
                Debug.Log("RunXP debug component validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"RunXP debug component validation failed: {exception}");
            }
        }

        private static void ValidateInternal()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                PlayerPrefabPath);
            try
            {
                RunExperienceDebugController controller =
                    root.GetComponent<RunExperienceDebugController>();
                if (controller == null ||
                    controller.ExperiencePerGrant != ExperiencePerGrant ||
                    controller.GrantKey != GrantKey)
                {
                    throw new InvalidOperationException(
                        "Player prefab RunXP debug component is missing " +
                        "or has unexpected settings.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before RunXP debug component wiring.");
            }
        }
    }
}
