using UnityEngine;

/// <summary>
/// Stores information about the target for the UI
/// </summary>
public class TargetInfo : MonoBehaviour
{
    [SerializeField] private string displayName = "Target";
    [SerializeField] private int level = 1;
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public int Level => level;
    public Sprite Icon => icon;
}
