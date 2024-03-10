using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    // Start is called before the first frame update
    float max_velocity=0;
    Rigidbody rb;
    void Start()
    {
         rb = GetComponent<Rigidbody>();   
    }

    // Update is called once per frame
    void Update()
    {
        //print the velocities when height is around 61 meters
        if(transform.position.y>60 && transform.position.y<62){
            //print both height and velocity
            print("Height: "+transform.position.y+" Velocity: "+rb.velocity.magnitude);
            if(rb.velocity.magnitude>max_velocity){
                max_velocity=rb.velocity.magnitude;
            }
        }
    }
}
