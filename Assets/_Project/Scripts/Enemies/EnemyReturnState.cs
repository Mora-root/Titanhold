using UnityEngine;

public class EnemyReturnState : IState
{
    private EnemyBrain brain;

    public EnemyReturnState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Tick()
    {
        float distance = Vector3.Distance(
            brain.transform.position,
            brain.Wander.CurrentCenter
        );

        if (distance < 1f)
        {
            brain.StateMachine.ChangeState(brain.Idle);
            return;
        }

        brain.Movement.MoveTo(brain.Wander.CurrentCenter);
    }

    public void Enter() { }
    public void Exit() { }
}