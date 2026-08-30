using UnityEngine;
using UnityEngine.AI;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class LocalAssaultArenaGateway : MonoBehaviour, IAssaultArenaGateway
    {
        [SerializeField] private Transform assaultDestination;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;

        private Transform occupant;
        private Vector3 returnPosition;
        private Quaternion returnRotation;

        public bool IsOccupied => occupant != null;
        public Transform Occupant => occupant;
        public Transform AssaultDestination => assaultDestination;

        public AssaultArenaTravelResult TryEnter(Transform actor)
        {
            if (actor == null)
            {
                return AssaultArenaTravelResult.Failed(
                    AssaultArenaTravelError.MissingActor);
            }

            if (assaultDestination == null)
            {
                return AssaultArenaTravelResult.Failed(
                    AssaultArenaTravelError.MissingDestination);
            }

            if (IsOccupied)
            {
                return AssaultArenaTravelResult.Failed(
                    AssaultArenaTravelError.AlreadyOccupied);
            }

            Vector3 originalPosition = actor.position;
            Quaternion originalRotation = actor.rotation;
            AssaultArenaTravelResult travel = TryMoveActor(
                actor,
                assaultDestination.position,
                assaultDestination.rotation);
            if (!travel.Success)
                return travel;

            occupant = actor;
            returnPosition = originalPosition;
            returnRotation = originalRotation;
            return AssaultArenaTravelResult.Succeeded();
        }

        public AssaultArenaTravelResult TryReturn(Transform actor)
        {
            if (!IsOccupied)
            {
                return AssaultArenaTravelResult.Failed(
                    AssaultArenaTravelError.NotOccupied);
            }

            if (actor == null)
            {
                return AssaultArenaTravelResult.Failed(
                    AssaultArenaTravelError.MissingActor);
            }

            if (actor != occupant)
            {
                return AssaultArenaTravelResult.Failed(
                    AssaultArenaTravelError.ActorMismatch);
            }

            AssaultArenaTravelResult travel = TryMoveActor(
                actor,
                returnPosition,
                returnRotation);
            if (!travel.Success)
                return travel;

            occupant = null;
            returnPosition = default;
            returnRotation = Quaternion.identity;
            return AssaultArenaTravelResult.Succeeded();
        }

        private AssaultArenaTravelResult TryMoveActor(
            Transform actor,
            Vector3 requestedPosition,
            Quaternion requestedRotation)
        {
            NavMeshAgent agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled)
            {
                actor.SetPositionAndRotation(
                    requestedPosition,
                    requestedRotation);
                return AssaultArenaTravelResult.Succeeded();
            }

            if (!NavMesh.SamplePosition(
                    requestedPosition,
                    out NavMeshHit hit,
                    navMeshSampleRadius,
                    agent.areaMask))
            {
                return AssaultArenaTravelResult.Failed(
                    AssaultArenaTravelError.DestinationOutsideNavMesh);
            }

            agent.isStopped = true;
            if (agent.isOnNavMesh)
                agent.ResetPath();

            if (!agent.Warp(hit.position))
            {
                return AssaultArenaTravelResult.Failed(
                    AssaultArenaTravelError.NavMeshWarpRejected);
            }

            actor.rotation = requestedRotation;
            return AssaultArenaTravelResult.Succeeded();
        }

        private void OnValidate()
        {
            navMeshSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);
        }
    }
}
