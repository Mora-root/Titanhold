using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int level = 1;

    public string PlayerName => playerName;
    public int Level => level;
}
