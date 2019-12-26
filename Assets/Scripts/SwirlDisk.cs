using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwirlDisk : MonoBehaviour
{
    //take a trail
    public TrailRenderer tr;
    List<TrailRenderer> trails;
    public int n = 500;
    // Start is called before the first frame update
    void Start()
    {
        //instantiate it n times in an array
        //tr = GetComponent<TrailRenderer>();
        //trails = List<TrailRenderer>();
        //for (int i = 0; i < n; i++)
        //{
        //    trails.Add(Instantiate(
    }

    // Update is called once per frame
    void Update()
    {
        //for all trails in initial phase, swirl from outside in
        //fade in initially
        //if trail near threshold, intensify brightness
        //if trail within threshold, start over
    }
}
