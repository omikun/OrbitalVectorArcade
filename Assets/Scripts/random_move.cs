using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class random_move : MonoBehaviour
{
    // Start is called before the first frame update
    Vector3 dir;
    float speed;
    void Start()
    {
        dir = Random.onUnitSphere;
        speed = Random.value;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position += dir * speed * .0004f;
    }
}
