using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetMinOf
{
    public float index = float.PositiveInfinity;
    public float time = 0;
    public Vector3 result = Vector3.zero;
    public GetMinOf() { }
    public void Clear()
    {
        index = float.PositiveInfinity;
        time = 0;
        result = Vector3.zero;
    }
    public void Update(float i, float t, Vector3 d)
    {
        if (i < index)
        {
            index = i;
            time = t;
            result = d;
        }
    }
    public float GetMinIndex() { return index; }
    public Vector3 GetMin() { return result; }
}

public class MissileControl : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject target;
    Rigidbody rb;
    public float max_acceleration = 4f;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //define initial velocity?
    }

    //https://pastebin.com/yu9ZmKbS
    Vector3 APN()
    {
        float N = 4;
        float kp = 0.02f;
        var target_dir = target.transform.position - transform.position;
        //LoS, rotational velocity
        //pitch arcsin(sin(target_dir.y - old_dir.y * Mathf.pi) / 180f)
        //yaw arcsin(sin(target_dir.x - old_dir.x * Mathf.pi) / 180f)
        var old_dir = target_dir;
        //pitch/yaw rate = pitch or yaw / Time.fixedDeltaTime;
        //float speed_to_target = (rb.velocity - target.velocity).magnitude;

        //acceleration of correction
        //acc of pitch and yaw...?
        
        //rb.velocity = dir.normalized * speed;
        return rb.velocity;
    }

    GetMinOf min = new GetMinOf();
    Vector3 SlowDogCurve()
    {
        min.Clear();
        //find desired velocity vector
        var Vt = Vector3.zero;//FIXME target.GetComponent<Rigidbody>().velocity;
        var Vm0 = rb.velocity;
        var Pt0 = target.transform.position;
        var Pm0 = transform.position;
        float Sm = 20; //speed of missile, fixed constant
        //loop over all time t = [0, 100)
        int i = 0;
        for (float t = 0.1f; t < 97; t += 0.1f)
        {
            //assuming constant acceleration
            var Pt = t * Vt + Pt0;
            //assuming constant velocity
            Vector3 Vm, Pm;
            if (false) //constant velocity
            {
                Vm = (Pt - Pm0).normalized * Sm;
                Pm = t * Vm + Pm0;
            } else
            {
                //a = 2 * (Vt * t + Pt0 - Pm0 - Vm0 * t) / (t*t);
                var Pm1 = Vm0 * t;
                Vm = Pt - Pm0 - Pm1;
                var a = Vm.normalized * max_acceleration;
                Pm = Pm0 + Pm1 + a * t * t / 2;
            }
            var dist = (Pt - Pm).magnitude;
            min.Update(dist, t, Vm);
            //line.SetPosition(i+3, new Vector3((float)i / 10, dist/10, 0));
            i++;
        }
        //Logdump.Log("t=" + min.time + " minDist: " + min.index + " minV: " + min.result.magnitude);
        //InterceptionMarker.transform.position = min.time * Vt + Pt0;
        //if too slow, use target velocity vector
        //var desiredVm = (min.index > 1 ) ? min.result : Vt;
        var desiredVm = min.result;
        transform.LookAt(desiredVm.normalized + transform.position);
        //InterceptionMarker.transform.position = desiredVm.normalized * 5 + transform.position;
        var changeReq = max_acceleration * desiredVm.normalized;// desiredVm - rb.velocity;
        //changeReq = changeReq.normalized * max_acceleration;// Mathf.Min(changeReq.magnitude, max_acceleration); //cap change speed, but still in direction of desired vel, not necessarily orthogonal to current Vm though...
        //rb.velocity = minVm;
        return changeReq;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var dest = target.transform.position;
        var pos = transform.position;
        var dir = dest - pos;

        var rocket_accel = Vector3.zero;
        if (true)
        {
            rocket_accel = SlowDogCurve();
        } else {
            rocket_accel = dir.normalized * max_acceleration;
        }
        rb.velocity += rocket_accel * Time.fixedDeltaTime;
        if (dir.magnitude < .1f) {
            gameObject.SetActive(false);
        }
    }
}
