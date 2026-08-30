using UnityEngine;

namespace Titanhold.Run
{
    public interface IAssaultArenaGateway
    {
        bool IsOccupied { get; }
        Transform Occupant { get; }

        AssaultArenaTravelResult TryEnter(Transform actor);
        AssaultArenaTravelResult TryReturn(Transform actor);
    }
}
