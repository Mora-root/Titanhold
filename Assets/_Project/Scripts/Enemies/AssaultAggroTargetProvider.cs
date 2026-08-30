using Titanhold.Combat;
using Titanhold.Run;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AssaultAggroTargetProvider : MonoBehaviour, IEnemyTargetProvider
{
    private AssaultTargetRegistry targetRegistry;
    private CombatActorReference currentTargetActor;

    public bool IsBound => targetRegistry != null;
    public CombatActorReference CurrentTargetActor => currentTargetActor;

    public void Bind(AssaultTargetRegistry registry)
    {
        targetRegistry = registry;
        currentTargetActor = default;
    }

    public ITargetable GetTarget()
    {
        if (targetRegistry == null)
            return null;

        if (targetRegistry.TryGet(
                currentTargetActor,
                out AssaultTargetParticipant current))
        {
            return current.Target;
        }

        if (targetRegistry.TryGetNearest(
                transform.position,
                out AssaultTargetParticipant nearest))
        {
            currentTargetActor = nearest.Actor;
            return nearest.Target;
        }

        currentTargetActor = default;
        return null;
    }

    public bool TrySetCurrentTarget(CombatActorReference actor)
    {
        if (targetRegistry == null ||
            !targetRegistry.TryGet(actor, out _))
        {
            return false;
        }

        currentTargetActor = actor;
        return true;
    }

    public void ClearCurrentTarget()
    {
        currentTargetActor = default;
    }
}
