using UnityEngine;

public class ChaseState : IState
{
    private PlayerBrain brain;

    public ChaseState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Tick()
    {
        if (brain.Input.HasPosition)
        {
            brain.StateMachine.ChangeState(brain.MoveState);
            return;
        }

        if (brain.CurrentTarget == null)
        {
            brain.StateMachine.ChangeState(brain.IdleState);
            return;
        }

        float distance = Vector3.Distance(
            brain.transform.position,
            brain.CurrentTarget.GetTransform().position
        );

        // ✅ вход в атаку
        if (distance <= brain.Combat.AttackEnterRange)
        {
            brain.StateMachine.ChangeState(brain.AttackState);
            return;
        }

        brain.Movement.MoveTo(brain.CurrentTarget.GetTransform().position);

        if (!brain.Movement.IsMoving)
        {
            brain.Movement.MoveTo(brain.CurrentTarget.GetTransform().position);
        }

        RotateToTarget();
    }

    public void Enter() { }

    public void Exit()
    {
        //brain.Movement.Stop();
    }

    private void RotateToTarget()
    {
        Vector3 dir = brain.CurrentTarget.GetTransform().position - brain.transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.01f)
            brain.Movement.Rotate(dir.normalized);
    }
}
