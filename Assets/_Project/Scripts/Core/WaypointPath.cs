using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    public int Length => waypoints.Length;

    public Transform GetWaypoint(int index)
    {
        if (index < 0 || index >= waypoints.Length)
        {
            return null;
        }
        return waypoints[index];
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            return;
        }
        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 0.3f);
            }
            if (i < waypoints.Length - 1 && waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    } 
}
