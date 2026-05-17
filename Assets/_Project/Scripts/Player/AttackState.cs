using UnityEngine;

public class AttackState : IState
{
    private PlayerBrain brain;

    public AttackState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Movement.Stop();
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

        if (!brain.Combat.CanAttack())
        {
            RotateToTarget();
            return;
        }

        // ✅ hysteresis выход
        if (distance > brain.Combat.AttackExitRange)
        {
            brain.StateMachine.ChangeState(brain.ChaseState);
            return;
        }

        // ✅ атака по кулдауну
        brain.Combat.TryAttack(brain.CurrentTarget);

        RotateToTarget();
    }

    public void Exit() { }

    private void RotateToTarget()
    {
        Vector3 dir = brain.CurrentTarget.GetTransform().position - brain.transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.01f)
            brain.Movement.Rotate(dir.normalized);
    }
}