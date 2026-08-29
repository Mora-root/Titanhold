using Titanhold.Combat;

namespace Titanhold.Run
{
    public readonly struct ExplorationKillRecord
    {
        public ExplorationKillRecord(
            DeathContext deathContext,
            CombatActorReference defeatedActor,
            ExplorationKillContribution contribution)
        {
            DeathContext = deathContext;
            DefeatedActor = defeatedActor;
            Contribution = contribution;
        }

        public DeathContext DeathContext { get; }
        public CombatActorReference DefeatedActor { get; }
        public ExplorationKillContribution Contribution { get; }
        public CombatExecutionId ExecutionId => DeathContext.ExecutionId;
        public CombatActorReference Source => DeathContext.Source;
    }
}
