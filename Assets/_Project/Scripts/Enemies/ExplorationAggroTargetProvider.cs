using Titanhold.Combat;
using Titanhold.Enemies;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExplorationAggroTargetProvider :
    MonoBehaviour,
    IEnemyTargetProvider
{
    [SerializeField] private EnemySensor localAggroSensor;

    private ExplorationTargetRegistry targetRegistry;
    private CombatActorReference currentTargetActor;

    public bool IsBound => targetRegistry != null;
    public CombatActorReference CurrentTargetActor => currentTargetActor;

    private void Awake()
    {
        localAggroSensor ??= GetComponent<EnemySensor>();
    }

    public void Bind(ExplorationTargetRegistry registry)
    {
        targetRegistry = registry;
        currentTargetActor = default;
    }

    public ITargetable GetTarget()
    {
        if (targetRegistry == null)
            return localAggroSensor != null
                ? localAggroSensor.GetTarget()
                : null;

        float detectionRange = localAggroSensor != null
            ? localAggroSensor.DetectionRange
            : 0f;
        if (targetRegistry.TryGet(
                currentTargetActor,
                out ExplorationTargetParticipant current) &&
            IsWithinDetectionRange(current.Target, detectionRange))
        {
            return current.Target;
        }

        if (targetRegistry.TryGetNearest(
                transform.position,
                detectionRange,
                out ExplorationTargetParticipant nearest))
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

    private bool IsWithinDetectionRange(
        ITargetable target,
        float detectionRange)
    {
        if (target == null || !target.IsTargetable ||
            target.AimPoint == null || detectionRange <= 0f)
        {
            return false;
        }

        return (target.AimPoint.position - transform.position).sqrMagnitude <=
               detectionRange * detectionRange;
    }
}
