using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    public StateMachine StateMachine { get; private set; }
    public EnemyAnimator Animator { get; private set; }

    public EnemyIdleState Idle;
    public EnemyWanderState WanderState;
    public EnemyChaseState Chase;
    public EnemyAttackState Attack;
    public EnemyReturnState Return;

    public EnemyMovement Movement { get; private set; }
    public EnemySensor Sensor { get; private set; }
    public EnemyCombat Combat { get; private set; }
    public WanderComponent Wander { get; private set; }

    private Health health;

    private void Awake()
    {
        Movement = GetComponent<EnemyMovement>();
        Sensor = GetComponent<EnemySensor>();
        Combat = GetComponent<EnemyCombat>();
        Wander = GetComponent<WanderComponent>();
        health = GetComponent<Health>();
        Animator = GetComponentInChildren<EnemyAnimator>();

        StateMachine = new StateMachine();

        Idle = new EnemyIdleState(this);
        WanderState = new EnemyWanderState(this);
        Chase = new EnemyChaseState(this);
        Attack = new EnemyAttackState(this);
        Return = new EnemyReturnState(this);

    }

    private void Start()
    {
        Wander.Initialize(transform.position);
        health.OnDeath += Health_OnDeath;
        StateMachine.ChangeState(Idle);
    }

    private void Health_OnDeath()
    {
        StateMachine.ChangeState(null);
    }

    private void Update()
    {
        Wander.Tick();
        Movement.Tick();
        StateMachine.Update();
    }
}

