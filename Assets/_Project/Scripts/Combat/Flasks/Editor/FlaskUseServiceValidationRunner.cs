using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Combat.Flasks.Editor
{
    public static class FlaskUseServiceValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Flask Use Foundation")]
        public static void Validate()
        {
            try
            {
                Gateway gateway = new();
                FlaskUseService service = new(
                    new[]
                    {
                        new FlaskDefinitionSnapshot(
                            "flask:health",
                            FlaskRecoveryTarget.Health,
                            0.5f,
                            30d),
                        new FlaskDefinitionSnapshot(
                            "flask:primary-resource",
                            FlaskRecoveryTarget.PrimaryResource,
                            0.5f,
                            30d)
                    },
                    gateway);

                gateway.Health = 25f;
                FlaskUseResult health = service.TryUse(0, 10d);
                Assert(health.Success &&
                       Mathf.Approximately(gateway.Health, 75f) &&
                       Mathf.Approximately(health.RestoredAmount, 50f),
                    "Health flask did not restore 50% of maximum health.");
                Assert(service.TryGetCooldown(0, 10d, out var healthCooldown) &&
                       healthCooldown.IsCoolingDown &&
                       Math.Abs(healthCooldown.Remaining - 30d) < 0.000001d &&
                       Math.Abs(healthCooldown.NormalizedRemaining - 1d) < 0.000001d,
                    "Health flask cooldown snapshot is invalid.");

                gateway.Resource = 20f;
                FlaskUseResult resource = service.TryUse(1, 10d);
                Assert(resource.Success &&
                       Mathf.Approximately(gateway.Resource, 70f),
                    "Primary-resource flask did not use its independent slot.");
                Assert(service.TryUse(0, 20d).Status ==
                       FlaskUseStatus.CoolingDown,
                    "A cooling-down flask was accepted.");

                gateway.Health = gateway.MaximumHealth;
                Assert(service.TryUse(0, 41d).Status ==
                       FlaskUseStatus.AlreadyFull,
                    "A full recovery target consumed a flask.");
                Assert(service.TryGetCooldown(0, 41d, out healthCooldown) &&
                       !healthCooldown.IsCoolingDown,
                    "A rejected full-health use started cooldown.");

                gateway.Alive = false;
                gateway.Resource = 10f;
                Assert(service.TryUse(1, 41d).Status ==
                       FlaskUseStatus.UserUnavailable,
                    "A dead user consumed a flask.");
                Assert(service.TryUse(-1, 41d).Status ==
                       FlaskUseStatus.InvalidSlot,
                    "An invalid slot was accepted.");

                Debug.Log("Flask use foundation validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Flask use foundation validation failed: {exception}");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class Gateway : IFlaskRecoveryGateway
        {
            public bool Alive = true;
            public float Health = 100f;
            public float Resource = 100f;
            public float MaximumHealth = 100f;
            public float MaximumResource = 100f;

            public bool IsAlive => Alive;

            public bool TryGetRecoveryState(
                FlaskRecoveryTarget target,
                out float current,
                out float maximum)
            {
                if (target == FlaskRecoveryTarget.Health)
                {
                    current = Health;
                    maximum = MaximumHealth;
                    return true;
                }

                current = Resource;
                maximum = MaximumResource;
                return true;
            }

            public bool TryRestore(
                FlaskRecoveryTarget target,
                float requestedAmount,
                out float restoredAmount)
            {
                float before;
                if (target == FlaskRecoveryTarget.Health)
                {
                    before = Health;
                    Health = Mathf.Min(MaximumHealth, Health + requestedAmount);
                    restoredAmount = Health - before;
                    return restoredAmount > 0f;
                }

                before = Resource;
                Resource = Mathf.Min(
                    MaximumResource,
                    Resource + requestedAmount);
                restoredAmount = Resource - before;
                return restoredAmount > 0f;
            }
        }
    }
}
