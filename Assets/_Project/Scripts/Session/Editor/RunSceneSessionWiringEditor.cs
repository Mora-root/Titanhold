using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Session.Editor
{
    public static class RunSceneSessionWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string EntryObjectName = "RunSessionEntryPoint";

        [MenuItem("Tools/Titanhold/Install Run Scene Session Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

                PlayerInventory inventory =
                    UnityEngine.Object.FindAnyObjectByType<PlayerInventory>(
                        FindObjectsInactive.Include);
                if (inventory == null)
                    throw new InvalidOperationException("PlayerInventory is missing.");

                GameObject player = inventory.gameObject;
                PlayerEquipmentRuntime equipment =
                    player.GetComponent<PlayerEquipmentRuntime>();
                PlayerExperience experience =
                    player.GetComponent<PlayerExperience>();
                PlayerGold gold = player.GetComponent<PlayerGold>();
                RunSceneParticipantBinding binding = new(
                    "player:local",
                    "character:warrior",
                    inventory,
                    equipment,
                    experience,
                    gold);
                if (!binding.IsValid)
                {
                    throw new InvalidOperationException(
                        "Player runtime does not contain every snapshot component.");
                }

                GameObject entryObject = FindRootObject(scene, EntryObjectName);
                if (entryObject == null)
                    entryObject = new GameObject(EntryObjectName);

                RunSceneSessionEntryPoint entry =
                    entryObject.GetComponent<RunSceneSessionEntryPoint>();
                if (entry == null)
                    entry = entryObject.AddComponent<RunSceneSessionEntryPoint>();

                entry.ConfigureForEditor(new[] { binding });
                EditorUtility.SetDirty(entry);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save run scene.");

                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("Run Scene Session wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Scene Session wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Scene Session Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Run Scene Session wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Scene Session wiring validation failed: {exception}");
            }
        }

        private static void ValidateInternal()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open '{ScenePath}'.");

            GameObject entryObject = FindRootObject(scene, EntryObjectName);
            RunSceneSessionEntryPoint entry = entryObject != null
                ? entryObject.GetComponent<RunSceneSessionEntryPoint>()
                : null;
            if (entry == null || entry.transform.parent != null)
            {
                throw new InvalidOperationException(
                    "Run Session entry point is missing or is not a root object.");
            }

            if (entry.Participants.Count != 1)
            {
                throw new InvalidOperationException(
                    "Vertical slice must have exactly one run participant binding.");
            }

            RunSceneParticipantBinding binding = entry.Participants[0];
            if (binding == null || !binding.IsValid ||
                binding.PlayerId != "player:local" ||
                binding.CharacterId != "character:warrior")
            {
                throw new InvalidOperationException(
                    "Local Warrior participant binding is invalid.");
            }

            GameObject player = binding.Inventory.gameObject;
            if (binding.Equipment.gameObject != player ||
                binding.Experience.gameObject != player ||
                binding.Gold.gameObject != player)
            {
                throw new InvalidOperationException(
                    "Participant snapshot components must belong to one Player object.");
            }
        }

        private static GameObject FindRootObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];
            }

            return null;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before Run Scene Session {operation}.");
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
