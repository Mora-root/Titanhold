using System;
using System.Collections.Generic;
using Titanhold.Combat.Flasks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerFlaskController : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerResource primaryResource;
    [SerializeField] private FlaskDefinition[] equippedFlasks =
        Array.Empty<FlaskDefinition>();

    private FlaskUseService service;

    public Health Health => health;
    public PlayerResource PrimaryResource => primaryResource;
    public IReadOnlyList<FlaskDefinition> EquippedFlasks =>
        equippedFlasks ?? Array.Empty<FlaskDefinition>();
    public bool IsInitialized => service != null;
    public int SlotCount => service?.SlotCount ?? 0;

#if UNITY_EDITOR
    public void ConfigureForEditor(
        Health configuredHealth,
        PlayerResource configuredPrimaryResource,
        FlaskDefinition[] configuredFlasks)
    {
        health = configuredHealth;
        primaryResource = configuredPrimaryResource;
        equippedFlasks = configuredFlasks ?? Array.Empty<FlaskDefinition>();
        service = null;
    }
#endif

    private void Awake()
    {
        TryInitialize();
    }

    public bool TryInitialize()
    {
        if (service != null)
            return true;

        health ??= GetComponent<Health>();
        primaryResource ??= GetComponent<PlayerResource>();
        if (health == null ||
            primaryResource == null ||
            equippedFlasks == null ||
            equippedFlasks.Length == 0)
        {
            return false;
        }

        FlaskDefinitionSnapshot[] snapshots =
            new FlaskDefinitionSnapshot[equippedFlasks.Length];
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < equippedFlasks.Length; i++)
        {
            FlaskDefinition definition = equippedFlasks[i];
            if (definition == null ||
                !definition.TryCreateSnapshot(out snapshots[i]) ||
                !ids.Add(snapshots[i].FlaskId))
            {
                return false;
            }
        }

        service = new FlaskUseService(
            snapshots,
            new PlayerRecoveryGateway(health, primaryResource));
        return true;
    }

    public FlaskUseResult TryUseSlot(int slotIndex)
    {
        if (!TryInitialize())
        {
            return new FlaskUseResult(
                FlaskUseStatus.TargetUnavailable,
                slotIndex,
                string.Empty,
                0f);
        }

        return service.TryUse(slotIndex, Time.timeAsDouble);
    }

    public bool TryGetCooldown(
        int slotIndex,
        double now,
        out FlaskCooldownSnapshot snapshot)
    {
        snapshot = default;
        return TryInitialize() &&
               service.TryGetCooldown(slotIndex, now, out snapshot);
    }

    public bool TryGetDefinition(
        int slotIndex,
        out FlaskDefinition definition)
    {
        definition = null;
        if (equippedFlasks == null ||
            slotIndex < 0 ||
            slotIndex >= equippedFlasks.Length)
        {
            return false;
        }

        definition = equippedFlasks[slotIndex];
        return definition != null;
    }

    private sealed class PlayerRecoveryGateway : IFlaskRecoveryGateway
    {
        private readonly Health health;
        private readonly PlayerResource primaryResource;

        public PlayerRecoveryGateway(
            Health health,
            PlayerResource primaryResource)
        {
            this.health = health ??
                throw new ArgumentNullException(nameof(health));
            this.primaryResource = primaryResource ??
                throw new ArgumentNullException(nameof(primaryResource));
        }

        public bool IsAlive => health.IsAlive;

        public bool TryGetRecoveryState(
            FlaskRecoveryTarget target,
            out float current,
            out float maximum)
        {
            switch (target)
            {
                case FlaskRecoveryTarget.Health:
                    current = health.CurrentHealth;
                    maximum = health.MaxHealth;
                    return true;

                case FlaskRecoveryTarget.PrimaryResource:
                    current = primaryResource.CurrentResource;
                    maximum = primaryResource.MaxResource;
                    return true;

                default:
                    current = 0f;
                    maximum = 0f;
                    return false;
            }
        }

        public bool TryRestore(
            FlaskRecoveryTarget target,
            float requestedAmount,
            out float restoredAmount)
        {
            restoredAmount = 0f;
            if (!IsAlive || requestedAmount <= 0f)
                return false;

            switch (target)
            {
                case FlaskRecoveryTarget.Health:
                {
                    float before = health.CurrentHealth;
                    health.Heal(requestedAmount);
                    restoredAmount = health.CurrentHealth - before;
                    return restoredAmount > 0f;
                }

                case FlaskRecoveryTarget.PrimaryResource:
                {
                    float before = primaryResource.CurrentResource;
                    primaryResource.Restore(requestedAmount);
                    restoredAmount = primaryResource.CurrentResource - before;
                    return restoredAmount > 0f;
                }

                default:
                    return false;
            }
        }
    }
}
