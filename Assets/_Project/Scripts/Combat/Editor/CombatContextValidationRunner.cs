using System;
using System.Reflection;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

public static class CombatContextValidationRunner
{
    private const string MenuPath = "Tools/Titanhold/Validate Combat Context";

    [MenuItem(MenuPath)]
    public static void ValidateFromMenu()
    {
        try
        {
            Debug.Log(RunValidation());
        }
        catch (Exception exception)
        {
            Debug.LogError($"Combat context validation failed: {exception}");
        }
    }

    public static string RunValidation()
    {
        ValidateContextualDamageAndArmor();
        ValidateRejectedDamage();
        ValidateAttributedDeath();
        ValidateSharedExecutionId();
        ValidateLegacyDamageFallback();

        return "Combat context validation passed.";
    }

    private static void ValidateContextualDamageAndArmor()
    {
        GameObject target = new GameObject("CombatContext_ArmoredTarget");

        try
        {
            CharacterStats stats = target.AddComponent<CharacterStats>();
            stats.Block.SetBaseValue(StatType.MaxHealth, 100f);
            stats.Block.SetBaseValue(StatType.Armor, 100f);

            Health health = target.AddComponent<Health>();
            SerializedObject serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("characterStats").objectReferenceValue = stats;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
            health.RestoreFull();

            CombatExecutionId executionId = CombatExecutionId.New();
            CombatActorReference source = new CombatActorReference("player:test", CombatActorKind.Player);
            DamageRequest request = new DamageRequest(
                executionId,
                source,
                40f,
                DamageCause.BasicAttack);

            int resolvedCount = 0;
            health.OnDamageResolved += _ => resolvedCount++;

            DamageResult result = health.ApplyDamage(request);

            Assert(result.Status == DamageResolutionStatus.Applied, "Contextual damage was not applied.");
            Assert(result.HasDetailedResult, "Contextual damage did not return details.");
            Assert(!result.Killed, "Non-lethal damage was marked lethal.");
            Assert(result.Request.ExecutionId == executionId, "Execution id changed during damage resolution.");
            Assert(result.Request.Source == source, "Damage source changed during resolution.");
            AssertApproximately(result.AppliedDamage, 20f, "Applied armored damage");
            AssertApproximately(result.HealthBefore, 100f, "Health before damage");
            AssertApproximately(result.HealthAfter, 80f, "Health after damage");
            AssertApproximately(health.CurrentHealth, 80f, "Target health after contextual damage");
            Assert(resolvedCount == 1, "Damage resolved event count mismatch.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void ValidateRejectedDamage()
    {
        GameObject target = new GameObject("CombatContext_RejectedDamageTarget");

        try
        {
            Health health = target.AddComponent<Health>();
            health.RestoreFull();
            CombatActorReference source = new CombatActorReference("player:test", CombatActorKind.Player);

            DamageResult zero = health.ApplyDamage(new DamageRequest(
                CombatExecutionId.New(),
                source,
                0f,
                DamageCause.BasicAttack));
            DamageResult notANumber = health.ApplyDamage(new DamageRequest(
                CombatExecutionId.New(),
                source,
                float.NaN,
                DamageCause.BasicAttack));
            DamageResult missingExecution = health.ApplyDamage(new DamageRequest(
                default,
                source,
                10f,
                DamageCause.BasicAttack));

            Assert(!zero.WasApplied && zero.RejectionReason == DamageRejectionReason.InvalidAmount,
                "Zero damage should be rejected.");
            Assert(!notANumber.WasApplied && notANumber.RejectionReason == DamageRejectionReason.InvalidAmount,
                "NaN damage should be rejected.");
            Assert(!missingExecution.WasApplied &&
                   missingExecution.RejectionReason == DamageRejectionReason.InvalidExecutionId,
                "Damage without an execution id should be rejected.");
            AssertApproximately(health.CurrentHealth, 100f, "Rejected damage mutated health");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void ValidateAttributedDeath()
    {
        GameObject target = new GameObject("CombatContext_AttributedDeathTarget");

        try
        {
            Health health = target.AddComponent<Health>();
            EnemyDeathNotifier notifier = target.AddComponent<EnemyDeathNotifier>();
            InitializeNotifier(notifier);
            health.RestoreFull();

            int contextualDeathCount = 0;
            int legacyDeathCount = 0;
            DeathContext observedContext = default;
            notifier.DiedWithContext += (_, context) =>
            {
                contextualDeathCount++;
                observedContext = context;
            };
            notifier.Died += _ => legacyDeathCount++;

            CombatExecutionId executionId = CombatExecutionId.New();
            DamageRequest request = new DamageRequest(
                executionId,
                new CombatActorReference("player:test", CombatActorKind.Player),
                200f,
                DamageCause.Ability,
                "ability:test");

            DamageResult result = health.ApplyDamage(request);

            Assert(result.Killed && result.HasDeathContext, "Lethal damage did not create a death context.");
            Assert(contextualDeathCount == 1, "Contextual death event count mismatch.");
            Assert(legacyDeathCount == 1, "Legacy death event compatibility was broken.");
            Assert(observedContext.IsPlayerAttributed, "Player-attributed death lost its attribution.");
            Assert(observedContext.ExecutionId == executionId, "Death execution id mismatch.");
            Assert(observedContext.KillingDamage.AbilityId == "ability:test", "Ability id was not preserved.");
            Assert(health.LastDeathContext.ExecutionId == executionId, "Health lost its last death context.");
            Assert(notifier.LastDeathContext.ExecutionId == executionId, "Notifier lost its last death context.");

            DamageResult repeatedDamage = health.ApplyDamage(request);
            Assert(!repeatedDamage.WasApplied &&
                   repeatedDamage.RejectionReason == DamageRejectionReason.TargetAlreadyDead,
                "Dead target accepted repeated damage.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void ValidateSharedExecutionId()
    {
        GameObject firstTarget = new GameObject("CombatContext_BatchTargetA");
        GameObject secondTarget = new GameObject("CombatContext_BatchTargetB");

        try
        {
            Health firstHealth = firstTarget.AddComponent<Health>();
            Health secondHealth = secondTarget.AddComponent<Health>();
            firstHealth.RestoreFull();
            secondHealth.RestoreFull();

            CombatExecutionId sharedExecution = CombatExecutionId.New();
            CombatActorReference source = new CombatActorReference("player:test", CombatActorKind.Player);
            DamageRequest firstRequest = new DamageRequest(
                sharedExecution,
                source,
                200f,
                DamageCause.Ability,
                "ability:area-test");
            DamageRequest secondRequest = new DamageRequest(
                sharedExecution,
                source,
                200f,
                DamageCause.Ability,
                "ability:area-test");

            DamageResult firstResult = firstHealth.ApplyDamage(firstRequest);
            DamageResult secondResult = secondHealth.ApplyDamage(secondRequest);

            Assert(firstResult.HasDeathContext && secondResult.HasDeathContext,
                "Shared execution did not produce both death contexts.");
            Assert(firstResult.DeathContext.ExecutionId == secondResult.DeathContext.ExecutionId,
                "One area execution produced different execution ids.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstTarget);
            UnityEngine.Object.DestroyImmediate(secondTarget);
        }
    }

    private static void ValidateLegacyDamageFallback()
    {
        GameObject target = new GameObject("CombatContext_LegacyTarget");

        try
        {
            Health health = target.AddComponent<Health>();
            EnemyDeathNotifier notifier = target.AddComponent<EnemyDeathNotifier>();
            InitializeNotifier(notifier);
            health.RestoreFull();

            int legacyDeathCount = 0;
            notifier.Died += _ => legacyDeathCount++;

            health.TakeDamage(200f);

            Assert(!health.IsAlive, "Legacy TakeDamage did not kill the target.");
            Assert(legacyDeathCount == 1, "Legacy death listener was not invoked.");
            Assert(health.LastDeathContext.IsValid, "Legacy damage did not create a traceable execution id.");
            Assert(!health.LastDeathContext.IsPlayerAttributed,
                "Unattributed legacy damage was incorrectly attributed to a player.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void InitializeNotifier(EnemyDeathNotifier notifier)
    {
        InvokeLifecycle(notifier, "Awake");
        InvokeLifecycle(notifier, "OnEnable");
    }

    private static void InvokeLifecycle(EnemyDeathNotifier notifier, string methodName)
    {
        MethodInfo method = typeof(EnemyDeathNotifier).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new InvalidOperationException($"EnemyDeathNotifier.{methodName} was not found.");

        method.Invoke(notifier, null);
    }

    private static void AssertApproximately(float actual, float expected, string label)
    {
        if (Math.Abs(actual - expected) <= 0.0001f)
            return;

        throw new InvalidOperationException($"{label} failed. Expected {expected}, got {actual}.");
    }
}
