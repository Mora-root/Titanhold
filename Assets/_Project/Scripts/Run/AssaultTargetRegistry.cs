using System.Collections.Generic;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    public readonly struct AssaultTargetParticipant
    {
        public AssaultTargetParticipant(
            CombatActorReference actor,
            ITargetable target)
        {
            Actor = actor;
            Target = target;
        }

        public CombatActorReference Actor { get; }
        public ITargetable Target { get; }
    }

    [DisallowMultipleComponent]
    public sealed class AssaultTargetRegistry : MonoBehaviour
    {
        private readonly Dictionary<CombatActorReference, ITargetable>
            participants = new();

        public int Count => participants.Count;

        public bool TryRegister(
            CombatActorReference actor,
            ITargetable target)
        {
            if (!actor.IsValid || !actor.IsPlayer || !IsValidTarget(target))
                return false;

            return participants.TryAdd(actor, target);
        }

        public bool Unregister(CombatActorReference actor)
        {
            return actor.IsValid && participants.Remove(actor);
        }

        public void Clear()
        {
            participants.Clear();
        }

        public bool TryGet(
            CombatActorReference actor,
            out AssaultTargetParticipant participant)
        {
            if (actor.IsValid &&
                participants.TryGetValue(actor, out ITargetable target) &&
                IsValidTarget(target))
            {
                participant = new AssaultTargetParticipant(actor, target);
                return true;
            }

            participant = default;
            return false;
        }

        public bool TryGetNearest(
            Vector3 origin,
            out AssaultTargetParticipant participant)
        {
            float nearestDistanceSqr = float.PositiveInfinity;
            participant = default;
            bool found = false;

            foreach (KeyValuePair<CombatActorReference, ITargetable> entry
                     in participants)
            {
                ITargetable target = entry.Value;
                if (!IsValidTarget(target))
                    continue;

                float distanceSqr =
                    (target.AimPoint.position - origin).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                    continue;

                nearestDistanceSqr = distanceSqr;
                participant = new AssaultTargetParticipant(entry.Key, target);
                found = true;
            }

            return found;
        }

        private static bool IsValidTarget(ITargetable target)
        {
            if (target == null)
                return false;

            if (target is Object unityObject && unityObject == null)
                return false;

            return target.IsTargetable && target.AimPoint != null;
        }
    }
}
