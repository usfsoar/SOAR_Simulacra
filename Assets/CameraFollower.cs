using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public GameObject rocket;
    Vector3 relativePosition;
    // Start is called before the first frame update
    void Start()
    {
        //Get the position relative to the rocket
        relativePosition = transform.position - rocket.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //Keep the camera at the same relative position to the rocket
        transform.position = rocket.transform.position + relativePosition;
    }
}
