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
        brain.Stop();
        Debug.Log("Entering Idle State");

        idleTime = Random.Range(3f, 6f); // 🔥 вариативность
        timer = 0f;
    }

    public void Tick()
    {
        // 🔥 1. проверка цели
        var target = brain.Sensor.GetTarget();

        if (target != null)
        {
            brain.ChangeToChase();
            Debug.Log("Target spotted, switching to Chase State");
            return;
        }

        // 🔥 2. считаем время
        timer += Time.deltaTime;

        // 🔥 3. пошли гулять
        if (timer >= idleTime)
        {
            brain.ChangeToWander(); // 👈 добавь этот метод в Brain
            Debug.Log("Idle time over, switching to Wander State");
        }
    }

    public void Exit() { }
}
