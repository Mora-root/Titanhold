using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AreaDamageAbilityValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Area Damage Ability")]
    public static void Validate()
    {
        try
        {
            ValidateSnapshot();
            ValidateResourceCommit();
            ValidateAreaRelease();
            ValidateExecutorSelection();
            Debug.Log("Area damage ability validation passed (4 scenarios).");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Area damage ability validation failed: {exception}");
        }
    }

    private static void ValidateSnapshot()
    {
        AreaDamageAbilityDefinition definition = ScriptableObject.CreateInstance<AreaDamageAbilityDefinition>();
        try
        {
            Assert(!definition.TryCreateSnapshot(20f, out _), "Unconfigured definition was usable.");
            SerializedObject data = new(definition);
            data.FindProperty("abilityId").stringValue = "ability:spin";
            data.FindProperty("targetMask").intValue = 1;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert(definition.TryCreateSnapshot(20f, out var snapshot), "Valid definition was rejected.");
            data.FindProperty("damageMultiplier").floatValue = 4f;
            data.FindProperty("resourceCost").floatValue = 80f;
            data.FindProperty("radius").floatValue = 8f;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert(snapshot.Damage == 30f && snapshot.Execution.ResourceCost == 20f && snapshot.Radius == 2.5f,
                "Authored changes mutated an existing snapshot.");
            Assert(definition.TryCreateSnapshot(40f, out var next) && next.Damage == 160f,
                "The next cast did not use updated offensive values.");
            Assert(!definition.TryCreateSnapshot(float.PositiveInfinity, out _) &&
                   !definition.TryCreateSnapshot(float.MaxValue, out _), "Invalid/overflowing damage was accepted.");
            data.FindProperty("windUp").floatValue = -1f;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert(!definition.TryCreateSnapshot(20f, out _), "Negative timing was accepted.");
        }
        finally { Object.DestroyImmediate(definition); }
    }

    private static void ValidateResourceCommit()
    {
        GameObject owner = TemporaryObject("Ability_Resource");
        try
        {
            PlayerResource resource = owner.AddComponent<PlayerResource>();
            resource.RestoreFull();
            AbilityExecutionService service = new(Actor(), new ResourceGateway(resource));
            AbilityExecutionDefinition definition = new("ability:spin", 20f, 3d, 0.2d, 0.3d);
            CombatExecutionId id = CombatExecutionId.New();
            int notifications = 0;
            resource.OnResourceChanged += (balance, _) =>
            {
                notifications++;
                Assert(balance == 80f && service.CurrentExecution?.ExecutionId == id,
                    "Observer saw resource spend before committed execution.");
            };

            IDisposable outer = resource.DeferNotifications();
            using (resource.DeferNotifications())
                Assert(service.TryCommit(id, definition, 0d).Success, "Resource-backed commit failed.");
            Assert(notifications == 0, "Nested deferral flushed early.");
            outer.Dispose();
            outer.Dispose();
            Assert(notifications == 1, "Commit did not publish exactly one resource change.");
            Assert(service.TryCancel(id, 0.1d).Success, "Committed ability could not be cancelled.");
            using (resource.DeferNotifications())
                Assert(!service.TryCommit(CombatExecutionId.New(), definition, 1d).Success,
                    "Cancelled ability bypassed cooldown.");
            using (resource.DeferNotifications())
                Assert(!service.TryCommit(CombatExecutionId.New(),
                    new AbilityExecutionDefinition("expensive", 200f, 0d, 0d, 0d), 3d).Success,
                    "Unaffordable ability was committed.");
            Assert(notifications == 1 && resource.CurrentResource == 80f,
                "Failed commands or cancellation changed committed spend.");
        }
        finally { Object.DestroyImmediate(owner); }
    }

    private static void ValidateAreaRelease()
    {
        GameObject source = TemporaryObject("Ability_Source");
        GameObject first = TemporaryObject("Ability_FirstTarget");
        GameObject second = TemporaryObject("Ability_SecondTarget");
        try
        {
            Vector3 origin = new(12345f, 12345f, 12345f);
            source.transform.position = origin;
            first.transform.position = origin + Vector3.right;
            second.transform.position = origin + Vector3.left;
            Health self = source.AddComponent<Health>();
            self.RestoreFull();
            source.AddComponent<SphereCollider>();
            CharacterStats stats = first.AddComponent<CharacterStats>();
            stats.Block.SetBaseValue(StatType.MaxHealth, 100f);
            Health firstHealth = first.AddComponent<Health>();
            SerializedObject healthData = new(firstHealth);
            healthData.FindProperty("characterStats").objectReferenceValue = stats;
            healthData.ApplyModifiedPropertiesWithoutUndo();
            firstHealth.RestoreFull();
            first.AddComponent<SphereCollider>();
            first.AddComponent<BoxCollider>();
            Health secondHealth = second.AddComponent<Health>();
            secondHealth.RestoreFull();
            second.AddComponent<SphereCollider>();
            Physics.SyncTransforms();

            AbilityExecutionDefinition definition = new("ability:spin", 0f, 3d, 0.2d, 0.3d);
            AreaDamageAbilitySnapshot ability = new(definition, 30f, 2.5f, 1, "Spin");
            AbilityExecutionService service = new(Actor());
            CombatExecutionId id = CombatExecutionId.New();
            Assert(service.TryCommit(id, definition, 0d).Success, "Area commit failed.");
            Assert(!service.TryRelease(id, 0.1d).Success, "Area released during wind-up.");
            stats.Block.SetBaseValue(StatType.Armor, 100f);
            AbilityExecutionResult release = service.TryRelease(id, 0.2d);
            Assert(release.Success, "Area release failed.");
            CombatExecutionReport report = AreaDamageAbilityEffect.Apply(source.transform, release.Execution, ability);
            Assert(report.ResolutionCount == 2 && report.ExecutionId == id,
                "Area did not produce one batch with two distinct targets.");
            Assert(firstHealth.CurrentHealth == 85f && secondHealth.CurrentHealth == 70f && self.CurrentHealth == 100f,
                "Area ignored release-time defense, hit twice, or damaged its source.");
            for (int i = 0; i < report.ResolutionCount; i++)
            {
                DamageRequest request = report[i].Result.Request;
                Assert(request.ExecutionId == id && request.Source == Actor() &&
                       request.AbilityId == "ability:spin" && request.RawDamage == 30f,
                    "Area lost its committed identity or damage.");
            }
            Assert(!service.TryRelease(id, 0.3d).Success, "Area authorized a duplicate release.");
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }

    private static void ValidateExecutorSelection()
    {
        GameObject legacyOwner = TemporaryObject("Ability_LegacySelection");
        GameObject newOwner = TemporaryObject("Ability_NewSelection");
        GameObject invalidOwner = TemporaryObject("Ability_InvalidSelection");
        try
        {
            PlayerSkillExecutor legacy = legacyOwner.AddComponent<PlayerSkillExecutor>();
            PlayerBrain legacyBrain = legacyOwner.AddComponent<PlayerBrain>();
            Assert(ReferenceEquals(legacyBrain.Skills, legacy), "Existing player lost legacy selection.");
            newOwner.AddComponent<PlayerSkillExecutor>();
            PlayerAbilityExecutor replacement = newOwner.AddComponent<PlayerAbilityExecutor>();
            PlayerBrain newBrain = newOwner.AddComponent<PlayerBrain>();
            SerializedObject brainData = new(newBrain);
            brainData.FindProperty("skillExecutorOverride").objectReferenceValue = replacement;
            brainData.ApplyModifiedPropertiesWithoutUndo();
            Assert(ReferenceEquals(newBrain.Skills, replacement) &&
                   ReferenceEquals(PlayerSkillCommands.Resolve(newOwner), replacement),
                "Brain and reward adapters selected different executors.");
            invalidOwner.AddComponent<PlayerSkillExecutor>();
            PlayerBrain invalidBrain = invalidOwner.AddComponent<PlayerBrain>();
            SerializedObject invalidData = new(invalidBrain);
            invalidData.FindProperty("skillExecutorOverride").objectReferenceValue = replacement;
            invalidData.ApplyModifiedPropertiesWithoutUndo();
            Assert(PlayerSkillCommands.Resolve(invalidOwner) == null,
                "Invalid cross-player override silently enabled legacy skills.");
        }
        finally
        {
            Object.DestroyImmediate(legacyOwner);
            Object.DestroyImmediate(newOwner);
            Object.DestroyImmediate(invalidOwner);
        }
    }

    private static GameObject TemporaryObject(string name) => new(name) { hideFlags = HideFlags.DontSave };
    private static CombatActorReference Actor() => new("player:ability-validation", CombatActorKind.Player);
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ResourceGateway : IAbilityResourceGateway
    {
        private readonly PlayerResource resource;
        public ResourceGateway(PlayerResource resource) => this.resource = resource;
        public bool TrySpend(float amount) => resource.TrySpend(amount);
    }
}
