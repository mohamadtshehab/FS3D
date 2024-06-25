using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraMovement : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }
    public float speed = 5.0f;
    private Vector3 dragOrigin;
    public float sensitivity = 5f;
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;


    void Update()
    {
        // Button movement
        float h = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float v = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        transform.Translate(h, v, 0);


        // if (Input.GetMouseButtonDown(0))
        // {
        //     dragOrigin = Input.mousePosition;
        //     return;
        // }

        // if (!Input.GetMouseButton(0)) return;

        // Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
        // Vector3 move = new Vector3(pos.x * -1, pos.y * -1, 0);
        // transform.Translate(move, Space.World);

        rotationX += Input.GetAxis("Mouse X") * sensitivity;
        rotationY -= Input.GetAxis("Mouse Y") * sensitivity;
        rotationY = Mathf.Clamp(rotationY, -90, 90);

        transform.eulerAngles = new Vector3(rotationY, rotationX, 0.0f);

    }

}

