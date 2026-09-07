using Titanhold.Combat;

namespace Titanhold.Combat.Effects
{
    public interface ITimedStackingStatEffectReceiver
    {
        bool TryApply(
            TimedStackingStatEffectDefinition definition,
            CombatActorReference source,
            double simulationTime,
            out TimedStatEffectSnapshot snapshot);
    }
}
