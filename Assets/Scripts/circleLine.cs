using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class circleLine : MonoBehaviour
{
    // Start is called before the first frame update
    public float radius = 3;
    public float line_width = .05f;
    public int segments = 60;
    LineRenderer lr;
    void Start()
    {
        lr = gameObject.GetComponent<LineRenderer>();

        lr.SetWidth(line_width, line_width);
        lr.SetVertexCount (segments + 1);
    }

    // Update is called once per frame
    void Update()
    {
        var points = new Vector3[segments+1];
        float angle = 0f;
        for (int i = 0; i < (segments + 1); i++)
        {
            float x = Mathf.Sin (Mathf.Deg2Rad * angle) * radius + transform.position.x;
            float z = Mathf.Cos (Mathf.Deg2Rad * angle) * radius + transform.position.z;
            points[i] = new Vector3(x, transform.position.y, z);

            angle += (360f / segments);
        }
        lr.SetPositions(points);
    }
}
