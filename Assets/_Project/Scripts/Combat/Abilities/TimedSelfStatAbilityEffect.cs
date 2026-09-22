using Titanhold.Combat.Effects;

namespace Titanhold.Combat.Abilities
{
    public static class TimedSelfStatAbilityEffect
    {
        public static bool TryApply(
            AbilityUseContext context,
            TimedStackingStatEffectDefinition definition,
            CombatActorReference source,
            double simulationTime)
        {
            if (!context.HasSource || definition == null)
                return false;

            ITimedStackingStatEffectReceiver receiver =
                context.Source.GetComponentInParent<
                    ITimedStackingStatEffectReceiver>();
            return receiver != null && receiver.TryApply(
                definition,
                source,
                simulationTime,
                out _);
        }
    }
}
