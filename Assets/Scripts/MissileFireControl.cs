using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Phase 1 fire missiles at all targets not self
//Phase 2 limit firing range
//Phase 3 limit missile firing rate
//Phase 4 limit missile count; maybe reload?
//
public class MissileFireControl : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject target;
    public GameObject missile_base;
    float firing_interval = 1.5f;
    float start_fire_offset = .1f;
    float last_fire_time = 0f;
    bool can_fire = false;

    void Start()
    {
        //missile_base.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //find all valid targets
        //spawn missiles with target
        bool can_fire_now = Time.time - last_fire_time > firing_interval;
        if (can_fire_now && can_fire)
        {
            can_fire = false;
            last_fire_time = Time.time;
            //instantiate missile, set target on missile
            var missile = Instantiate(missile_base);
            missile.SetActive(true);
            missile.GetComponent<MissileControl>().target = target;
            //pop missile out in a random direction
            var offset = start_fire_offset * Random.onUnitSphere;
            missile.transform.position = transform.position + offset;
            missile.GetComponent<Rigidbody>().velocity = offset;
        }
    }

    public void Fire()
    {
        can_fire = true;
    }
}
