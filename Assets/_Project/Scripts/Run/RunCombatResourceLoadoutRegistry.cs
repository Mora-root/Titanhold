using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunCombatResourceLoadoutRegistry :
        IRunCombatResourceLoadoutResolver
    {
        private readonly Dictionary<string, RunCombatResourceLoadout>
            loadoutsByArchetype;

        private RunCombatResourceLoadoutRegistry(
            Dictionary<string, RunCombatResourceLoadout> loadoutsByArchetype)
        {
            this.loadoutsByArchetype = loadoutsByArchetype;
        }

        public int Count => loadoutsByArchetype.Count;

        public static bool TryCreate(
            IReadOnlyList<RunCombatResourceLoadout> source,
            out RunCombatResourceLoadoutRegistry registry,
            out string error)
        {
            registry = null;
            error = string.Empty;
            if (source == null || source.Count == 0)
            {
                error = "At least one combat resource loadout is required.";
                return false;
            }

            Dictionary<string, RunCombatResourceLoadout> byArchetype =
                new(StringComparer.Ordinal);
            HashSet<string> loadoutIds = new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                RunCombatResourceLoadout loadout = source[i];
                if (!TryValidate(loadout, i, out error))
                    return false;

                if (!loadoutIds.Add(loadout.LoadoutId))
                {
                    error =
                        $"Combat resource loadout id '{loadout.LoadoutId}' occurs more than once.";
                    return false;
                }

                if (!byArchetype.TryAdd(
                        loadout.CharacterArchetypeId,
                        loadout))
                {
                    error =
                        $"Character archetype '{loadout.CharacterArchetypeId}' has more than one combat resource loadout.";
                    return false;
                }
            }

            registry = new RunCombatResourceLoadoutRegistry(byArchetype);
            return true;
        }

        public bool TryResolve(
            string characterArchetypeId,
            out RunCombatResourceLoadout loadout)
        {
            loadout = null;
            string normalizedId =
                characterArchetypeId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   loadoutsByArchetype.TryGetValue(normalizedId, out loadout);
        }

        private static bool TryValidate(
            RunCombatResourceLoadout loadout,
            int index,
            out string error)
        {
            error = string.Empty;
            if (loadout == null)
            {
                error = $"Combat resource loadout {index} is missing.";
                return false;
            }

            if (!HasStrictId(loadout.LoadoutId))
            {
                error = $"Combat resource loadout {index} has an invalid stable id.";
                return false;
            }

            if (!HasStrictId(loadout.CharacterArchetypeId))
            {
                error =
                    $"Combat resource loadout '{loadout.LoadoutId}' has an invalid character archetype id.";
                return false;
            }

            if (loadout.Resources.Count == 0)
            {
                error =
                    $"Combat resource loadout '{loadout.LoadoutId}' contains no resources.";
                return false;
            }

            HashSet<string> resourceIds = new(StringComparer.Ordinal);
            for (int i = 0; i < loadout.Resources.Count; i++)
            {
                RunCombatResourceDefinition resource = loadout.Resources[i];
                if (!resource.IsValid || !HasStrictId(resource.ResourceId))
                {
                    error =
                        $"Resource {i} in combat resource loadout '{loadout.LoadoutId}' is invalid.";
                    return false;
                }

                if (!resourceIds.Add(resource.ResourceId))
                {
                    error =
                        $"Resource id '{resource.ResourceId}' occurs more than once in combat resource loadout '{loadout.LoadoutId}'.";
                    return false;
                }
            }

            return true;
        }

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }
    }
}
