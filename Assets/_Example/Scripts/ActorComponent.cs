using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorComponent<T> : MonoBehaviour where T:Actor
{
    private T m_actor;
    public T actor 
    {
        get 
        {
            if(m_actor==null)
            {
                m_actor = GetComponentInParent<T>();
            }
            return m_actor;
        }
    }
}
