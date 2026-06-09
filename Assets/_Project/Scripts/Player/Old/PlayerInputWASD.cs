using UnityEngine;

public class PlayerInputWASD : MonoBehaviour
{
    public Vector3 MoveDirection { get; private set; }
    public bool AttackPressed { get; private set; }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        MoveDirection = new Vector3(horizontal, 0f, vertical);
        AttackPressed = Input.GetKeyDown(KeyCode.Mouse0);
    }
}
