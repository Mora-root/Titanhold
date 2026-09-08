using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunCombatResourceLoadoutValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Combat Resource Loadouts")]
        public static void ValidateFromMenu()
        {
            RunCombatResourceLoadoutDefinition warrior = null;
            RunCombatResourceLoadoutDefinition invalid = null;
            RunCombatResourceLoadoutCatalog catalog = null;
            try
            {
                ValidateRegistry();

                warrior = CreateDefinition(
                    "combat-resources:warrior",
                    "archetype:warrior",
                    new RunCombatResourceEntry(
                        "resource:rage",
                        8f,
                        0f));
                catalog = ScriptableObject.CreateInstance<
                    RunCombatResourceLoadoutCatalog>();
                catalog.ConfigureForEditor(new[] { warrior });
                Assert(catalog.IsValid &&
                       catalog.TryResolve(
                           " archetype:warrior ",
                           out RunCombatResourceLoadout resolved) &&
                       resolved.Resources.Count == 1 &&
                       resolved.Resources[0].ResourceId == "resource:rage" &&
                       resolved.Resources[0].Maximum == 8f &&
                       resolved.Resources[0].Initial == 0f,
                    "Valid authored combat resources did not resolve.");

                catalog.ConfigureForEditor(new[] { warrior, null });
                Assert(!catalog.IsValid &&
                       !catalog.TryResolve("archetype:warrior", out _),
                    "A null entry left a partially usable resource catalog.");

                invalid = CreateDefinition(
                    "combat-resources:invalid",
                    "archetype:invalid",
                    new RunCombatResourceEntry(
                        "resource:rage",
                        8f,
                        9f));
                catalog.ConfigureForEditor(new[] { warrior, invalid });
                Assert(!catalog.IsValid &&
                       !catalog.TryResolve("archetype:warrior", out _),
                    "An invalid resource left a partially usable catalog.");

                Debug.Log("Combat Resource Loadouts validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Combat Resource Loadouts validation failed: {exception}");
            }
            finally
            {
                Destroy(warrior);
                Destroy(invalid);
                Destroy(catalog);
            }
        }

        private static void ValidateRegistry()
        {
            RunCombatResourceLoadout warrior = new(
                "combat-resources:warrior",
                "archetype:warrior",
                new[]
                {
                    new RunCombatResourceDefinition(
                        "resource:rage",
                        8f,
                        0f)
                });
            Assert(RunCombatResourceLoadoutRegistry.TryCreate(
                       new[] { warrior },
                       out RunCombatResourceLoadoutRegistry registry,
                       out string error) &&
                   registry.Count == 1 &&
                   registry.TryResolve(
                       "archetype:warrior",
                       out RunCombatResourceLoadout resolved) &&
                   ReferenceEquals(warrior, resolved),
                $"Valid combat resource loadout was rejected: {error}");

            RunCombatResourceLoadout duplicateResource = new(
                "combat-resources:duplicate",
                "archetype:duplicate",
                new[]
                {
                    new RunCombatResourceDefinition(
                        "resource:rage",
                        8f,
                        0f),
                    new RunCombatResourceDefinition(
                        "resource:rage",
                        4f,
                        0f)
                });
            Assert(!RunCombatResourceLoadoutRegistry.TryCreate(
                       new[] { duplicateResource },
                       out _,
                       out _),
                "A duplicate resource id was accepted.");
            Assert(!RunCombatResourceLoadoutRegistry.TryCreate(
                       new[] { warrior, warrior },
                       out _,
                       out _),
                "A duplicate loadout was accepted.");
        }

        private static RunCombatResourceLoadoutDefinition CreateDefinition(
            string loadoutId,
            string archetypeId,
            params RunCombatResourceEntry[] resources)
        {
            RunCombatResourceLoadoutDefinition definition =
                ScriptableObject.CreateInstance<
                    RunCombatResourceLoadoutDefinition>();
            definition.ConfigureForEditor(
                loadoutId,
                archetypeId,
                resources);
            return definition;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Destroy(UnityEngine.Object instance)
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
