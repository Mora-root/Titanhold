using System;
using Titanhold.Combat.Abilities;
using Titanhold.UI.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Session.Editor
{
    public static class AbilityDefinitionCatalogWiringEditor
    {
        private const string HubScenePath =
            "Assets/_Project/Scenes/HubScene.unity";
        private const string CatalogFolder =
            "Assets/_Project/ScriptableObjects/Abilities";
        private const string CatalogPath =
            CatalogFolder + "/AbilityDefinitionCatalog.asset";
        private const string SpinPath =
            "Assets/_Project/ScriptableObjects/Configs/SpinAbility.asset";
        private const string SpinAbilityId = "ability:spin";

        [MenuItem("Tools/Titanhold/Install Ability Definition Catalog Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();

                AreaDamageAbilityDefinition spin =
                    AssetDatabase.LoadAssetAtPath<AreaDamageAbilityDefinition>(
                        SpinPath);
                if (spin == null || spin.AbilityId != SpinAbilityId)
                {
                    throw new InvalidOperationException(
                        $"'{SpinPath}' must define '{SpinAbilityId}'.");
                }

                LoadOrCreateCatalog(spin);
                Scene scene = EditorSceneManager.OpenScene(
                    HubScenePath,
                    OpenSceneMode.Single);
                AbilityDefinitionCatalog catalog =
                    AssetDatabase.LoadAssetAtPath<AbilityDefinitionCatalog>(
                        CatalogPath);
                GameSessionRuntimeHost host =
                    UnityEngine.Object.FindAnyObjectByType<
                        GameSessionRuntimeHost>(FindObjectsInactive.Include);
                HubRunPreparationView view =
                    UnityEngine.Object.FindAnyObjectByType<
                        HubRunPreparationView>(FindObjectsInactive.Include);
                HubRunLaunchController launch =
                    UnityEngine.Object.FindAnyObjectByType<
                        HubRunLaunchController>(FindObjectsInactive.Include);
                if (host == null || view == null || launch == null)
                {
                    throw new InvalidOperationException(
                        "Hub session host or run launch wiring is missing.");
                }

                host.ConfigureForEditor(
                    host.ItemDefinitions,
                    catalog,
                    host.RunProgression,
                    host.ConclusionRewards);
                launch.ConfigureForEditor(
                    view,
                    host,
                    "player:local",
                    "character:warrior",
                    "archetype:warrior",
                    SpinAbilityId,
                    "difficulty:prototype",
                    "SampleScene");
                EditorUtility.SetDirty(host);
                EditorUtility.SetDirty(launch);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save Hub scene.");

                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("Ability Definition Catalog wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Ability Definition Catalog wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Ability Definition Catalog Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Ability Definition Catalog wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Ability Definition Catalog wiring validation failed: {exception}");
            }
        }

        private static AbilityDefinitionCatalog LoadOrCreateCatalog(
            AreaDamageAbilityDefinition spin)
        {
            if (!AssetDatabase.IsValidFolder(CatalogFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets/_Project/ScriptableObjects",
                    "Abilities");
            }

            AbilityDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<AbilityDefinitionCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<
                    AbilityDefinitionCatalog>();
                catalog.ConfigureForEditor(new ScriptableObject[] { spin });
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                return catalog;
            }

            catalog.RebuildIndex();
            if (!catalog.IsValid ||
                !catalog.TryResolve(
                    SpinAbilityId,
                    out IAbilityDefinition resolved) ||
                !ReferenceEquals(resolved, spin))
            {
                throw new InvalidOperationException(
                    "Existing ability catalog is invalid or does not contain " +
                    $"the project Spin definition: {catalog.ValidationError}");
            }

            return catalog;
        }

        private static void ValidateInternal()
        {
            AreaDamageAbilityDefinition spin =
                AssetDatabase.LoadAssetAtPath<AreaDamageAbilityDefinition>(
                    SpinPath);
            AbilityDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<AbilityDefinitionCatalog>(
                    CatalogPath);
            if (spin == null || spin.AbilityId != SpinAbilityId)
            {
                throw new InvalidOperationException(
                    "The project Spin definition is missing or has an invalid stable id.");
            }

            if (catalog == null || !catalog.IsValid ||
                !catalog.TryResolve(
                    SpinAbilityId,
                    out IAbilityDefinition resolved) ||
                !ReferenceEquals(resolved, spin))
            {
                throw new InvalidOperationException(
                    "The ability catalog does not resolve the project Spin definition.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != HubScenePath)
                throw new InvalidOperationException($"Open '{HubScenePath}'.");

            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            HubRunLaunchController launch =
                UnityEngine.Object.FindAnyObjectByType<HubRunLaunchController>(
                    FindObjectsInactive.Include);
            if (host == null || host.AbilityDefinitions != catalog)
            {
                throw new InvalidOperationException(
                    "Hub session host does not reference the ability catalog.");
            }

            if (launch == null || launch.SessionHost != host ||
                launch.CharacterArchetypeId != "archetype:warrior" ||
                launch.StartingAbilityId != SpinAbilityId)
            {
                throw new InvalidOperationException(
                    "Hub launch does not seed Spin as the starting run ability.");
            }
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before ability catalog {operation}.");
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
