using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using Titanhold.Combat.Effects;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TargetedMovementAbilityValidationRunner
{
    private const int ObstacleLayer = 30;

    [MenuItem("Tools/Titanhold/Validate Targeted Movement Ability")]
    public static void Validate()
    {
        try
        {
            ValidateSnapshot();
            ValidateTargetingAndMovement();
            ValidateReleaseEffect();
            Debug.Log(
                "Targeted movement ability validation passed (3 scenarios).");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Targeted movement ability validation failed: {exception}");
        }
    }

    private static void ValidateSnapshot()
    {
        TargetedMovementAbilityDefinition definition =
            ScriptableObject.CreateInstance<
                TargetedMovementAbilityDefinition>();
        try
        {
            Assert(!definition.TryCreateSnapshot(out _),
                "Unconfigured movement definition was usable.");
            SerializedObject data = new(definition);
            data.FindProperty("abilityId").stringValue =
                "ability:test-charge";
            SerializedProperty effect = data.FindProperty("selfEffect");
            effect.FindPropertyRelative("enabled").boolValue = true;
            effect.FindPropertyRelative("effectId").stringValue =
                "effect:test-charge-speed";
            effect.FindPropertyRelative("statType").enumValueIndex =
                (int)StatType.MoveSpeed;
            effect.FindPropertyRelative("modifierType").enumValueIndex =
                (int)StatModifierType.Increased;
            effect.FindPropertyRelative("valuePerStack").floatValue = 20f;
            effect.FindPropertyRelative("maximumStacks").intValue = 1;
            effect.FindPropertyRelative("duration").floatValue = 3f;
            data.ApplyModifiedPropertiesWithoutUndo();

            Assert(definition.TryCreateSnapshot(
                       out TargetedMovementAbilitySnapshot snapshot) &&
                   snapshot.Execution.AbilityId ==
                       "ability:test-charge" &&
                   snapshot.Execution.ResourceCost == 20f &&
                   snapshot.Execution.Cooldown == 10d &&
                   snapshot.UseRange == 8f &&
                   snapshot.SpeedMultiplier == 4f &&
                   snapshot.ArrivalDistance == 1.25f &&
                   snapshot.AnimatorTrigger.Length == 0 &&
                   snapshot.SelfEffect?.StatType == StatType.MoveSpeed &&
                   snapshot.SelfEffect.ValuePerStack == 20f &&
                   snapshot.SelfEffect.Duration == 3d &&
                   snapshot.PostActionPolicy ==
                       PostAbilityActionPolicy
                           .ContinueBasicAttackOnPrimaryTarget,
                "Movement definition did not create the expected snapshot.");

            data.FindProperty("useRange").floatValue = 12f;
            data.FindProperty("movementSpeedMultiplier").floatValue = 6f;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert(snapshot.UseRange == 8f &&
                   snapshot.SpeedMultiplier == 4f,
                "Authored changes mutated an existing movement snapshot.");
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    private static void ValidateTargetingAndMovement()
    {
        GameObject source = TemporaryObject("Charge_Source");
        GameObject targetObject = TemporaryObject("Charge_Target");
        GameObject obstacle = TemporaryObject("Charge_Obstacle");
        try
        {
            source.transform.position = Vector3.zero;
            targetObject.transform.position = Vector3.right * 4f;
            ValidationTarget target =
                targetObject.AddComponent<ValidationTarget>();
            obstacle.layer = ObstacleLayer;
            obstacle.transform.position = Vector3.right * 2f;
            obstacle.transform.localScale = Vector3.one * 0.2f;
            obstacle.AddComponent<BoxCollider>();
            Physics.SyncTransforms();

            TargetedMovementAbilitySnapshot snapshot = Snapshot(
                1 << ObstacleLayer);
            Assert(snapshot.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       null)).Status == AbilityCommitStatus.MissingTarget,
                "Movement ability accepted a missing target.");
            Assert(snapshot.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       target)).Status == AbilityCommitStatus.Obstructed,
                "Movement ability ignored an obstruction.");

            obstacle.transform.position = Vector3.forward * 20f;
            Physics.SyncTransforms();
            Assert(snapshot.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       target)).Status == AbilityCommitStatus.NeedsFacing,
                "Movement ability skipped its facing requirement.");
            source.transform.rotation = Quaternion.LookRotation(Vector3.right);
            AbilityUseContext ready = new(source.transform, target);
            Assert(snapshot.CanCommit(ready) &&
                   snapshot.TryGetMovement(
                       ready,
                       out AbilityMovementDirective movement) &&
                   movement.Destination == targetObject.transform.position &&
                   movement.SpeedMultiplier == 4f &&
                   movement.ArrivalDistance == 1.25f,
                "Movement ability lost its target or authored motion.");

            targetObject.transform.position = Vector3.right * 8.01f;
            Physics.SyncTransforms();
            Assert(snapshot.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       target)).Status == AbilityCommitStatus.OutOfRange,
                "Movement ability committed outside its range.");
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(obstacle);
        }
    }

    private static void ValidateReleaseEffect()
    {
        GameObject source = TemporaryObject("ChargeRelease_Source");
        GameObject targetObject = TemporaryObject("ChargeRelease_Target");
        try
        {
            ValidationEffectReceiver receiver =
                source.AddComponent<ValidationEffectReceiver>();
            ValidationTarget target =
                targetObject.AddComponent<ValidationTarget>();
            TargetedMovementAbilitySnapshot snapshot = Snapshot(0);
            AbilityExecutionService service = new(
                Actor(),
                new FreeResourceGateway());
            CombatExecutionId executionId = CombatExecutionId.New();
            Assert(service.TryCommit(
                       executionId,
                       snapshot.Execution,
                       0d).Success,
                "Movement ability did not commit.");
            AbilityExecutionResult release = service.TryRelease(
                executionId,
                snapshot.Execution.WindUp);
            Assert(release.Success,
                "Movement ability did not release.");
            CombatExecutionReport report = snapshot.Release(
                new AbilityUseContext(source.transform, target),
                release.Execution,
                snapshot.Execution.WindUp);
            Assert(report.ExecutionId == executionId &&
                   report.ResolutionCount == 0 &&
                   receiver.ApplicationCount == 1 &&
                   receiver.LastDefinition == snapshot.SelfEffect &&
                   receiver.LastSource == Actor() &&
                   receiver.LastSimulationTime ==
                       snapshot.Execution.WindUp,
                "Movement release lost its self effect or attribution.");
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(targetObject);
        }
    }

    private static TargetedMovementAbilitySnapshot Snapshot(
        int obstructionMask)
    {
        TimedStackingStatEffectDefinition effect = new(
            "effect:test-charge-speed",
            StatType.MoveSpeed,
            StatModifierType.Increased,
            20f,
            1,
            3d);
        return new TargetedMovementAbilitySnapshot(
            new AbilityExecutionDefinition(
                "ability:test-charge",
                20f,
                10d,
                0.4d,
                0.2d),
            8f,
            obstructionMask,
            45f,
            4f,
            1.25f,
            string.Empty,
            effect);
    }

    private static CombatActorReference Actor()
    {
        return new CombatActorReference(
            "player:charge-validation",
            CombatActorKind.Player);
    }

    private static GameObject TemporaryObject(string name)
    {
        GameObject value = new(name)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return value;
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

    private sealed class ValidationTarget : MonoBehaviour, ITargetable
    {
        public Transform AimPoint => transform;
        public bool IsTargetable => true;
    }

    private sealed class ValidationEffectReceiver :
        MonoBehaviour,
        ITimedStackingStatEffectReceiver
    {
        public int ApplicationCount { get; private set; }
        public TimedStackingStatEffectDefinition LastDefinition { get; private set; }
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
