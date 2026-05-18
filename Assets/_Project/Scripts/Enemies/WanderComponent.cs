using UnityEngine;
using UnityEngine.AI;

public class WanderComponent : MonoBehaviour
{
    [Header("Wander Settings")]
    [SerializeField] private float smallRadius = 5f;
    [SerializeField] private float bigRadius = 20f;
    [SerializeField] private float centerChangeInterval = 20f;

    private Vector3 globalCenter;
    private Vector3 currentCenter;

    private float timer;

    public Vector3 CurrentCenter => currentCenter;

    public void Initialize(Vector3 startPos)
    {
        globalCenter = startPos;
        currentCenter = startPos;
    }

    public void Tick()
    {
        timer += Time.deltaTime;

        if (timer >= centerChangeInterval)
        {
            timer = 0f;
            MoveCenter();
        }
    }

    private void MoveCenter()
    {
        Vector3 offset = Random.insideUnitSphere * bigRadius;
        offset.y = 0;

        Vector3 newCenter = globalCenter + offset;

        if (NavMesh.SamplePosition(newCenter, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            currentCenter = hit.position;
        }
    }

    public Vector3 GetNextPoint()
    {
        Debug.Log("GetNextPoint called");
        Vector3 offset = Random.insideUnitSphere * smallRadius;
        offset.y = 0;

        Vector3 point = currentCenter + offset;

        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return currentCenter;
    }

    public void SetCenter(Vector3 pos)
    {
        currentCenter = pos;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(globalCenter == Vector3.zero ? transform.position : globalCenter, bigRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(currentCenter == Vector3.zero ? transform.position : currentCenter, smallRadius);
    }
}
