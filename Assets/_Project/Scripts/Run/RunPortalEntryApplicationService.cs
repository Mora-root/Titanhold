using System;

namespace Titanhold.Run
{
    public sealed class RunPortalEntryApplicationService
    {
        private readonly RunFlowService runFlowService;

        public RunPortalEntryApplicationService(RunFlowService runFlowService)
        {
            this.runFlowService = runFlowService ??
                throw new ArgumentNullException(nameof(runFlowService));
        }

        public RunPortalEntryResult TryEnter(RunPortalEntryCommand command)
        {
            if (!command.Entrant.IsValid || !command.Entrant.IsPlayer)
            {
                return RunPortalEntryResult.Failed(
                    RunPortalEntryError.InvalidEntrant,
                    command);
            }

            if (command.ExpectedRound <= 0)
            {
                return RunPortalEntryResult.Failed(
                    RunPortalEntryError.InvalidExpectedRound,
                    command);
            }

            if (command.ExpectedRound != runFlowService.State.RoundNumber)
            {
                return RunPortalEntryResult.Failed(
                    RunPortalEntryError.StalePortal,
                    command);
            }

            RunFlowTransitionResult transition = runFlowService.TryBeginAssaultTransition();
            if (!transition.Success)
            {
                return RunPortalEntryResult.Failed(
                    RunPortalEntryError.RunFlowRejected,
                    command,
                    transition);
            }

            return RunPortalEntryResult.Succeeded(command, transition);
        }
    }
}
