using System;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Combat.Editor
{
    public static class AbilityExecutionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Ability Execution Foundation")]
        public static void ValidateFromMenu()
        {
            try
            {
                Debug.Log(RunValidation());
            }
            catch (Exception exception)
            {
                Debug.LogError($"Ability execution foundation validation failed: {exception}");
            }
        }

        public static string RunValidation()
        {
            ValidateLifecycleAndCooldownSnapshot();
            ValidateRejectedCommandsDoNotSpend();
            ValidateCancellationAndStaleCommands();
            ValidateIndependentActors();
            ValidateTimeAndConfiguration();
            ValidateInstantFreeAbility();
            ValidateReentrantResourceGateway();
            return "Ability execution foundation validation passed (7 scenarios).";
        }

        private static void ValidateLifecycleAndCooldownSnapshot()
        {
            TestResources resources = new(40f);
            AbilityExecutionService service = CreatePlayer(resources);
            AbilityExecutionDefinition definition = CreateDefinition();
            CombatExecutionId first = new("cast:first");
            AbilityExecutionSnapshot snapshot = Success(service.TryCommit(first, definition, 10d));
            Assert(resources.Amount == 30f && resources.SpendCount == 1 &&
                   snapshot.Actor == service.Actor && snapshot.Definition == definition &&
                   snapshot.CommittedAt == 10d && snapshot.ReleaseAt == 10.5d &&
                   snapshot.FinishAt == 10.75d,
                "Commit did not capture the actor, definition and complete timeline once.");

            Error(service.TryCommit(new CombatExecutionId("cast:busy"), definition, 10d),
                AbilityExecutionError.Busy);
            Error(service.TryRelease(first, 10.49d), AbilityExecutionError.NotReady);
            Error(service.TryFinish(first, 10.75d), AbilityExecutionError.EffectNotReleased);
            Assert(Success(service.TryRelease(first, 10.5d)) == snapshot,
                "Release replaced the committed snapshot.");
            Error(service.TryRelease(first, 10.5d), AbilityExecutionError.AlreadyReleased);
            Error(service.TryFinish(first, 10.74d), AbilityExecutionError.NotReady);
            Success(service.TryFinish(first, 10.75d));
            Assert(service.CurrentExecution == null && service.Phase == AbilityExecutionPhase.Idle,
                "Finished execution kept the actor busy.");

            // Rebuilding data/modifiers cannot shorten an already committed cooldown.
            AbilityExecutionDefinition changedDefinition = new("ability:test", 10f, 0d, 0d, 0d);
            CombatExecutionId second = new("cast:second");
            Error(service.TryCommit(second, changedDefinition, 14.99d), AbilityExecutionError.OnCooldown);
            Error(service.TryCommit(first, definition, 15d), AbilityExecutionError.DuplicateExecution);
            Success(service.TryCommit(second, changedDefinition, 15d));
            Assert(resources.Amount == 20f && resources.SpendCount == 2 &&
                   snapshot.Definition.Cooldown == 5d,
                "Rejected/replayed commands spent resources or modified the original snapshot.");
        }

        private static void ValidateRejectedCommandsDoNotSpend()
        {
            TestResources resources = new(4f);
            AbilityExecutionService service = CreatePlayer(resources);
            AbilityExecutionDefinition definition = CreateDefinition();
            CombatExecutionId id = new("cast:retry");
            Error(service.TryCommit(id, null, 0d), AbilityExecutionError.InvalidDefinition);
            Error(service.TryCommit(default, definition, 0d), AbilityExecutionError.InvalidExecutionId);
            Error(service.TryCommit(id, definition, 0d), AbilityExecutionError.InsufficientResource);
            Assert(resources.Amount == 4f && resources.SpendCount == 0 &&
                   service.CurrentExecution == null && service.Phase == AbilityExecutionPhase.Idle,
                "Rejected commit changed resource, phase or execution.");

            // A failed command is retryable: it did not consume the id or cooldown.
            resources.Amount = 10f;
            Success(service.TryCommit(id, definition, 0d));
            Error(service.TryCommit(id, definition, 0d), AbilityExecutionError.DuplicateExecution);
            Assert(resources.Amount == 0f && resources.SpendCount == 1,
                "Retry did not spend the cost exactly once.");

            Error(CreatePlayer(null).TryCommit(id, definition, 0d),
                AbilityExecutionError.MissingResourceGateway);
        }

        private static void ValidateCancellationAndStaleCommands()
        {
            TestResources resources = new(40f);
            AbilityExecutionService service = CreatePlayer(resources);
            AbilityExecutionDefinition firstDefinition = CreateDefinition();
            AbilityExecutionDefinition secondDefinition = CreateDefinition("ability:other");
            CombatExecutionId first = new("cast:interrupted");
            CombatExecutionId second = new("cast:replacement");
            Success(service.TryCommit(first, firstDefinition, 0d));
            Success(service.TryCancel(first, 0.2d));
            Error(service.TryRelease(first, 0.5d), AbilityExecutionError.NoActiveExecution);
            Success(service.TryCommit(second, secondDefinition, 0.5d));

            Error(service.TryRelease(first, 1d), AbilityExecutionError.WrongExecution);
            Error(service.TryFinish(first, 1d), AbilityExecutionError.WrongExecution);
            Error(service.TryCancel(first, 1d), AbilityExecutionError.WrongExecution);
            Assert(service.CurrentExecution.ExecutionId == second &&
                   service.Phase == AbilityExecutionPhase.Committed,
                "An old callback changed the replacement cast.");

            Success(service.TryRelease(second, 1d));
            Success(service.TryCancel(second, 1d));
            Error(service.TryRelease(second, 1d), AbilityExecutionError.NoActiveExecution);
            Assert(resources.Amount == 20f && resources.SpendCount == 2,
                "Cancellation refunded a committed cost.");
            Error(service.TryCommit(new CombatExecutionId("cast:early"), firstDefinition, 4.99d),
                AbilityExecutionError.OnCooldown);
            Error(service.TryCommit(first, firstDefinition, 5d), AbilityExecutionError.DuplicateExecution);
            Success(service.TryCommit(new CombatExecutionId("cast:after-cooldown"), firstDefinition, 5d));
        }

        private static void ValidateIndependentActors()
        {
            TestResources playerResources = new(20f);
            TestResources enemyResources = new(10f);
            AbilityExecutionService player = CreatePlayer(playerResources);
            AbilityExecutionService enemy = new(
                new CombatActorReference("enemy:test", CombatActorKind.Enemy), enemyResources);
            AbilityExecutionDefinition sharedDefinition = CreateDefinition();
            CombatExecutionId playerId = new("cast:player");
            CombatExecutionId enemyId = new("cast:enemy");
            Success(player.TryCommit(playerId, sharedDefinition, 0d));
            Success(enemy.TryCommit(enemyId, sharedDefinition, 0d));
            Success(player.TryCancel(playerId, 0.1d));
            AbilityExecutionSnapshot enemyRelease = Success(enemy.TryRelease(enemyId, 0.5d));
            Assert(enemyRelease.Actor.IsEnemy && enemyRelease.Actor == enemy.Actor &&
                   playerResources.Amount == 10f && enemyResources.Amount == 0f &&
                   player.Phase == AbilityExecutionPhase.Idle && enemy.Phase == AbilityExecutionPhase.Released,
                "Player and enemy sharing a definition also shared mutable state.");
        }

        private static void ValidateTimeAndConfiguration()
        {
            Throws(() => new AbilityExecutionDefinition("", 0f, 0d, 0d, 0d));
            Throws(() => new AbilityExecutionDefinition("ability:invalid", float.NaN, 0d, 0d, 0d));
            Throws(() => new AbilityExecutionDefinition("ability:invalid", -1f, 0d, 0d, 0d));
            Throws(() => new AbilityExecutionDefinition("ability:invalid", 0f, -1d, 0d, 0d));
            Throws(() => new AbilityExecutionDefinition("ability:invalid", 0f, 0d, double.PositiveInfinity, 0d));
            Throws(() => new AbilityExecutionDefinition("ability:invalid", 0f, 0d, double.MaxValue, double.MaxValue));
            Throws(() => new AbilityExecutionService(default));

            TestResources resources = new(20f);
            AbilityExecutionService service = CreatePlayer(resources);
            AbilityExecutionDefinition definition = CreateDefinition();
            CombatExecutionId id = new("cast:time");
            foreach (double time in new[] { double.NaN, double.PositiveInfinity, -1d })
                Error(service.TryCommit(id, definition, time), AbilityExecutionError.InvalidTime);

            AbilityExecutionDefinition overflowing = new("ability:overflow", 10f, double.MaxValue, 0d, 0d);
            Error(service.TryCommit(id, overflowing, double.MaxValue), AbilityExecutionError.InvalidTime);
            Assert(resources.SpendCount == 0, "An invalid timeline spent resources.");
            Success(service.TryCommit(id, definition, 10d));
            Error(service.TryRelease(id, 9d), AbilityExecutionError.InvalidTime);
            Error(service.TryCancel(id, double.NaN), AbilityExecutionError.InvalidTime);
            Success(service.TryRelease(id, 10.5d));
            Error(service.TryFinish(id, 10d), AbilityExecutionError.InvalidTime);
            Success(service.TryFinish(id, 10.75d));
            Error(service.TryCommit(new CombatExecutionId("cast:rewind"), definition, 0d),
                AbilityExecutionError.InvalidTime);
        }

        private static void ValidateInstantFreeAbility()
        {
            AbilityExecutionService service = CreatePlayer(null);
            AbilityExecutionDefinition definition = new("ability:free", 0f, 0d, 0d, 0d);
            CombatExecutionId id = new("cast:free");
            Success(service.TryCommit(id, definition, 0d));
            Success(service.TryRelease(id, 0d));
            Success(service.TryFinish(id, 0d));
            Error(service.TryCommit(id, definition, 0d), AbilityExecutionError.DuplicateExecution);
            Success(service.TryCommit(new CombatExecutionId("cast:free-again"), definition, 0d));
        }

        private static void ValidateReentrantResourceGateway()
        {
            TestResources resources = new(20f);
            AbilityExecutionService service = CreatePlayer(resources);
            AbilityExecutionDefinition definition = CreateDefinition();
            CombatExecutionId id = new("cast:outer");
            resources.DuringSpend = () =>
            {
                Error(service.TryCommit(new CombatExecutionId("cast:nested"), definition, 0d),
                    AbilityExecutionError.ReentrantCommand);
                Error(service.TryRelease(id, 1d), AbilityExecutionError.ReentrantCommand);
                Error(service.TryCancel(id, 1d), AbilityExecutionError.ReentrantCommand);
            };
            Success(service.TryCommit(id, definition, 0d));
            Assert(resources.Amount == 10f && resources.SpendCount == 1 &&
                   service.CurrentExecution.ExecutionId == id,
                "Reentrant command replaced or charged the outer cast.");
        }

        private static AbilityExecutionDefinition CreateDefinition(string id = "ability:test")
        {
            return new AbilityExecutionDefinition(id, 10f, 5d, 0.5d, 0.25d);
        }

        private static AbilityExecutionService CreatePlayer(IAbilityResourceGateway resources)
        {
            return new AbilityExecutionService(
                new CombatActorReference("player:test", CombatActorKind.Player), resources);
        }

        private static AbilityExecutionSnapshot Success(AbilityExecutionResult result)
        {
            Assert(result.Success && result.Execution != null, $"Command failed: {result.Error}.");
            return result.Execution;
        }

        private static void Error(AbilityExecutionResult result, AbilityExecutionError expected)
        {
            Assert(!result.Success && result.Error == expected && result.Execution == null,
                $"Expected {expected}, got success={result.Success}, error={result.Error}.");
        }

        private static void Throws(Action action)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            throw new InvalidOperationException("Invalid configuration was accepted.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TestResources : IAbilityResourceGateway
        {
            public TestResources(float amount) { Amount = amount; }
            public float Amount { get; set; }
            public int SpendCount { get; private set; }
            public Action DuringSpend { get; set; }

            public bool TrySpend(float amount)
            {
                DuringSpend?.Invoke();
                if (amount > Amount)
                    return false;
                Amount -= amount;
                SpendCount++;
                return true;
            }
        }
    }
}
