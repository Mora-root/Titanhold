using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CombatResourceValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Combat Resources")]
    public static void Validate()
    {
        try
        {
            ValidateBoundedState();
            ValidateAbilityAuthoring();
            ValidateSuccessfulDamageGeneration();
            Debug.Log(
                "Combat resource validation passed (3 scenarios).");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Combat resource validation failed: {exception}");
        }
    }

    private static void ValidateBoundedState()
    {
        CombatResourceState resource = new(
            " resource:rage ",
            10f,
            2f);
        int notifications = 0;
        resource.Changed += (_, _) => notifications++;

        Assert(resource.ResourceId == "resource:rage" &&
               resource.Current == 2f && resource.Maximum == 10f,
            "Combat resource did not normalize or initialize its state.");
        Assert(!resource.TryGain("resource:mana", 3f) &&
               !resource.TryGain("resource:rage", float.NaN) &&
               resource.Current == 2f,
            "Invalid resource generation mutated the state.");
        Assert(resource.TryGain("resource:rage", 20f) &&
               resource.Current == 10f && notifications == 1,
            "Resource generation did not clamp at its maximum.");
        Assert(resource.TryGain("resource:rage", 1f) &&
               notifications == 1,
            "A capped valid gain emitted a false state change.");
        Assert(!resource.TrySpend(11f) && resource.TrySpend(4f) &&
               resource.Current == 6f && notifications == 2,
            "Resource spending was not atomic.");
        Assert(!resource.TrySetCurrent(float.PositiveInfinity) &&
               resource.TrySetCurrent(3f) &&
               resource.Current == 3f && notifications == 3,
            "Explicit resource restoration accepted invalid state.");
    }

    private static void ValidateAbilityAuthoring()
    {
        TargetedDamageAbilityDefinition definition =
            ScriptableObject.CreateInstance<TargetedDamageAbilityDefinition>();
        try
        {
            SerializedObject data = new(definition);
            data.FindProperty("abilityId").stringValue =
                "ability:heavy-strike";
            SerializedProperty gain =
                data.FindProperty("sourceResourceGain");
            gain.FindPropertyRelative("enabled").boolValue = true;
            gain.FindPropertyRelative("resourceId").stringValue =
                "resource:rage";
            gain.FindPropertyRelative("amount").floatValue = 5f;
            data.ApplyModifiedPropertiesWithoutUndo();

            Assert(definition.TryCreateSnapshot(
                       20f,
                       out TargetedDamageAbilitySnapshot snapshot) &&
                   snapshot.SourceResourceGain.IsValid &&
                   snapshot.SourceResourceGain.ResourceId ==
                   "resource:rage" &&
                   snapshot.SourceResourceGain.Amount == 5f,
                "Targeted ability did not snapshot its authored resource gain.");

            gain.FindPropertyRelative("amount").floatValue = 0f;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert(!definition.TryCreateSnapshot(20f, out _),
                "Enabled resource generation accepted a zero amount.");
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    private static void ValidateSuccessfulDamageGeneration()
    {
        AbilitySourceResourceGain gain = new("resource:rage", 3f);
        AreaDamageAbilitySnapshot ability = new(
            new AbilityExecutionDefinition(
                "ability:test-generator",
                0f,
                0d,
                0d,
                0d),
            10f,
            2f,
            1,
            "Attack",
            gain);
        CombatResourceState resource = new("resource:rage", 10f);
        CombatExecutionId executionId = CombatExecutionId.New();
        DamageRequest request = new(
            executionId,
            new CombatActorReference(
                "player:test",
                CombatActorKind.Player),
            10f,
            DamageCause.Ability,
            ability.Execution.AbilityId);
        DamageTargetResolution applied = new(
            null,
            DamageResult.LegacyFallback(request));
        CombatExecutionReport multiTargetReport = new(
            executionId,
            new[] { applied, applied });

        Assert(AbilitySourceResourceGainResolver.TryApply(
                   ability,
                   executionId,
                   multiTargetReport,
                   resource) &&
               resource.Current == 3f,
            "Successful multi-target damage did not grant exactly one amount.");
        Assert(AbilitySourceResourceGainResolver.TryApply(
                   ability,
                   executionId,
                   multiTargetReport,
                   resource) &&
               resource.Current == 3f,
            "Replaying a resolved execution duplicated its resource gain.");
        Assert(!AbilitySourceResourceGainResolver.TryApply(
                   ability,
                   CombatExecutionId.New(),
                   multiTargetReport,
                   resource) &&
               !AbilitySourceResourceGainResolver.TryApply(
                   ability,
                   executionId,
                   CombatExecutionReport.Empty(executionId),
                   resource) &&
               resource.Current == 3f,
            "A mismatched or empty release generated combat resource.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
