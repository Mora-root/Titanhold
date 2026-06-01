using UnityEngine;

public class EnemyIdleState : IState
{
    [SerializeField] private float minIdleTime = 3f;
    [SerializeField] private float maxIdleTime = 6f;

    private EnemyBrain brain;

    private float idleTime;
    private float timer;

    public EnemyIdleState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Stop();

        idleTime = Random.Range(minIdleTime, maxIdleTime); // The time after which it goes to a new point
        timer = 0f;
    }

    public void Tick()
    {
        var target = brain.GetTarget();

        if (target != null)
        {
            brain.ChangeToChase();
            return;
        }

        timer += Time.deltaTime;

        if (timer >= idleTime)
        {
            brain.ChangeToWander();
        }
    }

    public void Exit() { }
}
