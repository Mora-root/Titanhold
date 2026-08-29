using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunPortalValidationRunner
    {
        private const string MenuPath = "Tools/Titanhold/Validate Run Portal";

        [MenuItem(MenuPath)]
        public static void ValidateFromMenu()
        {
            try
            {
                Debug.Log(RunValidation());
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Portal validation failed: {exception}");
            }
        }

        public static string RunValidation()
        {
            ValidateEntryCommandBoundaries();
            ValidateInteractableSubmitsPlayerCommand();
            return "Run Portal validation passed.";
        }

        private static void ValidateEntryCommandBoundaries()
        {
            RunFlowService flow = CreatePortalOpenFlow();
            RunPortalEntryApplicationService portal =
                new RunPortalEntryApplicationService(flow);
            CombatActorReference player = new CombatActorReference(
                "player:portal-validation",
                CombatActorKind.Player);

            RunPortalEntryResult stale = portal.TryEnter(
                new RunPortalEntryCommand(player, flow.State.RoundNumber + 1));
            Assert(!stale.Success && stale.Error == RunPortalEntryError.StalePortal,
                "A stale portal command was accepted.");
            Assert(flow.State.Phase == RunPhase.PortalOpen,
                "A rejected stale command changed the run phase.");

            RunPortalEntryResult invalidEntrant = portal.TryEnter(
                new RunPortalEntryCommand(
                    new CombatActorReference("enemy:invalid", CombatActorKind.Enemy),
                    flow.State.RoundNumber));
            Assert(!invalidEntrant.Success &&
                   invalidEntrant.Error == RunPortalEntryError.InvalidEntrant,
                "A non-player portal entrant was accepted.");

            RunPortalEntryResult accepted = portal.TryEnter(
                new RunPortalEntryCommand(player, flow.State.RoundNumber));
            Assert(accepted.Success, "A valid portal command was rejected.");
            Assert(flow.State.Phase == RunPhase.TransitionToAssault,
                "Portal entry did not start the Assault transition.");
            Assert(accepted.RunFlowResult.PreviousPhase == RunPhase.PortalOpen &&
                   accepted.RunFlowResult.CurrentPhase == RunPhase.TransitionToAssault,
                "Portal entry returned an invalid phase transition result.");

            RunPortalEntryResult repeated = portal.TryEnter(
                new RunPortalEntryCommand(player, flow.State.RoundNumber));
            Assert(!repeated.Success && repeated.Error == RunPortalEntryError.RunFlowRejected,
                "Repeated portal entry was not rejected.");
        }

        private static void ValidateInteractableSubmitsPlayerCommand()
        {
            GameObject runtimeObject = new GameObject("RunPortal_RuntimeValidation");
            GameObject portalObject = new GameObject("RunPortal_InteractableValidation");
            GameObject playerObject = new GameObject("RunPortal_PlayerValidation");

            try
            {
                RunFlowRuntime runtime = runtimeObject.AddComponent<RunFlowRuntime>();
                Assert(runtime.Service.TryRegisterExplorationKill(
                        new ExplorationKillContribution(100f, 0)).PortalOpened,
                    "Could not prepare runtime PortalOpen state.");

                RunPortalInteractable portal =
                    portalObject.AddComponent<RunPortalInteractable>();
                portal.Initialize(runtime, runtime.State.RoundNumber);
                PlayerCombat playerCombat = playerObject.AddComponent<PlayerCombat>();
                RunPortalEntryResult observedResult = default;
                bool resultObserved = false;
                portal.EntryResolved += result =>
                {
                    observedResult = result;
                    resultObserved = true;
                };

                Assert(playerCombat.ActorReference.IsPlayer,
                    "Player combat did not expose a valid player actor reference.");
                Assert(portal.IsInteractable,
                    "Initialized portal was not interactable in PortalOpen phase.");

                portal.Interact(playerObject);

                Assert(resultObserved && observedResult.Success,
                    "Portal interactable did not submit a successful player command.");
                Assert(runtime.State.Phase == RunPhase.TransitionToAssault,
                    "Portal interactable did not advance the runtime state.");
                Assert(!portal.IsInteractable,
                    "Portal remained interactable after its command was committed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(portalObject);
                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static RunFlowService CreatePortalOpenFlow()
        {
            RunFlowService flow = new RunFlowService(
                RunFlowConfiguration.CreateVerticalSliceDefaults());
            ExplorationKillBatchResult result = flow.TryRegisterExplorationKill(
                new ExplorationKillContribution(100f, 0));
            Assert(result.Success && result.PortalOpened,
                "Could not prepare PortalOpen state.");
            return flow;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
