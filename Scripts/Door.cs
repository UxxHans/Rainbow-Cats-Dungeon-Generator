using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    public bool isPlaying;

    public void OpenDoor()
    {
        isPlaying = true;
        StopAllCoroutines();
        StartCoroutine(DoorOpenAnimation());
    }

    IEnumerator DoorOpenAnimation()
    {
        float lerpAmount = 1.1f;
        float lerpTime = 0.01f;
        float waitTime = 1.00f;
        float stopThreshold = 0.01f;

        while (transform.localScale.x > stopThreshold && transform.localScale.y > stopThreshold && transform.localScale.z > stopThreshold)
        {
            transform.localScale /= lerpAmount;
            yield return new WaitForSeconds(lerpTime);
        }
        isPlaying = false;

        yield return new WaitForSeconds(waitTime);
        StartCoroutine(DoorCloseAnimation());
    }
    IEnumerator DoorCloseAnimation()
    {
        float lerpAmount = 10f;
        float lerpTime = 0.01f;
        Vector3 targetScale = Vector3.one;

        while (transform.localScale.x < targetScale.x && transform.localScale.y < targetScale.y && transform.localScale.z < targetScale.z)
        {
            transform.localScale += (targetScale - transform.localScale) / lerpAmount;
            yield return new WaitForSeconds(lerpTime);
        }
    }
}
