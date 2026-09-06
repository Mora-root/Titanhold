using System;
using Titanhold.Combat.Abilities;
using Titanhold.Run;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AbilityDefinitionResolutionValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Ability Definition Resolution")]
    public static void Validate()
    {
        AreaDamageAbilityDefinition spin = null;
        AreaDamageAbilityDefinition guard = null;
        AreaDamageAbilityDefinition duplicate = null;
        AreaDamageAbilityDefinition invalid = null;
        try
        {
            spin = CreateDefinition("ability:spin");
            guard = CreateDefinition("ability:guard");
            duplicate = CreateDefinition("ability:spin");
            invalid = CreateDefinition(" ability:invalid ");

            Assert(AbilityDefinitionRegistry.TryCreate(
                       new IAbilityDefinition[] { spin, guard },
                       out AbilityDefinitionRegistry registry,
                       out string error) &&
                   string.IsNullOrEmpty(error) &&
                   registry.Count == 2 &&
                   registry.TryResolve(
                       " ability:spin ",
                       out IAbilityDefinition resolved) &&
                   ReferenceEquals(resolved, spin),
                "Valid ability definitions did not build a stable-id registry.");
            Assert(!AbilityDefinitionRegistry.TryCreate(
                       new IAbilityDefinition[] { spin, duplicate },
                       out _,
                       out _) &&
                   !AbilityDefinitionRegistry.TryCreate(
                       new IAbilityDefinition[] { spin, invalid },
                       out _,
                       out _) &&
                   !AbilityDefinitionRegistry.TryCreate(
                       Array.Empty<IAbilityDefinition>(),
                       out _,
                       out _),
                "Invalid ability catalog data produced a partial registry.");

            RunAbilityLoadoutService loadout = new();
            RunParticipantAbilityState state = null;
            Assert(loadout.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:local",
                           "character:warrior")).Success &&
                   loadout.TryGetParticipant(
                       "player:local",
                       out state),
                "Could not prepare participant ability slots.");
            AbilitySlotDefinitionResolver slotResolver = new(state, registry);
            Assert(!slotResolver.TryResolve(0, out _),
                "An empty ability slot resolved a definition.");
            Assert(loadout.TryGrantAndAssignAbility(
                       "player:local",
                       "ability:spin",
                       0).Success &&
                   slotResolver.TryResolve(0, out resolved) &&
                   ReferenceEquals(resolved, spin),
                "Assigned stable id did not resolve its definition.");
            Assert(loadout.TryGrantAndAssignAbility(
                       "player:local",
                       "ability:guard",
                       0).Success &&
                   slotResolver.TryResolve(0, out resolved) &&
                   ReferenceEquals(resolved, guard),
                "Live slot replacement required rebuilding the resolver.");
            Assert(!slotResolver.TryResolve(-1, out _) &&
                   !slotResolver.TryResolve(state.AbilitySlotCount, out _),
                "Ability definition resolver accepted an invalid slot.");

            Debug.Log("Ability Definition Resolution validation passed.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Ability Definition Resolution validation failed: {exception}");
        }
        finally
        {
            Object.DestroyImmediate(spin);
            Object.DestroyImmediate(guard);
            Object.DestroyImmediate(duplicate);
            Object.DestroyImmediate(invalid);
        }
    }

    private static AreaDamageAbilityDefinition CreateDefinition(string abilityId)
    {
        AreaDamageAbilityDefinition definition =
            ScriptableObject.CreateInstance<AreaDamageAbilityDefinition>();
        SerializedObject serialized = new(definition);
        serialized.FindProperty("abilityId").stringValue = abilityId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return definition;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
