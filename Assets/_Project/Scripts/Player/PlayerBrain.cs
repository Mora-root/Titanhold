using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    public PlayerInput Input { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerTargeting Targeting { get; private set; }
    public PlayerCombat Combat { get; private set; }

    public ITargetable CurrentTarget => Targeting.CurrentTarget;
    public TargetSelection TargetSelection { get; private set; }

    public StateMachine StateMachine { get; private set; }

    // States
    public IState IdleState { get; private set; }
    public IState MoveState { get; private set; }
    public IState ChaseState { get; private set; }
    public IState AttackState { get; private set; }

    private void Awake()
    {
        Input = GetComponent<PlayerInput>();
        Movement = GetComponent<PlayerMovement>();
        Targeting = GetComponent<PlayerTargeting>();
        Combat = GetComponent<PlayerCombat>();
        TargetSelection = GetComponent<TargetSelection>();

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
        HandleInput();

        StateMachine.Update();
        Movement.Tick();
    }

    private void HandleInput()
    {
        if (Input.RightClicked)
        {
            TargetSelection.HandleRightClick();
        }

        // 🔥 если цель умерла — сброс
        if (TargetSelection.CurrentTarget is ITargetable t && !t.IsTargetable)
        {
            TargetSelection.ClearTarget();
        }
        // 🔥 КЛИК
        if (Input.LeftClicked)
        {
            Targeting.TrySelectTarget();

            if (Targeting.CurrentTarget != null)
            {
                Input.ClearAll();
                return;
            }

            Targeting.ClearTarget();
        }
        // 🔥 УДЕРЖАНИЕ (ТОЛЬКО ЕСЛИ НЕ БЫЛО КЛИКА)
        else if (Input.IsDragging)
        {
            Targeting.ClearTarget();
        }

        // 🔥 цель умерла
        if (CurrentTarget != null && !CurrentTarget.IsTargetable)
        {
            Targeting.ClearTarget();
        }
    }

    // API

    public void MoveTo(Vector3 pos) => Movement.MoveTo(pos);
    public void Stop() => Movement.Stop();

    public void TryAttack(ITargetable target) => Combat.TryAttack(target);
    public bool CanAttack() => Combat.CanAttack();

    public void ChangeToIdle() => StateMachine.ChangeState(IdleState);
    public void ChangeToMove() => StateMachine.ChangeState(MoveState);
    public void ChangeToChase() => StateMachine.ChangeState(ChaseState);
    public void ChangeToAttack() => StateMachine.ChangeState(AttackState);
}
