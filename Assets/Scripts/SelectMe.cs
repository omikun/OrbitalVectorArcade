using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectMe : MonoBehaviour
{
    EnvironmentManager em;
    // Start is called before the first frame update
    void Start()
    {
        var go = GameObject.FindObjectOfType<EnvironmentManager>();
        em = go.GetComponent<EnvironmentManager>();

    }
    //TODO make sure OnMouseDown always occurs before Update, SelectionManager now depends on this
    private void OnMouseDown()
    {
        //TODO play a sound
        bool lmb = Input.GetMouseButtonDown(0);
        bool rmb = Input.GetMouseButtonDown(1);

        if (Input.GetKey(KeyCode.LeftAlt))
        {
            Debug.Log("alt key");
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            Debug.Log("ctrl key");
        }

        Debug.Log("mouse down! " + lmb.ToString() + " " + rmb.ToString());
        em.newFocusTargets.Add(gameObject);
    }
    // Update is called once per frame
    void Update()
    {

    }
}
