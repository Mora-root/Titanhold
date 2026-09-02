using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int level = 1;
    [SerializeField] private PlayerExperience playerExperience;

    public string PlayerName => playerName;
    public int Level => playerExperience != null
        ? playerExperience.CurrentLevel
        : level;

    private void Awake()
    {
        if (playerExperience == null)
            playerExperience = GetComponent<PlayerExperience>();
    }
}
