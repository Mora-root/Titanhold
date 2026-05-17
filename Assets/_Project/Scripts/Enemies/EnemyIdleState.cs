using UnityEngine;

public class EnemyIdleState : IState
{
    private EnemyBrain brain;
    private float idleTime;
    private float timer;

    public EnemyIdleState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Movement.Stop();

        idleTime = Random.Range(3f, 5f);
        timer = 0f;
    }

    public void Tick()
    {
        brain.Sensor.UpdateSensor();

        // 🔴 если увидели цель — сразу в бой
        if (brain.Sensor.HasTarget)
        {
            brain.StateMachine.ChangeState(brain.Chase);
            Debug.Log(brain.StateMachine.CurrentState + " from Idle");
            return;
        }

        timer += Time.deltaTime;

        // 🟢 постояли → идём гулять
        if (timer >= idleTime)
        {
            brain.StateMachine.ChangeState(brain.WanderState);
            Debug.Log(brain.StateMachine.CurrentState + " from Idle");
        }
    }

    public void Exit() { }
}
