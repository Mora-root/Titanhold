using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStatsConfig", menuName = "Configs/CharacterStatsConfig")]
public class CharacterStatsConfig : ScriptableObject
{
    [SerializeField] private StatEntry[] baseStats;

    public float GetBaseValue(StatType type)
    {
        foreach (var stat in baseStats)
        {
            if (stat.Type == type)
                return stat.Value;
        }

        return 0f;
    }
}
