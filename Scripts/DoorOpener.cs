using UnityEngine;

public class DoorOpener : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Door") 
        {
            Door door = collision.GetComponent<Door>();
            if (!door.isPlaying) 
            {
                collision.GetComponent<Door>().OpenDoor(); 
            }
        }
    }
}
