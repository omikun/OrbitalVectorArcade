using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class TestEvent : MonoBehaviour
{
    public UnityEvent m_MyEvent;

    void Start()
    {
        if (m_MyEvent == null)
        {
            m_MyEvent = new UnityEvent();
            m_MyEvent.AddListener(Ping);
        }
    }

    void Update()
    {
    }

    void Ping()
    {
        Debug.Log("Default Ping");
    }
}
