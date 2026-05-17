using UnityEngine;
using UnityEngine.AI;

public class WanderComponent : MonoBehaviour
{
    public Vector3 CurrentCenter { get; private set; }

    [SerializeField] private float smallRadius = 5f;
    [SerializeField] private float bigRadius = 20f;

    [SerializeField] private float centerChangeInterval = 30f;

    private float centerTimer;
    private Vector3 globalCenter;

    public void Initialize(Vector3 startPos)
    {
        globalCenter = startPos;
        CurrentCenter = startPos;
    }

    public void Tick()
    {
        centerTimer += Time.deltaTime;

        // 🔥 меняем центр раз в N секунд
        if (centerTimer >= centerChangeInterval)
        {
            ResetCenterTimer();
            MoveCenter();
        }
    }

    private void MoveCenter()
    {
        Vector3 randomOffset = Random.insideUnitSphere * bigRadius;
        randomOffset.y = 0;

        Vector3 newCenter = globalCenter + randomOffset;

        // 🔥 привязка к NavMesh
        if (NavMesh.SamplePosition(newCenter, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            CurrentCenter = hit.position;
        }
    }

    public Vector3 GetNextPoint()
    {
        Vector3 randomOffset = Random.insideUnitSphere * smallRadius;
        randomOffset.y = 0;

        Vector3 point = CurrentCenter + randomOffset;

        // 🔥 гарантия что точка достижима
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return CurrentCenter;
    }

    public void SetCurrentCenter(Vector3 newCenter)
    {
        CurrentCenter = newCenter;
    }

    public void ResetCenterTimer()
    {
        centerTimer = 0;
    }

    // 🔥 ВИЗУАЛИЗАЦИЯ
    private void OnDrawGizmosSelected()
    {
        // 🔵 большая зона (глобальная)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(globalCenter == Vector3.zero ? transform.position : globalCenter, bigRadius);

        // 🟢 текущий центр блуждания
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(CurrentCenter == Vector3.zero ? transform.position : CurrentCenter, smallRadius);

        // 🔴 точка врага
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
