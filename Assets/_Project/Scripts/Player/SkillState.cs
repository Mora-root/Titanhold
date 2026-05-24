public class SkillState : IState
{
    private PlayerBrain brain;

    public SkillState(PlayerBrain brain)
    {
        this.brain = brain;
    }

    public void Enter()
    {
        brain.Stop();
    }

    public void Tick()
    {
        brain.Stop();

        if (!brain.Skills.IsUsingSkill)
        {
            brain.ChangeToIdle();
        }
    }

    public void Exit() { }
}
