using UnityEngine;

public interface IState
{
    void Enter();
    void Tick();
    void Exit();
}
