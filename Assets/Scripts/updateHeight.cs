using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class updateHeight : MonoBehaviour
{
    LineRenderer lr;
    public GameObject target;                       //target game object
    //public GameObject base_ring; //the game object this class is attached to
    //public GameObject selection_ring;
    //public GameObject dashed_ring;
    public Color stem_color;
    float base_height = 0.05f;
    float offset = 0.15f;
    float line_width = 0.004f;
    private MaterialPropertyBlock _propBlock;
    void Start()
    {
        _propBlock = new MaterialPropertyBlock();

        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetWidth(line_width, line_width);
        lr.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", stem_color);
        lr.SetPropertyBlock(_propBlock);
    }

    // Update is called once per frame
    Vector3 ChangeY(float y, Vector3 pos) {
        return new Vector3(pos.x, y, pos.z);
    }

    void UpdateGo(GameObject g)
    {
        target = g;
    }

    void Update()
    {
        //TODO change base height if camera is below ecliptic
        var points = new Vector3[2];
        float y = target.transform.position.y;
        if (Mathf.Abs(y) > offset) {
            lr.enabled = true;
            float s = Mathf.Sign(y);
            points[0] = ChangeY(y - s * offset, target.transform.position);
            points[1] = ChangeY(base_height, target.transform.position);
            lr.SetPositions(points);
        } else {
            lr.enabled = false;
        }

        //var base = base_ring.transform.position;
        //base.y = base_height;
        transform.position = ChangeY(base_height, target.transform.position);
    }
}
