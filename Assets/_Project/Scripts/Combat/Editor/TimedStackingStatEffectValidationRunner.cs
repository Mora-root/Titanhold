using System;
using System.Collections.Generic;
using Titanhold.Combat;
using Titanhold.Combat.Effects;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TimedStackingStatEffectValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Timed Stacking Stat Effects")]
    public static void Validate()
    {
        try
        {
            ValidateRules();
            ValidateStackingRefreshAndExpiry();
            ValidateIndependentSources();
            ValidateAtomicCharacterStatReplacement();
            Debug.Log(
                "Timed stacking stat effect validation passed (4 scenarios).");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Timed stacking stat effect validation failed: {exception}");
        }
    }

    private static void ValidateRules()
    {
        AssertThrows(() => new TimedStackingStatEffectDefinition(
            string.Empty,
            StatType.Armor,
            StatModifierType.Increased,
            -8f,
            5,
            8d));
        AssertThrows(() => new TimedStackingStatEffectDefinition(
            "effect:invalid",
            StatType.Armor,
            StatModifierType.More,
            -8f,
            5,
            8d));
        AssertThrows(() => new TimedStackingStatEffectDefinition(
            "effect:invalid",
            StatType.Armor,
            StatModifierType.Increased,
            -8f,
            0,
            8d));
        AssertThrows(() => new TimedStackingStatEffectDefinition(
            "effect:invalid",
            StatType.Armor,
            StatModifierType.Increased,
            float.MaxValue,
            2,
            8d));
    }

    private static void ValidateStackingRefreshAndExpiry()
    {
        RecordingGateway gateway = new();
        TimedStackingStatEffectService service = new(gateway);
        TimedStackingStatEffectDefinition definition = ArmorBreak();
        CombatActorReference source = Actor("player:one");

        for (int i = 1; i <= 5; i++)
        {
            Assert(service.TryApply(
                       definition,
                       source,
                       i - 1d,
                       out TimedStatEffectSnapshot snapshot) &&
                   snapshot.StackCount == i &&
                   snapshot.ExpiresAt == i - 1d + 8d,
                $"Stack {i} was not applied or refreshed correctly.");
        }

        Assert(gateway.GetValue(source, definition.EffectId) == -40f,
            "Five armor-break stacks did not aggregate to -40%. ");
        Assert(service.TryApply(definition, source, 5d, out var capped) &&
               capped.StackCount == 5 && capped.ExpiresAt == 13d,
            "Application at the cap did not refresh the common duration.");
        Assert(gateway.SetCount == 5,
            "Refreshing a capped effect redundantly replaced its modifier.");
        Assert(service.Tick(12.999d) == 0 && service.ActiveCount == 1,
            "Effect expired before its common duration elapsed.");
        Assert(service.Tick(13d) == 1 && service.ActiveCount == 0 &&
               gateway.ActiveCount == 0,
            "All stacks were not removed together at expiry.");
    }

    private static void ValidateIndependentSources()
    {
        RecordingGateway gateway = new();
        TimedStackingStatEffectService service = new(gateway);
        TimedStackingStatEffectDefinition definition = ArmorBreak();
        CombatActorReference first = Actor("player:one");
        CombatActorReference second = Actor("player:two");

        Assert(service.TryApply(definition, first, 0d, out _) &&
               service.TryApply(definition, second, 0d, out _) &&
               service.ActiveCount == 2 && gateway.ActiveCount == 2,
            "Explicit effect sources overwrote one another.");
        service.Clear();
        Assert(service.ActiveCount == 0 && gateway.ActiveCount == 0,
            "Clearing effects left sourced modifiers behind.");
    }

    private static void ValidateAtomicCharacterStatReplacement()
    {
        GameObject owner = new("TimedEffect_Stats")
        {
            hideFlags = HideFlags.DontSave
        };
        try
        {
            CharacterStats stats = owner.AddComponent<CharacterStats>();
            stats.Block.SetBaseValue(StatType.Armor, 100f);
            StatModifierSource source = StatModifierSource.ForBuff(
                "effect:armor-break|player:one");
            int notifications = 0;
            stats.OnStatChanged += type =>
            {
                if (type == StatType.Armor)
                    notifications++;
            };

            Assert(stats.TrySetModifierFromSource(
                       new StatModifier(
                           StatType.Armor,
                           StatModifierType.Increased,
                           -8f),
                       source) &&
                   stats.GetValue(StatType.Armor) == 92f &&
                   notifications == 1,
                "First sourced modifier was not applied atomically.");
            Assert(stats.TrySetModifierFromSource(
                       new StatModifier(
                           StatType.Armor,
                           StatModifierType.Increased,
                           -16f),
                       source) &&
                   stats.GetValue(StatType.Armor) == 84f &&
                   notifications == 2,
                "Replacing a sourced modifier emitted a transient state.");
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    private static TimedStackingStatEffectDefinition ArmorBreak()
    {
        return new TimedStackingStatEffectDefinition(
            "effect:armor-break",
            StatType.Armor,
            StatModifierType.Increased,
            -8f,
            5,
            8d);
    }

    private static CombatActorReference Actor(string id)
    {
        return new CombatActorReference(id, CombatActorKind.Player);
    }

    private static void AssertThrows(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }

        throw new InvalidOperationException(
            "Invalid effect rules were accepted.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class RecordingGateway : ITimedStatModifierGateway
    {
        private readonly Dictionary<StatModifierSource, StatModifier> active =
            new();

        public int ActiveCount => active.Count;
        public int SetCount { get; private set; }

        public bool TrySetModifier(
            StatModifierSource modifierSource,
            StatModifier modifier)
        {
            SetCount++;
            active[modifierSource] = modifier;
            return true;
        }

        public void RemoveModifier(StatModifierSource modifierSource)
        {
            active.Remove(modifierSource);
        }

        public float GetValue(
            CombatActorReference source,
            string effectId)
        {
            TimedStatEffectKey key = new(effectId, source);
            return active.TryGetValue(
                key.ModifierSource,
                out StatModifier modifier)
                ? modifier.Value
                : 0f;
        }
    }
}
