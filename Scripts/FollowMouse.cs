using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    private void Update()
    {
        var mousePos = Input.mousePosition;
        mousePos.z = 100;
        transform.position = Camera.main.ScreenToWorldPoint(mousePos);
    }
}
