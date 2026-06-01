using UnityEngine;

public sealed class WaveEnemyTargetProvider : MonoBehaviour, IEnemyTargetProvider
{
    [SerializeField] private EnemySensor localAggroSensor;
    [SerializeField] private CampCoreTarget campCoreTarget;

    private void Awake()
    {
        localAggroSensor ??= GetComponent<EnemySensor>();
        campCoreTarget ??= FindAnyObjectByType<CampCoreTarget>();
    }

    public ITargetable GetTarget()
    {
        ITargetable localTarget = localAggroSensor != null ? localAggroSensor.GetTarget() : null;

        if (localTarget != null && localTarget.IsTargetable)
            return localTarget;

        if (campCoreTarget != null && campCoreTarget.IsTargetable)
            return campCoreTarget;

        return null;
    }
}
