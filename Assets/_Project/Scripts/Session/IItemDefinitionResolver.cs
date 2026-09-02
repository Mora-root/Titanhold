namespace Titanhold.Session
{
    public interface IItemDefinitionResolver
    {
        bool TryResolve(string definitionId, out ItemDefinition definition);
    }
}
