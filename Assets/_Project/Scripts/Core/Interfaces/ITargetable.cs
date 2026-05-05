using UnityEngine;

public interface ITargetable
{
    Transform AimPoint { get; }
    bool IsTargetable { get; }
}

