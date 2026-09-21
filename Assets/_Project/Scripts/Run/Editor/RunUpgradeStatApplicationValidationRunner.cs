using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunUpgradeStatApplicationValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Upgrade Stat Application")]
        public static void Validate()
        {
            GameObject actor = null;
            RunUpgradeStatApplicationService application = null;
            try
            {
                const string PlayerId = "player:local";
                const string DamageId = "run-upgrade:damage";
                const string HealthId = "run-upgrade:max-health";
                ValidationStatUpgrade[] definitions =
                {
                    new(
                        DamageId,
                        new StatModifierData(
                            StatType.Damage,
                            StatModifierType.Increased,
                            10f)),
                    new(
                        HealthId,
                        new StatModifierData(
                            StatType.MaxHealth,
                            StatModifierType.Increased,
                            10f))
                };
                Assert(RunUpgradeDefinitionRegistry.TryCreate(
                           definitions,
                           out RunUpgradeDefinitionRegistry registry,
                           out string registryError),
                    $"Could not prepare upgrade definitions: {registryError}");

                RunUpgradeChoiceService choices = new(registry);
                Assert(choices.TryRegisterParticipant(
                           new RunParticipantIdentity(
                               PlayerId,
                               "character:warrior")).Success,
                    "Could not register the validation participant.");
                application = new RunUpgradeStatApplicationService(
                    choices,
                    registry);

                actor = new GameObject("RunUpgradeStatApplicationValidation");
                CharacterStats stats = actor.AddComponent<CharacterStats>();
                stats.Block.SetBaseValue(StatType.Damage, 100f);
                stats.Block.SetBaseValue(StatType.MaxHealth, 100f);
                stats.AddModifier(
                    new StatModifier(
                        StatType.Damage,
                        StatModifierType.Flat,
                        20f),
                    StatModifierSource.ForSystem("Validation.BaseDamage"));
                Health health = actor.AddComponent<Health>();
                SerializedObject healthData = new(health);
                healthData.FindProperty("characterStats")
                    .objectReferenceValue = stats;
                healthData.ApplyModifiedPropertiesWithoutUndo();
                health.RestoreFull();
                health.TakeDamage(50f);

                Select(
                    choices,
                    PlayerId,
                    "choice:damage:first",
                    DamageId,
                    DamageId,
                    HealthId);
                CharacterStatsRunUpgradeModifierGateway gateway = new(stats);
                RunUpgradeStatApplicationResult bind =
                    application.TryBindParticipant(PlayerId, gateway);
                Assert(bind.Success && bind.AppliedModifierCount == 1 &&
                       Approximately(
                           stats.GetValue(StatType.Damage),
                           132f),
                    "Late binding did not reconcile the existing upgrade stack.");

                Select(
                    choices,
                    PlayerId,
                    "choice:damage:second",
                    DamageId,
                    DamageId,
                    HealthId);
                Assert(Approximately(
                           stats.GetValue(StatType.Damage),
                           144f),
                    "A repeated increased-damage upgrade did not stack additively.");

                Select(
                    choices,
                    PlayerId,
                    "choice:health",
                    HealthId,
                    DamageId,
                    HealthId);
                Assert(Approximately(health.MaxHealth, 110f) &&
                       Approximately(health.CurrentHealth, 50f),
                    "Maximum-health upgrade healed the actor or was not " +
                    $"applied (current: {health.CurrentHealth}, max: " +
                    $"{health.MaxHealth}).");

                StatModifierSource duplicateSource =
                    StatModifierSource.ForRunUpgrade(
                        "validation:duplicate");
                Assert(!stats.TryReplaceModifiersFromSourceKind(
                           StatModifierSourceKind.RunUpgrade,
                           new[]
                           {
                               new StatModifierAssignment(
                                   duplicateSource,
                                   new StatModifier(
                                       StatType.Damage,
                                       StatModifierType.Increased,
                                       5f)),
                               new StatModifierAssignment(
                                   duplicateSource,
                                   new StatModifier(
                                       StatType.Armor,
                                       StatModifierType.Increased,
                                       5f))
                           }) &&
                       Approximately(
                           stats.GetValue(StatType.Damage),
                           144f) &&
                       Approximately(health.MaxHealth, 110f),
                    "Rejected atomic replacement changed existing modifiers.");

                RunUpgradeStatApplicationResult unbind =
                    application.TryUnbindParticipant(PlayerId);
                Assert(unbind.Success &&
                       application.BoundParticipantCount == 0 &&
                       Approximately(
                           stats.GetValue(StatType.Damage),
                           120f) &&
                       Approximately(health.MaxHealth, 100f) &&
                       Approximately(health.CurrentHealth, 50f),
                    "Unbinding did not clear only the run-upgrade modifiers.");

                Debug.Log(
                    "Run Upgrade Stat Application validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Upgrade Stat Application validation failed: {exception}");
            }
            finally
            {
                application?.Dispose();
                if (actor != null)
                    UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        private static void Select(
            RunUpgradeChoiceService choices,
            string playerId,
            string choiceId,
            string selectedUpgradeId,
            params string[] candidates)
        {
            RunUpgradeChoiceResult offer = choices.TryOfferChoice(
                new RunUpgradeChoiceRequest(
                    playerId,
                    choiceId,
                    candidates,
                    candidates.Length,
                    123));
            Assert(offer.Success &&
                   choices.TrySelectUpgrade(
                       playerId,
                       choiceId,
                       selectedUpgradeId).Success,
                $"Could not select validation upgrade '{selectedUpgradeId}'.");
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) <= 0.0001f;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class ValidationStatUpgrade :
            IRunStatUpgradeDefinition
        {
            private readonly StatModifierData[] modifiers;

            public ValidationStatUpgrade(
                string upgradeId,
                params StatModifierData[] modifiers)
            {
                UpgradeId = upgradeId;
                this.modifiers = modifiers;
            }

            public string UpgradeId { get; }
            public IReadOnlyList<StatModifierData> Modifiers => modifiers;

            public bool TryValidate(out string error)
            {
                error = string.Empty;
                return modifiers != null && modifiers.Length > 0;
            }
        }
    }
}
