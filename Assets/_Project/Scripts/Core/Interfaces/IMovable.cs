using UnityEngine;

public interface IMovable
{
    void MoveTo(Vector3 position);
    void Stop();
    bool IsMoving { get; }
}
