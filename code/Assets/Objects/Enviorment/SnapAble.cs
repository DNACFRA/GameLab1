using System;
using UnityEngine;

namespace Objects.Enviorment
{
    public class SnapAble : MonoBehaviour
    {
        /// <summary>
        /// If an object implements this, it can be snapped by the snap tool
        /// Could've been an interface, but i wanted to be a bit more flexible
        /// </summary>

    
        public Vector3 Position => 
            transform.position;

    }
}
