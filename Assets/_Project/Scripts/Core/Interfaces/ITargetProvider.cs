using UnityEngine;

public interface ITargetProvider
{
    Transform GetTarget();
    bool HasTarget { get; }

}
