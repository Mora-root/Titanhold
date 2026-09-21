public readonly struct StatModifierAssignment
{
    public StatModifierAssignment(
        StatModifierSource source,
        StatModifier modifier)
    {
        Source = source;
        Modifier = modifier;
    }

    public StatModifierSource Source { get; }
    public StatModifier Modifier { get; }
    public bool IsValid => Source.IsValid && Modifier != null;
}
