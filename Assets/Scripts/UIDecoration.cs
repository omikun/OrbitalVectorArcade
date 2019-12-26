using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDecoration : MonoBehaviour
{
    public GameObject cross;
    const int dimx = 10;
    const int dimy = 5;
    const int dimz = 10;
    const int scale = 1;
    float hx = dimx / 2 * scale - 0.5f;
    float hy = dimy / 2 * scale - 0.5f;
    float hz = dimz / 2 * scale + 1f;
    GameObject[] crosses = new GameObject[dimx * dimy * dimz];
    // Start is called before the first frame update
    void Start()
    {
        //instantiate cross for 20x20 grid size 2
        for (int x = 0; x < dimx; x++)
        {
            for (int y = 0; y < dimy; y++)
            {
                for (int z = 0; z < dimz; z++)
                {
                    var idx = x*dimy*dimz+y*dimz+z;
                    var c = Instantiate(cross);
                    crosses[idx] = c;
                    c.transform.position = new Vector3(scale*x-hx, scale*y-hy, scale*z-hy);
                    c.SetActive(true);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
