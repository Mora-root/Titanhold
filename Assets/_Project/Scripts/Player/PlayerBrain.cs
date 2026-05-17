using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    public PlayerInput Input { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerCombat Combat { get; private set; }

    public StateMachine StateMachine { get; private set; }

    // States
    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public ChaseState ChaseState { get; private set; }
    public AttackState AttackState { get; private set; }

    public Enemy CurrentTarget { get; set; }

    private void Awake()
    {
        Input = GetComponent<PlayerInput>();
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();

        StateMachine = new StateMachine();

        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this);
    }

    private void Start()
    {
        StateMachine.ChangeState(IdleState);
    }

    private void Update()
    {
        UpdateTarget();
        StateMachine.Update();
    }

    private void UpdateTarget()
    {
        // 🟢 если кликнули по земле — сбрасываем цель
        if (Input.HasPosition)
        {
            CurrentTarget = null;
            return;
        }

        // 🟢 если кликнули по врагу — ставим цель
        if (Input.HasEnemy)
        {
            CurrentTarget = Input.TargetEnemy;
        }

        // 🟢 если цель умерла — сброс
        if (CurrentTarget != null && !CurrentTarget.IsTargetable)
        {
            CurrentTarget = null;
            Input.ClearAll();
        }
    }
}
