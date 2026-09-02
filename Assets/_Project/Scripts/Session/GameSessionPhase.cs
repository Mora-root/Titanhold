namespace Titanhold.Session
{
    public enum GameSessionPhase
    {
        Hub,
        TransitionToRun,
        Run,
        TransitionToHub
    }

    public enum RunOutcome
    {
        Victory,
        Defeat,
        Abandoned
    }
}
