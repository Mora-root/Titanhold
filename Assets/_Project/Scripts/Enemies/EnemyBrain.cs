using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    public EnemyMovement Movement { get; private set; }
    public EnemyCombat Combat { get; private set; }
    public EnemySensor Sensor { get; private set; }
    public WanderComponent Wander { get; private set; }
    public EnemyAnimator Animator { get; private set; }

    private StateMachine stateMachine;
    private Health health;
    private bool isDead;

    private IState idleState;
    private IState wanderState;
    private IState chaseState;
    private IState attackState;

    public bool IsDead => isDead;

    private void Awake()
    {
        Animator = GetComponentInChildren<EnemyAnimator>();
        Movement = GetComponent<EnemyMovement>();
        Combat = GetComponent<EnemyCombat>();
        Sensor = GetComponent<EnemySensor>();
        Wander = GetComponent<WanderComponent>();
        health = GetComponent<Health>();

        stateMachine = new StateMachine();

        idleState = new EnemyIdleState(this);
        wanderState = new EnemyWanderState(this);
        chaseState = new EnemyChaseState(this);
        attackState = new EnemyAttackState(this);
    }

    private void Start()
    {
        health.OnDeath += Health_OnDeath;
        stateMachine.ChangeState(idleState);
        Wander.Initialize(transform.position);
    }

    private void Health_OnDeath()
    {
        isDead = true;

        stateMachine.ChangeState(null);

        Movement.Stop();

        Sensor.enabled = false;

        Animator?.PlayDeath();
    }

    private void Update()
    {
        if (isDead) return;
        stateMachine.Update();
        Movement.Tick();
        Wander.Tick();
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= Health_OnDeath;
    }

    // 🔥 API ДЛЯ STATE

    public void MoveTo(Vector3 pos)
    {
        Movement.MoveTo(pos);
    }

    public void Stop()
    {
        Movement.Stop();
    }

    public void Attack(ITargetable target)
    {
        Combat.TryAttack(target);
    }

    public bool CanAttack()
    {
        return Combat.CanAttack();
    }

    public void ChangeToIdle() => stateMachine.ChangeState(idleState);
    public void ChangeToChase() => stateMachine.ChangeState(chaseState);
    public void ChangeToAttack() => stateMachine.ChangeState(attackState);
    public void ChangeToWander() => stateMachine.ChangeState(wanderState);
}

