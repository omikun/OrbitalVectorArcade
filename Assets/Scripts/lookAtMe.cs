using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lookAtMe : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject camera;
    public float y_rotation = 0;
    void Start()
    {
        if (!camera)
        {
            camera = GameObject.Find("Main Camera");
        }
    }

    // Update is called once per frame
    void Update()
    {
        //rotate self to actually look at camera, shifting up vector
        var atCamera = camera.transform.position - transform.position;
        var cross = Vector3.Cross(Vector3.up, atCamera);
        var up = -Vector3.Cross(cross, atCamera);
        transform.LookAt(camera.transform.position, up);
        transform.Rotate(0f, y_rotation, 0f, Space.Self);

        //shrink with distance?
    }
}
