using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed;
    public float zoomSpeed;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
            transform.Translate(new Vector3(0, 1 * moveSpeed * Time.deltaTime, 0), Space.World);
        if (Input.GetKey(KeyCode.A))
            transform.Translate(new Vector3(-1 * moveSpeed * Time.deltaTime, 0, 0), Space.World);
        if (Input.GetKey(KeyCode.S))
            transform.Translate(new Vector3(0, -1 * moveSpeed * Time.deltaTime, 0), Space.World);
        if (Input.GetKey(KeyCode.D))
            transform.Translate(new Vector3(1 * moveSpeed * Time.deltaTime, 0, 0), Space.World);

        if (Input.GetKey(KeyCode.Q))
            Camera.main.orthographicSize += zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            Camera.main.orthographicSize -= zoomSpeed * Time.deltaTime;
    }
}
