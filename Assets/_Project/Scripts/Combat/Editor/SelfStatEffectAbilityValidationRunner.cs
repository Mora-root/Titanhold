using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using Titanhold.Combat.Effects;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SelfStatEffectAbilityValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Self Stat Effect Ability")]
    public static void Validate()
    {
        try
        {
            ValidateDefinitionAndSnapshot();
            ValidateRelease();
            Debug.Log(
                "Self stat-effect ability validation passed (2 scenarios).");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Self stat-effect ability validation failed: {exception}");
        }
    }

    private static void ValidateDefinitionAndSnapshot()
    {
        SelfStatEffectAbilityDefinition definition =
            ScriptableObject.CreateInstance<
                SelfStatEffectAbilityDefinition>();
        try
        {
            Assert(!definition.TryCreateSnapshot(out _),
                "Unconfigured self-effect ability was usable.");
            ConfigureDefinition(definition);
            GameObject source = TemporaryObject("SelfEffect_Source");
            try
            {
                Assert(definition.EvaluateUse(
                           new AbilityUseContext(null, null)).Status ==
                       AbilityCommitStatus.MissingSource &&
                       definition.EvaluateUse(
                           new AbilityUseContext(source.transform, null))
                           .IsReady,
                    "Self-effect ability did not validate its source.");
                Assert(definition.TryCreateSnapshot(
                           out SelfStatEffectAbilitySnapshot snapshot),
                    "Configured self-effect ability did not create a snapshot.");
                Assert(snapshot.Execution.AbilityId ==
                           "ability:test-self-effect" &&
                       snapshot.Execution.ResourceCost == 20f &&
                       snapshot.Execution.Cooldown == 20d &&
                       snapshot.Execution.WindUp == 0d &&
                       Math.Abs(
                           snapshot.Execution.Recovery - 0.2d) < 0.000001d &&
                       snapshot.AnimatorTrigger.Length == 0 &&
                       snapshot.Effect.EffectId ==
                           "effect:test-self-effect" &&
                       snapshot.Effect.StatType == StatType.Armor &&
                       snapshot.Effect.ModifierType ==
                           StatModifierType.Increased &&
                       snapshot.Effect.ValuePerStack == 30f &&
                       snapshot.Effect.MaximumStacks == 1 &&
                       snapshot.Effect.Duration == 8d &&
                       snapshot.PostActionPolicy ==
                           PostAbilityActionPolicy.None,
                    "Self-effect definition did not create the expected " +
                    "immutable snapshot.");

                SerializedObject data = new(definition);
                data.FindProperty("cooldown").floatValue = 5f;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert(snapshot.Execution.Cooldown == 20d,
                    "Authored changes mutated an existing self-effect snapshot.");
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    private static void ValidateRelease()
    {
        GameObject source = TemporaryObject("SelfEffectRelease_Source");
        try
        {
            ValidationEffectReceiver receiver =
                source.AddComponent<ValidationEffectReceiver>();
            TimedStackingStatEffectDefinition effect = new(
                "effect:test-self-effect",
                StatType.Armor,
                StatModifierType.Increased,
                30f,
                1,
                8d);
            SelfStatEffectAbilitySnapshot snapshot = new(
                new AbilityExecutionDefinition(
                    "ability:test-self-effect",
                    20f,
                    20d,
                    0d,
                    0.2d),
                string.Empty,
                effect);
            AbilityExecutionService service = new(
                Actor(),
                new FreeResourceGateway());
            CombatExecutionId executionId = CombatExecutionId.New();
            Assert(service.TryCommit(
                       executionId,
                       snapshot.Execution,
                       2d).Success,
                "Self-effect ability did not commit.");
            AbilityExecutionResult release = service.TryRelease(
                executionId,
                2d);
            Assert(release.Success,
                "Self-effect ability did not release.");
            CombatExecutionReport report = snapshot.Release(
                new AbilityUseContext(source.transform, null),
                release.Execution,
                2d);
            Assert(report.ExecutionId == executionId &&
                   report.ResolutionCount == 0 &&
                   receiver.ApplicationCount == 1 &&
                   receiver.LastDefinition == effect &&
                   receiver.LastSource == Actor() &&
                   receiver.LastSimulationTime == 2d,
                "Self-effect release lost its effect, source or timing.");
        }
        finally
        {
            Object.DestroyImmediate(source);
        }
    }

    private static void ConfigureDefinition(
        SelfStatEffectAbilityDefinition definition)
    {
        SerializedObject data = new(definition);
        data.FindProperty("abilityId").stringValue =
            "ability:test-self-effect";
        SerializedProperty effect = data.FindProperty("selfEffect");
        effect.FindPropertyRelative("enabled").boolValue = true;
        effect.FindPropertyRelative("effectId").stringValue =
            "effect:test-self-effect";
        effect.FindPropertyRelative("statType").enumValueIndex =
            (int)StatType.Armor;
        effect.FindPropertyRelative("modifierType").enumValueIndex =
            (int)StatModifierType.Increased;
        effect.FindPropertyRelative("valuePerStack").floatValue = 30f;
        effect.FindPropertyRelative("maximumStacks").intValue = 1;
        effect.FindPropertyRelative("duration").floatValue = 8f;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CombatActorReference Actor()
    {
        return new CombatActorReference(
            "player:self-effect-validation",
            CombatActorKind.Player);
    }

    private static GameObject TemporaryObject(string name)
    {
        return new GameObject(name)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class FreeResourceGateway : IAbilityResourceGateway
    {
        public bool CanSpend(
            float amount,
            AbilityCombatResourceCost combatResourceCost)
        {
            return true;
        }

        public bool TrySpend(
            float amount,
            AbilityCombatResourceCost combatResourceCost)
        {
            return true;
        }
    }

    private sealed class ValidationEffectReceiver :
        MonoBehaviour,
        ITimedStackingStatEffectReceiver
    {
        public int ApplicationCount { get; private set; }
        public TimedStackingStatEffectDefinition LastDefinition {
            get;
            private set;
        }
        public CombatActorReference LastSource { get; private set; }
        public double LastSimulationTime { get; private set; }

        public bool TryApply(
            TimedStackingStatEffectDefinition definition,
            CombatActorReference source,
            double simulationTime,
            out TimedStatEffectSnapshot snapshot)
        {
            ApplicationCount++;
            LastDefinition = definition;
            LastSource = source;
            LastSimulationTime = simulationTime;
            snapshot = default;
            return true;
        }
    }
}
