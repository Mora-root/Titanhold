using Titanhold.Combat;
using Titanhold.Run;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyDeathNotifier))]
public sealed class EnemyRunContributionSource : MonoBehaviour
{
    [SerializeField, Min(0f)] private float threatAmount = 10f;
    [SerializeField, Min(0)] private int instabilityPoints = 1;

    public float ThreatAmount => threatAmount;
    public int InstabilityPoints => instabilityPoints;

    public ExplorationKillRecord CreateKillRecord(DeathContext deathContext)
    {
        CombatActorReference defeatedActor = new CombatActorReference(
            $"enemy:{gameObject.GetEntityId()}",
            CombatActorKind.Enemy);
        ExplorationKillContribution contribution = new ExplorationKillContribution(
            threatAmount,
            instabilityPoints);

        return new ExplorationKillRecord(deathContext, defeatedActor, contribution);
    }
}
