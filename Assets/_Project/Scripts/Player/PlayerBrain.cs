using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    private const int Skill1SlotIndex = 0;

    public PlayerInput Input { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerTargeting Targeting { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public TargetSelection TargetSelection { get; private set; }
    [SerializeField] private MonoBehaviour skillExecutorOverride;
    private IPlayerSkillCommands skills;
    public IPlayerSkillCommands Skills => skills ??= ResolveSkillExecutor();

    public StateMachine StateMachine { get; private set; }

    public ISelectable InspectTarget => TargetSelection.CurrentSelection;
    public ISelectable SelectedObject => InspectTarget;

    public ISelectable ActionSelection { get; private set; } // get selected target
    public ISelectable ActionTarget => ActionSelection;

    public ITargetable CombatTarget => ActionSelection as ITargetable;
    public ITargetable CurrentTarget => CombatTarget; // for attack stage
    public IInteractable CurrentInteractable => ActionSelection as IInteractable; // for interact stage
    public ILootable CurrentLoot => ActionSelection as ILootable; // for loot stage
    public bool HasMoveTarget => Input.CurrentIntent.HasMoveTarget;
    public Vector3 MoveTargetPosition => Input.CurrentIntent.TargetPosition;
    public IState IdleState { get; private set; }
    public IState MoveState { get; private set; }
    public IState ApproachState { get; private set; }
    public IState SkillState { get; private set; }
    public IState AttackState { get; private set; }
    public IState InteractState { get; private set; }
    public IState LootState { get; private set; }

    private readonly PlayerSkillCommandBuffer skillCommandBuffer = new();
    private Health health;
    private PlayerAnimator playerAnimator;
    private bool isDead;

    public bool HasQueuedSkillCommand =>
        skillCommandBuffer.HasPendingCommand;
    public bool IsDead => isDead;

    private void Awake()
    {
        Input = GetComponent<PlayerInput>();
        Movement = GetComponent<PlayerMovement>();
        Targeting = GetComponent<PlayerTargeting>();
        Combat = GetComponent<PlayerCombat>();
        skills = ResolveSkillExecutor();
        TargetSelection = GetComponent<TargetSelection>();
        health = GetComponent<Health>();
        playerAnimator = GetComponentInChildren<PlayerAnimator>();

        StateMachine = new StateMachine();

        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        ApproachState = new ApproachState(this);
        SkillState = new SkillState(this);
        AttackState = new AttackState(this);
        InteractState = new InteractState(this);
        LootState = new LootState(this);
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        if (health != null && !health.IsAlive)
        {
            HandleDeath();
            return;
        }

        StateMachine.ChangeState(IdleState);
    }

    private void Update()
    {
        if (isDead)
            return;

        HandleInput();

        StateMachine.Update();
        // Debug.Log(StateMachine.CurrentState.GetType().Name);
        Movement.Tick();
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;
        StateMachine.ChangeState(null);
        skillCommandBuffer.Clear();
        Input?.ClearAll();
        ClearAllSelections();
        Combat?.CancelAttack();
        Skills?.CancelCurrentSkill();
        Movement?.Stop();
        playerAnimator?.SetSpeed(0f);
        playerAnimator?.PlayDeath();
    }

    private void HandleInput()
    {
        PlayerInputIntent intent = Input.CurrentIntent;

        if (intent.Skill1Pressed)
        {
            PlayerSkillCommand command =
                new PlayerSkillCommand(Skill1SlotIndex);
            if (Combat.IsAttacking || Skills?.IsUsingSkill == true)
            {
                skillCommandBuffer.TryBuffer(command);
                return;
            }

            skillCommandBuffer.Clear();
            if (TryExecuteSkill(command))
                return;
        }

        if (Skills?.IsUsingSkill == true)
            return;

        if (!Combat.IsAttacking &&
            skillCommandBuffer.TryTake(out PlayerSkillCommand queuedCommand) &&
            TryExecuteSkill(queuedCommand))
        {
            return;
        }

        // Right click = for UI-selection
        if (intent.RightClicked)
        {
            var selectable = Targeting.GetSelectableUnderMouse();

            if (selectable != null)
            {
                TargetSelection.Select(selectable);
            }
            else
            {
                TargetSelection.Clear();
            }

            return;
        }

        // Left click = action
        if (intent.LeftClicked)
        {
            var selectable = Targeting.GetSelectableUnderMouse();

            if (selectable != null)
            {
                // Left click on an object also highlights it for the UI
                TargetSelection.Select(selectable);

                // But the action is stored separately
                ActionSelection = selectable;

                Input.ClearAll();
                return;
            }

            // Left click:
            // action is being reset, UI-selection is NOT being touched
            ClearActionSelection();
            return;
        }

        // Left click hold = movement only
        if (intent.IsDragging)
        {
            // action is being reset, UI-selection is NOT being touched
            ClearActionSelection();
        }

        // If the action object has become invalid
        if (ActionSelection != null && !ActionSelection.IsSelectable)
        {
            ClearActionSelection();
        }

        // If the UI-selected object has become invalid
        if (SelectedObject != null && !SelectedObject.IsSelectable)
        {
            TargetSelection.Clear();
        }
    }

    public void SetActionSelection(ISelectable selectable)
    {
        ActionSelection = selectable;
    }

    public void ClearActionSelection()
    {
        ActionSelection = null;
    }

    public void ClearAllSelections()
    {
        ClearActionSelection();
        TargetSelection.Clear();
    }

    public void ClearQueuedAction()
    {
        skillCommandBuffer.Clear();
    }

    public void MoveTo(Vector3 pos) => Movement.MoveTo(pos);
    public void Stop() => Movement.Stop();

    public void TryAttack(ITargetable target) => Combat.TryAttack(target);
    public bool CanAttack() => Combat.CanAttack();

    public void ChangeToIdle() => StateMachine.ChangeState(IdleState);
    public void ChangeToMove() => StateMachine.ChangeState(MoveState);
    public void ChangeToApproach() => StateMachine.ChangeState(ApproachState);
    public void ChangeToSkill() => StateMachine.ChangeState(SkillState);
    public void ChangeToAttack() => StateMachine.ChangeState(AttackState);
    public void ChangeToInteract() => StateMachine.ChangeState(InteractState);
    public void ChangeToLoot() => StateMachine.ChangeState(LootState);

    private bool TryExecuteSkill(PlayerSkillCommand command)
    {
        if (!command.IsValid || Skills == null ||
            !Skills.TryUseSkillSlot(command.SlotIndex))
        {
            return false;
        }

        Stop();
        ChangeToSkill();
        return true;
    }

    private IPlayerSkillCommands ResolveSkillExecutor()
    {
        // An assigned but invalid replacement must not silently activate legacy skills.
        if (skillExecutorOverride != null)
            return skillExecutorOverride.gameObject == gameObject
                ? skillExecutorOverride as IPlayerSkillCommands
                : null;

        return GetComponent<PlayerSkillExecutor>();
    }
}
