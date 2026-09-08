using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using Titanhold.Combat.Effects;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ConeDamageAbilityValidationRunner
{
    private const int TargetLayer = 28;

    [MenuItem("Tools/Titanhold/Validate Cone Damage Ability")]
    public static void Validate()
    {
        try
        {
            ValidateDefinitionSnapshot();
            ValidateCommitRules();
            ValidateReleaseAndEffect();
            Debug.Log(
                "Cone damage ability validation passed (3 scenarios).");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Cone damage ability validation failed: {exception}");
        }
    }

    private static void ValidateDefinitionSnapshot()
    {
        ConeDamageAbilityDefinition definition =
            ScriptableObject.CreateInstance<ConeDamageAbilityDefinition>();
        try
        {
            Assert(!definition.TryCreateSnapshot(20f, out _),
                "Unconfigured cone definition was usable.");
            SerializedObject data = new(definition);
            data.FindProperty("abilityId").stringValue =
                "ability:cleave";
            data.FindProperty("targetMask").intValue = 1 << TargetLayer;
            data.ApplyModifiedPropertiesWithoutUndo();

            Assert(definition.TryCreateSnapshot(
                       20f,
                       out ConeDamageAbilitySnapshot snapshot) &&
                   snapshot.PrimaryDamage == 20f &&
                   snapshot.SecondaryDamage == 6f &&
                   snapshot.SecondaryDamageMultiplier == 0.3f &&
                   snapshot.UseRange == 2.5f &&
                   snapshot.ConeAngle == 120f &&
                   snapshot.OnHitEffect == null &&
                   snapshot.PostActionPolicy ==
                       PostAbilityActionPolicy
                           .ContinueBasicAttackOnPrimaryTarget,
                "Valid Cleave definition did not create a clean damage snapshot.");

            data.FindProperty("damageMultiplier").floatValue = 4f;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert(snapshot.PrimaryDamage == 20f &&
                   snapshot.SecondaryDamage == 6f &&
                   snapshot.OnHitEffect == null,
                "Authored changes mutated an existing cone snapshot.");
            Assert(definition.TryCreateSnapshot(
                       20f,
                       out ConeDamageAbilitySnapshot next) &&
                   next.PrimaryDamage == 80f &&
                   next.SecondaryDamage == 24f &&
                   next.OnHitEffect == null,
                "The next cone snapshot ignored updated authoring values.");
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    private static void ValidateCommitRules()
    {
        GameObject source = TemporaryObject("ConeCommit_Source");
        GameObject targetObject = TemporaryObject("ConeCommit_Target");
        try
        {
            source.transform.position = Vector3.zero;
            source.transform.forward = Vector3.forward;
            targetObject.transform.position = Vector3.forward;
            ValidationTarget target =
                targetObject.AddComponent<ValidationTarget>();
            ConeDamageAbilitySnapshot ability = Snapshot(null);

            Assert(ability.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       null)).Status == AbilityCommitStatus.MissingTarget,
                "Cone ability accepted a missing selected target.");
            Assert(ability.CanCommit(new AbilityUseContext(
                       source.transform,
                       target)),
                "Cone ability rejected a reachable target.");
            targetObject.transform.position = Vector3.back;
            Assert(ability.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       target)).Status == AbilityCommitStatus.NeedsFacing,
                "Cone ability ignored its pre-commit facing rule.");
            targetObject.transform.position = Vector3.forward * 2.51f;
            Assert(ability.EvaluateCommit(new AbilityUseContext(
                       source.transform,
                       target)).Status == AbilityCommitStatus.OutOfRange,
                "Cone ability committed outside its use range.");
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(targetObject);
        }
    }

    private static void ValidateReleaseAndEffect()
    {
        Vector3 origin = new(12345f, 12345f, 12345f);
        GameObject source = TemporaryObject("ConeRelease_Source");
        GameObject front = CreateDamageTarget(
            "ConeRelease_Front",
            origin + Vector3.forward);
        GameObject diagonal = CreateDamageTarget(
            "ConeRelease_Diagonal",
            origin + new Vector3(0.7f, 0f, 1f));
        GameObject behind = CreateDamageTarget(
            "ConeRelease_Behind",
            origin + Vector3.back);
        try
        {
            source.transform.position = origin;
            source.transform.forward = Vector3.forward;
            front.AddComponent<BoxCollider>();
            Physics.SyncTransforms();

            ConeDamageAbilitySnapshot ability = Snapshot(null);
            AbilityExecutionService service = new(Actor());
            CombatExecutionId executionId = CombatExecutionId.New();
            Assert(service.TryCommit(
                       executionId,
                       ability.Execution,
                       0d).Success,
                "Cone ability did not commit.");
            AbilityExecutionResult release = service.TryRelease(
                executionId,
                ability.Execution.WindUp);
            Assert(release.Success, "Cone ability did not release.");

            CombatExecutionReport report = ability.Release(
                new AbilityUseContext(
                    source.transform,
                    front.GetComponent<ValidationTarget>()),
                release.Execution,
                ability.Execution.WindUp);
            Assert(report.ResolutionCount == 2,
                "Cone did not hit two distinct in-angle targets once each.");
            Assert(front.GetComponent<Health>().CurrentHealth == 80f &&
                   diagonal.GetComponent<Health>().CurrentHealth == 94f &&
                   behind.GetComponent<Health>().CurrentHealth == 100f,
                "Cleave lost its full primary hit, 30% secondary hit, " +
                "directional filter, or target deduplication.");
            bool foundPrimary = false;
            bool foundSecondary = false;
            for (int i = 0; i < report.ResolutionCount; i++)
            {
                float rawDamage = report[i].Result.Request.RawDamage;
                foundPrimary |= rawDamage == 20f;
                foundSecondary |= rawDamage == 6f;
            }
            Assert(foundPrimary && foundSecondary,
                "Cleave report did not distinguish primary and secondary damage.");

            Assert(front.GetComponent<ValidationEffectReceiver>()
                       .ApplicationCount == 0 &&
                   diagonal.GetComponent<ValidationEffectReceiver>()
                       .ApplicationCount == 0,
                "Cleave applied an unauthored effect.");
            Assert(behind.GetComponent<ValidationEffectReceiver>()
                       .ApplicationCount == 0,
                "Cone applied its effect behind the source.");
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(front);
            Object.DestroyImmediate(diagonal);
            Object.DestroyImmediate(behind);
        }
    }

    private static ConeDamageAbilitySnapshot Snapshot(
        TimedStackingStatEffectDefinition effect)
    {
        return new ConeDamageAbilitySnapshot(
            new AbilityExecutionDefinition(
                "ability:cleave",
                0f,
                3d,
                0.2d,
                0.3d),
            20f,
            0.3f,
            2.5f,
            120f,
            1 << TargetLayer,
            0,
            45f,
            "Attack",
            effect);
    }

    private static GameObject CreateDamageTarget(
        string name,
        Vector3 position)
    {
        GameObject target = TemporaryObject(name);
        target.layer = TargetLayer;
        target.transform.position = position;
        CharacterStats stats = target.AddComponent<CharacterStats>();
        stats.Block.SetBaseValue(StatType.MaxHealth, 100f);
        Health health = target.AddComponent<Health>();
        SerializedObject healthData = new(health);
        healthData.FindProperty("characterStats").objectReferenceValue = stats;
        healthData.ApplyModifiedPropertiesWithoutUndo();
        health.RestoreFull();
        target.AddComponent<ValidationTarget>();
        target.AddComponent<ValidationEffectReceiver>();
        target.AddComponent<SphereCollider>();
        return target;
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
            "player:cone-validation",
            CombatActorKind.Player);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class ValidationTarget : MonoBehaviour, ITargetable
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

    private sealed class ValidationEffectReceiver :
        MonoBehaviour,
        ITimedStackingStatEffectReceiver
    {
        public int ApplicationCount { get; private set; }

        public bool TryApply(
            TimedStackingStatEffectDefinition definition,
            CombatActorReference source,
            double simulationTime,
            out TimedStatEffectSnapshot snapshot)
        {
            ApplicationCount++;
            snapshot = default;
            return true;
        }
    }
}
