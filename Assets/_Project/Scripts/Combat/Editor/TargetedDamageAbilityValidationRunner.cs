using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TargetedDamageAbilityValidationRunner
{
    private const int ObstacleLayer = 30;

    [MenuItem("Tools/Titanhold/Validate Targeted Damage Ability")]
    public static void Validate()
    {
        try
        {
            ValidateSnapshot();
            ValidateCommitRules();
            ValidateReleaseRules();
            Debug.Log(
                "Targeted damage ability validation passed (3 scenarios).");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Targeted damage ability validation failed: {exception}");
        }
    }

    private static void ValidateSnapshot()
    {
        TargetedDamageAbilityDefinition definition =
            ScriptableObject.CreateInstance<TargetedDamageAbilityDefinition>();
        try
        {
            Assert(!definition.TryCreateSnapshot(20f, out _),
                "Unconfigured targeted definition was usable.");
            SerializedObject data = new(definition);
            data.FindProperty("abilityId").stringValue =
                "ability:power-strike";
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert(definition.TryCreateSnapshot(
                       20f,
                       out TargetedDamageAbilitySnapshot snapshot) &&
                   snapshot.Damage == 30f &&
                   snapshot.UseRange == 2f &&
                   snapshot.ReleaseRange == 3f,
                "Valid targeted definition did not create its snapshot.");

            data.FindProperty("damageMultiplier").floatValue = 4f;
            data.FindProperty("useRange").floatValue = 5f;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert(snapshot.Damage == 30f && snapshot.UseRange == 2f,
                "Authored changes mutated an existing targeted snapshot.");
            Assert(((IRuntimeAbilityDefinition)definition)
                       .TryCreateRuntimeSnapshot(
                           new AbilityActorSnapshot(20f),
                           out IRuntimeAbilitySnapshot runtime) &&
                   runtime is TargetedDamageAbilitySnapshot next &&
                   next.Damage == 80f && next.UseRange == 5f,
                "Runtime contract did not return the updated targeted snapshot.");
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    private static void ValidateCommitRules()
    {
        GameObject source = TemporaryObject("Targeted_Source");
        GameObject targetObject = TemporaryObject("Targeted_Target");
        GameObject obstacle = TemporaryObject("Targeted_Obstacle");
        try
        {
            source.transform.position = Vector3.zero;
            ValidationTarget target =
                targetObject.AddComponent<ValidationTarget>();
            targetObject.transform.position = Vector3.right;
            obstacle.layer = ObstacleLayer;
            obstacle.transform.position = Vector3.right * 0.5f;
            obstacle.transform.localScale = Vector3.one * 0.2f;
            obstacle.AddComponent<BoxCollider>();
            Physics.SyncTransforms();

            TargetedDamageAbilitySnapshot ability = Snapshot(
                1 << ObstacleLayer);
            Assert(ability.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       null)).Status == AbilityCommitStatus.MissingTarget,
                "Targeted ability accepted a missing target.");
            Assert(!ability.CanCommit(new AbilityUseContext(
                       source.transform,
                       source.AddComponent<ValidationTarget>())),
                "Targeted ability accepted its own actor.");
            Assert(ability.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       target)).Status == AbilityCommitStatus.Obstructed,
                "Targeted ability ignored an obstruction.");

            obstacle.transform.position = Vector3.forward * 5f;
            Physics.SyncTransforms();
            Assert(ability.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       target)).Status == AbilityCommitStatus.NeedsFacing,
                "Targeted ability did not request facing before commit.");
            source.transform.rotation = Quaternion.LookRotation(Vector3.right);
            Assert(ability.CanCommit(new AbilityUseContext(
                       source.transform,
                       target)),
                "Targeted ability rejected a reachable selected target.");
            targetObject.transform.position = Vector3.right * 2.01f;
            Physics.SyncTransforms();
            AbilityCommitEvaluation outOfRange = ability.EvaluateCommit(
                new AbilityUseContext(
                       source.transform,
                       target));
            Assert(outOfRange.Status == AbilityCommitStatus.OutOfRange &&
                   outOfRange.CanReposition,
                "Targeted ability committed outside its use range.");
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(obstacle);
        }
    }

    private static void ValidateReleaseRules()
    {
        GameObject source = TemporaryObject("TargetedRelease_Source");
        GameObject targetObject = TemporaryObject("TargetedRelease_Target");
        try
        {
            source.transform.position = Vector3.zero;
            targetObject.transform.position = Vector3.right;
            CharacterStats stats = targetObject.AddComponent<CharacterStats>();
            stats.Block.SetBaseValue(StatType.MaxHealth, 100f);
            stats.Block.SetBaseValue(StatType.Armor, 100f);
            Health health = targetObject.AddComponent<Health>();
            SerializedObject healthData = new(health);
            healthData.FindProperty("characterStats").objectReferenceValue =
                stats;
            healthData.ApplyModifiedPropertiesWithoutUndo();
            health.RestoreFull();
            ValidationTarget target =
                targetObject.AddComponent<ValidationTarget>();
            TargetedDamageAbilitySnapshot ability = Snapshot(0);
            AbilityExecutionDefinition executionDefinition =
                ability.Execution;
            AbilityExecutionService service = new(Actor());
            CombatExecutionId executionId = CombatExecutionId.New();
            Assert(service.TryCommit(
                       executionId,
                       executionDefinition,
                       0d).Success,
                "Targeted ability did not commit.");

            targetObject.transform.position = Vector3.right * 2.5f;
            Physics.SyncTransforms();
            AbilityExecutionResult release = service.TryRelease(
                executionId,
                executionDefinition.WindUp);
            Assert(release.Success, "Targeted ability did not release.");
            CombatExecutionReport report = ability.Release(
                new AbilityUseContext(source.transform, target),
                release.Execution);
            Assert(report.ResolutionCount == 1 &&
                   health.CurrentHealth == 85f,
                $"Targeted release ignored its grace range or release-time armor " +
                $"(resolutions: {report.ResolutionCount}, health: {health.CurrentHealth}).");
            DamageRequest request = report[0].Result.Request;
            Assert(request.ExecutionId == executionId &&
                   request.Source == Actor() &&
                   request.AbilityId == "ability:power-strike" &&
                   request.RawDamage == 30f,
                "Targeted release lost its execution attribution.");

            targetObject.transform.position = Vector3.right * 3.01f;
            Physics.SyncTransforms();
            Assert(ability.Release(
                       new AbilityUseContext(source.transform, target),
                       release.Execution).ResolutionCount == 0,
                "Targeted release hit outside its expanded release range.");
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(targetObject);
        }
    }

    private static TargetedDamageAbilitySnapshot Snapshot(
        int obstructionMask)
    {
        return new TargetedDamageAbilitySnapshot(
            new AbilityExecutionDefinition(
                "ability:power-strike",
                0f,
                3d,
                0.2d,
                0.3d),
            30f,
            2f,
            1.5f,
            obstructionMask,
            45f,
            "Attack");
    }

    private static GameObject TemporaryObject(string name)
    {
        return new GameObject(name)
        {
            hideFlags = HideFlags.DontSave
        };
    }

    private static CombatActorReference Actor()
    {
        return new CombatActorReference(
            "player:targeted-validation",
            CombatActorKind.Player);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class ValidationTarget :
        MonoBehaviour,
        ITargetable
    {
        public Transform AimPoint => transform;
        public bool IsTargetable
        {
            get
            {
                Health health = GetComponent<Health>();
                return health == null || health.IsAlive;
            }
        }
    }
}
