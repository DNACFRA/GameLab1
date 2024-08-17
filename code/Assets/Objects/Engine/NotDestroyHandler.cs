using System;
using System.Collections;
using System.Collections.Generic;
using Objects.Render;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Objects.Engine
{
    /// <summary>
    /// Guarantees that only one of this object exists at a time
    /// </summary>
    public class NotDestroyHandler : MonoBehaviour
    {
        private void Start()
        {
            if(CoKnos.NotDestroyHandler!=null)
                DestroyImmediate(this.gameObject);
            else
            {
                CoKnos.NotDestroyHandler = this;
                DontDestroyOnLoad(this.gameObject);
                
                IRelayStart[] relayStart = this.GetComponents<IRelayStart>();
                foreach (var relay in relayStart)
                {
                    relay.RunRelayStart();
                }
            }
        }

       
    }
}